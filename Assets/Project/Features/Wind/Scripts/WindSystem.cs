using System;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Sistema Global de Vento (Prova de Conceito).
/// Fonte única de verdade sobre vento para todos os demais sistemas
/// (física das velas, propulsão, HUD, clima futuro, VFX/SFX futuros).
///
/// Deve existir apenas UMA instância na cena. Outros sistemas devem
/// referenciar essa instância manualmente via campo [SerializeField]
/// arrastado no Inspector (sem singleton estático).
///
/// Convenção de direção: 0°-360°, onde 0° = Leste, 90° = Norte,
/// 180° = Oeste, 270° = Sul. Mapeamento de eixos no mundo (plano X/Z):
///   Leste = +X, Norte = +Z (ângulo padrão Unity, sentido anti-horário).
/// A direção representa PARA ONDE o vento está soprando.
/// </summary>
[DisallowMultipleComponent]
public class WindSystem : MonoBehaviour
{
    [Header("Intensidade a física usa m/s internamente, ver GetWindVector())")]
    [SerializeField] private float minIntensity = 2f;
    [SerializeField] private float maxIntensity = 30f;
    [SerializeField] private float initialIntensity = 8f;

    [Header("Direção (graus, 0-360 | 0=Leste, 90=Norte, 180=Oeste, 270=Sul)")]
    [SerializeField] private float initialDirection = 90f;

    [Header("Intervalo entre mudanças principais (segundos)")]
    [SerializeField] private float minTimeBetweenChanges = 60f;
    [SerializeField] private float maxTimeBetweenChanges = 240f;

    [Header("Variação de direção por ciclo (graus, em torno da direção atual)")]
    [SerializeField] private float minDirectionVariation = 10f;
    [SerializeField] private float maxDirectionVariation = 40f;

    [Header("Variação de intensidade por ciclo m/s")]
    [SerializeField] private float minIntensityVariation = 2f;
    [SerializeField] private float maxIntensityVariation = 6f;

    [Header("Velocidade de interpolação")]
    [Tooltip("Graus por segundo, usados na transição da direção base e no decaimento/aumento das rajadas de direção.")]
    [SerializeField] private float directionInterpolationSpeed = 5f;
    [Tooltip("Metros por segundo, usados na transição da intensidade base e no decaimento/aumento das rajadas de intensidade.")]
    [SerializeField] private float intensityInterpolationSpeed = 1f;

    [Header("Rajadas (Gusts)")]
    [Tooltip("Intervalo mínimo entre o início de uma rajada e o início da próxima (segundos). Campo adicional não listado explicitamente na spec original.")]
    [SerializeField] private float minGustInterval = 10f;
    [Tooltip("Intervalo máximo entre o início de uma rajada e o início da próxima (segundos). Campo adicional não listado explicitamente na spec original.")]
    [SerializeField] private float maxGustInterval = 40f;
    [SerializeField] private float minGustDuration = 2f;
    [SerializeField] private float maxGustDuration = 6f;
    [Tooltip("Variação mínima de intensidade causada por uma rajada (m/s). Campo adicional não listado explicitamente na spec original.")]
    [SerializeField] private float minGustIntensity = 0.5f;
    [SerializeField] private float maxGustIntensity = 3f;
    [Tooltip("Variação máxima de direção causada por uma rajada (graus). ATENÇÃO: assumido por não haver campo equivalente na lista de configs expostas da spec original — ajuste livremente.")]
    [SerializeField] private float maxGustDirectionVariation = 5f;

    [Header("Eventos")]
    [Tooltip("Disparado sempre que o sistema escolhe novos alvos na variação principal (mudança significativa). Rajadas não disparam este evento.")]
    [SerializeField] private WindChangedUnityEvent onWindChangedUnityEvent;

    [Serializable]
    public class WindChangedUnityEvent : UnityEvent<float, float> { }

    /// <summary>Disparado com (novaDirecaoAlvo, novaIntensidadeAlvo) a cada mudança principal significativa.</summary>
    public event Action<float, float> OnWindChanged;

    // --- Estado interno: variação principal ---
    private float _currentBaseDirection;
    private float _currentBaseIntensity;
    private float _targetDirection;
    private float _targetIntensity;
    private float _mainChangeTimer;

    // --- Estado interno: rajadas ---
    private float _gustTimer;
    private float _gustDirectionOffsetCurrent;
    private float _gustDirectionOffsetTarget;
    private float _gustIntensityOffsetCurrent;
    private float _gustIntensityOffsetTarget;
    private float _gustDurationTimer;
    private bool _gustActive;

    // --- Override manual (controlado externamente por um orquestrador de
    // testes via SetManualOverride/ReleaseManualOverride - ver API pública
    // no fim da classe). Substitui o antigo toggle "Modo Manual" do
    // Inspector: nenhum campo de debug embutido no sistema, só hooks que um
    // componente externo pode chamar.
    private bool _manualOverrideActive = false;
    private float _manualOverrideDirection;
    private float _manualOverrideIntensity;

