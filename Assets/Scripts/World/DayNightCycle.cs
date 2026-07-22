using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Ciclo dia/noite: rotaciona a luz direcional (Sol) e ajusta cor/
/// intensidade dela ao longo de 4 fases (Amanhecer, Dia, Entardecer,
/// Noite), com as durações que você passou (2/10/2/6 min por padrão).
///
/// PREMISSA (Physically Based Sky): a aparência do céu (cores de nascer/
/// pôr do sol, escurecimento à noite) é derivada AUTOMATICAMENTE pelo
/// Physically Based Sky a partir da rotação da luz direcional - não mexo
/// em nenhum parâmetro do Sky diretamente, só na Transform e nos
/// parâmetros (cor/intensidade em Lux) da própria Light. Isso é o
/// comportamento padrão conhecido do Physically Based Sky, mas não testei
/// visualmente neste projeto - se o céu não reagir como esperado, o
/// primeiro lugar a conferir é se essa Light está marcada como a luz que
/// afeta o Physically Based Sky (normalmente a luz direcional mais
/// intensa da cena, ou a configurada como "Sun Source" nas Lighting
/// Settings).
///
/// FOG (Volumetric Fog): opcionalmente interpola o tint do Fog (Volume
/// override) entre as mesmas 3 cores-chave (horizonte/dia/noite) usadas na
/// luz - só tem efeito visível se o Fog estiver configurado com Color Mode
/// = Constant Color; se estiver em Sky Color, o fog já acompanha o céu
/// sozinho e essa parte do script não faz diferença visual (mas não tem
/// problema deixar ligado).
///
/// ÂNGULO DO SOL: convenção padrão Unity (rotação no eixo X do Transform) -
/// 0° = sol no horizonte, valores positivos = sol subindo/alto, negativos
/// = sol abaixo do horizonte (noite). Ver os 4 ângulos-chave abaixo
/// (nightSideAngle/daySideAngle/peakAngle/troughAngle) - Amanhecer e
/// Entardecer são tratados como simetricos por padrão (mesmos ângulos de
/// fronteira nightSideAngle/daySideAngle nos dois), o que é uma
/// simplificação razoável - ajuste os ângulos se quiser algo diferente.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    private enum Phase { Amanhecer, Dia, Entardecer, Noite }

    [Header("Referências")]
    [Tooltip("Luz direcional que representa o Sol - será rotacionada e terá cor/intensidade ajustadas a cada frame.")]
    [SerializeField] private Light sunLight;
    [Tooltip("Volume Global com o profile de Sky and Fog - usado só pra tentar achar o override Fog (Volumetric Fog) e interpolar o tint dele. Opcional - deixe vazio se não quiser mexer no fog por aqui.")]
    [SerializeField] private Volume skyAndFogVolume;
    [Tooltip("Se desligado, não mexe no Fog mesmo com skyAndFogVolume atribuído (só rotaciona/colore o sol).")]
    [SerializeField] private bool driveFogTint = true;

    [Header("Duração das Fases (minutos)")]
    [SerializeField] private float amanhecerMinutes = 2f;
    [SerializeField] private float diaMinutes = 10f;
    [SerializeField] private float entardecerMinutes = 2f;
    [SerializeField] private float noiteMinutes = 6f;

    [Header("Ângulos-Chave do Sol (graus, eixo X - 0=horizonte, +=dia, -=noite)")]
    [Tooltip("Ângulo na fronteira noite/amanhecer e entardecer/noite (sol raso, cruzando o horizonte). Ex.: -5° (levemente abaixo).")]
    [SerializeField] private float nightSideAngle = -5f;
    [Tooltip("Ângulo na fronteira amanhecer/dia e dia/entardecer (sol já claramente acima do horizonte, início/fim do 'dia pleno'). Ex.: 15°.")]
    [SerializeField] private float daySideAngle = 15f;
    [Tooltip("Ângulo no auge do Dia (meio da fase Dia - meio-dia solar).")]
    [SerializeField] private float peakAngle = 80f;
    [Tooltip("Ângulo no fundo da Noite (meio da fase Noite - meia-noite solar, ponto mais baixo).")]
    [SerializeField] private float troughAngle = -70f;

    [Header("Cores-Chave da Luz (Lux)")]
    [SerializeField] private Color horizonColor = new Color(1f, 0.55f, 0.3f);
    [SerializeField] private float horizonIntensityLux = 4000f;
    [SerializeField] private Color dayColor = Color.white;
    [SerializeField] private float dayIntensityLux = 100000f;
    [SerializeField] private Color nightColor = new Color(0.35f, 0.45f, 0.65f);
    [Tooltip("Intensidade da luz direta no fundo da noite - bem baixa (não zero), como um luar fraco.")]
    [SerializeField] private float nightIntensityLux = 0.3f;

    [Header("Progresso (público - outros scripts podem ler/setar)")]
    [Tooltip("Segundos já decorridos no ciclo atual (0 a duração total). Pode ser setado externamente, ex.: um menu 'pular para de noite'.")]
    public float elapsedSeconds = 0f;
    public bool paused = false;

    private Fog _fog;
    private bool _triedGetFog = false;

    private float TotalCycleSeconds =>
        Mathf.Max(0.01f, (amanhecerMinutes + diaMinutes + entardecerMinutes + noiteMinutes) * 60f);

    /// <summary>Fase atual do ciclo - útil pra outros sistemas (ex.: NPCs "dormem" à noite, iluminação de janelas liga ao anoitecer).</summary>
    public string CurrentPhaseName { get; private set; }
    /// <summary>Progresso (0-1) dentro da fase atual.</summary>
    public float CurrentPhaseProgress01 { get; private set; }

    private void Update()
    {
        if (paused || sunLight == null) return;

        elapsedSeconds += Time.deltaTime;
        float total = TotalCycleSeconds;
        elapsedSeconds %= total;

        ApplyForTime(elapsedSeconds);
    }

    private void ApplyForTime(float t)
    {
        float amanhecerDur = Mathf.Max(0.001f, amanhecerMinutes * 60f);
        float diaDur = Mathf.Max(0.001f, diaMinutes * 60f);
        float entardecerDur = Mathf.Max(0.001f, entardecerMinutes * 60f);
        float noiteDur = Mathf.Max(0.001f, noiteMinutes * 60f);

        float amanhecerEnd = amanhecerDur;
        float diaEnd = amanhecerEnd + diaDur;
        float entardecerEnd = diaEnd + entardecerDur;
        // noiteEnd == TotalCycleSeconds (fecha o ciclo)

        float angle;
        Color color;
        float intensityLux;

        if (t < amanhecerEnd)
        {
            float p = t / amanhecerDur;
            CurrentPhaseName = "Amanhecer";
            CurrentPhaseProgress01 = p;

            angle = Mathf.Lerp(nightSideAngle, daySideAngle, p);
            color = Color.Lerp(horizonColor, dayColor, p);
            intensityLux = Mathf.Lerp(horizonIntensityLux, dayIntensityLux, p);
        }
        else if (t < diaEnd)
        {
            float localT = t - amanhecerEnd;
            float half = diaDur * 0.5f;
            float p = localT / diaDur;
            CurrentPhaseName = "Dia";
            CurrentPhaseProgress01 = p;

            angle = localT < half
                ? Mathf.Lerp(daySideAngle, peakAngle, localT / half)
                : Mathf.Lerp(peakAngle, daySideAngle, (localT - half) / half);
            // Cor/intensidade ficam uniformemente "dia" durante toda a fase -
            // ao meio-dia não fica mais branco que no início/fim do Dia,
            // simplificação razoável (luz do "dia pleno" tratada como
            // constante).
            color = dayColor;
            intensityLux = dayIntensityLux;
        }
        else if (t < entardecerEnd)
        {
            float p = (t - diaEnd) / entardecerDur;
            CurrentPhaseName = "Entardecer";
            CurrentPhaseProgress01 = p;

            angle = Mathf.Lerp(daySideAngle, nightSideAngle, p);
            color = Color.Lerp(dayColor, horizonColor, p);
            intensityLux = Mathf.Lerp(dayIntensityLux, horizonIntensityLux, p);
        }
        else
        {
            float localT = t - entardecerEnd;
            float half = noiteDur * 0.5f;
            float p = localT / noiteDur;
            CurrentPhaseName = "Noite";
            CurrentPhaseProgress01 = p;

            angle = localT < half
                ? Mathf.Lerp(nightSideAngle, troughAngle, localT / half)
                : Mathf.Lerp(troughAngle, nightSideAngle, (localT - half) / half);
            color = localT < half
                ? Color.Lerp(horizonColor, nightColor, localT / half)
                : Color.Lerp(nightColor, horizonColor, (localT - half) / half);
            intensityLux = localT < half
                ? Mathf.Lerp(horizonIntensityLux, nightIntensityLux, localT / half)
                : Mathf.Lerp(nightIntensityLux, horizonIntensityLux, (localT - half) / half);
        }

        Vector3 euler = sunLight.transform.eulerAngles;
        euler.x = angle;
        sunLight.transform.eulerAngles = euler;

        sunLight.color = color;
        sunLight.intensity = intensityLux;

        if (driveFogTint) ApplyFogTint(color);
    }

    private void ApplyFogTint(Color tint)
    {
        if (!_triedGetFog)
        {
            _triedGetFog = true;
            if (skyAndFogVolume != null && skyAndFogVolume.profile != null)
            {
                skyAndFogVolume.profile.TryGet(out _fog);
                if (_fog == null)
                {
                    Debug.LogWarning($"{nameof(DayNightCycle)}: não encontrei um override 'Fog' no profile de {skyAndFogVolume.name} - driveFogTint não terá efeito.");
                }
            }
        }

        if (_fog == null) return;
        _fog.tint.value = tint;
    }

    private void OnValidate()
    {
        if (amanhecerMinutes < 0f) amanhecerMinutes = 0f;
        if (diaMinutes < 0f) diaMinutes = 0f;
        if (entardecerMinutes < 0f) entardecerMinutes = 0f;
        if (noiteMinutes < 0f) noiteMinutes = 0f;
    }
}