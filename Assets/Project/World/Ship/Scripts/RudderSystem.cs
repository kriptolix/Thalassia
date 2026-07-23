using UnityEngine;

/// <summary>
/// Sistema de Leme — componente auxiliar do Sistema de Movimento.
/// Armazena o estado do leme, recebe comandos de controle e CALCULA a força/torque
/// resultantes da interação leme-água. NÃO aplica forças no Rigidbody diretamente —
/// quem lê esses valores e chama Rigidbody.AddForceAtPosition/AddTorque é o
/// ShipMovementSystem, que continua sendo o responsável pela aplicação final.
///
/// Fluxo: Player Control -> RudderSystem (calcula) -> ShipMovementSystem (aplica) -> Rigidbody.
///
/// Input: RudderInput (-1 a +1) chega via método público SetRudderInput, mesmo
/// padrão do SailSystem. O retorno ao centro quando o input volta a 0 é tratado
/// aqui automaticamente (o alvo de ângulo é proporcional ao input, então input=0
/// naturalmente centraliza o leme).
/// </summary>
[DisallowMultipleComponent]
public class RudderSystem : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Rigidbody do barco (leitura apenas, via GetPointVelocity — este sistema não aplica forças).")]
    [SerializeField] private Rigidbody shipRigidbody;
    [Tooltip("Transform indicando a posição física do leme (Rudder Position).")]
    [SerializeField] private Transform rudderPosition;

    [Header("Configurações Expostas")]
    [Tooltip("Ângulo máximo do leme (Rudder Limit), em graus.")]
    [SerializeField] private float rudderLimit = 45f;
    [Tooltip("Eficiência hidrodinâmica (Rudder Efficiency).")]
    [SerializeField] private float rudderEfficiency = 1f;
    [Tooltip("Tamanho efetivo do leme (Rudder Area).")]
    [SerializeField] private float rudderArea = 1f;
    [Tooltip("Multiplicador de força de rotação (Turn Strength).")]
    [SerializeField] private float turnStrength = 1f;
    [Tooltip("Resistência adicional durante curvas (Turn Drag).")]
    [SerializeField] private float turnDrag = 1f;
    [Tooltip("Multiplicador opcional de inclinação causada pelo leme, para ajuste artístico (RudderHeelMultiplier). 0 = sem influência.")]
    [SerializeField] private float rudderHeelMultiplier = 0f;

    [Header("Velocidade de Movimento do Leme")]
    [Tooltip("Graus por segundo. Campo adicional não listado nominalmente na spec, necessário para o movimento gradual do leme (não instantâneo).")]
    [SerializeField] private float rudderTurnSpeed = 90f;

    [Header("Resistência de Curva - Acúmulo por Duração")]
    [Tooltip("Quanto o Turn Drag cresce por segundo de curva sustentada (fator multiplicativo). Campo adicional não listado nominalmente na spec, para satisfazer 'a resistência aumenta com a duração da curva'.")]
    [SerializeField] private float turnDragDurationGrowth = 0.1f;

    [Header("Auto-Sugestão de Escala")]
    [Tooltip("Velocidade de referência (m/s) usada apenas para calcular a sugestão de Console abaixo - não afeta o comportamento em si.")]
    [SerializeField] private float referenceSpeedForSuggestion = 6f;
    [Tooltip("Aceleração angular de guinada (graus/s²) desejada com leme no máximo e na velocidade de referência acima. Usado só para o log de sugestão.")]
    [SerializeField] private float desiredYawAccelDegPerSec2 = 15f;

    // --- Input recebido externamente ---
    private float _rudderInput; // -1 a +1

    // --- Estado interno ---

    [SerializeField] private Transform wheel;
    [SerializeField] private float wheelRatio = 10f;
    private float _turnDurationTimer;

    // Multiplicador externo (0-1) de eficiência de manobra, definido pelo
    // ShipMovementSystem a cada frame com base no adernamento (heel) atual
    // do barco - "redução da eficiência de manobra, fazendo o barco
    // responder mais lentamente aos comandos de leme" conforme a spec de
    // adernamento. Default 1 (sem penalidade) se nunca for definido, ou se
    // o ShipMovementSystem não estiver presente/rodando.
    private float _heelEfficiencyMultiplier = 1f;

    // --- Saídas calculadas (lidas pelo ShipMovementSystem) ---
    public float RudderAngle { get; private set; }
    public Vector3 RudderForce { get; private set; }
    public float TurnDragForceMagnitude { get; private set; }
    public float RudderHeelTorque { get; private set; }

    public Vector3 RudderForceApplicationPoint =>
        rudderPosition != null ? rudderPosition.position : transform.position;

    private void Start()
    {
        LogSuggestedTurnStrength();
    }

    // Sugere um valor de partida para turnStrength baseado na massa e no
    // tensor de inércia REAL do Rigidbody (lido em runtime, então já reflete
    // o collider/mesh do casco, não um chute). O anterior turnStrength=1
    // (placeholder) produzia ~8N num barco de 3000kg - imperceptível.
    // Esta é só uma sugestão de partida, não um valor final: a geometria
    // real do casco não é perfeitamente simétrica, então ainda vale ajustar
    // observando o comportamento em jogo.
    private void LogSuggestedTurnStrength()
    {
        if (shipRigidbody == null || rudderPosition == null)
        {
            Debug.LogWarning($"{nameof(RudderSystem)}: shipRigidbody ou rudderPosition não atribuídos - não é possível sugerir turnStrength.");
            return;
        }

        // Alavanca: distância horizontal entre o leme e o centro de massa real do barco.
        Vector3 leverVector = rudderPosition.position - shipRigidbody.worldCenterOfMass;
        leverVector.y = 0f;
        float leverArm = Mathf.Max(leverVector.magnitude, 0.1f);

        // Momento de inércia no eixo de guinada (Y). Assume que os eixos
        // principais do tensor estão perto dos eixos do mundo/local, o que é
        // razoável para um casco sem inclinação de massa muito assimétrica.
        float yawInertia = shipRigidbody.inertiaTensor.y;

        float desiredAccelRad = desiredYawAccelDegPerSec2 * Mathf.Deg2Rad;
        float torqueNeeded = yawInertia * desiredAccelRad;
        float forceNeeded = torqueNeeded / leverArm;

        float rudderAngleRadAtLimit = rudderLimit * Mathf.Deg2Rad;
        float denom = referenceSpeedForSuggestion * Mathf.Sin(rudderAngleRadAtLimit) * Mathf.Max(rudderArea, 0.01f) * Mathf.Max(rudderEfficiency, 0.01f);
        float suggestedTurnStrength = denom > 0.0001f ? forceNeeded / denom : 0f;

        Debug.Log(
            $"[RudderSystem] mass={shipRigidbody.mass}kg, yawInertia~={yawInertia:F0} kg·m², leverArm~={leverArm:F1}m. " +
            $"At {referenceSpeedForSuggestion}m/s and full rudder ({rudderLimit}°), targeting {desiredYawAccelDegPerSec2}°/s² yaw accel " +
            $"-> suggested turnStrength ~= {suggestedTurnStrength:F0} (keeping current rudderArea={rudderArea}, rudderEfficiency={rudderEfficiency}). " +
            $"This is a starting point - tune from here by feel."
        );
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // Movimento gradual do leme; input=0 já converge naturalmente para o centro (0°).
        float targetAngle = _rudderInput * rudderLimit;
        RudderAngle = Mathf.MoveTowards(RudderAngle, targetAngle, rudderTurnSpeed * dt);

        // Atualiza o timão
        wheel.localRotation = Quaternion.Euler(0f, 0f, RudderAngle * wheelRatio);

        if (shipRigidbody == null || rudderPosition == null)
        {
            RudderForce = Vector3.zero;
            TurnDragForceMagnitude = 0f;
            RudderHeelTorque = 0f;
            return;
        }

        // Velocidade relativa da água no ponto do leme (considera velocidade linear + rotação do barco).
        Vector3 pointVelocity = shipRigidbody.GetPointVelocity(rudderPosition.position);
        float forwardFlow = Vector3.Dot(pointVelocity, transform.forward);

        float rudderAngleRad = RudderAngle * Mathf.Deg2Rad;

        // Força do Leme = Velocidade da Água x Ângulo do Leme x Eficiência (lateral ao eixo do barco).
        // heelEfficiencyMultiplier: penalidade de manobra por adernamento (ver
        // SetHeelEfficiencyMultiplier) - 1 = sem penalidade, cai conforme o
        // barco aderna, simulando o leme perdendo mordida na água quando o
        // casco está muito inclinado.
        float lateralForceMagnitude = forwardFlow * Mathf.Sin(rudderAngleRad) * rudderArea * rudderEfficiency * turnStrength * _heelEfficiencyMultiplier;
        RudderForce = transform.right * lateralForceMagnitude;

        // Resistência em curvas: aumenta com velocidade, ângulo do leme e duração da curva.
        float turnFactor = Mathf.Abs(RudderAngle) / Mathf.Max(0.01f, rudderLimit);
        _turnDurationTimer = turnFactor > 0.01f ? _turnDurationTimer + dt : 0f;

        float boatSpeed = shipRigidbody.linearVelocity.magnitude;
        float durationMultiplier = 1f + _turnDurationTimer * turnDragDurationGrowth;
        TurnDragForceMagnitude = boatSpeed * turnFactor * turnDrag * durationMultiplier;

        // Influência opcional na inclinação (ajuste artístico).
        RudderHeelTorque = lateralForceMagnitude * rudderHeelMultiplier;
    }

    // --- API pública de input (chamada pelo Player Control System) ---

    /// <summary>Define o input normalizado do leme (-1 = bombordo, +1 = estibordo, 0 = centralizar).</summary>
    public void SetRudderInput(float input)
    {
        _rudderInput = Mathf.Clamp(input, -1f, 1f);
    }

    /// <summary>
    /// Define o multiplicador de eficiência de manobra por adernamento
    /// (0-1), chamado pelo ShipMovementSystem a cada FixedUpdate. NOTA DE
    /// ORDEM DE EXECUÇÃO: como RudderSystem calcula sua própria força em seu
    /// PRÓPRIO FixedUpdate (não sob demanda), este valor só afeta o cálculo
    /// do PRÓXIMO frame físico em relação a quando é definido - mesma
    /// defasagem de 1 frame que já existe na leitura de GetRudderForce()
    /// pelo ShipMovementSystem. Imperceptível a ~50Hz.
    /// </summary>
    public void SetHeelEfficiencyMultiplier(float multiplier)
    {
        _heelEfficiencyMultiplier = Mathf.Clamp01(multiplier);
    }

    // --- API pública de leitura (HUD, áudio, VFX, sistemas futuros de dano) ---

    public float GetCurrentRudderAngle() => RudderAngle;
    public float GetRudderLimit() => rudderLimit;
    public Vector3 GetRudderForce() => RudderForce;
    public Vector3 GetRudderForceApplicationPoint() => RudderForceApplicationPoint;
    public float GetTurnDragForceMagnitude() => TurnDragForceMagnitude;
    public float GetRudderHeelTorque() => RudderHeelTorque;

    private void OnValidate()
    {
        if (rudderLimit < 0f) rudderLimit = 0f;
        if (rudderLimit > 90f) rudderLimit = 90f;
        if (rudderEfficiency < 0f) rudderEfficiency = 0f;
        if (rudderArea < 0f) rudderArea = 0f;
        if (turnStrength < 0f) turnStrength = 0f;
        if (turnDrag < 0f) turnDrag = 0f;
        if (rudderHeelMultiplier < 0f) rudderHeelMultiplier = 0f;
        if (rudderTurnSpeed < 0f) rudderTurnSpeed = 0f;
        if (turnDragDurationGrowth < 0f) turnDragDurationGrowth = 0f;
    }
}