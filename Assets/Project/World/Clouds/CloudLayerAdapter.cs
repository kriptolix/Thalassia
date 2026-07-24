using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Weather
{
    /// <summary>
    /// Faz o papel de "Background Clouds" (CloudSystem.md, seções 5.4 e 6)
    /// usando o Cloud Layer nativo do HDRP — uma camada 2D de nuvens
    /// projetada no céu — em vez de meshes/material customizados. Isso
    /// resolve nativamente a exigência de ficarem sempre "à distância", já
    /// que o Cloud Layer é renderizado como parte da esfera do céu, nunca se
    /// aproximando da câmera.
    ///
    /// ATENÇÃO — PONTO A CONFERIR NO EDITOR:
    /// A estrutura de CloudLayer.layerA / layerB (cada uma do tipo
    /// CloudLayer.CloudMap, expondo cloudMap, tint, exposure e
    /// opacityR/opacityG/opacityB/opacityA) deve ser conferida via
    /// autocomplete, pois pode variar ligeiramente entre versões do pacote
    /// HDRP. "useDoubleLayer" é passado explicitamente pelo CloudSystem (não
    /// lido do componente) para evitar depender do nome exato do enum
    /// Single/Double no seu pacote instalado.
    ///
    /// Ainda não há um cloud map/material definido pelo projeto — os
    /// nomes de canal (R/G/B/A) usados como "Coverage" são configuráveis via
    /// CoverageChannel e devem ser ajustados assim que o cloud map final for
    /// escolhido.
    /// </summary>
    public class CloudLayerAdapter
    {
        public enum CoverageChannel { Red, Green, Blue, Alpha, All }

        private readonly CloudLayer _cloudLayer;
        private readonly CoverageChannel _coverageChannel;
        private readonly bool _useDoubleLayer;

        public CloudLayerAdapter(CloudLayer cloudLayer, CoverageChannel coverageChannel, bool useDoubleLayer)
        {
            _cloudLayer = cloudLayer;
            _coverageChannel = coverageChannel;
            _useDoubleLayer = useDoubleLayer;
        }

        public bool IsValid => _cloudLayer != null;

        public void Apply(CloudSettings state)
        {
            if (_cloudLayer == null) return;

            ApplyLayer(_cloudLayer.layerA, state.coverage, state.cloudBottomTint, state.ambientExposure);

            if (_useDoubleLayer)
            {
                ApplyLayer(_cloudLayer.layerB, state.coverage, state.cloudTopTint, state.ambientExposure);
            }
        }

        private void ApplyLayer(CloudLayer.CloudMap layer, float coverage, Color tint, float exposure)
        {
            if (layer == null) return;

            switch (_coverageChannel)
            {
                case CoverageChannel.Red:
                    layer.opacityR.value = coverage;
                    break;
                case CoverageChannel.Green:
                    layer.opacityG.value = coverage;
                    break;
                case CoverageChannel.Blue:
                    layer.opacityB.value = coverage;
                    break;
                case CoverageChannel.Alpha:
                    layer.opacityA.value = coverage;
                    break;
                case CoverageChannel.All:
                    layer.opacityR.value = coverage;
                    layer.opacityG.value = coverage;
                    layer.opacityB.value = coverage;
                    layer.opacityA.value = coverage;
                    break;
            }

            layer.tint.value = tint;
            layer.exposure.value = exposure;
        }
    }
}
