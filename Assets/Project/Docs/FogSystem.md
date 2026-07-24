Especificação de Arquitetura — FogSystem
Objetivo

O FogSystem é o subsistema responsável pelo gerenciamento de neblina e efeitos atmosféricos relacionados à redução de visibilidade.

Sua função é controlar tanto a atmosfera global do mundo quanto fenômenos localizados de neblina através de zonas.

O sistema combina:

parâmetros climáticos vindos do WeatherPreset;
zonas de neblina posicionadas no mundo;
eventos dinâmicos de atmosfera.

O FogSystem não controla regras de gameplay relacionadas à visibilidade. Ele apenas fornece a representação visual e atmosférica.

Responsabilidades

O FogSystem é responsável por:

Controlar o fog global do HDRP Volume.
Gerenciar neblina volumétrica.
Controlar densidade, cor e altura da neblina.
Gerenciar zonas localizadas de neblina.
Fazer transições entre estados atmosféricos.
Combinar múltiplas influências de neblina.
Aplicar variações naturais através de noise.

O FogSystem não é responsável por:

Criar eventos climáticos.
Decidir quando uma neblina aparece.
Alterar lógica de navegação.
Esconder objetos manualmente.
Controlar chuva ou nuvens.
Estrutura Geral

O sistema possui duas camadas:

FogSystem

    ├── Global Fog
    │
    └── Fog Zones
Global Fog

O fog global representa as condições atmosféricas gerais do mundo.

Seus valores vêm do WeatherPreset.

Exemplo:

WeatherPreset

    FogSettings

        Density

        Color

        Height

        Volumetric Intensity

        Distance Falloff

Exemplo:

Overcast

Fog:
    Density = 0.15
    Color = Gray
    Height = 200m

O FogSystem aplica esses valores no HDRP Volume.

Fog Zones

Fog Zones representam fenômenos localizados de neblina.

Exemplos:

bancos de neblina no oceano;
regiões costeiras;
neblina matinal em determinadas áreas;
eventos atmosféricos especiais.

Cada zona possui seus próprios parâmetros.

Exemplo:

FogZone

    Position

    Radius

    Density

    Color

    Height

    Falloff

    Priority
Influência das Zonas

Quando a câmera ou o barco entra em uma Fog Zone, o FogSystem calcula a influência dessa zona.

Exemplo:

Fog Final State =

    Global Fog

    +

    Active Fog Zones

A influência depende de:

distância até a zona;
raio;
falloff;
prioridade.
Múltiplas Zonas

O sistema deve suportar múltiplas Fog Zones simultâneas.

Exemplo:

Zona A

Density = 0.3


Zona B

Density = 0.5

O FogSystem deve combinar os valores usando regras definidas.

Possíveis estratégias:

maior intensidade vence;
soma limitada;
prioridade.

A regra deve ser configurável.

Integração com WeatherPreset

O WeatherPreset define apenas o estado atmosférico base.

Exemplo:

Storm

FogSettings

    Density = 0.25

Ele não define zonas.

As zonas pertencem ao mundo.

Isso permite:

o mesmo clima em mapas diferentes;
diferentes bancos de neblina no mesmo clima;
eventos locais independentes do clima global.
Transições

Quando o clima muda:

WeatherSystem sinaliza alteração.
FogSystem recebe o novo preset.
Atualiza os valores alvo globais.
Faz interpolação gradual.

A transição global pertence ao FogSystem.

Exemplo:

Clear

Fog Density:
0.0


↓

Fog Density:
0.25
Noise

O FogSystem pode aplicar variações naturais.

Exemplos:

pequenas oscilações na densidade;
variações de intensidade volumétrica;
mudanças lentas na visibilidade.

O objetivo é evitar uma neblina completamente estática.

Fog Zones Dinâmicas

Além das zonas fixas criadas pelo designer, o sistema pode permitir criação dinâmica.

Exemplos:

banco de neblina gerado por tempestade;
evento de gameplay;
mudança de região.

Uma zona dinâmica deve possuir o mesmo comportamento de uma zona fixa.

A origem da criação não deve importar para o FogSystem.

Fluxo de Funcionamento
WeatherSystem

        │

        ▼

WeatherPreset

        │

        ▼

FogSystem

        │

        ├── Atualiza HDRP Global Fog
        │
        ├── Avalia Fog Zones próximas
        │
        ├── Combina influências
        │
        ├── Aplica transição
        │
        └── Aplica noise
Extensibilidade

O sistema deve permitir futuros efeitos atmosféricos relacionados:

poeira;
fumaça;
névoa marítima intensa;
partículas atmosféricas;
efeitos de horizonte.

Esses efeitos devem ser adicionados como módulos internos sem alterar a comunicação com o WeatherSystem.

Princípios da Arquitetura
O FogSystem controla representação atmosférica, não lógica de gameplay.
O clima define a condição global.
O mundo define fenômenos locais.
Fog Zones são independentes do WeatherPreset.
O fog HDRP Volume é encapsulado pelo FogSystem.
Múltiplas fontes de neblina podem coexistir.
O sistema suporta tanto condições climáticas permanentes quanto eventos atmosféricos localizados.