using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Responsável exclusivamente pela representação VISUAL do ciclo dia/noite.
/// Consome dados astronômicos do TimeManager (fonte única de tempo) e:
/// - posiciona Sol e Lua;
/// - controla intensidade luminosa do Sol e da Lua;
/// - faz transições artísticas entre Aurora / Dia / Crepúsculo / Noite;
/// - expõe parâmetros visuais (SunIntensity, MoonIntensity, AmbientNightFactor,
///   SolarIntensity01, NightFactor, TwilightFactor) para outros sistemas
///   (ex: LightingEnvironment cuidando de Sky/Fog).
///
/// O DayNightCycle NÃO controla passagem de tempo — isso é responsabilidade
/// exclusiva do TimeManager.
///
/// Decisões assumidas (confirmadas antes da implementação):
/// - Piso mínimo de visibilidade lunar durante o dia: 5% (a Lua nunca
///   desliga completamente, mesmo com sol forte).
/// - NightFactor (seção 15) é um alias de AmbientNightFactor (seção 13):
///   representam o mesmo valor, exposto sob os dois nomes por conveniência
///   de integração com sistemas futuros de Sky/Fog.
/// - A representação visual da Lua no céu usa o Celestial Body nativo do
///   HDRP (configurado diretamente no componente Light da Moon Light, com
///   Physically Based Sky no Volume), em vez de um material/mesh próprio.
///   Por isso este script não expõe mais um material/_MoonPhase manual: as
///   fases da Lua emergem naturalmente da iluminação física do Celestial
///   Body pela direção do Sol. O TimeManager.MoonIllumination continua
///   sendo usado apenas para calcular a intensidade da Moon Light.
///
/// Outras decisões de implementação, não especificadas numericamente na
/// spec, estão documentadas nos comentários dos respectivos métodos
/// (TwilightFactor, cor da luz via Gradient).
/// </summary>
public class SunMoonSystem : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Referências (Inspector)
    // ---------------------------------------------------------------
    [Header("Fonte de tempo")]
    [SerializeField] private TimeManager timeManager;

    [Header("Luzes")]
    [SerializeField] private Light sunLight;
    [SerializeField] private Light moonLight;   

    // ---------------------------------------------------------------
    // Períodos artísticos
    // ---------------------------------------------------------------
    [Serializable]
    public class DayPeriod
    {
        public float durationMinutes;
        public AnimationCurve intensityCurve;
    }

    public enum DayState
    {
        Dawn,
        Day,
        Dusk,
        Night
    }

    [Header("Períodos (duração artística)")]
    public DayPeriod dawn = new DayPeriod { durationMinutes = 30f };
    public DayPeriod day = new DayPeriod { durationMinutes = 120f };
    public DayPeriod dusk = new DayPeriod { durationMinutes = 45f };
    public DayPeriod night = new DayPeriod { durationMinutes = 90f };

    public DayState CurrentState { get; private set; }

    // ---------------------------------------------------------------
    // Intensidade
    // ---------------------------------------------------------------
    [Header("Intensidade do Sol")]
    [SerializeField] private float maxSunIntensity = 100000f; // lux (HDRP)
    [SerializeField] private float minSunIntensity = 0f;

    [Header("Intensidade da Lua")]
    [SerializeField] private float maxMoonIntensity = 1f;   // lux equivalente
    [SerializeField] private float minMoonIntensity = 0f;

    [Range(0f, 1f)]
    [Tooltip("Visibilidade mínima da Lua durante o dia (nunca desliga completamente).")]
    [SerializeField] private float minMoonDaylightFactor = 0.05f;

    [Header("Piso de visibilidade noturna (gameplay, não físico)")]
    [Tooltip("Intensidade mínima de luz ambiente à noite, independente da fase da Lua, só pra manter silhuetas visíveis.")]
    [SerializeField] private float minNightAmbientIntensity = 0.03f;   

    // ---------------------------------------------------------------
    // API pública para outros sistemas
    // ---------------------------------------------------------------
    public float SunIntensity { get; private set; }
    public float MoonIntensity { get; private set; }

    /// <summary>Quão "noturno" está o ambiente (0 = dia pleno, 1 = noite plena).</summary>
    public float AmbientNightFactor { get; private set; }  

    /// <summary>Fração normalizada (0-1) da intensidade solar dentro do ciclo artístico.</summary>
    public float SolarIntensity01 { get; private set; }

    /// <summary>
    /// Quão "em transição" (aurora/crepúsculo) está o momento atual.
    /// Não definido numericamente na spec; implementado como uma parábola
    /// que vale 0 em dia pleno / noite plena e 1 no meio de uma transição
    /// (SolarIntensity01 = 0.5).
    /// </summary>
    public float TwilightFactor { get; private set; }

    // ---------------------------------------------------------------
    // Eventos
    // ---------------------------------------------------------------
    public UnityEvent OnDawnStarted;
    public UnityEvent OnDayStarted;
    public UnityEvent OnDuskStarted;
    public UnityEvent OnNightStarted;

    private bool stateInitialized = false;
    private DayState lastState;

    // ---------------------------------------------------------------
    // Debug (somente leitura) — espelha os valores calculados no Inspector,
    // já que propriedades (get; private set;) não aparecem por padrão.
    // ---------------------------------------------------------------
    [Header("Debug (somente leitura, ver durante Play)")]
    [SerializeField] private DayState debugCurrentState;
    [SerializeField] private float debugSolarIntensity01;
    [SerializeField] private float debugSunIntensity;
    [SerializeField] private float debugMoonIntensity;
    [SerializeField] private float debugSunriseTime;
    [SerializeField] private float debugSunsetTime;

    // ---------------------------------------------------------------
    // Curvas padrão (seção 12)
    // ---------------------------------------------------------------
    private void Reset()
    {
        dawn.intensityCurve = new AnimationCurve(
            new Keyframe(0f, 0f),
            new Keyframe(0.5f, 0.25f),
            new Keyframe(1f, 1f));
        EaseInDawnCurve(dawn.intensityCurve);

        day.intensityCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

        dusk.intensityCurve = new AnimationCurve(
            new Keyframe(0f, 1f),
            new Keyframe(0.5f, 0.4f),
            new Keyframe(1f, 0f));

        night.intensityCurve = AnimationCurve.Linear(0f, 0f, 1f, 0f);
    }

    private static void EaseInDawnCurve(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            curve.SmoothTangents(i, 0f);
        }
    }

    // ---------------------------------------------------------------
    // Update principal (seção 6)
    // ---------------------------------------------------------------
    private void Update()
    {
        if (timeManager == null || sunLight == null || moonLight == null)
        {
            Debug.LogWarning(
                "[DayNightCycle] Referência(s) não atribuída(s) no Inspector " +
                $"(timeManager={timeManager != null}, sunLight={sunLight != null}, moonLight={moonLight != null}). " +
                "As luzes não serão atualizadas até que todas estejam preenchidas.",
                this);
            return;
        }

        // 1. Obter dados TimeManager (já calculados lá) e posicionar astros.
        UpdateSunPosition();
        UpdateMoonPosition();

        // 2. Determinar período visual atual + progresso dentro dele.
        DetermineState(out DayState state, out float progress);
        CurrentState = state;

        // 3. Aplicar curva de intensidade do período -> SolarIntensity01.
        DayPeriod activePeriod = GetPeriod(state);
        SolarIntensity01 = Mathf.Clamp01(activePeriod.intensityCurve.Evaluate(progress));

        // 4. Atualizar iluminação (Sol, Lua, fase da Lua, fatores auxiliares).
        //UpdateSunLighting();
        //UpdateMoonLighting();
        UpdateAuxiliaryFactors();

        // 5. Disparar eventos de transição de estado.
        FireStateEvents(state);

        // Debug: espelha os valores calculados para inspeção no Inspector.
        debugCurrentState = CurrentState;
        debugSolarIntensity01 = SolarIntensity01;
        debugSunIntensity = SunIntensity;
        debugMoonIntensity = MoonIntensity;
    }

    // ---------------------------------------------------------------
    // Posição do Sol / Lua (seções 7 e 8)
    // ---------------------------------------------------------------
    private void UpdateSunPosition()
    {
        if (timeManager.SunDirection.sqrMagnitude > 0.0001f)
        {
            sunLight.transform.rotation = Quaternion.LookRotation(timeManager.SunDirection);
        }
    }

    private void UpdateMoonPosition()
    {
        if (timeManager.MoonDirection.sqrMagnitude > 0.0001f)
        {
            moonLight.transform.rotation = Quaternion.LookRotation(timeManager.MoonDirection);
        }
    }

    // ---------------------------------------------------------------
    // Determinação do período visual (seção 4: ponderação pelo TimeManager)
    // ---------------------------------------------------------------
    private void DetermineState(out DayState state, out float periodProgress)
    {
        float dayLength = timeManager.DayLength;     // horas de sol
        float nightLength = timeManager.NightLength; // horas de noite

        // Assume-se meio-dia solar às 12:00 (mesma convenção usada pelo
        // TimeManager no cálculo do ângulo horário). Logo:
        float sunriseTime = 12f - dayLength * 0.5f;
        float sunsetTime = 12f + dayLength * 0.5f;
        debugSunriseTime = sunriseTime;
        debugSunsetTime = sunsetTime;

        float totalArtificialDaylightMinutes = Mathf.Max(
            dawn.durationMinutes + day.durationMinutes + dusk.durationMinutes, 0.0001f);

        float dawnFraction = dawn.durationMinutes / totalArtificialDaylightMinutes;
        float dayFraction = day.durationMinutes / totalArtificialDaylightMinutes;
        // duskFraction implícito = 1 - dawnFraction - dayFraction

        float dawnEnd = sunriseTime + dayLength * dawnFraction;
        float dayEnd = dawnEnd + dayLength * dayFraction;
        // duskEnd deve coincidir com sunsetTime

        float t = timeManager.TimeOfDay;

        if (t >= sunriseTime && t < sunsetTime)
        {
            if (t < dawnEnd)
            {
                state = DayState.Dawn;
                periodProgress = InverseLerpSafe(sunriseTime, dawnEnd, t);
            }
            else if (t < dayEnd)
            {
                state = DayState.Day;
                periodProgress = InverseLerpSafe(dawnEnd, dayEnd, t);
            }
            else
            {
                state = DayState.Dusk;
                periodProgress = InverseLerpSafe(dayEnd, sunsetTime, t);
            }
        }
        else
        {
            state = DayState.Night;

            // Progresso contínuo da noite, mesmo atravessando a meia-noite.
            float sinceSunset = (t >= sunsetTime) ? (t - sunsetTime) : (t + 24f - sunsetTime);
            periodProgress = InverseLerpSafe(0f, Mathf.Max(nightLength, 0.0001f), sinceSunset);
        }
    }

    private static float InverseLerpSafe(float a, float b, float value)
    {
        float denom = b - a;
        if (Mathf.Abs(denom) < 0.0001f)
        {
            return 0f;
        }
        return Mathf.Clamp01((value - a) / denom);
    }

    private DayPeriod GetPeriod(DayState state)
    {
        switch (state)
        {
            case DayState.Dawn: return dawn;
            case DayState.Day: return day;
            case DayState.Dusk: return dusk;
            default: return night;
        }
    }

    // ---------------------------------------------------------------
    // Iluminação solar (seção 9)
    // ---------------------------------------------------------------
    private void UpdateSunLighting()
    {
        SunIntensity = Mathf.Lerp(minSunIntensity, maxSunIntensity, SolarIntensity01);
        sunLight.intensity = SunIntensity;
        
    }

    // ---------------------------------------------------------------
    // Iluminação lunar (seção 10)
    // ---------------------------------------------------------------
    private void UpdateMoonLighting()
    {
        // Altura acima do horizonte, normalizada 0-1 (0 = no horizonte ou abaixo).
        float altitudeFactor = Mathf.Clamp01(Mathf.Sin(timeManager.MoonAltitude * Mathf.Deg2Rad));

        // Fase lunar (0 = nova, 1 = cheia), já normalizada pelo TimeManager.
        float phaseFactor = Mathf.Clamp01(timeManager.MoonIllumination);

        // Redução pela luminosidade solar durante o dia, com piso mínimo
        // (a Lua nunca é totalmente apagada — "não desligar a Lua").
        float skyFactor = Mathf.Lerp(1f, minMoonDaylightFactor, SolarIntensity01);

        float moonVisibility = altitudeFactor * phaseFactor * skyFactor;

        MoonIntensity = Mathf.Lerp(minMoonIntensity, maxMoonIntensity, moonVisibility);
        moonLight.intensity = MoonIntensity; // luz nunca é desabilitada, só sua intensidade varia
    }

    // ---------------------------------------------------------------
    // Fatores auxiliares para outros sistemas (seções 13 e 15)
    // ---------------------------------------------------------------
    private void UpdateAuxiliaryFactors()
    {
        AmbientNightFactor = Mathf.Clamp01(1f - SolarIntensity01);
        RenderSettings.ambientIntensity = Mathf.Lerp(1f, minNightAmbientIntensity, AmbientNightFactor);

        // TwilightFactor: pico de 1 no meio de uma transição (SolarIntensity01 = 0.5),
        // 0 em dia pleno ou noite plena. Fórmula não definida na spec original.
        TwilightFactor = Mathf.Clamp01(4f * SolarIntensity01 * (1f - SolarIntensity01));
        
    }

    // ---------------------------------------------------------------
    // Eventos de transição (seção 14)
    // ---------------------------------------------------------------
    private void FireStateEvents(DayState state)
    {
        if (stateInitialized && state == lastState)
        {
            return;
        }

        stateInitialized = true;
        lastState = state;

        switch (state)
        {
            case DayState.Dawn: OnDawnStarted?.Invoke(); break;
            case DayState.Day: OnDayStarted?.Invoke(); break;
            case DayState.Dusk: OnDuskStarted?.Invoke(); break;
            case DayState.Night: OnNightStarted?.Invoke(); break;
        }
    }
}