using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Representa um estado climático completo do mundo em um determinado momento.
    ///
    /// É um objeto exclusivamente de dados: não contém lógica, não executa
    /// transições e não aplica efeitos. Sua única responsabilidade é armazenar
    /// os valores de referência de cada sistema climático (ver WeatherPreset.md).
    ///
    /// Cada subsistema (CloudSystem, FogSystem, PrecipitationSystem, WaterSystem...)
    /// deve consultar apenas a seção que lhe pertence.
    /// </summary>
    [CreateAssetMenu(fileName = "New Weather Preset", menuName = "Weather/Weather Preset", order = 0)]
    public class WeatherPreset : ScriptableObject
    {
        [Header("Identificação")]
        [Tooltip("Tipo climático usado pela WeatherProgressionTable e pelo WeatherSystem para localizar este preset.")]
        public WeatherType weatherType;

        [Tooltip("Nome amigável exibido no Editor. Não é usado em lógica de runtime.")]
        public string presetName;

        [TextArea(2, 4)]
        public string description;

        [Header("Módulos de Configuração")]
        public WindSettings wind = new WindSettings();
        public CloudSettings clouds = new CloudSettings();
        public WaterSettings water = new WaterSettings();
        public FogSettings fog = new FogSettings();
        public RainSettings rain = new RainSettings();        

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(presetName))
            {
                presetName = weatherType.ToString();
            }
        }
#endif
    }
}
