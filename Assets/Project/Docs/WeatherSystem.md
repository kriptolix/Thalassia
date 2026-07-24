Especificação de Arquitetura — WeatherSystem
Objetivo

O WeatherSystem é o orquestrador central do sistema climático. Ele não aplica efeitos visuais ou físicos diretamente, sendo responsável apenas por:

Gerenciar presets de clima.
Controlar a progressão lógica entre estados climáticos.
Sinalizar mudanças de clima para os subsistemas.
Permitir que o clima seja controlado automaticamente ou forçado por eventos do jogo.

Toda a execução do clima é responsabilidade dos subsistemas especializados.

Responsabilidades
Gerenciamento de Presets

O sistema utiliza WeatherPreset (ScriptableObject) para representar um estado climático completo.

Cada preset contém os valores de referência para todos os subsistemas existentes.

Exemplo:

Wind
Clouds
Day/Night
HDRP Volumes
Water
Fog
Rain
etc.

O WeatherSystem não interpreta esses valores.

Ele apenas mantém uma referência ao preset ativo.

Progressão Climática

O WeatherSystem mantém uma tabela de progressão entre presets.

Exemplo:

Clear
 ├── PartlyCloudy
 └── Fog

PartlyCloudy
 ├── Clear
 ├── Overcast
 └── Rain

Rain
 ├── Overcast
 └── Storm

Storm
 └── Rain

Essa tabela determina quais estados podem suceder o estado atual.

As transições são escolhidas aleatoriamente apenas entre os estados permitidos.

Isso evita mudanças ilógicas como:

Clear -> Storm

caso essa transição não exista.

Controle Automático

No modo automático o sistema:

mantém um timer interno;
ao término do tempo escolhe um novo preset válido;
altera o preset ativo;
notifica todos os subsistemas.

O sistema não controla a duração das transições.

Apenas determina quando ocorre uma mudança.

Controle Forçado

O sistema permite interromper a progressão automática.

Nesse modo um sistema externo pode definir explicitamente o clima.

Exemplos:

missão inicia tempestade;
região força neblina;
cutscene fixa determinado clima.

Enquanto o modo forçado estiver ativo:

nenhuma progressão automática ocorre;
o preset permanece fixo.

Ao sair do modo forçado o sistema retorna ao funcionamento automático utilizando o preset atual como ponto de partida.

Comunicação com Subsistemas

O WeatherSystem conhece apenas a existência dos subsistemas.

Não conhece sua implementação interna.

Quando ocorre uma mudança de clima:

Weather mudou para Storm

ele apenas emite uma notificação.

Cada subsistema então:

recebe a notificação;
consulta o WeatherPreset ativo;
lê apenas os dados que lhe pertencem;
inicia sua própria transição.
Responsabilidade dos Subsistemas

Cada subsistema é completamente independente.

Ele decide:

como interpretar seus dados;
como interpolar os valores;
quanto tempo dura sua transição;
como aplicar noise;
como atualizar HDRP, Water System ou qualquer outro componente.

O WeatherSystem não possui conhecimento desses detalhes.

Sobrescritas Locais

Os valores do WeatherPreset representam apenas o estado alvo.

Um subsistema pode sobrescrever qualquer parâmetro internamente quando necessário.

Exemplo:

Preset Storm
    Wind Speed = 25 m/s

O WindSystem pode decidir utilizar:

Wind Speed = 35 m/s

para um evento específico sem modificar o preset original.

Essas sobrescritas pertencem exclusivamente ao subsistema.

Noise

O WeatherSystem não gera variações.

Ele fornece apenas os valores alvo definidos no preset.

Cada subsistema aplica seu próprio algoritmo de noise para produzir pequenas oscilações ao redor do valor de referência.

Isso permite que diferentes propriedades tenham comportamentos distintos sem acoplamento ao sistema central.

Fluxo de Funcionamento
WeatherSystem

        │
        │ escolhe próximo preset
        ▼

WeatherPreset

        │
        │ evento "WeatherChanged"
        ▼

────────────────────────────────────

WindSystem
    lê WindSettings
    inicia transição
    aplica noise

CloudSystem
    lê CloudSettings
    inicia transição
    aplica noise

DayNightSystem
    lê LightingSettings
    inicia transição

WaterSystem
    lê WaterSettings
    inicia transição

FogSystem
    lê FogSettings
    inicia transição

...
Princípios da Arquitetura
O WeatherSystem é um orquestrador, não um executor.
Subsistemas são independentes e desacoplados.
O WeatherSystem nunca manipula diretamente componentes da Unity ou HDRP.
Cada subsistema é responsável pela lógica de interpolação, aplicação e variação dos seus próprios parâmetros.
WeatherPreset representa sempre um estado climático completo e de referência.
A progressão entre estados é controlada por uma tabela explícita de transições válidas.
O sistema suporta tanto progressão automática quanto controle forçado por eventos de gameplay.
Novos subsistemas podem ser adicionados sem necessidade de modificar a lógica central do WeatherSystem, desde que respondam ao evento de mudança de clima e saibam interpretar sua seção do WeatherPreset.