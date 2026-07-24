Especificação de Arquitetura — WaterSystem
Objetivo

O WaterSystem é o subsistema responsável pelo controle do ambiente marítimo utilizando o Unity Water System.

Sua função é traduzir condições climáticas e parâmetros de ambiente em comportamento visual e dinâmico do oceano.

O sistema não simula hidrodinâmica realista. Ele busca uma representação convincente para a experiência de navegação do veleiro.

O WaterSystem é influenciado principalmente pelo WindSystem, pois o estado do mar é uma consequência do vento acumulado.

Responsabilidades

O WaterSystem é responsável por:

Controlar parâmetros do Unity Water System.
Gerenciar ondas.
Gerenciar swell.
Controlar ripples.
Controlar espuma.
Aplicar variações naturais ao oceano.
Fazer transições graduais entre estados marítimos.
Integrar influência do vento no comportamento da água.

O WaterSystem não é responsável por:

Definir velocidade ou direção do vento.
Definir condições climáticas.
Controlar chuva.
Controlar iluminação.
Simular física real do barco.
Fonte de Dados

O WaterSystem recebe dados de duas fontes:

WeatherPreset

Define o estado base esperado do oceano.

Exemplo:

WeatherPreset

    WaterSettings

Contém parâmetros como:

WaterSettings

    Base Wave Height

    Base Swell Strength

    Wave Direction

    Foam Intensity

    Ripple Intensity

    Choppiness

    Surface Roughness

Esses valores representam o estado climático de referência.

WindSystem

O WaterSystem consulta o estado atual do vento para influenciar a superfície da água.

O vento não altera diretamente o preset.

Ele funciona como um modificador dinâmico.

Exemplo:

Final Ocean State =
    WaterSettings
    +
    Wind Influence
Relação com o Vento

O WaterSystem utiliza diferentes influências do vento conforme o tipo de fenômeno.

Local Wind

Afeta principalmente:

ripples;
pequenas ondas;
movimentação superficial.

Representa a ação imediata do vento próximo à superfície.

Exemplo:

Wind Speed ↑

Ripple Intensity ↑
Distant Wind

Afeta principalmente:

swell;
ondas grandes;
movimentação de longo período.

Representa o vento atuando em uma grande área oceânica antes da onda chegar ao jogador.

Exemplo:

Distant Wind ↑

Swell Height ↑
Estrutura do WaterSettings

O WaterSettings representa o estado base do oceano.

Exemplo:

WaterSettings

    Waves

        Height

        Steepness

        Direction


    Swell

        Strength

        Direction

        Scale


    Ripples

        Intensity

        Scale


    Foam

        Amount

        Threshold


    Surface

        Roughness

        Color

A estrutura deve refletir os parâmetros disponíveis no Unity Water System.

Controle de Ondas

O sistema controla:

altura das ondas;
intensidade;
direção predominante;
deformação da superfície.

O valor final é resultado de:

Wave State = Base WaterSettings + Wind Influence

O sistema deve permitir que um mar permaneça agitado mesmo após uma redução momentânea do vento, caso exista swell acumulado.

Controle de Swell

O swell representa ondas de grande escala.

Características:

resposta lenta;
influenciado pelo vento distante;
possui persistência temporal.

O WaterSystem deve tratar swell separadamente das ondas locais.

Exemplo:

Storm Ending

Wind ↓

Ripples ↓ rapidamente

Swell ↓ lentamente
Controle de Ripples

Ripples representam pequenas perturbações na superfície.

São influenciados principalmente por:

vento local;
intensidade da superfície.

A transição deve ser mais rápida que ondas e swell.

Controle de Foam

A espuma é influenciada por:

intensidade das ondas;
inclinação das ondas;
vento;
condições do mar.

O WaterSystem deve evitar que a espuma seja apenas um valor fixo do preset.

Exemplo:

Foam =
    Base Foam
    +
    Wave Energy
    +
    Wind Influence
Transições

Quando um novo clima é aplicado:

WeatherSystem sinaliza mudança.
WaterSystem lê o novo WaterSettings.
Define novos valores alvo.
Calcula influência atual do vento.
Faz interpolação gradual.

O tempo de transição pertence ao WaterSystem.

Isso permite:

ondas aumentando lentamente;
swell persistindo;
espuma reagindo rapidamente.
Noise

O WaterSystem aplica variações naturais independentes.

Exemplos:

pequenas mudanças na altura das ondas;
variação da espuma;
irregularidade do movimento superficial.

O noise deve evitar padrões artificiais repetitivos.

Integração com Unity Water System

O WaterSystem encapsula todas as chamadas ao Unity Water System.

Nenhum outro sistema deve acessar diretamente:

componentes de água;
materiais;
parâmetros de ondas;
configurações de espuma.

A comunicação externa ocorre apenas através do WaterSystem.

Fluxo de Funcionamento
WeatherSystem

        │

        ▼

WeatherPreset

        │

        ▼

WaterSystem

        │
        ├── Lê WaterSettings
        ├── Consulta WindSystem
        ├── Calcula estado final do oceano
        ├── Atualiza Unity Water System
        ├── Aplica transições
        └── Aplica noise
Evolução

O WaterSystem deve permitir futuras extensões:

correntes marítimas;
zonas de turbulência;
interação barco/água;
maré;
tempestades extremas;
ondas especiais de eventos.

Esses sistemas devem ser adicionados como módulos internos sem alterar a comunicação com o WeatherSystem.

Princípios da Arquitetura
O WaterSystem representa o estado do oceano, não o clima.
O WeatherPreset fornece uma referência inicial.
O vento é um modificador dinâmico do estado marítimo.
Ondas locais e swell possuem comportamentos independentes.
O Unity Water System fica totalmente encapsulado.
A transição temporal pertence ao WaterSystem.
O sistema prioriza uma experiência de navegação convincente, não uma simulação física completa.