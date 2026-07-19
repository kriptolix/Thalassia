using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sail System — dono do estado das velas E do cálculo de força que elas
/// produzem, dado vento aparente e heading do barco (recebidos como
/// PARÂMETROS via CalculateForce, nunca como referência guardada). Não
/// guarda referência a WindSystem, Rigidbody, ou ShipMovementSystem.
///
/// ABERTURA: 3 estados discretos (Fechada / Meia Vela / Vela Cheia),
/// controlados por ordens (toque/segurar/agrupamento de comandos - ver
/// ProcessDiscreteInput). Fechada = 0% de área efetiva, o que já zera a
/// força de propulsão sozinho (ver CalculateForce) - é a ÚNICA forma de
/// parar de verdade.
///
/// TRIM: revisado para MODOS DE NAVEGAÇÃO (Contra o Vento, Bolina, Través,
/// Largo, Popa) em vez de ângulo em passos de grau. O jogador escolhe a
/// INTENÇÃO de navegação (mesma mecânica de toque/segurar/agrupamento); a
/// tripulação ajusta o ângulo continuamente dentro do modo para acompanhar
/// o vento aparente, sem intervenção do jogador, enquanto o vento for
/// compatível com o modo escolhido. A eficiência cai gradualmente conforme
/// o vento aparente sai da faixa ideal do modo selecionado.
///
/// PROPULSÃO RESIDUAL: a eficiência do trim nunca cai a zero por causa do
/// ângulo (piso mínimo, ver minResidualEfficiency) - só a ÁREA (abertura)
/// pode zerar a força de fato, evitando estagnação completa por pequenas
/// variações de eficiência sem exigir um caso especial no código: com vela
/// fechada, EffectiveSailArea já é 0, e a força é proporcional à área.
///
/// Outros sistemas devem referenciar esta instância manualmente via campo
/// [SerializeField] arrastado no Inspector (mesmo padrão do WindSystem).
/// </summary>
[DisallowMultipleComponent]
public class SailSystem : MonoBehaviour
{
    [Header("Ângulo Máximo Visual da Vela")]
    [Tooltip("Limite máximo de rotação VISUAL da vela em relação ao casco, em graus. Usado só para a animação/orientação do modelo 3D - não afeta mais o cálculo de força (ver header Trim/Modos).")]
    [SerializeField] private float sailAngleLimit = 90f;
    [Tooltip("Graus por segundo da transição VISUAL do ângulo da vela ao acompanhar o vento dentro do modo selecionado.")]
    [SerializeField] private float sailAngleSpeed = 45f;

    [Header("Abertura (Fechada / Meia Vela / Vela Cheia)")]
    [Tooltip("Unidades de abertura (0-1) por segundo, usadas na transição suave até o nível de abertura alvo.")]
    [SerializeField] private float sailOpenSpeed = 0.5f;
    [Tooltip("Nível inicial de abertura (0 = fechada, 1 = meia vela, 2 = vela cheia). Padrão: vela cheia, conforme a spec ('será a configuração padrão na maior parte da navegação').")]
    [SerializeField] private int initialSailOpenLevel = 2;

    [Header("Área")]
    [Tooltip("Área total efetiva das velas quando totalmente abertas (BaseSailArea).")]
    [SerializeField] private float baseSailArea = 100f;

    [Header("Aerodinâmica")]
    [Tooltip("Coeficiente de conversão vento->força. NÃO tem origem física (a vela real depende de forma/camber/ângulo de ataque, que este sistema não modela) - é um dial de CALIBRAÇÃO POR SENSAÇÃO. Ajuste ouvindo/sentindo o jogo, não tentando 'derivar' o valor.")]
    [SerializeField] private float sailForceCoefficient = 8f;
    [Tooltip("Força máxima que as velas podem produzir (cap final, em Newtons).")]
    [SerializeField] private float maxSailForce = 6000f;
    [Tooltip("Eficiência mínima garantida pelo TRIM, independente do ângulo do vento, para evitar estagnação completa por pequenas variações de eficiência. A força ainda cai a zero se a vela estiver fechada (área efetiva = 0) - ver header da classe.")]
    [Range(0f, 1f)]
    [SerializeField] private float minResidualEfficiency = 0.15f;

