Especificação — TimeManager Astronômico (Unity 6.5)
Objetivo

Criar um sistema central de gerenciamento de tempo do mundo capaz de simular um ciclo anual completo, fornecendo dados astronômicos simplificados para outros sistemas do jogo.

O TimeManager não controla diretamente iluminação, céu, sol ou lua. Ele apenas calcula e expõe o estado temporal e astronômico atual.
1. Responsabilidades

O TimeManager deve:

    controlar passagem de tempo;
    manter data do mundo;
    controlar dia do ano;
    controlar hora do dia;
    simular ciclo anual;
    simular ciclo solar;
    simular ciclo lunar;
    calcular posição aparente do Sol;
    calcular posição aparente da Lua;
    informar fase lunar;
    informar visibilidade aproximada da Lua;
    emitir eventos de mudança temporal.

Não deve:

    alterar Directional Light;
    alterar Sky and Fog Volume;
    alterar materiais;
    controlar iluminação ambiente;
    controlar clima.

2. Estrutura principal

Classe:

TimeManager : MonoBehaviour

Singleton opcional:

TimeManager.Instance

3. Dados públicos
Tempo
Dia do ano

int DayOfYear

Range:

0 - 364

Representa:

0 = primeiro dia do ano
364 = último dia

Leitura e escrita.
Hora do dia

float TimeOfDay

Range:

0.0 - 24.0

Exemplo:

0.0  = meia noite
6.0  = manhã
12.0 = meio dia
18.0 = tarde

Apesar do Inspector mostrar valores inteiros, internamente usar float para permitir aceleração suave.

Leitura e escrita.
Velocidade do tempo

float TimeScale

Exemplo:

1     = tempo real
60    = 1 minuto real representa 1 hora
1440  = 1 dia por minuto

4. Configurações
Ano

int DaysPerYear = 365

Não considerar ano bissexto.
Hemisfério

Enum:

enum Hemisphere
{
    North,
    South
}

Inspector:

Dropdown.
Latitude

float Latitude

Range:

-90 até 90

Mesmo com hemisfério fixo, manter latitude pois o jogador poderá navegar.

Regra:

Hemisfério define o sinal:
Norte = latitude positiva
Sul = latitude negativa

Exemplo:

Latitude 20 + Norte = +20°
Latitude 20 + Sul = -20°

5. Ciclo solar

O sistema deve calcular:
Declinação solar

Ângulo que representa a inclinação do Sol durante o ano.

Usar aproximação:

declinação =
23.44 * sin((360/365) * (284 + dia))

Resultado:

-23.44° inverno
0° equinócio
+23.44° verão

Altura solar

Calcular:

solarAltitude

Range:

-90 até +90

Interpretar:

>0  Sol acima do horizonte
<0  Sol abaixo do horizonte

Azimute solar

Calcular:

solarAzimuth

Range:

0-360 graus

Convenção:

90   Norte
0  Leste
270 Sul
180 Oeste

Dados expostos

float SunAltitude
float SunAzimuth
Vector3 SunDirection
bool IsSunVisible

6. Duração do dia

O TimeManager deve calcular:

DayLength
NightLength

em horas.

Baseado em:

    latitude;
    declinação solar;
    hemisfério.

Exemplo:

Equador:

dia ≈ 12h
noite ≈ 12h

Latitude alta:

verão → dias maiores
inverno → noites maiores

7. Ciclo lunar

Usar ciclo sinódico:

29.53059 dias

Idade da Lua

Propriedade:

float MoonAge

Range:

0 - 29.53

Fase lunar

Enum:

enum MoonPhase
{
    NewMoon,
    WaxingCrescent,
    FirstQuarter,
    WaxingGibbous,
    FullMoon,
    WaningGibbous,
    LastQuarter,
    WaningCrescent
}

Iluminação da lua

Valor:

float MoonIllumination

Range:

0-1

Exemplo:

0    Lua nova
0.5  Quarto
1    Lua cheia

8. Posição da Lua

Modelo simplificado:

    órbita circular;
    plano da eclíptica;
    sem inclinação orbital real.

Calcular:

MoonAltitude
MoonAzimuth

Assim será possível determinar:

    Lua visível durante o dia;
    Lua aparecendo à noite;
    Lua cheia nascendo próximo ao pôr do sol;
    Lua nova próxima ao Sol.

Dados expostos:

float MoonAltitude
float MoonAzimuth
Vector3 MoonDirection

bool IsMoonVisible

9. Relação Sol/Lua

Calcular separação angular:

float SunMoonAngle

Range:

0-180 graus

Interpretação:

0°
Lua próxima ao Sol
Lua nova

90°
quartos

180°
Lua cheia

Isso permite outros sistemas saberem:

    iluminação lunar;
    posição relativa;
    possibilidade de eclipse futuramente.

10. Eventos

Criar eventos C#:

OnMinuteChanged

Executado quando muda minuto.

OnHourChanged

Executado quando muda hora.

OnDayChanged

Executado quando muda dia.

OnYearChanged

Executado ao completar ciclo anual.

OnMoonPhaseChanged

Quando muda fase lunar.
11. Inspector

Campos:

Day Of Year [Slider]
0 ---------------- 364


Time Of Day [Slider]
0 ---------------- 23


Hemisphere [Dropdown]

Latitude

Time Scale

12. Atualização interna

Fluxo:

Update()

    deltaTime *= TimeScale

    atualizar TimeOfDay

    se TimeOfDay >= 24:
        TimeOfDay -= 24
        DayOfYear++

    se DayOfYear >=365:
        DayOfYear =0


    RecalculateAstronomy()

    InvokeEvents()

13. Interface para outros sistemas

Outros sistemas devem conseguir fazer:

Exemplo:

var time = TimeManager.Instance;

float sunHeight = time.SunAltitude;

MoonPhase phase = time.CurrentMoonPhase;

bool moonVisible = time.IsMoonVisible;

14. Salvamento

O TimeManager deve possuir métodos:

SaveState()

LoadState()

Não definir implementação nesta versão.

Estado mínimo salvo:

DayOfYear
TimeOfDay
Latitude
Hemisphere

Observação de implementação

Como o jogo é exclusivamente em um veleiro, a maior parte do valor vem de consistência visual:

    Lua cheia aparecer no lado oposto ao Sol;
    Lua nova acompanhar o Sol;
    duração do dia variar com latitude;
    estações inverterem no hemisfério sul.

Não é necessário implementar astronomia de alta precisão. Um modelo simplificado como este será suficiente e mais estável para gameplay.