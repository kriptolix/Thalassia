using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Sistema de flutuação baseado em física para embarcações (HDRP Water System, Unity 6.5).
///
/// VERSÃO DE DEPURAÇÃO INCREMENTAL:
/// Todos os recursos avançados da spec ficam atrás de toggles no Inspector,
/// desligados por padrão. Com tudo desligado, o comportamento é equivalente
/// ao script antigo (ShipBuoyancy) que já é sabidamente estável:
///   - ForceMode.Acceleration (independente de massa)
///   - Empuxo cru: force = submersion * buoyancy (sem curva, sem clamp)
///   - Damping global simples: velocity *= (1 - damping * dt)
///
/// Ligue um toggle de cada vez, teste em Play Mode, e veja qual introduz
/// instabilidade. Depois de identificar, me avisa qual foi.
///
/// Pré-requisitos de cena/projeto:
/// - HDRP Asset: seção Water > "Script Interactions" habilitada.
/// - Componente WaterSurface: "Script Interactions" habilitada no Inspector.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class BoatFloater : MonoBehaviour
{
    [Header("Referências")]
    public WaterSurface waterSurface;

    private Rigidbody rb;

    [Header("Centro de Massa")]
    [Tooltip("O script NUNCA calcula/altera o centro de massa automaticamente — apenas oferece a configuração.")]
    public bool overrideCenterOfMass = false;
    public Vector3 localCenterOfMass = new Vector3(0f, -0.35f, 0f);

    [Header("Pontos de Flutuação")]
    public FloatPoint[] floatPoints = new FloatPoint[6];

    [Header("Empuxo (base)")]
    public float buoyancy = 3f;

    [Header("Damping (base / legado)")]
    [Tooltip("Damping global simples, aplicado sobre linearVelocity/angularVelocity inteiras (igual ao script antigo). " +
             "Serve de baseline estável; desligue ao testar os toggles de damping/drag por ponto ou angular abaixo, " +
             "pois eles competem entre si.")]
    public bool useLegacyGlobalDamping = true;
    public float damping = 8f;

    // =====================================================================
    // TOGGLES INCREMENTAIS — ligue um de cada vez para isolar o que quebra.
    // =====================================================================

    [Header("[TOGGLE 1] Curva de Empuxo + Clamp de Submersão")]
    [Tooltip("OFF = empuxo linear cru (force = submersion * buoyancy), sem clamp — igual ao script antigo.\n" +
             "ON = usa buoyancyCurve normalizada (0-1) e maxSubmersionDepth, conforme a spec.")]
    public bool useBuoyancyCurve = false;
    public float maxSubmersionDepth = 0.6f;
    public AnimationCurve buoyancyCurve = new AnimationCurve(
        new Keyframe(0f, 0f), new Keyframe(0.5f, 0.6f), new Keyframe(1f, 1f));

    [Header("[TOGGLE 2] Damping Vertical por Ponto")]
    [Tooltip("OFF = sem damping por ponto (o damping global legado cuida disso).\n" +
             "ON = force -= pointVelocity.y * damping (conforme a spec), por ponto.")]
    public bool usePerPointVerticalDamping = false;

    [Header("[TOGGLE 3] Arrasto Horizontal (Forward/Right)")]
    public bool useHorizontalDrag = false;
    public float waterDrag = 1f;
    public float waterSideDrag = 4f;

    [Header("[TOGGLE 4] Resistência Rotacional por Eixo (Angular Drag Water)")]
    [Tooltip("Desligue 'useLegacyGlobalDamping' ao testar este, senão os dois competem.")]
    public bool useAngularDragWater = false;
    public Vector3 angularDragWater = new Vector3(1f, 1f, 1f); // X=Pitch, Y=Yaw, Z=Roll

    [Header("Segurança (Teto Fixo de Força)")]
    [Tooltip("Teto fixo e independente para a força final aplicada por ponto (em m/s², já que usamos ForceMode.Acceleration). " +
             "Protege contra picos (ex.: termo de damping reagindo a uma velocidade vertical já grande demais) " +
             "independente de erro de tuning na curva/clamp de submersão. Ajuste generosamente alto o suficiente " +
             "para não cortar em operação normal — ele deve ser uma rede de segurança, não um limitador ativo do dia a dia.")]
    public bool useMaxForceClamp = true;
    public float maxForcePerPoint = 25f;

    [Header("Gizmos")]
    public bool drawGizmos = true;

    private int pointCount;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        if (overrideCenterOfMass)
            rb.centerOfMass = localCenterOfMass;

        pointCount = floatPoints != null ? floatPoints.Length : 0;
    }

    void FixedUpdate()
    {
        if (waterSurface == null || floatPoints == null) return;

        float dt = Time.fixedDeltaTime;

        for (int i = 0; i < pointCount; i++)
        {
            FloatPoint fp = floatPoints[i];
            if (fp == null || fp.point == null) continue;

            ProcessFloatPoint(fp);
        }

        if (useAngularDragWater)
            ApplyAngularDragWater(dt);

        if (useLegacyGlobalDamping)
        {
            float factor = 1f - damping * dt;
            rb.linearVelocity *= factor;
            rb.angularVelocity *= factor;
        }
    }

    private void ProcessFloatPoint(FloatPoint fp)
    {
        Vector3 worldPos = fp.point.position;
        fp.worldPosition = worldPos;

        // --- Consulta da água ---
        fp.searchParams.startPositionWS = fp.hasSearchResult ? fp.searchResult.candidateLocationWS : worldPos;
        fp.searchParams.targetPositionWS = worldPos;
        fp.searchParams.error = 0.01f;
        fp.searchParams.maxIterations = 8;

        bool found = waterSurface.ProjectPointOnWaterSurface(fp.searchParams, out fp.searchResult);
        fp.hasSearchResult = found;

        if (!found)
        {
            fp.isSubmerged = false;
            fp.currentSubmersion = 0f;
            fp.appliedForce = Vector3.zero;
            return;
        }

        float waterHeight = fp.searchResult.projectedPositionWS.y;
        fp.waterHeight = waterHeight;

        Vector3 normal = fp.searchResult.normalWS;
        fp.waterNormal = normal.sqrMagnitude > 0.0001f ? normal : Vector3.up;

        float dt = Time.fixedDeltaTime;
        fp.estimatedSurfaceVelocity = dt > 0f ? (waterHeight - fp.previousWaterHeight) / dt : 0f;
        fp.previousWaterHeight = waterHeight;

        // --- Submersão ---
        float submersion = waterHeight - worldPos.y;

        if (submersion <= 0f)
        {
            fp.isSubmerged = false;
            fp.currentSubmersion = 0f;
            fp.appliedForce = Vector3.zero;
            return;
        }

        fp.isSubmerged = true;

        float force;

        if (useBuoyancyCurve)
        {
            // Comportamento avançado da spec: clamp + curva normalizada.
            float effectiveMax = fp.maxSubmersionOverride > 0f ? fp.maxSubmersionOverride : maxSubmersionDepth;
            float clamped = Mathf.Clamp(submersion, 0f, effectiveMax);
            fp.currentSubmersion = clamped;

            float normalizedSubmersion = effectiveMax > 0f ? clamped / effectiveMax : 0f;
            force = buoyancy * buoyancyCurve.Evaluate(normalizedSubmersion) * fp.buoyancyMultiplier;
        }
        else
        {
            // Comportamento do script antigo: linear, sem clamp.
            fp.currentSubmersion = submersion;
            force = buoyancy * submersion * fp.buoyancyMultiplier;
        }

        Vector3 pointVelocity = rb.GetPointVelocity(worldPos);

        if (usePerPointVerticalDamping)
        {
            force -= pointVelocity.y * damping * fp.dampingMultiplier;
        }

        Vector3 totalForce = Vector3.up * force;

        if (useHorizontalDrag)
        {
            Vector3 localVelocity = transform.InverseTransformDirection(pointVelocity);
            totalForce += (-transform.forward * (localVelocity.z * waterDrag)) +
                          (-transform.right * (localVelocity.x * waterSideDrag));
        }

        if (useMaxForceClamp)
        {
            totalForce = Vector3.ClampMagnitude(totalForce, maxForcePerPoint);
        }

        fp.appliedForce = totalForce;

        // ForceMode.Acceleration: independente de massa, igual ao script antigo.
        rb.AddForceAtPosition(totalForce, worldPos, ForceMode.Acceleration);
    }

    private void ApplyAngularDragWater(float dt)
    {
        // Decaimento exponencial direto sobre angularVelocity: incondicionalmente
        // estável para qualquer coeficiente/dt (ao contrário de AddTorque proporcional
        // à velocidade angular, que pode divergir).
        Vector3 localAngularVelocity = transform.InverseTransformDirection(rb.angularVelocity);

        localAngularVelocity.x *= Mathf.Exp(-angularDragWater.x * dt);
        localAngularVelocity.y *= Mathf.Exp(-angularDragWater.y * dt);
        localAngularVelocity.z *= Mathf.Exp(-angularDragWater.z * dt);

        rb.angularVelocity = transform.TransformDirection(localAngularVelocity);
    }

    [ContextMenu("Gerar 6 Pontos Padrão (Proa/Centro/Popa x Esq/Dir)")]
    private void GerarPontosPadrao()
    {
        float halfLength = 1.0f;
        float halfWidth = 0.5f;
        float waterlineHeight = 0f;

        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            halfLength = col.bounds.extents.z;
            halfWidth = col.bounds.extents.x;
            waterlineHeight = col.bounds.center.y - transform.position.y;
        }

        Vector3[] localOffsets = new Vector3[]
        {
            new Vector3(-halfWidth,  waterlineHeight,  halfLength),
            new Vector3( halfWidth,  waterlineHeight,  halfLength),
            new Vector3(-halfWidth,  waterlineHeight,  0f),
            new Vector3( halfWidth,  waterlineHeight,  0f),
            new Vector3(-halfWidth,  waterlineHeight, -halfLength),
            new Vector3( halfWidth,  waterlineHeight, -halfLength),
        };

        string[] names = { "FloatPoint_ProaEsq", "FloatPoint_ProaDir", "FloatPoint_CentroEsq", "FloatPoint_CentroDir", "FloatPoint_PopaEsq", "FloatPoint_PopaDir" };
        float[] defaultMultipliers = { 1.0f, 1.0f, 1.3f, 1.3f, 1.0f, 1.0f };

        floatPoints = new FloatPoint[localOffsets.Length];

        for (int i = 0; i < localOffsets.Length; i++)
        {
            GameObject go = new GameObject(names[i]);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = localOffsets[i];

            floatPoints[i] = new FloatPoint
            {
                point = go.transform,
                buoyancyMultiplier = defaultMultipliers[i],
                dampingMultiplier = 1f,
                maxSubmersionOverride = 0f
            };
        }

        pointCount = floatPoints.Length;
    }

    private void OnDrawGizmos()
    {
        if (!drawGizmos || floatPoints == null) return;

        for (int i = 0; i < floatPoints.Length; i++)
        {
            FloatPoint fp = floatPoints[i];
            if (fp == null || fp.point == null) continue;

            Vector3 pos = fp.point.position;

            Color pointColor;
            if (!fp.isSubmerged)
            {
                pointColor = Color.green;
            }
            else
            {
                float effectiveMax = fp.maxSubmersionOverride > 0f ? fp.maxSubmersionOverride : maxSubmersionDepth;
                float ratio = effectiveMax > 0f ? Mathf.Clamp01(fp.currentSubmersion / effectiveMax) : 0f;
                pointColor = ratio >= 0.99f ? Color.red : Color.Lerp(Color.yellow, Color.red, ratio);
            }

            Gizmos.color = pointColor;
            Gizmos.DrawSphere(pos, 0.08f);

            if (fp.hasSearchResult)
            {
                Vector3 surfacePoint = new Vector3(pos.x, fp.waterHeight, pos.z);
                Gizmos.color = pointColor;
                Gizmos.DrawLine(pos, surfacePoint);
            }

            if (fp.appliedForce.sqrMagnitude > 0.0001f)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(pos, pos + fp.appliedForce * 0.05f);
            }
        }
    }
}