    [Header("Modos de Navegação (Trim)")]
    [Tooltip("5 modos ordenados de 'Contra o Vento' (índice 0, mais próximo do vento) a 'Popa' (índice 4, vento de popa). O ângulo relevante é o ÂNGULO ENTRE O VENTO APARENTE E A PROA do barco (0° = vento vindo direto da proa, 180° = vento vindo direto da popa) - independente do ângulo da própria vela.")]
    [SerializeField]
    private SailModeRange[] sailModes = new SailModeRange[]
    {
        new SailModeRange { modeName = "Contra",         idealMin = 25f,  idealMax = 40f,  acceptMin = 20f,  acceptMax = 50f  },
        new SailModeRange { modeName = "Bolina",         idealMin = 40f,  idealMax = 65f,  acceptMin = 30f,  acceptMax = 75f  },
        new SailModeRange { modeName = "Través",         idealMin = 70f,  idealMax = 100f, acceptMin = 55f,  acceptMax = 115f },
        new SailModeRange { modeName = "Largo",          idealMin = 110f, idealMax = 145f, acceptMin = 95f,  acceptMax = 160f },
        new SailModeRange { modeName = "Popa",           idealMin = 155f, idealMax = 180f, acceptMin = 140f, acceptMax = 180f },
    };
    [Tooltip("Índice inicial do modo de navegação (0 = Contra o Vento .. sailModes.Length-1 = Popa).")]
    [SerializeField] private int initialSailModeIndex = 2;
    [Tooltip("Eficiência dentro da faixa ACEITÁVEL, fora da faixa ideal (ex.: 0.6 = 60%, conforme a spec).")]
    [Range(0f, 1f)]
    [SerializeField] private float acceptableRangeEfficiency = 0.6f;
    [Tooltip("ASSUMIDO: a spec pede 'queda acentuada' fora da faixa aceitável, mas não define a largura exata da transição. Graus além da borda da faixa aceitável até a eficiência (do trim) chegar a zero.")]
    [SerializeField] private float sharpFalloffRangeDegrees = 15f;
    [Tooltip("Sobreposição mínima desejada (graus) entre as faixas ACEITÁVEIS de modos vizinhos - dentro dela, os dois modos adjacentes ficam aceitáveis, então trocar de modo perto do limite não derruba a eficiência de repente. Só usada para avisar no Console (LogModeOverlaps) se você editar sailModes e a sobreposição ficar pequena demais - não afeta o cálculo de eficiência em si.")]
    [SerializeField] private float minDesiredModeOverlapDegrees = 10f;

    [Header("Suavização da Classificação de Modo")]
    [Tooltip("Constante de tempo (segundos) de um filtro passa-baixa aplicado ao ângulo vento-proa ANTES de decidir a eficiência do trim (ideal/aceitável/fora). Evita que variações pequenas e rápidas (rajadas de vento, jitter de guinada) façam o modo 'piscar' entre ideal/aceitável/fora quadro a quadro. NÃO afeta o ângulo bruto reportado no HUD (windAngleFromBow) nem o ângulo VISUAL da vela - só a decisão de eficiência do trim. Curvas de verdade (que levam vários segundos) ainda são detectadas normalmente.")]
    [SerializeField] private float modeClassificationSmoothingSeconds = 1.5f;

    [Header("Diferenciação entre tipos de vela (progressão futura do jogador)")]
    [Tooltip("Atraso/lentidão do mecanismo de ajuste: quanto maior, mais lenta fica a transição de abertura. 0 = sem penalidade.")]
    [SerializeField] private float adjustmentDelay = 0f;
    [Tooltip("Multiplicador de eficiência da vela, usado no próprio cálculo de força deste componente.")]
    [SerializeField] private float efficiencyMultiplier = 1f;

    [Header("Trim Lateral (lado da vela)")]
    [Tooltip("Lado inicial do trim (+1 ou -1), usado só até o primeiro FixedUpdate do ShipMovementSystem chamar SetTrimSideFromWind - depois disso o lado passa a ser recalculado automaticamente a partir do vento, a cada frame (a vela troca de lado sozinha ao cruzar o vento, como um veleiro real).")]
    [SerializeField] private int initialTrimSide = 1;

    [Header("Agrupamento de Comandos (Toque / Segurar)")]
    [Tooltip("Tempo (segundos) que o sistema aguarda a partir do PRIMEIRO toque antes de confirmar o pedido. Toques adicionais dentro dessa janela se acumulam no mesmo pedido; a janela não reinicia a cada toque novo.")]
    [SerializeField] private float commandGroupingWindowSeconds = 0.55f;
    [Tooltip("Tempo (segundos) que o botão precisa ficar pressionado continuamente, sem soltar, para virar um 'comando prolongado' (vai direto ao extremo).")]
    [SerializeField] private float holdThresholdSeconds = 0.6f;

