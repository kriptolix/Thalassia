namespace Game.Weather
{
    /// <summary>
    /// Identifica um estado climático dentro da tabela de progressão do WeatherSystem.
    /// Cada WeatherPreset deve estar associado a um destes tipos para que o
    /// WeatherSystem consiga localizá-lo durante a progressão automática e o
    /// controle forçado.
    ///
    /// A lista reflete os exemplos usados em WeatherSystem.md (Clear, PartlyCloudy,
    /// Overcast, Fog, Rain, Storm). Novos climas podem ser adicionados aqui sem
    /// exigir mudanças na lógica central do WeatherSystem.
    /// </summary>
    public enum WeatherType
    {
        Clear,
        PartlyCloudy,
        Overcast,
        Fog,
        Rain,
        Storm
    }
}
