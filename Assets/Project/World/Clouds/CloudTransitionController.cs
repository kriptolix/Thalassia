using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Interpola continuamente o estado atual de nuvens em direção ao estado
    /// alvo (que já contém o CloudNoise aplicado). Ver CloudSystem.md, seção 5.3.
    ///
    /// Usa suavização exponencial (independente de framerate) em vez de um
    /// Lerp direto por frame, evitando mudanças abruptas mesmo quando o
    /// WeatherPreset ativo muda repentinamente — a transição para o novo
    /// clima acontece de forma gradual, não instantânea.
    /// </summary>
    public class CloudTransitionController
    {
        public CloudSettings Current { get; private set; }

        public CloudTransitionController(CloudSettings initial)
        {
            Current = initial.Clone();
        }

        /// <summary>
        /// Avança a interpolação em direção a "target".
        /// smoothingSpeed maior = segue o alvo mais rapidamente.
        /// </summary>
        public void Tick(CloudSettings target, float smoothingSpeed, float deltaTime)
        {
            float t = 1f - Mathf.Exp(-Mathf.Max(0f, smoothingSpeed) * deltaTime);

            Current.coverage = Mathf.Lerp(Current.coverage, target.coverage, t);
            Current.density = Mathf.Lerp(Current.density, target.density, t);
            Current.erosion = Mathf.Lerp(Current.erosion, target.erosion, t);
            Current.shapeFactor = Mathf.Lerp(Current.shapeFactor, target.shapeFactor, t);
            Current.densityMultiplier = Mathf.Lerp(Current.densityMultiplier, target.densityMultiplier, t);
            Current.ambientExposure = Mathf.Lerp(Current.ambientExposure, target.ambientExposure, t);
            Current.powderEffect = Mathf.Lerp(Current.powderEffect, target.powderEffect, t);
            Current.multiScattering = Mathf.Lerp(Current.multiScattering, target.multiScattering, t);
            Current.cloudOpacity = Mathf.Lerp(Current.cloudOpacity, target.cloudOpacity, t);
            Current.altitude = Mathf.Lerp(Current.altitude, target.altitude, t);
            Current.thickness = Mathf.Lerp(Current.thickness, target.thickness, t);

            Current.cloudColor = Color.Lerp(Current.cloudColor, target.cloudColor, t);
            Current.cloudBottomTint = Color.Lerp(Current.cloudBottomTint, target.cloudBottomTint, t);
            Current.cloudTopTint = Color.Lerp(Current.cloudTopTint, target.cloudTopTint, t);
        }
    }
}