    [Header("Eventos de Ordem (para áudio/VFX)")]
    [Tooltip("Disparado quando um pedido de abertura é confirmado. Parâmetros: direção (+1 abrir, -1 fechar), tocarFalaCapitao (30% de chance - ver spec: 'Em 30% das vezes o capitão fala').")]
    [SerializeField] private SailOrderUnityEvent onSailOpenOrderUnityEvent;
    [Tooltip("Disparado quando um pedido de modo (trim) é confirmado. Parâmetros: direção (+1 folgar/em direção à popa, -1 caçar/em direção ao vento), tocarFalaCapitao.")]
    [SerializeField] private SailOrderUnityEvent onSailModeOrderUnityEvent;

    /// <summary>Direção confirmada de um pedido de abertura (+1 abrir, -1 fechar) e se deve tocar a fala do capitão (30% de chance).</summary>
    public event System.Action<int, bool> OnSailOpenOrderIssued;
    /// <summary>Direção confirmada de um pedido de modo (+1 folgar, -1 caçar) e se deve tocar a fala do capitão (30% de chance).</summary>
    public event System.Action<int, bool> OnSailModeOrderIssued;

    [System.Serializable]
    public class SailOrderUnityEvent : UnityEvent<int, bool> { }

    [System.Serializable]
    public class SailModeRange
    {
        public string modeName;
        [Tooltip("Faixa ideal - eficiência 100%.")]
        public float idealMin;
        public float idealMax;
        [Tooltip("Faixa aceitável (deve conter a faixa ideal) - eficiência = acceptableRangeEfficiency.")]
        public float acceptMin;
        public float acceptMax;
    }

    // --- Input recebido externamente (mesma interface -1/0/+1, "toque/segurar") ---
    private float _sailOpenInput;
    private float _sailModeInput;

    // --- Estado interno das ordens discretas ---
    private DiscreteCommandState _openState = new DiscreteCommandState();
    private DiscreteCommandState _modeState = new DiscreteCommandState();
    private int _trimSide = 1;

    // Filtro passa-baixa do ângulo vento-proa, usado só pra decidir a
    // eficiência do trim (ver header "Suavização da Classificação de Modo").
    private float _smoothedWindAngleFromBow;
    private bool _smoothedWindAngleInitialized = false;

    // --- Estado atual (somente leitura para outros sistemas) ---
    /// <summary>Ângulo VISUAL atual da vela em relação ao casco (para animação do modelo 3D). Não é mais usado no cálculo de força - ver CalculateForce.</summary>
    public float CurrentSailAngle { get; private set; }
    public float CurrentSailOpenAmount { get; private set; }
    public float EffectiveSailArea { get; private set; }

    /// <summary>Nível-alvo de abertura confirmado (0 = fechada, 1 = meia vela, 2 = vela cheia).</summary>
    public int TargetSailOpenLevel => _openState.targetLevel;
    /// <summary>Índice do modo de navegação atualmente selecionado (0 = Contra o Vento .. sailModes.Length-1 = Popa). Escolha discreta - não é interpolada, muda no instante em que a ordem é confirmada.</summary>
    public int CurrentSailModeIndex => _modeState.targetLevel;
    /// <summary>Nome do modo de navegação atual (para HUD/depuração).</summary>
    public string CurrentSailModeName => GetModeName(CurrentSailModeIndex);
    /// <summary>Lado atual do trim (+1 ou -1).</summary>
    public int TrimSide => _trimSide;

    /// <summary>
    /// Define o lado do trim a partir do vento (chamado pelo ShipMovementSystem
    /// a cada FixedUpdate, ANTES de CalculateForce - o SailSystem propositalmente
    /// não conhece vento, ver docstring da classe). +1 = estibordo, -1 = bombordo.
    /// </summary>
    public void SetTrimSideFromWind(int side)
    {
        _trimSide = side >= 0 ? 1 : -1;
    }

    public float SailAngleLimit => sailAngleLimit;
    public float EfficiencyMultiplier => efficiencyMultiplier;

