using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Responsável por converter os valores recebidos do WeatherPreset em um
    /// estado alvo de nuvens, aplicando o CloudNoise sobre cada propriedade
    /// (CloudSystem.md, seção 5.1). Não aplica valores diretamente no HDRP —
    /// apenas produz um novo CloudSettings "alvo" a cada chamada.
    ///
    /// É uma classe estática e sem estado: toda a continuidade vem do
    /// parâmetro "time", que deve crescer monotonicamente (ex.: acumulado
    /// pelo CloudSystem a cada Update).
    /// </summary>
    public static class CloudStateGenerator
    {
        public static CloudSettings GenerateTarget(CloudSettings baseSettings, CloudNoiseProfile noise, float time)
        {
            var target = new CloudSettings
            {
                coverage = Mathf.Clamp01(baseSettings.coverage + noise.coverage.Evaluate(time)),
                density = Mathf.Clamp01(baseSettings.density + noise.density.Evaluate(time)),

                erosion = Mathf.Clamp01(baseSettings.erosion + noise.erosion.Evaluate(time)),
                shapeFactor = Mathf.Clamp01(baseSettings.shapeFactor + noise.shapeFactor.Evaluate(time)),
                densityMultiplier = Mathf.Max(0f, baseSettings.densityMultiplier + noise.densityMultiplier.Evaluate(time)),

                ambientExposure = Mathf.Max(0f, baseSettings.ambientExposure + noise.ambientExposure.Evaluate(time)),
                powderEffect = Mathf.Clamp01(baseSettings.powderEffect + noise.powderEffect.Evaluate(time)),
                multiScattering = Mathf.Clamp01(baseSettings.multiScattering + noise.multiScattering.Evaluate(time)),
                cloudOpacity = Mathf.Clamp01(baseSettings.cloudOpacity + noise.cloudOpacity.Evaluate(time)),

                // Fases levemente distintas (time * 0.9 / 1.0 / 1.1) evitam que as
                // três cores oscilem em perfeito uníssono (efeito de "pulsar").
                cloudColor = ApplyBrightnessShift(baseSettings.cloudColor, noise.colorBrightness.Evaluate(time)),
                cloudBottomTint = ApplyBrightnessShift(baseSettings.cloudBottomTint, noise.colorBrightness.Evaluate(time * 0.9f)),
                cloudTopTint = ApplyBrightnessShift(baseSettings.cloudTopTint, noise.colorBrightness.Evaluate(time * 1.1f)),

                altitude = Mathf.Max(0f, baseSettings.altitude + noise.altitude.Evaluate(time)),
                thickness = Mathf.Max(0f, baseSettings.thickness + noise.thickness.Evaluate(time)),
            };

            return target;
        }

        private static Color ApplyBrightnessShift(Color color, float delta)
        {
            if (delta == 0f) return color;

            Color.RGBToHSV(color, out var h, out var s, out var v);
            v = Mathf.Clamp01(v + delta);
            var result = Color.HSVToRGB(h, s, v);
            result.a = color.a;
            return result;
        }
    }
}
