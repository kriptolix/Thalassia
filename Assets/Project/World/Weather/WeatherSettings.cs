using System;
using UnityEngine;

namespace Game.Weather
{
    // ---------------------------------------------------------------------
    // Todas as classes abaixo são contêineres de dados puros (sem lógica),
    // conforme WeatherPreset.md: "cada módulo contém apenas os parâmetros
    // pertencentes ao seu respectivo subsistema". Cada subsistema é dono da
    // estrutura dos seus próprios dados; os campos aqui refletem o que já foi
    // especificado nos documentos de arquitetura fornecidos.
    // ---------------------------------------------------------------------

    /// <summary>
    /// Parâmetros de vento de referência para o estado climático.
    /// Consumidos pelo (futuro) WindSystem, e indiretamente pelo WaterSystem
    /// e CloudSystem como influência dinâmica.
    /// </summary>
    [Serializable]
    public class WindSettings
    {
        [Tooltip("Indica se direção inicial está ou não ativada.")]
        public bool overrideDirection;       

        [Tooltip("Direção do vento em graus (0-360).")]
        [Range(0f, 360f)] public float direction = 0f;

        [Tooltip("Velocidade média do vento (m/s).")]
        public float averageSpeed = 5f;

        [Tooltip("Intensidade adicional das rajadas (m/s).")]
        public float gustStrength = 2f;

        [Tooltip("Frequência das rajadas (ocorrências por minuto).")]
        public float gustFrequency = 4f;
    }

    /// <summary>
    /// Valores alvo para as Volumetric Clouds (HDRP) e Background Clouds.
    /// Ver CloudSystem.md, seção 9 (Parâmetros Controlados).
    /// </summary>
    [Serializable]
    public class CloudSettings
    {
        [Range(0f, 1f)] public float coverage = 0.1f;
        [Range(0f, 1f)] public float density = 0.2f;
        public Color cloudColor = Color.white;
        public float ambientExposure = 1f;
        [Range(0f, 1f)] public float erosion = 0.6f;
        [Range(0f, 1f)] public float shapeFactor = 0.5f;
        public float densityMultiplier = 1f;
        [Range(0f, 1f)] public float powderEffect = 0.5f;
        [Range(0f, 1f)] public float multiScattering = 0.5f;
        [Range(0f, 1f)] public float cloudOpacity = 1f;
        public Color cloudBottomTint = Color.white;
        public Color cloudTopTint = Color.white;

        [Tooltip("Metros. Aplicável somente quando o layout de nuvens em uso suportar altitude.")]
        public float altitude = 1500f;

        [Tooltip("Metros. Aplicável somente quando o layout de nuvens em uso suportar espessura.")]
        public float thickness = 500f;

        /// <summary>Cria uma cópia independente deste estado (todos os campos são por valor).</summary>
        public CloudSettings Clone() => (CloudSettings)MemberwiseClone();
    }

    /// <summary>
    /// Valores alvo para o Global Fog (HDRP Volume). Ver FogSystem.md.
    /// Não inclui Fog Zones: essas pertencem ao mundo, não ao clima.
    /// </summary>
    [Serializable]
    public class FogSettings
    {
        [Range(0f, 1f)] public float density = 0f;
        public Color color = Color.gray;

        [Tooltip("Metros.")]
        public float height = 200f;

        [Range(0f, 1f)] public float volumetricIntensity = 0.5f;
        public float distanceFalloff = 1f;
    }

    /// <summary>
    /// Valores alvo para precipitação (chuva). Ver PrecipitationSystem.md.
    /// </summary>
    [Serializable]
    public class RainSettings
    {
        [Range(0f, 1f)] public float intensity = 0f;
        public float dropSize = 1f;
        public float fallSpeed = 8f;
        [Range(0f, 1f)] public float audioVolume = 0f;
        [Range(0f, 1f)] public float cameraEffectIntensity = 0f;
        [Range(0f, 1f)] public float waterRippleIntensity = 0f;
        [Range(0f, 1f)] public float surfaceWetness = 0f;
    }

    /// <summary>
    /// Valores alvo para o Unity Water System. Ver WaterSystem.md.
    /// Organizado por composição, espelhando a estrutura documentada
    /// (Waves / Swell / Ripples / Foam / Surface).
    /// </summary>
    [Serializable]
    public class WaterSettings
    {
        [Serializable]
        public class WaveSettings
        {
            public float height = 0.3f;
            [Range(0f, 1f)] public float steepness = 0.3f;
            [Range(0f, 360f)] public float direction = 0f;
        }

        [Serializable]
        public class SwellSettings
        {
            public float strength = 0.2f;
            [Range(0f, 360f)] public float direction = 0f;
            public float scale = 1f;
        }

        [Serializable]
        public class RippleSettings
        {
            [Range(0f, 1f)] public float intensity = 0.3f;
            public float scale = 1f;
        }

        [Serializable]
        public class FoamSettings
        {
            [Range(0f, 1f)] public float amount = 0.1f;
            [Range(0f, 1f)] public float threshold = 0.5f;
        }

        [Serializable]
        public class SurfaceSettings
        {
            [Range(0f, 1f)] public float roughness = 0.2f;
            public Color color = new Color(0.05f, 0.2f, 0.25f, 1f);
        }

        public WaveSettings waves = new WaveSettings();
        public SwellSettings swell = new SwellSettings();
        public RippleSettings ripples = new RippleSettings();
        public FoamSettings foam = new FoamSettings();
        public SurfaceSettings surface = new SurfaceSettings();
    }

}
