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
/// o vento, sem intervenção do jogador, enquanto o vento for compatível com
/// o modo escolhido. Cada modo tem um TETO FÍSICO de eficiência diferente
/// (idealEfficiencyTarget - ex.: Través=1.0, Popa=0.6, refletindo a curva
/// polar real de um veleiro, onde través é o ponto de vela mais rápido e
/// popa/contra são mais lentos mesmo perfeitamente trimados); dentro desse
/// teto, a eficiência ainda cai gradualmente conforme o vento sai da faixa
/// ideal do modo selecionado (ver CalculateTrimEfficiency).
///
/// IMPORTANTE - DOIS VENTOS, DOIS PROPÓSITOS DIFERENTES:
///   - Qual MODO é válido/ideal (CalculateTrimEfficiency, sailModes) usa o
///     vento REAL (realWind, recebido como parâmetro de CalculateForce) -
///     "que tipo de navegação é essa, geometricamente, em relação ao vento
///     de verdade".
///   - A FORÇA final (thrustForce/lateralForce) e o ângulo visual da vela
///     continuam usando o vento APARENTE (apparentWind) - a vela reage ao
///     vento que ela realmente "sente", que inclui a velocidade do barco.
/// Nunca trocar esses dois papéis entre si.
///
/// SISTEMA DE REFERÊNCIA da classificação de modo (vento real): círculo
/// trigonométrico 0°-360°, com a POPA do barco apontando para 90° e,
/// portanto, a PROA para 270° (oposto), e os dois través (bombordo/
/// estibordo) em 0° e 180°. O ângulo usado é a direção (convenção "para
/// onde sopra", igual ao WindSystem) do vento REAL, expressa nesse sistema
/// local do barco - NÃO confundir com windAngleFromBow (0°-180°, vento
/// aparente, usado só para força/HUD - ver mais abaixo).
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
    [Tooltip("5 modos. O ângulo relevante é a direção do VENTO REAL (não aparente!) expressa no sistema local do barco: círculo trigonométrico 0°-360°, popa=90°, proa=270°, través=0°/180° (ver docstring da classe). Contra e Popa usam 1 faixa; Bolina/Través/Largo usam 2 (uma para cada bordo) - índice 1 é o espelho do índice 0.")]
    [SerializeField]
    private SailModeRange[] sailModes = new SailModeRange[]
    {
        new SailModeRange
        {
            modeName = "Contra",
            idealEfficiencyTarget = 0.25f,
            toleranceEfficiencyTarget = 0.15f,
            idealRanges  = new[] { new AngleRange { min = 75f,  max = 105f } },
            acceptRanges = new[] { new AngleRange { min = 70f,  max = 110f } },
        },
        new SailModeRange
        {
            modeName = "Bolina",
            idealEfficiencyTarget = 0.7f,
            toleranceEfficiencyTarget = 0.5f,
            idealRanges  = new[] { new AngleRange { min = 45f,  max = 75f  }, new AngleRange { min = 105f, max = 135f } },
            acceptRanges = new[] { new AngleRange { min = 40f,  max = 80f  }, new AngleRange { min = 100f, max = 140f } },
        },
        new SailModeRange
        {
            modeName = "Través",
            idealEfficiencyTarget = 1.0f,
            toleranceEfficiencyTarget = 0.8f,
            idealRanges  = new[] { new AngleRange { min = 0f,   max = 45f  }, new AngleRange { min = 135f, max = 180f } },
            acceptRanges = new[] { new AngleRange { min = 355f, max = 50f  }, new AngleRange { min = 130f, max = 185f } },
        },
        new SailModeRange
        {
            modeName = "Largo",
            idealEfficiencyTarget = 0.9f,
            toleranceEfficiencyTarget = 0.7f,
            idealRanges  = new[] { new AngleRange { min = 180f, max = 240f }, new AngleRange { min = 300f, max = 360f } },
            acceptRanges = new[] { new AngleRange { min = 175f, max = 245f }, new AngleRange { min = 295f, max = 5f   } },
        },
        new SailModeRange
        {
            modeName = "Popa",
            idealEfficiencyTarget = 0.6f,
            toleranceEfficiencyTarget = 0.4f,
            idealRanges  = new[] { new AngleRange { min = 240f, max = 300f } },
            acceptRanges = new[] { new AngleRange { min = 235f, max = 305f } },
        },
    };
    [Tooltip("Índice inicial do modo de navegação (0 = Contra o Vento .. sailModes.Length-1 = Popa).")]
    [SerializeField] private int initialSailModeIndex = 2;
    [Tooltip("ASSUMIDO: a spec pede 'queda acentuada' fora da faixa aceitável, mas não define a largura exata da transição. Graus além da borda da faixa aceitável até a eficiência (do trim) chegar a zero.")]
    [SerializeField] private float sharpFalloffRangeDegrees = 15f;
    [Tooltip("Sobreposição mínima desejada (graus) entre as faixas ACEITÁVEIS de modos vizinhos - dentro dela, os dois modos adjacentes ficam aceitáveis, então trocar de modo perto do limite não derruba a eficiência de repente. Só usada para avisar no Console (LogModeOverlaps) se você editar sailModes e a sobreposição ficar pequena demais - não afeta o cálculo de eficiência em si.")]
    [SerializeField] private float minDesiredModeOverlapDegrees = 10f;

    [Header("Suavização da Classificação de Modo")]
    [Tooltip("Constante de tempo (segundos) de um filtro passa-baixa aplicado a DOIS ângulos, antes de decidir a eficiência do trim e o ângulo de referência da força: (1) o ângulo de classificação (vento REAL, sistema popa=90°) e (2) o ângulo vento-proa (vento APARENTE, 0°-180°, usado no trim de referência/força). Evita que variações pequenas e rápidas (rajadas de vento, jitter de guinada) façam o modo ou a força 'piscar' quadro a quadro. NÃO afeta o ângulo bruto reportado no HUD (windAngleFromBow) - só as decisões internas. Curvas de verdade (que levam vários segundos) ainda são detectadas normalmente.")]
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

    /// <summary>
    /// Faixa angular no sistema de referência do vento REAL (0°-360°, círculo
    /// trigonométrico, popa=90°/proa=270° - ver docstring da classe). Se
    /// min > max, a faixa cruza 0°/360° (ex.: 355° a 50°).
    /// </summary>
    [System.Serializable]
    public struct AngleRange
    {
        public float min;
        public float max;

        /// <summary>Verdadeiro se angle (0-360) está dentro desta faixa, cruzando 0°/360° quando min > max.</summary>
        public bool Contains(float angle)
        {
            angle = NormalizeAngle(angle);
            float a = NormalizeAngle(min);
            float b = NormalizeAngle(max);
            if (a <= b) return angle >= a && angle <= b;
            return angle >= a || angle <= b;
        }

        /// <summary>Distância angular (graus, >=0) até a borda mais próxima; 0 se angle já está dentro.</summary>
        public float DistanceTo(float angle)
        {
            if (Contains(angle)) return 0f;
            float toMin = Mathf.Abs(Mathf.DeltaAngle(angle, min));
            float toMax = Mathf.Abs(Mathf.DeltaAngle(angle, max));
            return Mathf.Min(toMin, toMax);
        }
    }

    [System.Serializable]
    public class SailModeRange
    {
        public string modeName;
        [Tooltip("Teto de eficiência FÍSICO deste modo (0-1), atingido quando o vento real está na(s) idealRanges - representa a velocidade máxima real desse ponto de vela (ex.: Través é o mais rápido, Popa é mais lento mesmo bem trimado) - independente da precisão do trim.")]
        [Range(0f, 1f)]
        public float idealEfficiencyTarget = 1f;
        [Tooltip("Eficiência na faixa de TOLERÂNCIA (acceptRanges, fora da ideal) DESTE modo - valor explícito e independente por modo (não é mais uma fração/razão global do teto - cada modo tem sua própria queda, ex.: Través 100%->80%, mas Popa 60%->40%). Também é o valor usado na zona de SOBREPOSIÇÃO entre dois modos adjacentes (ex.: Popa/Largo): nessa zona sempre vale a tolerância do modo que o jogador tem SELECIONADO no momento, nunca a do modo vizinho.")]
        [Range(0f, 1f)]
        public float toleranceEfficiencyTarget = 0.6f;
        [Tooltip("Faixa(s) IDEAIS (eficiência = idealEfficiencyTarget), no sistema do vento REAL (popa=90°). Contra/Popa: 1 faixa. Bolina/Través/Largo: 2 faixas (uma por bordo).")]
        public AngleRange[] idealRanges;
        [Tooltip("Faixa(s) ACEITÁVEIS/TOLERÂNCIA (deve conter as idealRanges de mesmo índice) - eficiência = toleranceEfficiencyTarget. Adjacente pode se sobrepor com a faixa aceitável de outro modo (ex.: Popa e Largo) - a sobreposição é intencional (permite trocar de modo sem ficar fora de faixa por um instante) e não muda qual toleranceEfficiencyTarget é usado: é sempre o do modo selecionado.")]
        public AngleRange[] acceptRanges;
    }

    // --- Input recebido externamente (mesma interface -1/0/+1, "toque/segurar") ---
    private float _sailOpenInput;
    private float _sailModeInput;

    // --- Estado interno das ordens discretas ---
    private DiscreteCommandState _openState = new DiscreteCommandState();
    private DiscreteCommandState _modeState = new DiscreteCommandState();
    private int _trimSide = 1;

    // Filtros passa-baixa (ver header "Suavização da Classificação de Modo"):
    // um para o ângulo de CLASSIFICAÇÃO (vento real, sistema popa=90°, usado
    // só pra decidir o modo/eficiência) e outro para o ângulo vento-proa
    // (vento aparente, 0°-180°, usado no ângulo de referência do trim/força).
    private float _smoothedClassificationAngle;
    private float _smoothedApparentWindAngleFromBow;
    private bool _smoothedAnglesInitialized = false;

    // --- Estado atual (somente leitura para outros sistemas) ---
    /// <summary>Ângulo VISUAL atual da vela em relação ao casco (para animação do modelo 3D). Não é mais usado no cálculo de força - ver CalculateForce.</summary>
    public float CurrentSailAngle { get; private set; }
    public float CurrentSailOpenAmount { get; private set; }
    public float EffectiveSailArea { get; private set; }
    /// <summary>Ângulo entre o vento APARENTE e a PROA (0°-180°, 0=vento na proa, 180=vento na popa) do último CalculateForce - público para dar base a animações (telltales, flâmulas, etc.) sem precisar ler o SailForceResult inteiro.</summary>
    public float CurrentApparentWindAngle { get; private set; }
    /// <summary>Força (intensidade, m/s) do vento APARENTE do último CalculateForce - público para dar base a animações (ex.: intensidade de flutter do pano/flâmulas escala com isso).</summary>
    public float CurrentApparentWindSpeed { get; private set; }

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
        /// <summary>Ângulo entre o vento APARENTE e a PROA do barco (0°-180°, independente do bordo). 0° = vento na proa, 180° = vento na popa. Usado só para força/HUD/agulha - NÃO decide mais o modo de navegação (ver realWindClassificationAngle).</summary>
        public float windAngleFromBow;
        /// <summary>Força (intensidade, m/s) do vento APARENTE - mesma fonte usada pra escalar sailForceMagnitude.</summary>
        public float apparentWindSpeed;
        /// <summary>Ângulo do vento REAL no sistema de referência do barco (0°-360°, círculo trigonométrico, popa=90°/proa=270° - ver docstring da classe). É este ângulo que decide o modo de navegação (inIdealRange/inAcceptableRange/trimEfficiency), não windAngleFromBow.</summary>
        public float realWindClassificationAngle;
        public int sailModeIndex;
        public string sailModeName;
        public bool inIdealRange;
        public bool inAcceptableRange;
        /// <summary>Eficiência final do trim (0 a idealEfficiencyTarget do modo, já com o piso residual aplicado) - escala SÓ o empuxo (thrustMagnitude), não a força bruta.</summary>
        public float trimEfficiency;
        /// <summary>Força BRUTA que a vela captura do vento (capacidade física do pano, antes do teto de eficiência por modo) - fonte comum de thrustMagnitude e lateralMagnitude, mas cada um escala essa magnitude de forma independente (ver CalculateForce).</summary>
        public float sailForceMagnitude;
        public Vector3 thrustForce;
        public Vector3 lateralForce;
        /// <summary>Empuxo = sailForceMagnitude * trimEfficiency (teto do modo + precisão do trim) - não depende do ângulo geométrico de trim.</summary>
        public float thrustMagnitude;
        /// <summary>Componente lateral da força da vela (heel + deriva) - sailForceMagnitude * coeficiente geométrico do ângulo de trim (cos), independente de trimEfficiency. Um valor só, usado tanto para o torque de heel quanto para a translação lateral (ver ShipMovementSystem).</summary>
        public float lateralMagnitude;
    }

    /// <summary>
    /// Calcula a força que a vela produz dado o vento aparente e o heading
    /// do barco - ambos recebidos como PARÂMETROS, nunca lidos de uma
    /// referência guardada. Também atualiza o ângulo VISUAL da vela (efeito
    /// colateral leve, cosmético, usando Time.fixedDeltaTime - espera-se
    /// que este método seja chamado uma vez por FixedUpdate).
    /// </summary>
    /// <param name="apparentWind">Vetor de vento aparente no plano XZ (windVector - velocidade do barco). Usado SOMENTE para a força final (thrust/lateral) e o ângulo visual da vela.</param>
    /// <param name="realWind">Vetor de vento REAL no plano XZ (WindSystem.GetWindVector(), sem subtrair a velocidade do barco). Usado SOMENTE para decidir o modo de navegação (ideal/aceitável/eficiência) - nunca para a força.</param>
    /// <param name="shipHeadingDeg">Heading do barco em graus (0-360, mesma convenção do WindSystem).</param>
    public SailForceResult CalculateForce(Vector3 apparentWind, Vector3 realWind, float shipHeadingDeg)
    {
        SailForceResult result = default;
        result.sailModeIndex = CurrentSailModeIndex;
        result.sailModeName = CurrentSailModeName;

        float apparentWindSpeed = apparentWind.magnitude;
        float apparentWindDirection = NormalizeAngle(Mathf.Atan2(apparentWind.z, apparentWind.x) * Mathf.Rad2Deg);

        // Ângulo entre o vento APARENTE e a PROA do barco (0° = vento vindo
        // direto de frente, 180° = vento vindo direto de trás empurrando o
        // barco) - usado só para força/HUD/agulha de vento, NÃO decide mais
        // o modo (ver realWindClassificationAngle abaixo). Independente do
        // ângulo da vela em si: apparentWindDirection é para ONDE o vento
        // sopra, então a direção DE ONDE ele vem é o oposto (+180°); a
        // diferença angular entre essa direção-de-origem e o heading, então
        // invertida (180 - |delta|), dá 0 quando o vento vem de frente e 180
        // quando vem de trás.
        float windAngleFromBow = 180f - Mathf.Abs(Mathf.DeltaAngle(shipHeadingDeg, apparentWindDirection));
        result.windAngleFromBow = windAngleFromBow; // valor instantâneo, sem filtro - pro HUD/agulha de vento
        result.apparentWindSpeed = apparentWindSpeed;
        CurrentApparentWindAngle = windAngleFromBow;
        CurrentApparentWindSpeed = apparentWindSpeed;

        // Ângulo de CLASSIFICAÇÃO de modo, a partir do vento REAL: círculo
        // trigonométrico local do barco, popa=90°/proa=270° (ver docstring
        // da classe). direction do vento real (convenção "para onde sopra",
        // igual ao WindSystem) menos o heading do barco, com um deslocamento
        // de -90° pra alinhar a popa (heading+180° no referencial do mundo)
        // com 90° neste sistema local.
        float realWindDirection = NormalizeAngle(Mathf.Atan2(realWind.z, realWind.x) * Mathf.Rad2Deg);
        float classificationAngle = NormalizeAngle(realWindDirection - shipHeadingDeg - 90f);
        result.realWindClassificationAngle = classificationAngle; // valor instantâneo, sem filtro

        // Filtros passa-baixa exponenciais (ver header do campo) - evitam que
        // rajadas/jitter de guinada façam o modo ou a força "piscar" quadro a
        // quadro. No primeiro frame, inicializa direto no valor bruto (sem
        // isso o barco começaria classificado erradamente por alguns
        // segundos até o filtro convergir). O ângulo de classificação (0-360)
        // usa uma suavização ciente de wraparound (via Mathf.DeltaAngle) -
        // Mathf.Lerp comum quebraria perto de 0°/360°.
        if (!_smoothedAnglesInitialized)
        {
            _smoothedApparentWindAngleFromBow = windAngleFromBow;
            _smoothedClassificationAngle = classificationAngle;
            _smoothedAnglesInitialized = true;
        }
        else
        {
            float smoothingAlpha = 1f - Mathf.Exp(-Time.fixedDeltaTime / Mathf.Max(0.01f, modeClassificationSmoothingSeconds));
            _smoothedApparentWindAngleFromBow = Mathf.Lerp(_smoothedApparentWindAngleFromBow, windAngleFromBow, smoothingAlpha);
            _smoothedClassificationAngle = NormalizeAngle(
                _smoothedClassificationAngle + Mathf.DeltaAngle(_smoothedClassificationAngle, classificationAngle) * smoothingAlpha);
        }

        float rawTrimEfficiency = CalculateTrimEfficiency(_smoothedClassificationAngle, CurrentSailModeIndex, out bool inIdeal, out bool inAcceptable);
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
        // vento incompatível com o modo selecionado. Reaproveita as mesmas
        // acceptRanges já usadas em CalculateTrimEfficiency (mesma fonte de
        // verdade, só convertida de escala - ver FoldRealRangeToApparentBowRange).
        //
        // CORRIGIDO: antes disso, o ângulo de trim usava windAngleFromBow
        // BRUTO sem clamp nenhum - então com vento de popa (180°) e modo
        // "Contra" selecionado, a vela calculava o mesmo ângulo de 90° que
        // teria em modo Popa de verdade (só a EFICIÊNCIA mudava, nunca a
        // DIREÇÃO da força) - por isso o empuxo ia quase 100% pra frente
        // mesmo com a vela fisicamente sheeted pra perto do centro, o que
        // não faz sentido (deveria estar mais de banda/lateral, ou
        // efetivamente gasguetada).
        //
        // As faixas de sailModes agora estão no sistema do vento REAL
        // (0°-360°, popa=90°) - incompatíveis em escala com o ângulo vento-
        // proa APARENTE (0°-180°) usado aqui para a força. Por isso o clamp
        // usa o equivalente 0°-180° da faixa aceitável do modo, derivado por
        // fórmula (FoldRealRangeToApparentBowRange) a partir das mesmas
        // acceptRanges já usadas em CalculateTrimEfficiency - nenhum número
        // novo precisa ser configurado no Inspector.
        SailModeRange currentModeRange = GetModeRange(CurrentSailModeIndex);
        AngleRange apparentAcceptRange = FoldRealRangeToApparentBowRange(currentModeRange?.acceptRanges);
        float trimReferenceAngle = Mathf.Clamp(_smoothedApparentWindAngleFromBow, apparentAcceptRange.min, apparentAcceptRange.max);

        // Ângulo VISUAL da vela dentro do modo: ASSUMIDO como metade do
        // ângulo de referência do trim acima (aproximação clássica - a vela
        // tende a "bisectar" o ângulo entre proa e vento, mas só dentro do
        // que o modo permite), clampado ao limite visual e usando o lado já
        // determinado externamente via SetTrimSideFromWind. A spec define a
        // EFICIÊNCIA por modo mas não uma fórmula exata para o ângulo visual.
        float targetVisualAngle = Mathf.Clamp(trimReferenceAngle * 0.5f, 0f, sailAngleLimit) * _trimSide;
        CurrentSailAngle = Mathf.MoveTowards(CurrentSailAngle, targetVisualAngle, sailAngleSpeed * Time.fixedDeltaTime);

        // Força BRUTA que a vela captura do vento (capacidade física do
        // pano, ANTES de qualquer coisa relacionada a modo/trim) - depende
        // só de vento aparente, área e o multiplicador de eficiência
        // "genérico" (ex. dano na vela), não mais de trimEfficiency: isso é
        // o que muda agora - trimEfficiency deixa de escalar a força bruta
        // como um todo e passa a escalar só o EMPUXO (ver abaixo), pra não
        // competir com a decomposição geométrica do ângulo de trim.
        float rawForceMagnitude = apparentWindSpeed * EffectiveSailArea * efficiencyMultiplier * sailForceCoefficient;
        float sailForceMagnitude = Mathf.Min(rawForceMagnitude, maxSailForce);
        result.sailForceMagnitude = sailForceMagnitude;

        if (apparentWindSpeed <= 0.0001f || sailForceMagnitude <= 0f)
        {
            return result;
        }

        float headingRad = shipHeadingDeg * Mathf.Deg2Rad;
        Vector3 shipForward = new Vector3(Mathf.Cos(headingRad), 0f, Mathf.Sin(headingRad));
        Vector3 shipRight = new Vector3(Mathf.Cos(headingRad - Mathf.PI / 2f), 0f, Mathf.Sin(headingRad - Mathf.PI / 2f));

        // EMPUXO e DERIVA/HEEL são calculados de forma DESACOPLADA agora -
        // não são mais frações complementares (sin/cos) de uma mesma
        // decomposição vetorial (thrust²+lateral² deixou de ser igual a
        // magnitude²). Motivo: aquela decomposição amarrava as duas coisas
        // a um único ângulo geométrico, o que competia diretamente com o
        // teto de eficiência por modo (idealEfficiencyTarget) - ex.: Popa
        // tinha um thrustFraction geométrico muito alto (vela quase no eixo
        // do casco) que quase anulava o teto de 60% definido pro modo,
        // deixando Popa mais rápido que Través na prática. Não estamos
        // simulando aerodinâmica de vela de verdade (não há um "teto"
        // físico real de 60%/90%/100% em veleiros reais - isso é uma
        // aproximação de jogo) - então não faz sentido forçar uma
        // consistência vetorial que só reintroduz o problema. O que
        // importa: EMPUXO condizente com o modo, e DERIVA convincente pro
        // jogador. Duas fontes de verdade separadas:
        //
        //   EMPUXO = sailForceMagnitude * trimEfficiency (teto do modo +
        //   quão bem trimado dentro dele - já calculado acima). Simples e
        //   direto: cada modo empurra proporcionalmente ao seu teto.
        float thrustMagnitude = sailForceMagnitude * trimEfficiency;
        result.thrustMagnitude = thrustMagnitude;
        result.thrustForce = shipForward * thrustMagnitude;

        //   DERIVA/HEEL = sailForceMagnitude * um coeficiente geométrico
        //   simples, do ÂNGULO DE TRIM (mesmo usado no ângulo visual da
        //   vela) - vela fechada perto do eixo do casco (trim baixo, típico
        //   de Contra) gera bastante força lateral/heel; vela bem aberta
        //   (trim alto, típico de Popa) gera pouca. Isso é só pra dar uma
        //   sensação de deriva/adernamento condizente (mais em Contra,
        //   menos em Popa) - não precisa (e não deve) ser amarrado ao
        //   empuxo. Independente de trimEfficiency de propósito: mesmo um
        //   modo com teto baixo (ex. Contra=20%) ainda gera bastante deriva
        //   de verdade, é isso que "convincente" quer dizer aqui.
        float sailTrimAngle = Mathf.Clamp(trimReferenceAngle * 0.5f, 0f, sailAngleLimit) * Mathf.Deg2Rad;
        float lateralFraction = Mathf.Cos(sailTrimAngle);

        // Lado: reaproveita _trimSide (já calculado externamente a partir do
        // mesmo vento, no mesmo frame) em vez de recalcular - garante
        // consistência entre o lado visual da vela e o lado da força.
        float lateralMagnitude = sailForceMagnitude * lateralFraction * _trimSide;
        result.lateralMagnitude = lateralMagnitude;
        result.lateralForce = shipRight * lateralMagnitude;

        return result;
    }

    // Eficiência do TRIM (0 a idealEfficiencyTarget) dado o ângulo de
    // CLASSIFICAÇÃO (vento real, sistema popa=90°) e o modo selecionado:
    // idealEfficiencyTarget em qualquer uma das idealRanges (teto FÍSICO
    // daquele ponto de vela - ex.: Través=1.0, Popa=0.6 - ver
    // SailModeRange), toleranceEfficiencyTarget (valor explícito e
    // independente por modo, não mais uma fração do teto) em qualquer
    // acceptRanges (com interpolação suave na borda entre as duas), e queda
    // até 0 ao longo de sharpFalloffRangeDegrees fora de todas as faixas
    // aceitáveis. idealRanges[i]/acceptRanges[i] de mesmo índice são
    // tratadas como o mesmo "lado" (bordo) do modo, para a interpolação.
    // Numa zona de SOBREPOSIÇÃO entre dois modos adjacentes (ex.: Popa e
    // Largo), modeIndex é sempre o do modo que o jogador tem SELECIONADO -
    // esta função nunca olha ou mistura o valor de um modo vizinho.
    private float CalculateTrimEfficiency(float classificationAngle, int modeIndex, out bool inIdeal, out bool inAcceptable)
    {
        inIdeal = false;
        inAcceptable = false;

        SailModeRange r = GetModeRange(modeIndex);
        if (r == null || r.idealRanges == null || r.acceptRanges == null || r.idealRanges.Length == 0)
            return 0f;

        float idealCeiling = r.idealEfficiencyTarget;
        float acceptCeiling = r.toleranceEfficiencyTarget;

        for (int i = 0; i < r.idealRanges.Length; i++)
        {
            if (r.idealRanges[i].Contains(classificationAngle))
            {
                inIdeal = true;
                inAcceptable = true;
                return idealCeiling;
            }
        }

        for (int i = 0; i < r.acceptRanges.Length; i++)
        {
            if (!r.acceptRanges[i].Contains(classificationAngle)) continue;

            inAcceptable = true;

            AngleRange ideal = i < r.idealRanges.Length ? r.idealRanges[i] : r.idealRanges[0];
            AngleRange accept = r.acceptRanges[i];

            // Descobre de qual lado (borda min ou max) da faixa ideal o
            // ângulo está mais próximo, pra interpolar usando o "ombro"
            // (accept-edge -> ideal-edge) correspondente àquele lado.
            float toIdealMin = Mathf.Abs(Mathf.DeltaAngle(classificationAngle, ideal.min));
            float toIdealMax = Mathf.Abs(Mathf.DeltaAngle(classificationAngle, ideal.max));
            bool nearMinSide = toIdealMin <= toIdealMax;

            float distanceIntoAcceptable = nearMinSide ? toIdealMin : toIdealMax;
            float shoulderWidth = nearMinSide
                ? Mathf.Max(0.01f, Mathf.Abs(Mathf.DeltaAngle(accept.min, ideal.min)))
                : Mathf.Max(0.01f, Mathf.Abs(Mathf.DeltaAngle(accept.max, ideal.max)));

            float t = Mathf.Clamp01(distanceIntoAcceptable / shoulderWidth);
            return Mathf.Lerp(idealCeiling, acceptCeiling, t);
        }

        float distanceOutside = float.MaxValue;
        for (int i = 0; i < r.acceptRanges.Length; i++)
        {
            distanceOutside = Mathf.Min(distanceOutside, r.acceptRanges[i].DistanceTo(classificationAngle));
        }

        float falloffT = Mathf.Clamp01(distanceOutside / Mathf.Max(0.01f, sharpFalloffRangeDegrees));
        return Mathf.Lerp(acceptCeiling, 0f, falloffT);
    }

    // --- Conversão entre o sistema do vento REAL (0°-360°, popa=90°/proa=270°,
    // usado em sailModes/CalculateTrimEfficiency) e o sistema do vento
    // APARENTE (0°-180°, windAngleFromBow, usado na força/trim visual). Só
    // usada para derivar automaticamente o clamp do trimReferenceAngle (ver
    // CalculateForce) - nenhum valor é guardado no Inspector.

    /// <summary>
    /// Converte um único ângulo do sistema novo (vento real, popa=90°) para
    /// o equivalente no sistema antigo (vento-proa, 0°-180°, independente de
    /// bordo). 90°/270° (popa/proa) mapeiam para 0°/180°; 0°/180° (través)
    /// mapeiam para 90°.
    /// </summary>
    private static float NewAngleToApparentBowAngle(float newAngle)
    {
        float relative = Mathf.Repeat(newAngle - 90f, 360f) - 180f;
        return 180f - Mathf.Abs(relative);
    }

    /// <summary>
    /// Dobra um conjunto de AngleRange (sistema novo, vento real) no
    /// equivalente 0°-180° (vento-proa aparente). Como a conversão acima não
    /// é monotônica nos pontos críticos 90°/270°, também inclui 0°/180° como
    /// candidatos quando a faixa de entrada cruza esses pontos.
    /// </summary>
    private static AngleRange FoldRealRangeToApparentBowRange(AngleRange[] ranges)
    {
        if (ranges == null || ranges.Length == 0)
            return new AngleRange { min = 0f, max = 180f };

        float oldMin = 180f;
        float oldMax = 0f;

        foreach (AngleRange r in ranges)
        {
            float foldMin = NewAngleToApparentBowAngle(r.min);
            float foldMax = NewAngleToApparentBowAngle(r.max);
            oldMin = Mathf.Min(oldMin, Mathf.Min(foldMin, foldMax));
            oldMax = Mathf.Max(oldMax, Mathf.Max(foldMin, foldMax));

            if (r.Contains(90f)) oldMin = 0f;  // Contra: mínimo verdadeiro (dead upwind)
            if (r.Contains(270f)) oldMax = 180f; // Popa: máximo verdadeiro (dead downwind)
        }

        return new AngleRange { min = oldMin, max = oldMax };
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

    // Reporta, para cada par de sub-faixas ACEITÁVEIS de modos diferentes que
    // efetivamente se sobrepõem, quantos graus de sobreposição existem - essa
    // sobreposição é o que permite que dois modos adjacentes sejam ambos
    // aceitáveis perto do limite entre eles (pedido: 10-15° de sobreposição).
    // Como cada modo agora pode ter até 2 sub-faixas (uma por bordo) no
    // sistema 0°-360°, a adjacência não é mais só "índice i com índice i+1"
    // - por isso este log verifica TODOS os pares de sub-faixas entre
    // modos diferentes (com wraparound) e só reporta os que realmente se
    // tocam; pares de modos não-vizinhos naturalmente não se sobrepõem.
    private void LogModeOverlaps()
    {
        if (sailModes == null) return;

        for (int i = 0; i < sailModes.Length; i++)
        {
            SailModeRange a = sailModes[i];
            if (a?.acceptRanges == null) continue;

            for (int j = i + 1; j < sailModes.Length; j++)
            {
                SailModeRange b = sailModes[j];
                if (b?.acceptRanges == null) continue;

                for (int ai = 0; ai < a.acceptRanges.Length; ai++)
                {
                    for (int bi = 0; bi < b.acceptRanges.Length; bi++)
                    {
                        float overlap = ComputeOverlapDegrees(a.acceptRanges[ai], b.acceptRanges[bi]);
                        if (overlap <= 0f) continue;

                        if (overlap < minDesiredModeOverlapDegrees)
                        {
                            Debug.LogWarning(
                                $"[SailSystem] Sobreposição entre '{a.modeName}' e '{b.modeName}' é de só {overlap:F1}° " +
                                $"(mínimo desejado: {minDesiredModeOverlapDegrees:F0}°). Ajuste as acceptRanges " +
                                $"correspondentes desses dois modos.");
                        }
                        else
                        {
                            Debug.Log(
                                $"[SailSystem] '{a.modeName}' / '{b.modeName}': {overlap:F1}° de sobreposição aceitável " +
                                $"(>= {minDesiredModeOverlapDegrees:F0}° desejado - ambos os modos ficam aceitáveis nessa faixa).");
                        }
                    }
                }
            }
        }
    }

    // Sobreposição (em graus) entre dois arcos circulares (0-360, cada um
    // podendo cruzar 0°/360°) - testa os 3 alinhamentos possíveis (-360/0/+360)
    // e retorna o maior overlap positivo encontrado; 0 se não há sobreposição.
    // Válido para arcos estreitos (<180°), que é sempre o caso aqui.
    private static float ComputeOverlapDegrees(AngleRange a, AngleRange b)
    {
        float aStart = NormalizeAngle(a.min);
        float aLen = Mathf.Repeat(a.max - a.min, 360f);
        float bStartBase = NormalizeAngle(b.min);
        float bLen = Mathf.Repeat(b.max - b.min, 360f);

        float best = 0f;
        for (int shift = -1; shift <= 1; shift++)
        {
            float bStart = bStartBase + shift * 360f;
            float overlap = Mathf.Min(aStart + aLen, bStart + bLen) - Mathf.Max(aStart, bStart);
            if (overlap > best) best = overlap;
        }
        return best;
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
    public float GetCurrentApparentWindAngle() => CurrentApparentWindAngle;
    public float GetCurrentApparentWindSpeed() => CurrentApparentWindSpeed;

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
            foreach (SailModeRange r in sailModes)
            {
                if (r != null)
                {
                    r.idealEfficiencyTarget = Mathf.Clamp01(r.idealEfficiencyTarget);
                    r.toleranceEfficiencyTarget = Mathf.Clamp01(r.toleranceEfficiencyTarget);
                }
            }
        }

        if (commandGroupingWindowSeconds < 0.05f) commandGroupingWindowSeconds = 0.05f;
        if (holdThresholdSeconds < 0.05f) holdThresholdSeconds = 0.05f;
        if (sharpFalloffRangeDegrees < 0.1f) sharpFalloffRangeDegrees = 0.1f;
        if (modeClassificationSmoothingSeconds < 0.05f) modeClassificationSmoothingSeconds = 0.05f;
        if (minDesiredModeOverlapDegrees < 0f) minDesiredModeOverlapDegrees = 0f;
    }
}