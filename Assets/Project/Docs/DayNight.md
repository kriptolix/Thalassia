Especificação — DayNightCycle System (Unity 6.5)
Objetivo

Criar um sistema responsável pela representação visual do ciclo dia/noite, utilizando os dados astronômicos fornecidos pelo TimeManager.

O sistema deve:

    posicionar Sol e Lua conforme dados astronômicos;
    controlar intensidade luminosa do Sol e Lua;
    criar transições visuais entre:
        Aurora;
        Dia;
        Crepúsculo;
        Noite;
    permitir duração artística dos períodos;
    controlar fases da Lua via material/shader;
    fornecer parâmetros visuais para outros sistemas.

O DayNightCycle não controla passagem de tempo.

Fonte única de tempo:

TimeManager
        |
        |
        v
DayNightCycle
        |
        +-- Sun Light
        +-- Moon Light
        +-- Moon Material
        +-- Sky visual

1. Classe principal

DayNightCycle : MonoBehaviour

Responsabilidade:

    consultar TimeManager;
    atualizar elementos visuais;
    interpolar estados.

2. Referências no Inspector
TimeManager

[SerializeField]
TimeManager timeManager;

Luz solar

[SerializeField]
Light sunLight;

Tipo esperado:

Directional Light

Usado para:

    direção do Sol;
    intensidade;
    cor.

Luz lunar

[SerializeField]
Light moonLight;

Tipo esperado:

Directional Light

Usado para:

    direção da Lua;
    intensidade;
    cor.

Material da Lua

[SerializeField]
Renderer moonRenderer;

ou:

[SerializeField]
Material moonMaterial;

O material deve possuir:

_MoonPhase

Range:

0-1

Representação:

0   Lua Nova
0.5 Quarto
1   Lua Cheia

3. Configuração dos períodos

O ciclo visual possui quatro estados:

NOITE
 |
Aurora
 |
DIA
 |
Crepúsculo
 |
Noite

Cada período possui duração artística.
Configuração

[Serializable]
public class DayPeriod
{
    public float durationMinutes;
    public AnimationCurve intensityCurve;
}

Campos:

public DayPeriod dawn;
public DayPeriod day;
public DayPeriod dusk;
public DayPeriod night;

Valores padrão:
Aurora

30 minutos

Dia

120 minutos

Crepúsculo

45 minutos

Noite

90 minutos

Esses valores são ajustáveis.
4. Ponderação pelo TimeManager

O tempo visual é calculado proporcionalmente ao tempo astronômico.

Exemplo:

TimeManager:

Dia real:
16 horas

Noite real:
8 horas

DayNight:

Aurora:
30 min

Dia:
120 min

Crepúsculo:
45 min

Noite:
90 min

Distribuição:

Dia:

Aurora + Dia + Crepúsculo

é escalado para ocupar as 16 horas solares.

Noite:

Noite

é escalada para ocupar as 8 horas noturnas.
5. Estados do ciclo

Enum:

public enum DayState
{
    Dawn,
    Day,
    Dusk,
    Night
}

Propriedade:

public DayState CurrentState;

6. Atualização principal

Executado:

Update()

Fluxo:

Obter dados TimeManager

        |
        v

Atualizar posição Sol

        |
        v

Atualizar posição Lua

        |
        v

Determinar período visual

        |
        v

Aplicar curvas de intensidade

        |
        v

Atualizar iluminação

7. Movimento do Sol

O DayNight não calcula astronomia.

Recebe:

timeManager.SunDirection

Aplica:

sunLight.transform.rotation =
Quaternion.LookRotation(
    timeManager.SunDirection
);

8. Movimento da Lua

Recebe:

timeManager.MoonDirection

Aplica:

moonLight.transform.rotation =
Quaternion.LookRotation(
    timeManager.MoonDirection
);

9. Controle de intensidade solar

A intensidade deve variar conforme estado.

Parâmetros:

float maxSunIntensity;
float minSunIntensity;

Exemplo:

Noite:
0

Aurora:
0 → 1

Dia:
1

Crepúsculo:
1 → 0

Noite:
0

A transição usa:

AnimationCurve

10. Controle da Lua

Parâmetros:

float maxMoonIntensity;
float minMoonIntensity;

Comportamento:

Lua abaixo do horizonte:

0


Lua acima do horizonte:

baseado na fase lunar


Durante dia:

redução pela luminosidade solar

Visibilidade lunar

Não desligar a Lua.

Usar fator:

MoonVisibility

Calculado:

MoonVisibility =
altura acima horizonte
*
fase lunar
*
fator luminosidade céu

Resultado:

    Lua cheia pode aparecer de dia;
    Lua nova praticamente desaparece;
    Lua crescente pode ser visível no fim da tarde.

11. Fases da Lua

O DayNight recebe:

timeManager.MoonPhase

e:

timeManager.MoonIllumination

Converte para shader:

moonMaterial.SetFloat(
    "_MoonPhase",
    value
);

12. Curvas padrão

Criar curvas padrão editáveis.
Aurora

0%  noite
50% luz baixa
100% dia

Curva:

Ease In

Dia

100%

Crepúsculo

100%
50% luz quente
0%

Noite

0%

13. API pública

Outros sistemas podem consultar:

public float SunIntensity;

public float MoonIntensity;

public float AmbientNightFactor;

14. Eventos

Expor:

public UnityEvent OnDawnStarted;

public UnityEvent OnDayStarted;

public UnityEvent OnDuskStarted;

public UnityEvent OnNightStarted;

15. Integração futura com Sky/Fog

O DayNight não controla o Volume.

Porém deve expor:

public float SolarIntensity01;

public float NightFactor;

public float TwilightFactor;

Outro sistema poderá usar:

    exposição;
    cor do céu;
    neblina;
    atmosfera.

16. Configuração inicial recomendada

Inspector:

Sun Light
    |
    Directional Light


Moon Light
    |
    Directional Light


Moon Renderer
    |
    Material com _MoonPhase


Periods:

Aurora:
30 min

Dia:
120 min

Crepúsculo:
45 min

Noite:
90 min


Sun:

Max intensity:
100000 lux (HDRP)

Moon:

Max intensity:
0.2 - 1 lux equivalente

17. Princípio arquitetural

Responsabilidades finais:

TimeManager
--------------
Tempo absoluto
Dia do ano
Hora
Latitude
Sol
Lua
Fases


DayNightCycle
--------------
Visualização
Luzes
Transições
Curvas
Intensidade


LightingEnvironment
--------------
Sky
Fog
Ambient Light
Exposição
Cores

Essa separação permite evoluir o sistema sem criar dependências entre simulação astronômica, iluminação e direção artística.
