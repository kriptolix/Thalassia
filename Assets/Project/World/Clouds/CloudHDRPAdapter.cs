using UnityEngine.Rendering.HighDefinition;

namespace Game.Weather
{
    /// <summary>
    /// Aplica o estado interpolado de nuvens ao HDRP Volumetric Clouds override.
    /// É a única classe do CloudSystem que efetivamente escreve no HDRP.
    ///
    /// ATENÇÃO — PONTO A CONFERIR NO EDITOR:
    /// Os campos abaixo existem na classe VolumetricClouds independentemente do
    /// Control Mode (Simple / Advanced / Manual) selecionado no Volume — o
    /// Control Mode só muda quais campos aparecem no Inspector, não quais
    /// existem via script. Como o projeto ainda está decidindo entre
    /// Advanced/Manual (cloud map + Volumetric Clouds), este adapter assume o
    /// caminho "Advanced": usa cumulusMapMultiplier/cumulonimbusMapMultiplier
    /// como a forma de expressar "Coverage" (que não existe como um float
    /// isolado no HDRP). Se o projeto migrar para Manual puro (sem cloud map),
    /// troque essas duas linhas por manipulação da densityCurve/AnimationCurve
    /// correspondente.
    ///
    /// CloudOpacity, CloudBottomTint e CloudTopTint (do CloudSettings) não têm
    /// equivalente direto no Volumetric Clouds — ficam por conta do
    /// CloudLayerAdapter (Background Clouds), que já expõe tint por camada.
    /// </summary>
    public class CloudHDRPAdapter
    {
        private readonly VolumetricClouds _clouds;

        public CloudHDRPAdapter(VolumetricClouds clouds)
        {
            _clouds = clouds;
        }

        public bool IsValid => _clouds != null;

        public void Apply(CloudSettings state)
        {
            if (_clouds == null) return;

            // Coverage -> multiplicadores de cobertura das camadas do cloud map (modo Advanced).
            _clouds.cumulusMapMultiplier.value = state.coverage;
            _clouds.cumulonimbusMapMultiplier.value = state.coverage;

            _clouds.densityMultiplier.value = state.density;
            _clouds.shapeFactor.value = state.shapeFactor;
            _clouds.erosionFactor.value = state.erosion;
            _clouds.ambientLightProbeDimmer.value = state.ambientExposure;
            _clouds.powderEffectIntensity.value = state.powderEffect;
            _clouds.multiScattering.value = state.multiScattering;
            _clouds.scatteringTint.value = state.cloudColor;
            _clouds.bottomAltitude.value = state.altitude;
            _clouds.altitudeRange.value = state.thickness;
        }
    }
}
