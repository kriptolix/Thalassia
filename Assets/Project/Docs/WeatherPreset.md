Especificação de Arquitetura — WeatherPreset
Objetivo

O WeatherPreset representa um estado climático completo do mundo em um determinado momento.

Ele é um objeto exclusivamente de dados, utilizado como referência pelos subsistemas durante mudanças de clima.

O preset não contém lógica, não executa transições e não aplica efeitos. Sua única responsabilidade é armazenar os valores de referência de cada sistema climático.

Responsabilidades

O WeatherPreset deve:

Centralizar todos os parâmetros que definem um estado climático.
Servir como fonte de dados para os subsistemas.
Permitir edição através do Inspector da Unity.
Representar um estado final e estático do clima.

Ele não deve:

Executar lógica de atualização.
Conter referências para objetos da cena.
Conhecer o WeatherSystem.
Conhecer outros presets.
Controlar interpolações.
Aplicar noise.
Estrutura

Cada WeatherPreset contém uma coleção de módulos de configuração, um para cada subsistema existente.

Exemplo:

WeatherPreset

    General

    WindSettings

    CloudSettings

    LightingSettings

    WaterSettings

    FogSettings

    RainSettings

    ...

Cada módulo contém apenas os parâmetros pertencentes ao seu respectivo subsistema.

Exemplo:

WindSettings

    Direction

    Average Speed

    Gust Strength

    Gust Frequency
CloudSettings

    Coverage

    Density

    Altitude

    Shadow Strength

O WeatherPreset apenas agrupa essas informações.

Organização por Composição

Cada grupo de configurações deve ser implementado como uma classe serializável independente.

Exemplo:

WeatherPreset

    WindSettings
        ...

    CloudSettings
        ...

    WaterSettings
        ...

Essa abordagem possui as seguintes vantagens:

separação clara entre sistemas;
melhor organização no Inspector;
baixo acoplamento entre subsistemas;
facilidade para adicionar novos módulos.
Leitura pelos Subsistemas

Quando ocorre uma mudança de clima:

o WeatherSystem define o novo WeatherPreset ativo;
notifica os subsistemas;
cada subsistema consulta apenas sua seção do preset.

Exemplo:

Weather Changed

        │

        ▼

WindSystem
    preset.WindSettings

CloudSystem
    preset.CloudSettings

WaterSystem
    preset.WaterSettings

Nenhum subsistema precisa conhecer os dados dos demais.

Estado Final

Cada WeatherPreset representa um estado completamente definido.

Exemplo:

Storm

Wind
    Speed = 22

Clouds
    Coverage = 100%

Fog
    Density = 0.35

Rain
    Intensity = 1.0

Não existem valores "parciais" ou "incrementais".

O preset sempre descreve como o ambiente deve estar após todas as transições terminarem.

Sobrescritas

O WeatherPreset representa apenas o estado de referência.

Os subsistemas podem modificar internamente qualquer valor durante a execução.

Exemplos:

aumentar o vento durante uma missão;
reduzir a chuva temporariamente;
alterar a direção do vento por gameplay;
aplicar rajadas dinâmicas.

Essas modificações não alteram o WeatherPreset nem afetam outros subsistemas.

Evolução do Sistema

Novos sistemas climáticos podem ser adicionados simplesmente criando um novo módulo de configuração.

Exemplo:

WeatherPreset

    WindSettings

    CloudSettings

    WaterSettings

    SeaStateSettings

    SnowSettings

    LightningSettings

O WeatherSystem não precisa ser modificado para interpretar esses dados. Basta que o novo subsistema saiba localizar sua seção dentro do WeatherPreset.

Princípios da Arquitetura
O WeatherPreset é um contêiner de dados.
Cada preset representa um estado climático completo.
Os dados são organizados por composição, agrupando parâmetros por subsistema.
Cada subsistema é proprietário da estrutura de seus próprios dados.
O WeatherPreset não contém lógica, comportamento ou referências para objetos da cena.
Os valores armazenados representam sempre o estado alvo do ambiente.
Sobrescritas e variações em tempo de execução pertencem exclusivamente aos subsistemas e não modificam o preset original.