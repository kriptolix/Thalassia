namespace Game.Weather
{
    /// <summary>
    /// Contrato opcional e recomendado para subsistemas climáticos (CloudSystem,
    /// FogSystem, PrecipitationSystem, WaterSystem, DayNightSystem, etc).
    ///
    /// O WeatherSystem NÃO depende desta interface — ele não conhece a
    /// implementação dos subsistemas (baixo acoplamento, conforme
    /// WeatherSystem.md). A comunicação real acontece através do evento
    /// WeatherSystem.WeatherChanged; cada subsistema se inscreve por conta própria.
    ///
    /// Implementar esta interface serve apenas para padronizar a assinatura do
    /// método de callback entre os diferentes subsistemas.
    /// </summary>
    public interface IWeatherSubsystem
    {
        /// <summary>
        /// Chamado quando o WeatherSystem notifica uma mudança de clima.
        /// O subsistema deve consultar apenas a sua própria seção dentro de
        /// args.NewPreset e iniciar sua própria transição/interpolação.
        /// </summary>
        void OnWeatherChanged(WeatherChangedEventArgs args);
    }

    /// <summary>
    /// Dados enviados junto ao evento WeatherSystem.WeatherChanged.
    /// </summary>
    public readonly struct WeatherChangedEventArgs
    {
        /// <summary>Preset anteriormente ativo. Pode ser nulo na primeira notificação.</summary>
        public readonly WeatherPreset PreviousPreset;

        /// <summary>Novo preset ativo. Nunca nulo.</summary>
        public readonly WeatherPreset NewPreset;

        /// <summary>Verdadeiro se a mudança veio de um ForceWeather (controle forçado).</summary>
        public readonly bool IsForced;

        public WeatherChangedEventArgs(WeatherPreset previousPreset, WeatherPreset newPreset, bool isForced)
        {
            PreviousPreset = previousPreset;
            NewPreset = newPreset;
            IsForced = isForced;
        }
    }
}
