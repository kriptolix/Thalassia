/******************************************************************************
 * FloaterHDRP.cs
 * 
 * Description:
 *   Realistic water buoyancy for boats/ships using HDRP's Water Surface
 *   system.
 *   
 *   It includes:
 *     - Real water height sampling via WaterSurface.ProjectPointOnWaterSurface
 *     - Damped spring buoyancy per point (force = k*depth - c*velocity),
 *       which is what prevents the boat from bouncing/oscillating forever
 *     - A smooth transition band around the waterline instead of a hard
 *       on/off cutoff, so points don't "step" as they cross the surface
 *     - Correct real-world force scaling: ForceMode.Force everywhere, so
 *       values are actual Newtons and scale properly with Rigidbody.mass
 *       (NOT ForceMode.Acceleration, which silently multiplies by mass
 *       internally and produces wildly oversized torque at off-center
 *       points - the cause of runaway spinning if used here)
 *     - Explicit centerOfMass / inertia tensor derived directly from the
 *       floating points (+ optional mast mass point), instead of Unity's
 *       automatic calculation from collider shape - so torque response
 *       (heel, pitch, yaw) doesn't depend on hull collider geometry, only
 *       on the floating points the user places on any body
 *     - Linear/angular drag is intentionally NOT handled here - it's owned
 *       by the movement system (anisotropic frontal/lateral hydrodynamic
 *       drag), to avoid two disconnected drag sources fighting each other
 *     - Rigidbody interpolation enabled, so the boat looks smooth on
 *       screen even between physics steps
 *     - A Console log (on Start) suggesting a sane starting value for
 *       buoyancyForcePerDepth/maxBuoyancyForce/buoyancyDamping based on
 *       the Rigidbody's real mass and your desired draft, so tuning starts
 *       from a physically reasonable number instead of trial and error
 *   
 * Requirements (must be enabled, or ProjectPointOnWaterSurface always fails):
 *   1. HDRP Asset -> Lighting -> Water -> "Script Interactions" enabled.
 *   2. WaterSurface component -> Script Interactions -> CPU Simulation ON.
 *   3. Water active in the scene (Volume with Water Rendering override).
 *   
 * Usage:
 *   Attach to the boat's GameObject (needs a Rigidbody with a real mass
 *   and a collider matching the hull shape - a convex Mesh Collider of the
 *   hull works well). Add empty child Transforms at the hull's floating
 *   points (e.g. bow, stern, 4 sides) and assign them below. Assign the
 *   scene's WaterSurface. Set centerOfMassLocal to match your model's real
 *   center of mass.
 *   
 * Version: 4.0 (final, phases removed)
 * Target: Unity 6.5 / HDRP
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Rigidbody))]
public class BuoyancySystem : MonoBehaviour
{
    [Header("HDRP Water Surface")]
    [Tooltip("The HDRP WaterSurface used as the reference for buoyancy. Must have 'CPU Simulation' enabled in its inspector.")]
    public WaterSurface waterSurface;

    [Tooltip("Max iterations used by the iterative search that projects a point onto the water surface.")]
    public int searchMaxIterations = 8;

    [Tooltip("Acceptable error (in meters) for the water height search to stop iterating.")]
    public float searchError = 0.01f;

    [Header("Buoyancy Settings (Newtons - ForceMode.Force)")]
    [Tooltip("Spring constant 'k', in N per meter of submersion, PER POINT. Check the Console on Start/Play for a suggested value based on your Rigidbody's mass and desiredDraftMeters below.")]
    public float buoyancyForcePerDepth = 5000.0f;

    [Tooltip("Maximum buoyancy force per point, in Newtons. Should have headroom above the force at your desired draft, so deep submersion doesn't clip too early.")]
    public float maxBuoyancyForce = 15000.0f;

    [Tooltip("Damping coefficient 'c', in N per (m/s), applied against each point's own vertical velocity while submerged. This is what stops the spring from oscillating - start from the Console suggestion and raise gradually.")]
    public float buoyancyDamping = 800f;

    [Header("Auto-Suggestion Helper")]
    [Tooltip("The depth (meters) you want each floating point submerged at rest. Used only to print a suggested buoyancyForcePerDepth/maxBuoyancyForce/buoyancyDamping to the Console on Start - doesn't affect behavior by itself.")]
    public float desiredDraftMeters = 1.0f;

    [Header("Smooth Transition")]
    [Tooltip("Vertical distance (meters) around the water line over which force and drag ramp in smoothly instead of switching abruptly as a point crosses the surface.")]
    public float surfaceTransitionBand = 0.2f;

    [Header("Floating Points")]
    [Tooltip("Transforms at the hull's floating points (e.g. bow, stern, sides).")]
    public Transform[] floatingPoints;

    [Header("Center of Mass")]
    [Tooltip("Local-space center of mass to force on the Rigidbody. Unity's automatic calculation from colliders is often wrong for hull shapes and causes uneven torque across floating points - this overrides it explicitly.")]
    public bool overrideCenterOfMass = true;
    public Vector3 centerOfMassLocal = new Vector3(0f, -2.75f, 0f);

    [Header("Tensor de Inércia (derivado dos pontos, sem depender do Collider)")]
    [Tooltip("Ponto de massa concentrada opcional (ex: mastro/verga) - contribui pro tensor de inércia com massa e posição reais, sem depender de collider. Deixe null se não quiser modelar isso.")]
    public Transform mastMassPoint;
    [Range(0f, 1f)]
    [Tooltip("Fração da massa total atribuída ao mastMassPoint (só usada se ele estiver atribuído).")]
    public float mastMassFraction = 0.08f;
    [Range(0f, 0.9f)]
    [Tooltip("Fração da massa restante tratada como concentrada perto do centro de massa (quilha/lastro/motor) - não contribui pro tensor (r≈0), só reduz a massa distribuída nos floatingPoints. 0 = toda a massa na borda (modelo tipo 'aro', superestima a inércia rotacional/deixa o barco mais difícil de girar). Ajuste livre - não precisa ser fisicamente exato, só serve pra calibrar 'o quão fácil o barco gira' sem depender de collider.")]
    public float coreMassFraction = 0.3f;

    [Header("Rigidbody Smoothing")]
    [Tooltip("Smooths the VISUAL motion between physics steps. Without this the boat can look jittery even when the physics itself is stable.")]
    public bool useInterpolation = true;

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public float gizmoRadius = 0.15f;


    private Rigidbody rigidBody;

    private WaterSearchParameters[] searchParams;
    private WaterSearchResult[] searchResults;


    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();

        if (floatingPoints == null || floatingPoints.Length == 0)
            Debug.LogWarning($"{gameObject.name}: FloaterHDRP has no floating points assigned! Buoyancy will not work.");

        if (waterSurface == null)
            Debug.LogWarning($"{gameObject.name}: FloaterHDRP has no WaterSurface assigned! Buoyancy will not work.");

        int count = floatingPoints != null ? floatingPoints.Length : 0;
        searchParams = new WaterSearchParameters[count];
        searchResults = new WaterSearchResult[count];
        for (int i = 0; i < count; i++)
        {
            searchParams[i] = new WaterSearchParameters();
            searchResults[i] = new WaterSearchResult();
        }

        if (overrideCenterOfMass)
        {
            rigidBody.automaticCenterOfMass = false;
            rigidBody.centerOfMass = centerOfMassLocal;
            CalculateInertiaTensorFromPoints(); // precisa do centerOfMass já definido acima
        }

        if (useInterpolation)
        {
            rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }

        LogSuggestedValues();
    }

    // Prints a starting-point suggestion for buoyancyForcePerDepth,
    // maxBuoyancyForce and buoyancyDamping based on the Rigidbody's real
    // mass, gravity, and floating point count. A sane starting point, not
    // a final answer - real hulls aren't evenly-distributed cylinders, so
    // you'll still tune from here.
    private void LogSuggestedValues()
    {
        if (floatingPoints == null || floatingPoints.Length == 0) return;

        float g = Mathf.Abs(Physics.gravity.y);
        float weight = rigidBody.mass * g;
        int n = floatingPoints.Length;

        float suggestedK = weight / (n * Mathf.Max(desiredDraftMeters, 0.01f));
        float effectiveMassPerPoint = rigidBody.mass / n;
        float suggestedDampingCritical = 2f * Mathf.Sqrt(suggestedK * effectiveMassPerPoint);

        Debug.Log(
            $"[FloaterHDRP] mass={rigidBody.mass}kg, weight={weight:F0}N, {n} points, " +
            $"desired draft={desiredDraftMeters}m -> suggested buoyancyForcePerDepth ~= {suggestedK:F0} N/m per point. " +
            $"Suggested maxBuoyancyForce ~= {suggestedK * desiredDraftMeters * 3f:F0} N (3x headroom). " +
            $"Suggested buoyancyDamping to start from ~= {suggestedDampingCritical * 0.15f:F0} " +
            $"(critical estimate was {suggestedDampingCritical:F0} - start well under it and raise gradually)."
        );
    }

    // Deriva o tensor de inércia inteiramente dos floatingPoints (+ mastMassPoint
    // opcional), sem depender de collider/forma. Massa uniforme entre os
    // floatingPoints (sem pesar por distância - isso duplicaria o r² que a
    // própria fórmula de inércia já aplica). O coreMassFraction simula a massa
    // que fica concentrada perto do centro (quilha/lastro), reduzindo a
    // superestimativa de inércia que uma distribuição só-na-borda causaria.
    // Chamado em Start(), DEPOIS de rigidBody.centerOfMass já estar definido.
    private void CalculateInertiaTensorFromPoints()
    {
        if (floatingPoints == null || floatingPoints.Length == 0)
        {
            Debug.LogWarning($"{gameObject.name}: sem floatingPoints para derivar o tensor de inércia. Mantendo automaticInertiaTensor.");
            return;
        }

        float mastFraction = mastMassPoint != null ? Mathf.Clamp01(mastMassFraction) : 0f;
        float remainingMass = rigidBody.mass * (1f - mastFraction) * (1f - Mathf.Clamp01(coreMassFraction));

        int validPointCount = 0;
        foreach (var p in floatingPoints) if (p != null) validPointCount++;
        if (validPointCount == 0) return;

        float massPerPoint = remainingMass / validPointCount;

        float Ipitch = 0f, Iyaw = 0f, Iroll = 0f; // eixos Unity: X=pitch, Y=yaw, Z=roll

        foreach (var p in floatingPoints)
        {
            if (p == null) continue;
            Vector3 r = transform.InverseTransformPoint(p.position) - centerOfMassLocal;
            Ipitch += massPerPoint * (r.y * r.y + r.z * r.z);
            Iyaw   += massPerPoint * (r.x * r.x + r.z * r.z);
            Iroll  += massPerPoint * (r.x * r.x + r.y * r.y);
        }

        // Massa do mastro: concentrada, posição real, contribui diretamente.
        if (mastMassPoint != null)
        {
            float mastMass = rigidBody.mass * mastFraction;
            Vector3 r = transform.InverseTransformPoint(mastMassPoint.position) - centerOfMassLocal;
            Ipitch += mastMass * (r.y * r.y + r.z * r.z);
            Iyaw   += mastMass * (r.x * r.x + r.z * r.z);
            Iroll  += mastMass * (r.x * r.x + r.y * r.y);
        }

        // coreMassFraction: massa perto de r≈0, contribui ~0 pro tensor -
        // já foi retirada de remainingMass acima, então não precisa de termo aqui.

        rigidBody.automaticInertiaTensor = false;
        rigidBody.inertiaTensorRotation = Quaternion.identity;
        rigidBody.inertiaTensor = new Vector3(Ipitch, Iyaw, Iroll);

        Debug.Log(
            $"[BuoyancySystem] Tensor de inércia derivado: pitch={Ipitch:F0}, yaw={Iyaw:F0}, roll={Iroll:F0} kg·m² " +
            $"({validPointCount} pontos de flutuação, mastro={(mastMassPoint != null ? $"{mastFraction:P0}" : "não usado")}, " +
            $"core={coreMassFraction:P0})."
        );
    }

    private void FixedUpdate()
    {
        ApplyBuoyancy();
    }

    private void ApplyBuoyancy()
    {
        if (waterSurface == null || floatingPoints == null) return;

        for (int i = 0; i < floatingPoints.Length; i++)
        {
            Transform point = floatingPoints[i];
            if (point == null) continue;

            Vector3 pointPosition = point.position;
            if (!TryGetWaterHeight(i, pointPosition, out float waterHeight)) continue;

            float depth = waterHeight - pointPosition.y;
            
            float submersionFactor = Mathf.Clamp01((depth + surfaceTransitionBand * 0.5f) / surfaceTransitionBand);

            if (submersionFactor <= 0f) continue;
           
            float springForce = Mathf.Min(Mathf.Max(depth, 0f) * buoyancyForcePerDepth, maxBuoyancyForce);
            
            float verticalVelocity = rigidBody.GetPointVelocity(pointPosition).y;
            float dampingForce = -verticalVelocity * buoyancyDamping;
            
            float totalForce = Mathf.Clamp((springForce + dampingForce) * submersionFactor, -maxBuoyancyForce, maxBuoyancyForce);
            
            rigidBody.AddForceAtPosition(Vector3.up * totalForce, pointPosition, ForceMode.Force);
        }
    }

    private bool TryGetWaterHeight(int index, Vector3 worldPosition, out float waterHeight)
    {
        waterHeight = 0f;
        if (waterSurface == null) return false;

        ref WaterSearchParameters sp = ref searchParams[index];
        ref WaterSearchResult sr = ref searchResults[index];

        // Start from the previous frame's candidate location - converges
        // faster than starting from scratch every frame.
        sp.startPositionWS = sr.candidateLocationWS;
        sp.targetPositionWS = worldPosition;
        sp.error = searchError;
        sp.maxIterations = searchMaxIterations;

        if (waterSurface.ProjectPointOnWaterSurface(sp, out sr))
        {
            waterHeight = sr.projectedPositionWS.y;
            return true;
        }
        return false;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || waterSurface == null || floatingPoints == null) return;

        WaterSearchParameters gp = new WaterSearchParameters();
        WaterSearchResult gr = new WaterSearchResult();

        foreach (Transform point in floatingPoints)
        {
            if (point == null) continue;

            Vector3 pos = point.position;
            gp.startPositionWS = gr.candidateLocationWS;
            gp.targetPositionWS = pos;
            gp.error = searchError;
            gp.maxIterations = searchMaxIterations;

            if (!waterSurface.ProjectPointOnWaterSurface(gp, out gr)) continue;

            float waterHeight = gr.projectedPositionWS.y;
            float depth = waterHeight - pos.y;

            Gizmos.color = depth > 0 ? Color.red : Color.green;
            Gizmos.DrawSphere(pos, gizmoRadius);

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(pos, new Vector3(pos.x, waterHeight, pos.z));
        }

        if (overrideCenterOfMass)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(centerOfMassLocal), gizmoRadius * 1.5f);
        }
    }   
}