    // --- Saídas públicas (base + rajada, sempre sincronizadas) ---
    public float CurrentDirection { get; private set; }
    public float CurrentIntensity { get; private set; }
    public Vector3 NormalizedDirectionVector { get; private set; }
    public Vector3 WindVector { get; private set; }

    public bool ManualOverrideActive => _manualOverrideActive;

    private void Awake()
    {
        _currentBaseDirection = NormalizeAngle(initialDirection);
        _currentBaseIntensity = Mathf.Clamp(initialIntensity, minIntensity, maxIntensity);
        _targetDirection = _currentBaseDirection;
        _targetIntensity = _currentBaseIntensity;

        ResetMainChangeTimer();
        ResetGustTimer();

        RecalculateOutputs();
    }

    private void Update()
    {
        float dt = Time.deltaTime;

        if (_manualOverrideActive)
        {
            _currentBaseDirection = NormalizeAngle(_manualOverrideDirection);
            _currentBaseIntensity = Mathf.Clamp(_manualOverrideIntensity, minIntensity, maxIntensity);
            _targetDirection = _currentBaseDirection;
            _targetIntensity = _currentBaseIntensity;

            _gustActive = false;
            _gustDirectionOffsetCurrent = 0f;
            _gustIntensityOffsetCurrent = 0f;

            RecalculateOutputs();
            return; // nenhuma variação automática enquanto o override estiver ativo
        }

        UpdateMainVariation(dt);
        UpdateGusts(dt);
        RecalculateOutputs();
    }

    private void UpdateMainVariation(float dt)
    {
        _mainChangeTimer -= dt;
        if (_mainChangeTimer <= 0f)
        {
            PickNewMainTargets();
            ResetMainChangeTimer();
        }

        _currentBaseDirection = NormalizeAngle(
            Mathf.MoveTowardsAngle(_currentBaseDirection, _targetDirection, directionInterpolationSpeed * dt));

        _currentBaseIntensity = Mathf.MoveTowards(_currentBaseIntensity, _targetIntensity, intensityInterpolationSpeed * dt);
    }

    private void PickNewMainTargets()
    {
        float dirVariation = UnityEngine.Random.Range(minDirectionVariation, maxDirectionVariation);
        if (UnityEngine.Random.value < 0.5f) dirVariation = -dirVariation;
        _targetDirection = NormalizeAngle(_currentBaseDirection + dirVariation);

        float intVariation = UnityEngine.Random.Range(minIntensityVariation, maxIntensityVariation);
        if (UnityEngine.Random.value < 0.5f) intVariation = -intVariation;
        _targetIntensity = Mathf.Clamp(_currentBaseIntensity + intVariation, minIntensity, maxIntensity);

        OnWindChanged?.Invoke(_targetDirection, _targetIntensity);
        onWindChangedUnityEvent?.Invoke(_targetDirection, _targetIntensity);
    }

    private void ResetMainChangeTimer()
    {
        _mainChangeTimer = UnityEngine.Random.Range(minTimeBetweenChanges, maxTimeBetweenChanges);
    }

    private void UpdateGusts(float dt)
    {
        if (_gustActive)
        {
            _gustDurationTimer -= dt;

            _gustDirectionOffsetCurrent = Mathf.MoveTowards(_gustDirectionOffsetCurrent, _gustDirectionOffsetTarget, directionInterpolationSpeed * dt);
            _gustIntensityOffsetCurrent = Mathf.MoveTowards(_gustIntensityOffsetCurrent, _gustIntensityOffsetTarget, intensityInterpolationSpeed * dt);

            if (_gustDurationTimer <= 0f)
            {
                // Sinaliza para os offsets voltarem a zero suavemente.
                _gustDirectionOffsetTarget = 0f;
                _gustIntensityOffsetTarget = 0f;

                bool settledBackToZero =
                    Mathf.Approximately(_gustDirectionOffsetCurrent, 0f) &&
                    Mathf.Approximately(_gustIntensityOffsetCurrent, 0f);

                if (settledBackToZero)
                {
                    _gustActive = false;
                    ResetGustTimer();
                }
            }
            return;
        }

        _gustTimer -= dt;
        if (_gustTimer <= 0f)
        {
            StartNewGust();
        }
    }

    private void StartNewGust()
    {
        _gustActive = true;
        _gustDurationTimer = UnityEngine.Random.Range(minGustDuration, maxGustDuration);

        float dirOffset = UnityEngine.Random.Range(0f, maxGustDirectionVariation);
        if (UnityEngine.Random.value < 0.5f) dirOffset = -dirOffset;
        _gustDirectionOffsetTarget = dirOffset;

        float intOffset = UnityEngine.Random.Range(minGustIntensity, maxGustIntensity);
        if (UnityEngine.Random.value < 0.5f) intOffset = -intOffset;
        _gustIntensityOffsetTarget = intOffset;
    }

