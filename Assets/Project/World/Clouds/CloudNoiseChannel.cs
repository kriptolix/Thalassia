using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Configuração de ruído contínuo para uma única propriedade de nuvem.
    /// Usa Perlin Noise (contínuo, sem saltos), conforme exigido pelo
    /// CloudSystem.md (seção 5.2): "não deve utilizar valores aleatórios
    /// independentes, pois isso causaria mudanças bruscas e artificiais".
    /// </summary>
    [System.Serializable]
    public class CloudNoiseChannel
    {
        [Tooltip("Amplitude máxima da variação, na mesma unidade do parâmetro controlado. Zero desativa o ruído para esta propriedade.")]
        public float amplitude = 0f;

        [Tooltip("Escala do padrão de ruído. Valores baixos = variação mais suave e de longo período.")]
        public float frequency = 0.1f;

        [Tooltip("Velocidade com que o tempo avança dentro do padrão de ruído.")]
        public float speed = 0.02f;

        [Tooltip("Offset de semente. Permite reproduzir o mesmo padrão (mesma seed) ou diversificar entre instâncias/propriedades.")]
        public float seedOffset = 0f;

        /// <summary>
        /// Avalia o ruído contínuo para o instante de tempo informado.
        /// Retorna um valor no intervalo [-amplitude, +amplitude].
        /// </summary>
        public float Evaluate(float time)
        {
            if (amplitude <= 0f) return 0f;

            float coordinate = seedOffset + time * speed * Mathf.Max(frequency, 0.0001f);
            float sample = Mathf.PerlinNoise(coordinate, seedOffset * 0.37f + 10f);

            // Perlin retorna [0,1]; remapeia para [-1,1] antes de aplicar a amplitude.
            return (sample * 2f - 1f) * amplitude;
        }
    }
}