    /// <summary>
    /// Resultado completo do cálculo de força da vela num instante - tudo
    /// que o ShipMovementSystem (ou HUD/debug) precisa para aplicar física
    /// ou exibir diagnóstico, sem precisar recalcular nada.
    /// </summary>
    public struct SailForceResult
    {
        /// <summary>Ângulo entre o vento aparente e a PROA do barco (0°-180°, independente do trim). 0° = vento na proa, 180° = vento na popa.</summary>
        public float windAngleFromBow;
        public int sailModeIndex;
        public string sailModeName;
        public bool inIdealRange;
        public bool inAcceptableRange;
        /// <summary>Eficiência final do trim (já com o piso residual aplicado).</summary>
        public float trimEfficiency;
        public float sailForceMagnitude;
        public Vector3 thrustForce;
        public Vector3 lateralForce;
        public float thrustMagnitude;
        /// <summary>Componente lateral da força da vela (heel + deriva) - um valor só, usado tanto para o torque de heel quanto para a translação lateral (ver ShipMovementSystem).</summary>
        public float lateralMagnitude;
    }

    /// <summary>
    /// Calcula a força que a vela produz dado o vento aparente e o heading
    /// do barco - ambos recebidos como PARÂMETROS, nunca lidos de uma
    /// referência guardada. Também atualiza o ângulo VISUAL da vela (efeito
    /// colateral leve, cosmético, usando Time.fixedDeltaTime - espera-se
    /// que este método seja chamado uma vez por FixedUpdate).
    /// </summary>
    /// <param name="apparentWind">Vetor de vento aparente no plano XZ (windVector - velocidade do barco).</param>
    /// <param name="shipHeadingDeg">Heading do barco em graus (0-360, mesma convenção do WindSystem).</param>
    public SailForceResult CalculateForce(Vector3 apparentWind, float shipHeadingDeg)
    {
        SailForceResult result = default;
        result.sailModeIndex = CurrentSailModeIndex;
        result.sailModeName = CurrentSailModeName;

        float apparentWindSpeed = apparentWind.magnitude;
        float apparentWindDirection = NormalizeAngle(Mathf.Atan2(apparentWind.z, apparentWind.x) * Mathf.Rad2Deg);

        // Ângulo entre o vento aparente e a PROA do barco (0° = vento vindo
        // direto de frente, 180° = vento vindo direto de trás empurrando o
        // barco) - o "ponto de vela" clássico. Independente do ângulo da
        // vela em si: apparentWindDirection é para ONDE o vento sopra, então
        // a direção DE ONDE ele vem é o oposto (+180°); a diferença angular
        // entre essa direção-de-origem e o heading, então invertida (180 -
        // |delta|), dá 0 quando o vento vem de frente e 180 quando vem de trás.
        float windAngleFromBow = 180f - Mathf.Abs(Mathf.DeltaAngle(shipHeadingDeg, apparentWindDirection));
        result.windAngleFromBow = windAngleFromBow; // valor instantâneo, sem filtro - pro HUD/agulha de vento

        // Filtro passa-baixa exponencial, só pra decidir a eficiência do trim -
        // evita que rajadas/jitter de guinada façam o modo "piscar" entre
        // ideal/aceitável/fora quadro a quadro (ver header do campo). No
        // primeiro frame, inicializa direto no valor bruto (sem isso o barco
        // começaria classificado erradamente por alguns segundos até o filtro
        // convergir).
        if (!_smoothedWindAngleInitialized)
        {
            _smoothedWindAngleFromBow = windAngleFromBow;
            _smoothedWindAngleInitialized = true;
        }
        else
        {
            float smoothingAlpha = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.01f, modeClassificationSmoothingSeconds));
            _smoothedWindAngleFromBow = Mathf.Lerp(_smoothedWindAngleFromBow, windAngleFromBow, smoothingAlpha);
        }

        float rawTrimEfficiency = CalculateTrimEfficiency(_smoothedWindAngleFromBow, CurrentSailModeIndex, out bool inIdeal, out bool inAcceptable);
        result.inIdealRange = inIdeal;
        result.inAcceptableRange = inAcceptable;

        // Propulsão residual: o TRIM nunca derruba a eficiência a zero
        // sozinho. Quem realmente zera a força é a ÁREA EFETIVA (abertura) -
        // com a vela fechada, EffectiveSailArea=0 e a força cai a zero de
        // qualquer forma, sem precisar de um caso especial aqui.
        float trimEfficiency = Mathf.Max(rawTrimEfficiency, minResidualEfficiency);
        result.trimEfficiency = trimEfficiency;

        // Ângulo de referência do TRIM: a tripulação só acompanha o vento
        // continuamente ENQUANTO ele estiver dentro da faixa aceitável do
        // modo escolhido (ver docstring da classe). Fora dela, a vela fica
        // travada no limite que o modo permite - ela não "sabe" reagir a um
        // vento incompatível com o modo selecionado. Reaproveita os mesmos
        // acceptMin/acceptMax já usados em CalculateTrimEfficiency (mesma
        // fonte de verdade, nenhum número novo).
        //
        // CORRIGIDO: antes disso, o ângulo de trim usava windAngleFromBow
        // BRUTO sem clamp nenhum - então com vento de popa (180°) e modo
        // "Contra" selecionado, a vela calculava o mesmo ângulo de 90° que
        // teria em modo Popa de verdade (só a EFICIÊNCIA mudava, nunca a
        // DIREÇÃO da força) - por isso o empuxo ia quase 100% pra frente
        // mesmo com a vela fisicamente sheeted pra perto do centro, o que
        // não faz sentido (deveria estar mais de banda/lateral, ou
        // efetivamente gasguetada).
        SailModeRange currentModeRange = GetModeRange(CurrentSailModeIndex);
        float trimReferenceAngle = currentModeRange != null
            ? Mathf.Clamp(_smoothedWindAngleFromBow, currentModeRange.acceptMin, currentModeRange.acceptMax)
            : _smoothedWindAngleFromBow;

        // Ângulo VISUAL da vela dentro do modo: ASSUMIDO como metade do
        // ângulo de referência do trim acima (aproximação clássica - a vela
        // tende a "bisectar" o ângulo entre proa e vento, mas só dentro do
        // que o modo permite), clampado ao limite visual e usando o lado já
        // determinado externamente via SetTrimSideFromWind. A spec define a
        // EFICIÊNCIA por modo mas não uma fórmula exata para o ângulo visual.
        float targetVisualAngle = Mathf.Clamp(trimReferenceAngle * 0.5f, 0f, sailAngleLimit) * _trimSide;
        CurrentSailAngle = Mathf.MoveTowards(CurrentSailAngle, targetVisualAngle, sailAngleSpeed * Time.fixedDeltaTime);

        float rawForceMagnitude = apparentWindSpeed * EffectiveSailArea * trimEfficiency * efficiencyMultiplier * sailForceCoefficient;
        float sailForceMagnitude = Mathf.Min(rawForceMagnitude, maxSailForce);
        result.sailForceMagnitude = sailForceMagnitude;

        if (apparentWindSpeed <= 0.0001f || sailForceMagnitude <= 0f)
        {
            return result;
        }

        float headingRad = shipHeadingDeg * Mathf.Deg2Rad;
        Vector3 shipForward = new Vector3(Mathf.Cos(headingRad), 0f, Mathf.Sin(headingRad));
        Vector3 shipRight = new Vector3(Mathf.Cos(headingRad - Mathf.PI / 2f), 0f, Mathf.Sin(headingRad - Mathf.PI / 2f));

        // Decomposição thrust/lateral: SEM driftFactor (removido - era um
        // knob artificial e constante, desconectado do ponto de vela real).
        // Em vez disso, deriva a fração de cada componente do próprio ÂNGULO
        // DE TRIM da vela (o mesmo usado no ângulo visual, reaproveitado
        // aqui pro cálculo de força) - fisicamente: uma vela fechada quase
        // no eixo do casco (trim baixo, vento de popa) empurra quase 100%
        // pra frente e quase nada de lado; uma vela bem aberta (trim alto,
        // vento mais de través/bolina) gera menos empuxo direto e mais
        // força lateral (heel + deriva) - o mesmo padrão de um veleiro real
        // (upwind é sempre mais lento e mais adernado que um través).
        // Usa sin/cos do mesmo ângulo, então thrust²+lateral² = magnitude²
        // sempre - uma decomposição vetorial real, não dois números
        // independentes competindo pela mesma força.
        // Usa o MESMO ângulo de referência do trim (já travado à faixa do
        // modo selecionado acima) - garante que a direção da força seja
        // consistente com o ângulo visual da vela: uma vela mal trimada pro
        // vento atual (ex.: "Contra" com vento de popa) empurra menos pra
        // frente e mais pro lado (ou quase nada, perto de estolar), em vez
        // de continuar empurrando 100% pra frente como se estivesse sempre
        // perfeitamente trimada.
        float sailTrimAngle = Mathf.Clamp(trimReferenceAngle * 0.5f, 0f, sailAngleLimit) * Mathf.Deg2Rad;
        float thrustFraction = Mathf.Sin(sailTrimAngle);
        float lateralFraction = Mathf.Cos(sailTrimAngle);

        float thrustMagnitude = sailForceMagnitude * thrustFraction;
        result.thrustMagnitude = thrustMagnitude;
        result.thrustForce = shipForward * thrustMagnitude;

        // Lado: reaproveita _trimSide (já calculado externamente a partir do
        // mesmo vento, no mesmo frame) em vez de recalcular - garante
        // consistência entre o lado visual da vela e o lado da força.
        float lateralMagnitude = sailForceMagnitude * lateralFraction * _trimSide;
        result.lateralMagnitude = lateralMagnitude;
        result.lateralForce = shipRight * lateralMagnitude;

        return result;
    }

    // Eficiência do TRIM (0-1) dado o ângulo vento-proa e o modo selecionado:
    // 100% na faixa ideal, acceptableRangeEfficiency na faixa aceitável (com
    // interpolação suave na borda entre as duas, evitando degrau abrupto),
    // e queda até 0 ao longo de sharpFalloffRangeDegrees fora da faixa
    // aceitável.
    private float CalculateTrimEfficiency(float windAngleFromBow, int modeIndex, out bool inIdeal, out bool inAcceptable)
    {
        inIdeal = false;
        inAcceptable = false;

        if (sailModes == null || modeIndex < 0 || modeIndex >= sailModes.Length || sailModes[modeIndex] == null)
            return 0f;

        SailModeRange r = sailModes[modeIndex];

        if (windAngleFromBow >= r.idealMin && windAngleFromBow <= r.idealMax)
        {
            inIdeal = true;
            inAcceptable = true;
            return 1f;
        }

        if (windAngleFromBow >= r.acceptMin && windAngleFromBow <= r.acceptMax)
        {
            inAcceptable = true;
            bool belowIdeal = windAngleFromBow < r.idealMin;
            float distanceIntoAcceptable = belowIdeal ? (r.idealMin - windAngleFromBow) : (windAngleFromBow - r.idealMax);
            float acceptableWidth = belowIdeal
                ? Mathf.Max(0.01f, r.idealMin - r.acceptMin)
                : Mathf.Max(0.01f, r.acceptMax - r.idealMax);
            float t = Mathf.Clamp01(distanceIntoAcceptable / acceptableWidth);
            return Mathf.Lerp(1f, acceptableRangeEfficiency, t);
        }

        bool belowAcceptable = windAngleFromBow < r.acceptMin;
        float distanceOutside = belowAcceptable ? (r.acceptMin - windAngleFromBow) : (windAngleFromBow - r.acceptMax);
        float falloffT = Mathf.Clamp01(distanceOutside / Mathf.Max(0.01f, sharpFalloffRangeDegrees));
        return Mathf.Lerp(acceptableRangeEfficiency, 0f, falloffT);
    }

    private string GetModeName(int idx)
    {
        if (sailModes != null && idx >= 0 && idx < sailModes.Length && sailModes[idx] != null)
            return sailModes[idx].modeName;
        return "?";
    }

    private SailModeRange GetModeRange(int idx)
    {
        if (sailModes != null && idx >= 0 && idx < sailModes.Length)
            return sailModes[idx];
        return null;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // Estado auxiliar de uma "ordem" discreta (abertura OU modo - a mesma
    // máquina de estados serve para as duas, só muda o maxLevel usado).
    private class DiscreteCommandState
    {
        public int targetLevel;

        public bool isPressed;
        public int heldDirection;
        public float holdTimer;
        public bool holdTriggered;

        public bool windowActive;
        public float windowTimer;
        public int windowBaseline;
        public int pendingDelta;
    }

    private void Awake()
    {
        _openState.targetLevel = Mathf.Clamp(initialSailOpenLevel, 0, 2);
        _modeState.targetLevel = Mathf.Clamp(initialSailModeIndex, 0, MaxModeIndex);
        _trimSide = initialTrimSide >= 0 ? 1 : -1;

        CurrentSailOpenAmount = _openState.targetLevel / 2f; // 0 / 0.5 / 1.0
        CurrentSailAngle = 0f;

        RecalculateEffectiveArea();
    }

    private void Start()
    {
        LogModeOverlaps();
    }

    // Reporta, por par de modos vizinhos, quantos graus de sobreposição
    // existem entre suas faixas ACEITÁVEIS - essa sobreposição é o que
    // permite que dois modos adjacentes sejam ambos aceitáveis perto do
    // limite entre eles (pedido: 10-15° de sobreposição). Com os valores
    // padrão de sailModes já existe ~20° de sobreposição em cada par
    // (ex.: Contra aceita até 50°, Bolina aceita a partir de 30° -> 20° de
    // sobreposição) - este log só confirma isso e avisa se você editar os
    // ranges no Inspector e a sobreposição ficar abaixo do desejado.
    private void LogModeOverlaps()
    {
        if (sailModes == null) return;

        for (int i = 0; i < sailModes.Length - 1; i++)
        {
            SailModeRange a = sailModes[i];
            SailModeRange b = sailModes[i + 1];
            if (a == null || b == null) continue;

            float overlap = Mathf.Min(a.acceptMax, b.acceptMax) - Mathf.Max(a.acceptMin, b.acceptMin);

            if (overlap < minDesiredModeOverlapDegrees)
            {
                Debug.LogWarning(
                    $"[SailSystem] Sobreposição entre '{a.modeName}' e '{b.modeName}' é de só {overlap:F1}° " +
                    $"(mínimo desejado: {minDesiredModeOverlapDegrees:F0}°). Aumente acceptMax de '{a.modeName}' " +
                    $"e/ou diminua acceptMin de '{b.modeName}'.");
            }
            else
            {
                Debug.Log(
                    $"[SailSystem] '{a.modeName}' / '{b.modeName}': {overlap:F1}° de sobreposição aceitável " +
                    $"(>= {minDesiredModeOverlapDegrees:F0}° desejado - ambos os modos ficam aceitáveis nessa faixa).");
            }
        }
    }

    private int MaxModeIndex => sailModes != null && sailModes.Length > 0 ? sailModes.Length - 1 : 0;

    private void Update()
    {
        float dt = Time.deltaTime;
        float delayFactor = 1f + Mathf.Max(0f, adjustmentDelay);

        ProcessDiscreteInput(_openState, _sailOpenInput, dt, 2, direction =>
        {
            bool captainLine = UnityEngine.Random.value < 0.30f;
            OnSailOpenOrderIssued?.Invoke(direction, captainLine);
            onSailOpenOrderUnityEvent?.Invoke(direction, captainLine);
        });

        ProcessDiscreteInput(_modeState, _sailModeInput, dt, MaxModeIndex, direction =>
        {
            bool captainLine = UnityEngine.Random.value < 0.30f;
            OnSailModeOrderIssued?.Invoke(direction, captainLine);
            onSailModeOrderUnityEvent?.Invoke(direction, captainLine);
        });

        // Abertura: transição suave até o nível-alvo (0 / 0.5 / 1.0).
        // Trocar de alvo no meio do caminho não reinicia nada - só
        // redireciona o MoveTowards a partir de onde já está.
        float targetOpenAmount = _openState.targetLevel / 2f;
        float effectiveOpenSpeed = sailOpenSpeed / delayFactor;
        CurrentSailOpenAmount = Mathf.MoveTowards(CurrentSailOpenAmount, targetOpenAmount, effectiveOpenSpeed * dt);

        // O MODO em si (índice 0-4) é uma escolha discreta - muda no
        // instante em que a ordem é confirmada, sem interpolação. O que
        // transiciona suavemente é o ÂNGULO VISUAL da vela dentro do modo,
        // feito em CalculateForce (que tem acesso ao vento).

        RecalculateEffectiveArea();
    }

    // Máquina de estados genérica: interpreta um sinal contínuo -1/0/+1
    // como toques/segurar, com janela de agrupamento e comando prolongado.
    // Serve tanto para abertura quanto para seleção de modo - só muda o maxLevel.
    private void ProcessDiscreteInput(DiscreteCommandState state, float rawInput, float dt, int maxLevel, System.Action<int> onOrderFinalized)
    {
        bool isPressedNow = Mathf.Abs(rawInput) > 0.01f;
        int direction = isPressedNow ? (int)Mathf.Sign(rawInput) : 0;

        if (isPressedNow)
        {
            if (!state.isPressed)
            {
                state.isPressed = true;
                state.heldDirection = direction;
                state.holdTimer = 0f;
                state.holdTriggered = false;
            }
            else
            {
                state.holdTimer += dt;
                if (!state.holdTriggered && state.holdTimer >= holdThresholdSeconds)
                {
                    state.holdTriggered = true;
                    state.targetLevel = state.heldDirection > 0 ? maxLevel : 0;
                    state.windowActive = false;
                    state.pendingDelta = 0;
                    onOrderFinalized?.Invoke(state.heldDirection);
                }
            }
        }
        else if (state.isPressed)
        {
            if (!state.holdTriggered)
            {
                if (!state.windowActive)
                {
                    state.windowActive = true;
                    state.windowTimer = commandGroupingWindowSeconds;
                    state.windowBaseline = state.targetLevel;
                    state.pendingDelta = state.heldDirection;
                }
                else
                {
                    state.pendingDelta += state.heldDirection;
                }
            }
            state.isPressed = false;
            state.holdTriggered = false;
        }

        if (state.windowActive)
        {
            state.windowTimer -= dt;
            if (state.windowTimer <= 0f)
            {
                state.targetLevel = Mathf.Clamp(state.windowBaseline + state.pendingDelta, 0, maxLevel);
                int finalDirection = state.pendingDelta > 0 ? 1 : (state.pendingDelta < 0 ? -1 : 0);
                state.windowActive = false;
                state.pendingDelta = 0;
                if (finalDirection != 0) onOrderFinalized?.Invoke(finalDirection);
            }
        }
    }

    private void RecalculateEffectiveArea()
    {
        EffectiveSailArea = baseSailArea * CurrentSailOpenAmount;
    }

    // --- API pública de input (chamada pelo Player Control System) ---

    /// <summary>Sinal de abertura (-1 = fechar, +1 = abrir, 0 = solto). Interpretado como toque/segurar.</summary>
    public void SetSailOpenInput(float input)
    {
        _sailOpenInput = Mathf.Clamp(input, -1f, 1f);
    }

    /// <summary>
    /// Sinal de seleção de modo de navegação (-1 = caçar/em direção ao vento,
    /// +1 = folgar/em direção à popa, 0 = solto). Interpretado como
    /// toque/segurar - move o índice do modo entre os 5 modos ordenados.
    /// RENOMEADO de SetSailAngleInput: PlayerControlSystem.cs precisa
    /// chamar este método agora (mesmo parâmetro de sempre, só o nome mudou
    /// para refletir que não é mais um ângulo).
    /// </summary>
    public void SetSailModeInput(float input)
    {
        _sailModeInput = Mathf.Clamp(input, -1f, 1f);
    }

    // --- API pública de leitura (consumida pelo Sistema de Movimento e HUD) ---

    public float GetCurrentSailAngle() => CurrentSailAngle;
    public float GetCurrentSailOpenAmount() => CurrentSailOpenAmount;
    public float GetEffectiveSailArea() => EffectiveSailArea;
    public float GetEfficiencyMultiplier() => efficiencyMultiplier;

    private void OnValidate()
    {
        if (sailAngleLimit < 0f) sailAngleLimit = 0f;
        if (sailAngleLimit > 90f) sailAngleLimit = 90f;
        if (sailAngleSpeed < 0f) sailAngleSpeed = 0f;
        if (sailOpenSpeed < 0f) sailOpenSpeed = 0f;
        if (baseSailArea < 0f) baseSailArea = 0f;

        if (sailForceCoefficient < 0f) sailForceCoefficient = 0f;
        if (maxSailForce < 0f) maxSailForce = 0f;

        if (adjustmentDelay < 0f) adjustmentDelay = 0f;
        if (efficiencyMultiplier < 0f) efficiencyMultiplier = 0f;

        initialSailOpenLevel = Mathf.Clamp(initialSailOpenLevel, 0, 2);
        initialTrimSide = initialTrimSide >= 0 ? 1 : -1;

        if (sailModes != null && sailModes.Length > 0)
        {
            initialSailModeIndex = Mathf.Clamp(initialSailModeIndex, 0, sailModes.Length - 1);
        }

        if (commandGroupingWindowSeconds < 0.05f) commandGroupingWindowSeconds = 0.05f;
        if (holdThresholdSeconds < 0.05f) holdThresholdSeconds = 0.05f;
        if (sharpFalloffRangeDegrees < 0.1f) sharpFalloffRangeDegrees = 0.1f;
        if (modeClassificationSmoothingSeconds < 0.05f) modeClassificationSmoothingSeconds = 0.05f;
        if (minDesiredModeOverlapDegrees < 0f) minDesiredModeOverlapDegrees = 0f;
    }
}