    private void ResetGustTimer()
    {
        _gustTimer = UnityEngine.Random.Range(minGustInterval, maxGustInterval);
    }

    // Nós -> m/s. A partir daqui, "nós" é usado SÓ para exibição
    // (CurrentIntensity/GetIntensityKnots) - toda a física (WindVector,
    // consumido por SailSystem/ShipMovementSystem) usa m/s de verdade.
    private const float KnotsToMetersPerSecond = 0.514444f;

    private void RecalculateOutputs()
    {
        CurrentDirection = NormalizeAngle(_currentBaseDirection + _gustDirectionOffsetCurrent);
        CurrentIntensity = Mathf.Clamp(_currentBaseIntensity + _gustIntensityOffsetCurrent, minIntensity, maxIntensity); 

        float rad = CurrentDirection * Mathf.Deg2Rad;
        // Leste = +X, Norte = +Z (ângulo padrão Unity, anti-horário no plano X/Z).
        NormalizedDirectionVector = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad));
        WindVector = NormalizedDirectionVector * CurrentIntensity;
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    // --- API pública para outros sistemas ---

    /// <summary>Direção atual do vento em graus (0-360). 0=Leste, 90=Norte, 180=Oeste, 270=Sul.</summary>
    public float GetDirectionDegrees() => CurrentDirection;

    /// <summary>Intensidade atual do vento em m/s (ver GetWindVector()).</summary>
    public float GetIntensitySpeed() => CurrentIntensity; // m/s

    /// <summary>Vetor unitário (normalizado) apontando para onde o vento sopra, no plano X/Z.</summary>
    public Vector3 GetNormalizedDirection() => NormalizedDirectionVector;

    /// <summary>Vetor completo: direção × intensidade, em M/S (unidade real de física - já convertido de nós).</summary>
    public Vector3 GetWindVector() => WindVector;

    // --- API pública para um orquestrador de testes externo (fora deste
    // arquivo) desligar a variação automática e forçar vento fixo - ex.:
    // testar um ponto de vela específico de forma reproduzível. Nenhum
    // campo de debug fica exposto aqui; quem orquestra o teste decide
    // quando chamar e com quais valores. ---

    /// <summary>
    /// Força direção/intensidade fixas (desliga variação principal e rajadas
    /// até ReleaseManualOverride ser chamado). Pensado para ser chamado por
    /// um componente orquestrador de testes, não pelo jogo em si.
    /// </summary>
    /// <param name="direction">Graus, 0-360 (mesma convenção do WindSystem).</param>
    /// <param name="intensity">NÓS (mesma unidade do Inspector/HUD) - convertido pra m/s internamente, igual à variação automática.</param>
    public void SetManualOverride(float direction, float intensity)
    {
        _manualOverrideActive = true;
        _manualOverrideDirection = NormalizeAngle(direction);
        _manualOverrideIntensity = Mathf.Clamp(intensity, minIntensity, maxIntensity);
    }

    /// <summary>Libera o override manual - a variação automática (principal + rajadas) volta a partir do vento atual, sem salto.</summary>
    public void ReleaseManualOverride()
    {
        if (!_manualOverrideActive) return;

        _manualOverrideActive = false;
        // Retoma a partir do vento atual (que era o valor forçado), evitando
        // um salto visual no instante da liberação.
        _currentBaseDirection = CurrentDirection;
        _currentBaseIntensity = CurrentIntensity;
        _targetDirection = _currentBaseDirection;
        _targetIntensity = _currentBaseIntensity;
        ResetMainChangeTimer();
    }

    private void OnValidate()
    {
        if (minIntensity > maxIntensity) maxIntensity = minIntensity;
        initialIntensity = Mathf.Clamp(initialIntensity, minIntensity, maxIntensity);

        if (minTimeBetweenChanges > maxTimeBetweenChanges) maxTimeBetweenChanges = minTimeBetweenChanges;
        if (minDirectionVariation > maxDirectionVariation) maxDirectionVariation = minDirectionVariation;
        if (minIntensityVariation > maxIntensityVariation) maxIntensityVariation = minIntensityVariation;
        if (minGustInterval > maxGustInterval) maxGustInterval = minGustInterval;
        if (minGustDuration > maxGustDuration) maxGustDuration = minGustDuration;
        if (minGustIntensity > maxGustIntensity) maxGustIntensity = minGustIntensity;
        if (maxGustDirectionVariation < 0f) maxGustDirectionVariation = 0f;

        initialDirection = NormalizeAngle(initialDirection);
    }
}