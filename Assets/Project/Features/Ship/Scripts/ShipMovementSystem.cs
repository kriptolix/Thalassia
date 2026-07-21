using UnityEngine;

/// <summary>
/// Sistema de Movimento da Embarcação à Vela.
/// Consome WindSystem + SailSystem + RudderSystem, calcula vento aparente, força
/// de propulsão das velas, resistência hidrodinâmica anisotrópica e inclinação
/// (heel) — e aplica também a força/torque calculados pelo RudderSystem. Tudo
/// via Rigidbody (nunca Transform.position/rotation).
///
/// Referências (WindSystem, SailSystem, RudderSystem) são manuais via Inspector,
/// mesmo padrão dos demais sistemas.
///
/// Sem modo manual/debug embutido: pra testar a física isoladamente ou
/// forçar condições de vento específicas, use um orquestrador externo (fora
/// destes arquivos de sistema) chamando WindSystem.SetManualOverride/
/// ReleaseManualOverride - ver WindSystem.cs.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipMovementSystem : MonoBehaviour
{
    [Header("Referências Externas")]
    [SerializeField] private WindSystem windSystem;
    [SerializeField] private SailSystem sailSystem;
    [SerializeField] private RudderSystem rudderSystem;
    [SerializeField] private Rigidbody shipRigidbody;

    [Tooltip("Ponto acima do centro de massa onde a força das velas é aplicada (Rigidbody.AddForceAtPosition). A altura do centro de esforço (usada no cálculo de inclinação) é derivada da posição local Y deste transform.")]
    [SerializeField] private Transform sailForceApplicationPoint;

    [Header("Configurações Físicas")]
    [SerializeField] private float shipMass = 3000f;
    [Tooltip("Resistência ao movimento frontal (baixa).")]
    [SerializeField] private float frontalWaterResistance = 300f;
    [Tooltip("Resistência ao movimento lateral (alta) - evita que o barco deslize de lado. RECALIBRADO: desde a remoção do driftFactor, a força lateral que chega aqui é a força CHEIA da vela (antes era só uma fração fixa de 35%) - por isso o valor subiu bem mais que proporcionalmente ao de antes (era 2000). Fórmula: maxSailForce / deriva_lateral_maxima_aceitavel_em_m_por_s.")]
    [SerializeField] private float lateralWaterResistance = 5700f;

    [Header("Inclinação (Heel)")]
    [Tooltip("Escala a força lateral usada para gerar o torque de heel (aplicada com o braço de alavanca real do sailForceApplicationPoint - Rigidbody.AddForceAtPosition já calcula o torque correto sozinho, não precisa de um teto manual em N·m). 1 = usa a força lateral cheia do vento no pano.")]
    [SerializeField] private float heelFactor = 1f;
    [Tooltip("Ângulo máximo de inclinação (graus). Usado como soft-cap: a força usada para gerar o torque de heel é reduzida conforme o barco se aproxima desse ângulo, na mesma direção do heel.")]
    [SerializeField] private float maxHeelAngle = 20f;
    [Tooltip("Pequena resistência rotacional (roll) para auxiliar a recuperação, que é feita principalmente pelo sistema de flutuação.")]
    [SerializeField] private float rollDamping = 200f;
    [Tooltip("Resistência rotacional de guinada (yaw, rotação no eixo vertical). Sem isso, qualquer torque residual (de uma manobra anterior de vela/leme) faz o barco continuar girando indefinidamente mesmo com o leme centralizado - nada mais no sistema amortece esse eixo. Veja o Console no Start/Play para uma sugestão calculada a partir da inércia real do casco.")]
    [SerializeField] private float yawDamping = 400f;
    [Tooltip("Tempo (segundos) desejado para a velocidade angular de guinada cair a ~10% do valor inicial, sem nenhum torque de leme atuando. Usado só para calcular a sugestão de yawDamping impressa no Console - não afeta o comportamento em si.")]
    [SerializeField] private float desiredYawSettleSeconds = 1.5f;
    [Tooltip("Quanto o arrasto hidrodinâmico (frontal e lateral) aumenta no adernamento MÁXIMO (maxHeelAngle), representando a perda de eficiência hidrodinâmica pelo excesso de inclinação. Cresce de forma progressiva (quadrática) com o ângulo de heel, não linear. Ex.: 1 = arrasto dobra no heel máximo.")]
    [SerializeField] private float heelDragPenaltyFactor = 1f;
    [Tooltip("Quanto a eficiência de manobra do leme cai no adernamento MÁXIMO (maxHeelAngle) - 'o barco responde mais lentamente aos comandos de leme'. Cresce de forma progressiva (quadrática). Ex.: 0.5 = o leme perde até 50% de força no heel máximo.")]
    [Range(0f, 1f)]
    [SerializeField] private float heelRudderPenaltyFactor = 0.5f;

    // --- Informações para HUD (somente leitura) ---
    public float CurrentSpeed { get; private set; }
    public Vector3 ApparentWind { get; private set; }
    public float ApparentWindSpeed { get; private set; }
    public float ApparentWindDirection { get; private set; }
    public float CurrentSailEfficiency { get; private set; }
    public float CurrentSailForce { get; private set; }
    public float CurrentHeelAngle { get; private set; }
    public float CurrentSailAngleUsed { get; private set; }
    public float CurrentSailOpenAmountUsed { get; private set; }
    public float CurrentShipHeadingDeg { get; private set; }
    /// <summary>Ângulo entre o vento aparente e a PROA (0°-180°, 0=vento na proa, 180=vento na popa). Só para HUD/agulha - não decide mais o modo de navegação.</summary>
    public float CurrentWindAngleFromBow { get; private set; }
    /// <summary>Ângulo do vento REAL no sistema de referência do barco (0°-360°, círculo trigonométrico, popa=90°/proa=270° - ver SailSystem). É este ângulo que decide o modo de navegação.</summary>
    public float CurrentRealWindClassificationAngle { get; private set; }
    /// <summary>Índice do modo de navegação atual (0=Contra o Vento .. 4=Popa).</summary>
    public int CurrentSailModeIndex { get; private set; }
    /// <summary>Nome do modo de navegação atual, para HUD.</summary>
    public string CurrentSailModeName { get; private set; } = "";
    public bool CurrentInIdealRange { get; private set; }
    public bool CurrentInAcceptableRange { get; private set; }
    public float CurrentThrustForce { get; private set; }
    /// <summary>Força lateral da vela (heel + deriva) - RENOMEADO de CurrentDriftForce: desde a remoção do driftFactor não existe mais uma versão "descontada" separada, é a força lateral real, a mesma usada tanto pro heel quanto pela translação.</summary>
    public float CurrentLateralForce { get; private set; }
    /// <summary>Multiplicador de arrasto atual devido ao adernamento (1 = sem penalidade).</summary>
    public float CurrentHeelDragMultiplier { get; private set; } = 1f;
    /// <summary>Multiplicador de eficiência do leme atual devido ao adernamento (1 = sem penalidade), já enviado ao RudderSystem.</summary>
    public float CurrentRudderHeelEfficiencyMultiplier { get; private set; } = 1f;

    private void Awake()
    {
        if (shipRigidbody == null) shipRigidbody = GetComponent<Rigidbody>();
        shipRigidbody.mass = shipMass;

        if (sailForceApplicationPoint == null)
        {
            Debug.LogWarning($"{nameof(ShipMovementSystem)}: sailForceApplicationPoint não atribuído. " +
                              "Crie um Transform filho posicionado acima do centro de massa e atribua-o no Inspector.");
        }
    }

    private void Start()
    {
        LogSuggestedYawDamping();
    }

    // Sugere um valor de partida para yawDamping baseado na inércia de
    // guinada REAL do Rigidbody (lida em runtime, refletindo o collider/mesh
    // do casco). O valor anterior (400) foi um chute sem esse dado - se a
    // inércia real for bem maior, o tempo de decaimento (inércia/damping)
    // fica tão longo que o giro parece nunca parar sozinho, mesmo com
    // damping "ativo" no código.
    private void LogSuggestedYawDamping()
    {
        float yawInertia = shipRigidbody.inertiaTensor.y;

        // Decaimento exponencial: omega(t) = omega0 * exp(-(yawDamping/I) * t).
        // Resolvendo para omega(t)/omega0 = 0.1 (cair a 10%) no tempo desejado:
        float suggested = -Mathf.Log(0.1f) * yawInertia / Mathf.Max(0.01f, desiredYawSettleSeconds);

        Debug.Log(
            $"[ShipMovementSystem] yawInertia~={yawInertia:F0} kg·m² -> suggested yawDamping ~= {suggested:F0} " +
            $"(para a velocidade angular de guinada cair a ~10% em {desiredYawSettleSeconds}s, sem leme atuando). " +
            $"Valor atual no Inspector: {yawDamping}."
        );
    }

    private void FixedUpdate()
    {
        if (windSystem == null || sailSystem == null)
        {
            Debug.LogWarning($"{nameof(ShipMovementSystem)}: WindSystem ou SailSystem não atribuídos.");
            return;
        }

        // --- 1. Obter vento ---
        Vector3 windVector = windSystem.GetWindVector();

        // --- 2. Vento aparente = vento real - velocidade da embarcação (plano horizontal) ---
        Vector3 boatVelocityHorizontal = shipRigidbody.linearVelocity;
        boatVelocityHorizontal.y = 0f;

        Vector3 apparentWind = windVector - boatVelocityHorizontal;
        ApparentWind = apparentWind;
        ApparentWindSpeed = apparentWind.magnitude;
        ApparentWindDirection = NormalizeAngle(Mathf.Atan2(apparentWind.z, apparentWind.x) * Mathf.Rad2Deg);

        float shipHeadingDeg = NormalizeAngle(Mathf.Atan2(transform.forward.z, transform.forward.x) * Mathf.Rad2Deg);
        CurrentShipHeadingDeg = shipHeadingDeg;

        Vector3 thrustForce;
        Vector3 lateralForce;

        // Informa o SailSystem de que lado o vento está batendo, pra ele
        // trocar de lado sozinho ao cruzar o vento (como um veleiro real) -
        // o SailSystem propositalmente não conhece vento, essa decisão
        // precisa vir de fora.
        if (ApparentWindSpeed > 0.0001f)
        {
            int windSide = Vector3.Dot(apparentWind, transform.right) >= 0f ? 1 : -1;
            sailSystem.SetTrimSideFromWind(windSide);
        }

        // Vento REAL (windVector, sem subtrair a velocidade do barco) e vento
        // APARENTE (apparentWind) são passados separadamente: o SailSystem usa
        // o real só para decidir o MODO de navegação e o aparente só para a
        // FORÇA (ver docstring de SailSystem.CalculateForce).
        SailSystem.SailForceResult sail = sailSystem.CalculateForce(apparentWind, windVector, shipHeadingDeg);

        CurrentSailAngleUsed = sailSystem.GetCurrentSailAngle();
        CurrentSailOpenAmountUsed = sailSystem.GetCurrentSailOpenAmount();
        CurrentWindAngleFromBow = sail.windAngleFromBow;
        CurrentRealWindClassificationAngle = sail.realWindClassificationAngle;
        CurrentSailModeIndex = sail.sailModeIndex;
        CurrentSailModeName = sail.sailModeName;
        CurrentInIdealRange = sail.inIdealRange;
        CurrentInAcceptableRange = sail.inAcceptableRange;
        CurrentSailEfficiency = sail.trimEfficiency;
        CurrentSailForce = sail.sailForceMagnitude;
        CurrentThrustForce = sail.thrustMagnitude;
        CurrentLateralForce = sail.lateralMagnitude;

        thrustForce = sail.thrustForce;
        lateralForce = sail.lateralForce;

        // --- 5/6. Força de propulsão + heel, aplicados via braço de alavanca
        //     REAL (Rigidbody.AddForceAtPosition já calcula torque = força x
        //     braço sozinho - não precisa reinventar esse cálculo à mão).
        //
        //     A força lateral da vela (sail.lateralMagnitude) já é a força
        //     real (sem desconto de driftFactor - removido). O único motivo
        //     de ainda existir uma correção no centro de massa aqui é manter
        //     heelFactor como um controle PURO de intensidade visual do heel,
        //     sem também mudar a força que empurra o casco de lado na
        //     translação (que fica calibrada só por lateralWaterResistance).
        //     Com heelFactor=1 a correção é zero (idêntico a aplicar a força
        //     direto). Se um dia heelFactor deixar de ser necessário, dá pra
        //     remover a correção e aplicar sail.lateralForce direto.
        Vector3 applicationPoint = sailForceApplicationPoint != null
            ? sailForceApplicationPoint.position
            : shipRigidbody.worldCenterOfMass + Vector3.up;

        float currentHeelAngle = Vector3.SignedAngle(Vector3.up, transform.up, transform.forward);
        CurrentHeelAngle = currentHeelAngle;

        float heelForceMagnitude = sail.lateralMagnitude * heelFactor;

        // Soft-cap em maxHeelAngle (graus, com significado físico real) -
        // reduz a força usada pro torque conforme o barco já se aproxima do
        // ângulo máximo, na mesma direção do heel atual.
        if (Mathf.Sign(heelForceMagnitude) == Mathf.Sign(currentHeelAngle) || Mathf.Approximately(currentHeelAngle, 0f))
        {
            float heelRatio = Mathf.Clamp01(Mathf.Abs(currentHeelAngle) / Mathf.Max(0.01f, maxHeelAngle));
            heelForceMagnitude *= (1f - heelRatio);
        }

        Vector3 heelForceAtPoint = transform.right * heelForceMagnitude;
        Vector3 centerOfMassCorrection = lateralForce - heelForceAtPoint;

        shipRigidbody.AddForceAtPosition(thrustForce + heelForceAtPoint, applicationPoint, ForceMode.Force);
        shipRigidbody.AddForce(centerOfMassCorrection, ForceMode.Force); // sem posição = sem torque

        // Torque adicional de heel vindo do leme (efeito artístico opcional, RudderHeelMultiplier).
        if (rudderSystem != null)
        {
            float rudderHeelTorque = rudderSystem.GetRudderHeelTorque();
            shipRigidbody.AddTorque(transform.forward * rudderHeelTorque, ForceMode.Force);
        }

        // Pequena resistência rotacional auxiliar (recuperação principal é do sistema de flutuação).
        float rollAngularVelocity = Vector3.Dot(shipRigidbody.angularVelocity, transform.forward);
        shipRigidbody.AddTorque(transform.forward * -rollAngularVelocity * rollDamping, ForceMode.Force);

        // Amortecimento de guinada (yaw) - eixo vertical do mundo. Sem isso o
        // barco gira indefinidamente após qualquer torque residual, mesmo
        // com o leme centralizado.
        float yawAngularVelocity = Vector3.Dot(shipRigidbody.angularVelocity, Vector3.up);
        shipRigidbody.AddTorque(Vector3.up * -yawAngularVelocity * yawDamping, ForceMode.Force);

        // --- 6b. Penalidades de Adernamento (Heel): redução de velocidade
        //          máxima (mais arrasto) e de eficiência de manobra (leme
        //          mais lento), crescendo progressivamente (quadrático, não
        //          linear) até o adernamento máximo. O objetivo NÃO é
        //          simular risco de capotamento, só representar a perda de
        //          eficiência do excesso de inclinação.
        float heelPenaltyRatio = Mathf.Clamp01(Mathf.Abs(currentHeelAngle) / Mathf.Max(0.01f, maxHeelAngle));
        float heelPenaltyRatioSquared = heelPenaltyRatio * heelPenaltyRatio;

        float heelDragMultiplier = 1f + heelDragPenaltyFactor * heelPenaltyRatioSquared;
        CurrentHeelDragMultiplier = heelDragMultiplier;

        float rudderHeelEfficiencyMultiplier = Mathf.Clamp01(1f - heelRudderPenaltyFactor * heelPenaltyRatioSquared);
        CurrentRudderHeelEfficiencyMultiplier = rudderHeelEfficiencyMultiplier;
        if (rudderSystem != null)
        {
            rudderSystem.SetHeelEfficiencyMultiplier(rudderHeelEfficiencyMultiplier);
        }

        // --- 7. Resistência hidrodinâmica anisotrópica (frontal baixa, lateral alta), já penalizada pelo heel ---
        Vector3 localVelocity = transform.InverseTransformDirection(shipRigidbody.linearVelocity);
        Vector3 dragForceLocal = new Vector3(
            -localVelocity.x * lateralWaterResistance * heelDragMultiplier,
            0f,
            -localVelocity.z * frontalWaterResistance * heelDragMultiplier);
        shipRigidbody.AddForce(transform.TransformDirection(dragForceLocal), ForceMode.Force);

        // --- 8. Leme: aplica a força/torque calculados pelo RudderSystem (natural torque via alavanca) ---
        if (rudderSystem != null)
        {
            Vector3 rudderForce = rudderSystem.GetRudderForce();
            Vector3 rudderApplicationPoint = rudderSystem.GetRudderForceApplicationPoint();
            shipRigidbody.AddForceAtPosition(rudderForce, rudderApplicationPoint, ForceMode.Force);

            float turnDragMagnitude = rudderSystem.GetTurnDragForceMagnitude();
            if (turnDragMagnitude > 0f && shipRigidbody.linearVelocity.sqrMagnitude > 0.0001f)
            {
                Vector3 dragDirection = -shipRigidbody.linearVelocity.normalized;
                shipRigidbody.AddForce(dragDirection * turnDragMagnitude, ForceMode.Force);
            }
        }

        CurrentSpeed = shipRigidbody.linearVelocity.magnitude;
    }

    // --- API pública de input ---
    
    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }

    private void OnValidate()
    {
        if (frontalWaterResistance < 0f) frontalWaterResistance = 0f;
        if (lateralWaterResistance < 0f) lateralWaterResistance = 0f;
        if (maxHeelAngle < 0f) maxHeelAngle = 0f;
        if (rollDamping < 0f) rollDamping = 0f;
        if (yawDamping < 0f) yawDamping = 0f;
        if (desiredYawSettleSeconds < 0.1f) desiredYawSettleSeconds = 0.1f;
        if (heelDragPenaltyFactor < 0f) heelDragPenaltyFactor = 0f;
    }
}