using System;
using UnityEngine;

/// <summary>
/// Gerenciador central de tempo do mundo (Unity 6.5).
/// Calcula e expõe dados temporais e astronômicos simplificados
/// (posição do Sol, posição da Lua, fase lunar, duração do dia/noite),
/// mas NÃO altera luzes, céu, materiais ou iluminação ambiente.
///
/// Convenções assumidas (definidas antes da implementação):
/// - Azimute: 90° = Norte, 0° = Leste, 270° = Sul, 180° = Oeste
///   (equivale a: azimuteSpec = 90 - rumoPadrao, onde rumoPadrao é a bússola
///   tradicional 0=N/90=E/180=S/270=O, sentido horário).
/// - Eixos do mundo Unity: Z+ = Norte, X+ = Leste, Y+ = Cima.
/// - SunDirection / MoonDirection representam a direção de propagação da luz
///   (do astro em direção ao chão), prontos para uso direto em
///   Quaternion.LookRotation() por uma Directional Light. Por isso são o
///   NEGATIVO do vetor "observador -> astro".
/// </summary>
public class TimeManager : MonoBehaviour
{
    // ---------------------------------------------------------------
    // Singleton (persiste entre cenas)
    // ---------------------------------------------------------------
    public static TimeManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        RecalculateAstronomy();
    }

    // ---------------------------------------------------------------
    // Enums
    // ---------------------------------------------------------------
    public enum Hemisphere
    {
        North,
        South
    }

    public enum MoonPhase
    {
        NewMoon,
        WaxingCrescent,
        FirstQuarter,
        WaxingGibbous,
        FullMoon,
        WaningGibbous,
        LastQuarter,
        WaningCrescent
    }

    // ---------------------------------------------------------------
    // Tempo (Inspector)
    // ---------------------------------------------------------------
    [Header("Tempo")]
    [SerializeField, Range(0, 364)]
    private int dayOfYear = 0;

    [SerializeField, Range(0f, 24f)]
    private float timeOfDay = 12f;

    [SerializeField]
    [Tooltip("1 = tempo real | 60 = 1 minuto real = 1 hora | 1440 = 1 dia por minuto")]
    private float timeScale = 1f;

    // ---------------------------------------------------------------
    // Configuração (Inspector)
    // ---------------------------------------------------------------
    [Header("Configuração")]
    [SerializeField]
    private int daysPerYear = 365;

    [SerializeField]
    private Hemisphere hemisphere = Hemisphere.North;

    [SerializeField, Range(-90f, 90f)]
    private float latitude = 0f;

    // ---------------------------------------------------------------
    // Constantes
    // ---------------------------------------------------------------
    private const float SynodicMonth = 29.53059f; // dias
    private const float MaxDeclination = 23.44f;   // graus

    // ---------------------------------------------------------------
    // Propriedades públicas de tempo
    // ---------------------------------------------------------------
    public int DayOfYear
    {
        get => dayOfYear;
        set
        {
            dayOfYear = ((value % daysPerYear) + daysPerYear) % daysPerYear;
            RecalculateAstronomy();
        }
    }

    public float TimeOfDay
    {
        get => timeOfDay;
        set
        {
            timeOfDay = value;
            while (timeOfDay >= 24f) { timeOfDay -= 24f; }
            while (timeOfDay < 0f) { timeOfDay += 24f; }
            RecalculateAstronomy();
        }
    }

    public float TimeScale
    {
        get => timeScale;
        set => timeScale = value;
    }

    public int DaysPerYear => daysPerYear;

    public Hemisphere CurrentHemisphere
    {
        get => hemisphere;
        set { hemisphere = value; RecalculateAstronomy(); }
    }

    /// <summary>Latitude assinada, aplicando o hemisfério (Norte = positiva, Sul = negativa).</summary>
    public float Latitude
    {
        get => hemisphere == Hemisphere.North ? Mathf.Abs(latitude) : -Mathf.Abs(latitude);
        set { latitude = Mathf.Clamp(Mathf.Abs(value), -90f, 90f); RecalculateAstronomy(); }
    }

    // ---------------------------------------------------------------
    // Dados solares expostos
    // ---------------------------------------------------------------
    public float SunAltitude { get; private set; }
    public float SunAzimuth { get; private set; }
    public Vector3 SunDirection { get; private set; }
    public bool IsSunVisible { get; private set; }

    public float DayLength { get; private set; }   // horas
    public float NightLength { get; private set; } // horas

    // ---------------------------------------------------------------
    // Dados lunares expostos
    // ---------------------------------------------------------------
    public float MoonAge { get; private set; } // 0 - 29.53
    public MoonPhase CurrentMoonPhase { get; private set; }
    public float MoonIllumination { get; private set; } // 0 - 1

    public float MoonAltitude { get; private set; }
    public float MoonAzimuth { get; private set; }
    public Vector3 MoonDirection { get; private set; }
    public bool IsMoonVisible { get; private set; }

    public float SunMoonAngle { get; private set; } // 0 - 180

    // ---------------------------------------------------------------
    // Eventos C#
    // ---------------------------------------------------------------
    public event Action OnMinuteChanged;
    public event Action OnHourChanged;
    public event Action OnDayChanged;
    public event Action OnYearChanged;
    public event Action OnMoonPhaseChanged;

    // Rastreamento de mudança para disparo de eventos
    private int lastMinute = -1;
    private int lastHour = -1;
    private int lastDayOfYear = -1;
    private MoonPhase lastMoonPhase;
    private bool phaseInitialized = false;

    // ---------------------------------------------------------------
    // Update principal
    // ---------------------------------------------------------------
    private void Update()
    {
        float deltaHours = Time.deltaTime * timeScale / 3600f;

        timeOfDay += deltaHours;

        while (timeOfDay >= 24f)
        {
            timeOfDay -= 24f;
            dayOfYear++;

            if (dayOfYear >= daysPerYear)
            {
                dayOfYear = 0;
                OnYearChanged?.Invoke();
            }

            OnDayChanged?.Invoke();
        }

        RecalculateAstronomy();
        InvokeTimeEvents();
    }

    private void InvokeTimeEvents()
    {
        int currentMinute = Mathf.FloorToInt((timeOfDay * 60f) % 60f);
        int currentHour = Mathf.FloorToInt(timeOfDay);

        if (currentMinute != lastMinute)
        {
            lastMinute = currentMinute;
            OnMinuteChanged?.Invoke();
        }

        if (currentHour != lastHour)
        {
            lastHour = currentHour;
            OnHourChanged?.Invoke();
        }

        lastDayOfYear = dayOfYear;

        if (!phaseInitialized || CurrentMoonPhase != lastMoonPhase)
        {
            lastMoonPhase = CurrentMoonPhase;
            phaseInitialized = true;
            OnMoonPhaseChanged?.Invoke();
        }
    }

    // ---------------------------------------------------------------
    // Cálculo astronômico
    // ---------------------------------------------------------------
    private void RecalculateAstronomy()
    {
        float lat = Latitude;
        float declination = MaxDeclination * Mathf.Sin(Mathf.Deg2Rad * (360f / daysPerYear) * (284f + dayOfYear));

        // ---- Sol ----
        float sunHourAngle = (timeOfDay - 12f) * 15f; // graus

        CalculateAltitudeAzimuth(lat, declination, sunHourAngle, out float sunAlt, out float sunAz);
        SunAltitude = sunAlt;
        SunAzimuth = sunAz;
        IsSunVisible = SunAltitude > 0f;

        Vector3 toSun = DirectionFromAltAzimuth(SunAltitude, SunAzimuth);
        SunDirection = -toSun; // direção de propagação da luz (para LookRotation)

        CalculateDayNightLength(lat, declination, out float dayLen, out float nightLen);
        DayLength = dayLen;
        NightLength = nightLen;

        // ---- Lua ----
        float totalDays = dayOfYear + timeOfDay / 24f;
        MoonAge = ((totalDays % SynodicMonth) + SynodicMonth) % SynodicMonth;

        float phaseAngle = (MoonAge / SynodicMonth) * 360f; // 0 = nova, 180 = cheia
        MoonIllumination = (1f - Mathf.Cos(phaseAngle * Mathf.Deg2Rad)) * 0.5f;
        SunMoonAngle = phaseAngle <= 180f ? phaseAngle : 360f - phaseAngle;
        CurrentMoonPhase = GetMoonPhaseFromAge(MoonAge);

        // Modelo simplificado: a Lua percorre a mesma "trilha" aparente do Sol
        // (mesma declinação, sem inclinação orbital real), porém defasada no
        // tempo conforme sua fase. Fase 0 (nova) => alinhada ao Sol.
        // Fase 180 (cheia) => oposta ao Sol (nasce ao pôr do sol).
        float moonHourAngle = sunHourAngle - phaseAngle;

        CalculateAltitudeAzimuth(lat, declination, moonHourAngle, out float moonAlt, out float moonAz);
        MoonAltitude = moonAlt;
        MoonAzimuth = moonAz;
        IsMoonVisible = MoonAltitude > 0f;

        Vector3 toMoon = DirectionFromAltAzimuth(MoonAltitude, MoonAzimuth);
        MoonDirection = -toMoon;
    }

    /// <summary>
    /// Calcula altitude e azimute (na convenção da spec: 90=N,0=E,270=S,180=O)
    /// a partir de latitude, declinação e ângulo horário, todos em graus.
    /// </summary>
    private static void CalculateAltitudeAzimuth(float latitudeDeg, float declinationDeg, float hourAngleDeg, out float altitudeDeg, out float azimuthDeg)
    {
        float latRad = latitudeDeg * Mathf.Deg2Rad;
        float decRad = declinationDeg * Mathf.Deg2Rad;
        float hourRad = hourAngleDeg * Mathf.Deg2Rad;

        float sinAlt = Mathf.Sin(latRad) * Mathf.Sin(decRad) + Mathf.Cos(latRad) * Mathf.Cos(decRad) * Mathf.Cos(hourRad);
        sinAlt = Mathf.Clamp(sinAlt, -1f, 1f);
        float altRad = Mathf.Asin(sinAlt);
        altitudeDeg = altRad * Mathf.Rad2Deg;

        float cosLatCosAlt = Mathf.Cos(latRad) * Mathf.Cos(altRad);
        float bearingStandard;

        if (Mathf.Abs(cosLatCosAlt) < 0.0001f)
        {
            // Evita divisão por zero (ex: polos ou sol no zênite)
            bearingStandard = 0f;
        }
        else
        {
            float cosAz = (Mathf.Sin(decRad) - Mathf.Sin(latRad) * sinAlt) / cosLatCosAlt;
            cosAz = Mathf.Clamp(cosAz, -1f, 1f);
            float bearing = Mathf.Acos(cosAz) * Mathf.Rad2Deg;

            // Normaliza o ângulo horário para decidir manhã/tarde
            float normalizedHour = ((hourAngleDeg % 360f) + 360f) % 360f;
            bool afternoon = normalizedHour > 0f && normalizedHour < 180f;

            bearingStandard = afternoon ? 360f - bearing : bearing;
        }

        // Converte de rumo padrão (0=N,90=E,180=S,270=O, horário) para a
        // convenção da spec (90=N,0=E,270=S,180=O).
        azimuthDeg = ((90f - bearingStandard) % 360f + 360f) % 360f;
    }

    /// <summary>
    /// Converte altitude/azimute (convenção da spec) em um vetor unitário
    /// "observador -> astro", assumindo Z+ = Norte, X+ = Leste, Y+ = Cima.
    /// </summary>
    private static Vector3 DirectionFromAltAzimuth(float altitudeDeg, float azimuthDeg)
    {
        // Reconverte para rumo padrão para montar o vetor horizontal.
        float bearingStandard = ((90f - azimuthDeg) % 360f + 360f) % 360f;
        float bearingRad = bearingStandard * Mathf.Deg2Rad;
        float altRad = altitudeDeg * Mathf.Deg2Rad;

        float cosAlt = Mathf.Cos(altRad);
        float x = cosAlt * Mathf.Sin(bearingRad); // Leste
        float y = Mathf.Sin(altRad);              // Cima
        float z = cosAlt * Mathf.Cos(bearingRad);  // Norte

        return new Vector3(x, y, z).normalized;
    }

    private static void CalculateDayNightLength(float latitudeDeg, float declinationDeg, out float dayLengthHours, out float nightLengthHours)
    {
        float latRad = latitudeDeg * Mathf.Deg2Rad;
        float decRad = declinationDeg * Mathf.Deg2Rad;

        float tanProduct = Mathf.Tan(latRad) * Mathf.Tan(decRad);

        if (tanProduct <= -1f)
        {
            // Sol nunca se põe (dia polar)
            dayLengthHours = 24f;
        }
        else if (tanProduct >= 1f)
        {
            // Sol nunca nasce (noite polar)
            dayLengthHours = 0f;
        }
        else
        {
            float cosH0 = -tanProduct;
            float h0Deg = Mathf.Acos(Mathf.Clamp(cosH0, -1f, 1f)) * Mathf.Rad2Deg;
            dayLengthHours = 2f * h0Deg / 15f;
        }

        nightLengthHours = 24f - dayLengthHours;
    }

    private static MoonPhase GetMoonPhaseFromAge(float moonAge)
    {
        float step = SynodicMonth / 8f;
        int index = Mathf.FloorToInt(moonAge / step) % 8;
        if (index < 0) index += 8;

        switch (index)
        {
            case 0: return MoonPhase.NewMoon;
            case 1: return MoonPhase.WaxingCrescent;
            case 2: return MoonPhase.FirstQuarter;
            case 3: return MoonPhase.WaxingGibbous;
            case 4: return MoonPhase.FullMoon;
            case 5: return MoonPhase.WaningGibbous;
            case 6: return MoonPhase.LastQuarter;
            default: return MoonPhase.WaningCrescent;
        }
    }

    // ---------------------------------------------------------------
    // Salvamento (estrutura mínima — implementação de I/O não definida
    // nesta versão, conforme a spec).
    // ---------------------------------------------------------------
    [Serializable]
    public struct TimeManagerState
    {
        public int dayOfYear;
        public float timeOfDay;
        public float latitude;
        public Hemisphere hemisphere;
    }

    public TimeManagerState SaveState()
    {
        return new TimeManagerState
        {
            dayOfYear = this.dayOfYear,
            timeOfDay = this.timeOfDay,
            latitude = this.latitude,
            hemisphere = this.hemisphere
        };
        // TODO: persistência em disco/PlayerPrefs/arquivo fica a critério
        // de um sistema de save externo, conforme a spec.
    }

    public void LoadState(TimeManagerState state)
    {
        dayOfYear = ((state.dayOfYear % daysPerYear) + daysPerYear) % daysPerYear;
        timeOfDay = Mathf.Repeat(state.timeOfDay, 24f);
        latitude = Mathf.Clamp(Mathf.Abs(state.latitude), -90f, 90f);
        hemisphere = state.hemisphere;

        RecalculateAstronomy();
        // TODO: leitura em disco/PlayerPrefs/arquivo fica a critério de um
        // sistema de save externo, conforme a spec.
    }
}