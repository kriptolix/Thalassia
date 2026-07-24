Especificação de Arquitetura — PrecipitationSystem
Objetivo

O PrecipitationSystem é o subsistema responsável por representar visual e sonoramente a precipitação no ambiente.

Sua função é interpretar os parâmetros de chuva (apenas chuva inicialmente) presentes no WeatherPreset e aplicá-los gradualmente ao mundo, produzindo todos os efeitos diretamente relacionados à precipitação.

O sistema não decide quando chove. Ele apenas responde às mudanças sinalizadas pelo WeatherSystem.

Responsabilidades

O PrecipitationSystem é responsável por:

Controlar a intensidade da chuva.
Gerenciar os efeitos de precipitação através do VFX Graph.
Controlar os efeitos sonoros da chuva.
Aplicar efeitos visuais causados pela precipitação.
Gerenciar efeitos secundários produzidos pela chuva.
Realizar a transição gradual entre estados climáticos.
Aplicar variações locais (noise) durante a execução.

O PrecipitationSystem não é responsável por:

Formação de nuvens.
Cobertura do céu.
Iluminação.
Vento.
Progressão climática.
Fonte de Dados

Ao receber uma mudança de clima, o PrecipitationSystem consulta exclusivamente sua seção do WeatherPreset.

Exemplo:

WeatherPreset

    RainSettings

Nenhum outro módulo do preset é interpretado pelo PrecipitationSystem.

Estrutura do RainSettings

O RainSettings representa o estado final da precipitação para aquele clima.

Exemplo de parâmetros:

RainSettings

    Intensity

    Drop Size

    Fall Speed

    Audio Volume

    Camera Effect Intensity

    Water Ripple Intensity

    Surface Wetness

A estrutura pode evoluir conforme novos efeitos forem adicionados ao jogo.

Transições

Quando um novo preset é ativado:

o PrecipitationSystem lê os novos valores;
define esses valores como alvo;
realiza a interpolação utilizando sua própria lógica.

O tempo de transição pertence exclusivamente ao PrecipitationSystem.

Isso permite que:

a chuva comece lentamente;
aumente de intensidade;
diminua gradualmente;
termine antes ou depois da mudança das nuvens.
VFX Graph

A representação visual da chuva é realizada através do Unity VFX Graph.

O PrecipitationSystem é responsável por controlar seus parâmetros em tempo de execução.

Exemplos:

emissão;
densidade;
velocidade;
tamanho das gotas;
largura da área de precipitação;
intensidade geral.

A implementação do efeito permanece encapsulada no sistema.

O restante da arquitetura não possui conhecimento sobre o VFX utilizado.

Áudio

O PrecipitationSystem controla os sons diretamente associados à precipitação.

Exemplos:

volume;
intensidade;
blend entre chuva fraca e chuva forte;
ativação e desativação dos loops.

A lógica de áudio acompanha a intensidade atual da chuva.

Efeitos Secundários

Além da precipitação, o PrecipitationSystem gerencia todos os efeitos diretamente causados pela chuva.

Exemplos:

ripples na superfície da água;
gotas na câmera;
redução de visibilidade causada pela precipitação;
molhamento de superfícies;
efeitos sobre materiais, caso existam futuramente.

Esses efeitos acompanham a intensidade atual da chuva e não dependem de outros subsistemas.

Noise

Após atingir os valores alvo, o PrecipitationSystem pode aplicar pequenas variações temporais.

Exemplos:

oscilações na intensidade;
pequenas mudanças na emissão do VFX;
leves alterações no volume do áudio.

O objetivo é evitar uma precipitação completamente constante.

O algoritmo utilizado é responsabilidade exclusiva do PrecipitationSystem.

Sobrescritas

O PrecipitationSystem pode substituir temporariamente qualquer parâmetro proveniente do WeatherPreset.

Exemplos:

aumentar a chuva durante uma missão;
interromper a precipitação temporariamente;
intensificar a chuva em uma região específica.

Essas alterações não modificam o preset original e permanecem restritas ao PrecipitationSystem.

Fluxo de Funcionamento
WeatherSystem

        │

        ▼

WeatherPreset

        │

        ▼

PrecipitationSystem

        │

        ├── Lê RainSettings
        ├── Define valores alvo
        ├── Interpola parâmetros
        ├── Atualiza VFX Graph
        ├── Atualiza áudio
        ├── Atualiza ripples
        ├── Atualiza efeitos de câmera
        └── Aplica noise
Evolução

O sistema deve permitir a adição de novos efeitos relacionados à precipitação sem alterar sua interface pública.

Exemplos futuros:

neve;
granizo;
tempestades de areia;
interação com gameplay;
acúmulo de água;
formação de poças.

Sempre que o efeito representar uma consequência direta da precipitação, sua implementação deve permanecer dentro do PrecipitationSystem.

Princípios da Arquitetura
O PrecipitationSystem é responsável exclusivamente pela precipitação e seus efeitos.
A representação visual da chuva é implementada com VFX Graph.
O sistema não controla nem interpreta outros aspectos do clima.
Cada mudança de clima resulta apenas na atualização dos valores alvo.
A transição, o noise e as sobrescritas são responsabilidades internas do PrecipitationSystem.
O sistema deve permanecer desacoplado dos demais subsistemas, comunicando-se apenas através do WeatherPreset e das notificações emitidas pelo WeatherSystem.