/******************************************************************************
 * FloaterHDRP_Phases.cs
 * 
 * Description:
 *   Diagnostic version of the HDRP-based Floater. Implements buoyancy in
 *   incremental phases, selectable in the Inspector, so you can isolate
 *   exactly which ingredient fixes the jitter/trepidação on your boat.
 *   
 *   Phase 0 - No buoyancy at all. The boat should just sink through the
 *             water surface. This confirms your WaterSurface reference,
 *             floating points, and Rigidbody are wired up correctly
 *             before any force is involved.
 *   
 *   Phase 1 - Hard-threshold spring (same approach as the original
 *             Floater/FloaterHDRP script). Force = depth * k, clamped,
 *             drag switches on/off abruptly. The boat floats, but this
 *             is the phase most likely to show trepidação: it's a pure
 *             spring with no damping and hard on/off transitions.
 *   
 *   Phase 2 - Adds a damped spring: force = depth * k - verticalVel * c.
 *             This alone usually removes most of the bouncing/jitter,
 *             because the boat stops overshooting the water line.
 *   
 *   Phase 3 - Adds a smooth transition band around the water line instead
 *             of a hard depth > 0 cutoff, and smooths the drag/angularDrag
 *             change (based on how much of the boat is submerged) instead
 *             of switching it in a single frame. This removes the small
 *             "steps" that happen when a point crosses the surface.
 *   
 *   Phase 4 - Full setup: everything above, plus an explicit centerOfMass
 *             override (important for your rig: points at y=-2.5, real
 *             center of mass at y=-2.75) and Rigidbody interpolation
 *             enabled at runtime. This is the recommended end state.
 *   
 * Requirements (same as before):
 *   1. HDRP Asset -> Lighting -> Water -> "Script Interactions" enabled.
 *   2. WaterSurface component -> Script Interactions -> CPU Simulation ON.
 *   3. Water active in the scene.
 *   
 * Version: 1.0 (phased diagnostic build)
 * Target: Unity 6.5 / HDRP
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

[RequireComponent(typeof(Rigidbody))]
public class FloaterHDRP_Phases : MonoBehaviour
{
    public enum BuoyancyPhase
    {
        Phase0_NoBuoyancy_JustSinks,
        Phase1_HardThresholdSpring,
        Phase2_DampedSpring,
        Phase3_SmoothTransition,
        Phase4_FullSetup
    }

    [Header("Diagnostic Phase")]
    [Tooltip("Switch this in Play Mode to compare behaviors directly.")]
    public BuoyancyPhase phase = BuoyancyPhase.Phase0_NoBuoyancy_JustSinks;

    [Header("HDRP Water Surface")]
    public WaterSurface waterSurface;
    public int searchMaxIterations = 8;
    public float searchError = 0.01f;

    [Header("Buoyancy Settings")]
    [Tooltip("With mass=3000 and ForceMode.Acceleration, your '50'/'100' values were actually being multiplied by mass internally (Unity does this to make Acceleration mode mass-independent), producing hundreds of thousands of Newtons of force and torque - that's what caused both the spin and the shaking. This script now uses ForceMode.Force (real Newtons), which scales correctly with your Rigidbody's actual mass. Use 'desiredDraftMeters' below to auto-calculate a sane starting value - check the Console on Play.")]
    public float buoyancyForcePerDepth = 5000.0f;

    [Tooltip("Maximum buoyancy force PER POINT, in Newtons. Should be a few times larger than the value suggested in the Console so deep submersion doesn't clip too early.")]
    public float maxBuoyancyForce = 15000.0f;

    [Header("Auto-Suggestion Helper")]
    [Tooltip("The depth (in meters) you want each floating point submerged at rest, e.g. how deep the hull should sit in the water. Used only to print a suggested buoyancyForcePerDepth to the Console on Start - it does not change behavior by itself.")]
    public float desiredDraftMeters = 1.0f;

    [Header("Damping (Phase 2+)")]
    [Tooltip("Damping coefficient 'c' in N per (m/s), applied against each point's vertical velocity while submerged. Now that forces are real Newtons (ForceMode.Force), this needs to scale with mass too. A rough starting point is printed to the Console on Start alongside the force suggestion - start there and adjust in small steps.")]
    public float buoyancyDamping = 800f;

    [Header("Smooth Transition (Phase 3+)")]
    [Tooltip("Vertical distance (meters) around the water line over which force and drag ramp in smoothly instead of switching abruptly. Try 0.15-0.3 for a boat this size.")]
    public float surfaceTransitionBand = 0.2f;

    [Header("Floating Points")]
    [Tooltip("6 points: 1 front, 1 back, 4 sides, at local Y = -2.5 for your boat.")]
    public Transform[] floatingPoints;

    [Header("Water Resistance")]
    public float waterDrag = 3.0f;
    public float waterAngularDrag = 1.0f;

    [Header("Center of Mass Override (Phase 4)")]
    [Tooltip("Your real center of mass is at local Y = -2.75. Leaving this unset lets Unity guess from colliders, which is often wrong and causes uneven rocking across the 6 points.")]
    public bool overrideCenterOfMass = true;
    public Vector3 centerOfMassLocal = new Vector3(0f, -2.75f, 0f);

    [Header("Rigidbody Smoothing (Phase 4)")]
    [Tooltip("Interpolation smooths the VISUAL motion between physics steps. Without this, the boat can look jittery even when the physics itself is stable.")]
    public bool useInterpolation = true;

    [Header("Debug Visualization")]
    public bool showGizmos = true;
    public float gizmoRadius = 0.15f;


    private Rigidbody rigidBody;
    private float originalDrag;
    private float originalAngularDrag;

    private WaterSearchParameters[] searchParams;
    private WaterSearchResult[] searchResults;

    // Smoothed 0-1 "how much of the boat is in water" factor, used to blend
    // drag instead of switching it, avoiding another source of stepping.
    private float smoothedWaterFactor;


    private void Start()
    {
        rigidBody = GetComponent<Rigidbody>();
        originalDrag = rigidBody.linearDamping;
        originalAngularDrag = rigidBody.angularDamping;

        if (floatingPoints == null || floatingPoints.Length == 0)
            Debug.LogWarning($"{gameObject.name}: No floating points assigned!");

        if (waterSurface == null)
            Debug.LogWarning($"{gameObject.name}: No WaterSurface assigned!");

        int count = floatingPoints != null ? floatingPoints.Length : 0;
        searchParams = new WaterSearchParameters[count];
        searchResults = new WaterSearchResult[count];
        for (int i = 0; i < count; i++)
        {
            searchParams[i] = new WaterSearchParameters();
            searchResults[i] = new WaterSearchResult();
        }

        ApplyPhaseSetup();
        LogSuggestedValues();
    }

    // Prints a starting-point suggestion for buoyancyForcePerDepth and
    // buoyancyDamping based on the Rigidbody's real mass, gravity, and how
    // many floating points you have. This is just a sane starting point -
    // real hull shapes are not evenly distributed cylinders, so you will
    // still need to tune from here, but it saves you from guessing blindly
    // like 1, 15, 50, 100 (none of which relate to a 3000kg mass).
    private void LogSuggestedValues()
    {
        if (floatingPoints == null || floatingPoints.Length == 0) return;

        float g = Mathf.Abs(Physics.gravity.y);
        float weight = rigidBody.mass * g; // total force needed to support the boat, in Newtons
        int n = floatingPoints.Length;

        // At equilibrium, sum of (depth * k) across all submerged points
        // should equal the boat's weight. Assuming all points reach
        // "desiredDraftMeters" at rest: n * desiredDraftMeters * k = weight
        float suggestedK = weight / (n * Mathf.Max(desiredDraftMeters, 0.01f));

        // Critical-damping-ish estimate treating each point as carrying an
        // even share of the total mass (a simplification, not exact for an
        // irregular hull, but a reasonable starting order of magnitude).
        float effectiveMassPerPoint = rigidBody.mass / n;
        float suggestedDampingCritical = 2f * Mathf.Sqrt(suggestedK * effectiveMassPerPoint);

        Debug.Log(
            $"[FloaterHDRP_Phases] mass={rigidBody.mass}kg, weight={weight:F0}N, {n} points, " +
            $"desired draft={desiredDraftMeters}m -> suggested buoyancyForcePerDepth ~= {suggestedK:F0} N/m per point. " +
            $"Suggested maxBuoyancyForce ~= {suggestedK * desiredDraftMeters * 3f:F0} N (3x the force at target draft, for headroom). " +
            $"Suggested buoyancyDamping to START from (well under critical, increase gradually) ~= {suggestedDampingCritical * 0.15f:F0} " +
            $"(critical estimate was {suggestedDampingCritical:F0}, but start much lower than critical and raise it)."
        );
    }
    // Center of mass and interpolation only need to be applied once, but
    // exposed here as a distinct step so you can see their effect in
    // isolation by toggling "overrideCenterOfMass" / "useInterpolation".
    private void ApplyPhaseSetup()
    {
        if (phase == BuoyancyPhase.Phase4_FullSetup && overrideCenterOfMass)
        {
            // Setting centerOfMass alone is not enough if "Automatic Center
            // of Mass" is still checked in the Inspector - Unity will keep
            // recomputing it from the colliders and override this value.
            // Same logic applies to the inertia tensor: if it's auto and
            // doesn't match the real hull shape, off-center forces (bow/
            // stern points) produce disproportionate angular acceleration,
            // which is almost certainly what caused the spinning you saw.
            rigidBody.automaticCenterOfMass = false;
            rigidBody.centerOfMass = centerOfMassLocal;

            rigidBody.automaticInertiaTensor = false;
        }

        if (phase == BuoyancyPhase.Phase4_FullSetup && useInterpolation)
        {
            rigidBody.interpolation = RigidbodyInterpolation.Interpolate;
        }
    }

    private void FixedUpdate()
    {
        switch (phase)
        {
            case BuoyancyPhase.Phase0_NoBuoyancy_JustSinks:
                // Intentionally does nothing. Confirm the boat sinks
                // realistically (no random spinning/jitter from something
                // else in the rig) before adding any force.
                break;

            case BuoyancyPhase.Phase1_HardThresholdSpring:
                ApplyHardThresholdBuoyancy();
                break;

            case BuoyancyPhase.Phase2_DampedSpring:
                ApplyDampedBuoyancy(smoothTransition: false);
                break;

            case BuoyancyPhase.Phase3_SmoothTransition:
            case BuoyancyPhase.Phase4_FullSetup:
                ApplyDampedBuoyancy(smoothTransition: true);
                break;
        }
    }

    // ------------------------------------------------------------------
    // PHASE 1: original approach - pure spring, hard on/off.
    // Expect visible bouncing/jitter here, especially at rest.
    // ------------------------------------------------------------------
    private void ApplyHardThresholdBuoyancy()
    {
        int submergedCount = 0;

        for (int i = 0; i < floatingPoints.Length; i++)
        {
            Transform point = floatingPoints[i];
            if (point == null) continue;

            Vector3 pointPosition = point.position;
            if (!TryGetWaterHeight(i, pointPosition, out float waterHeight)) continue;

            float depth = waterHeight - pointPosition.y;
            if (depth > 0)
            {
                submergedCount++;
                float force = Mathf.Min(depth * buoyancyForcePerDepth, maxBuoyancyForce);
                rigidBody.AddForceAtPosition(Vector3.up * force, pointPosition, ForceMode.Force);
            }
        }

        // Hard drag switch - another source of the "stepping" feel.
        if (submergedCount > 0)
        {
            rigidBody.linearDamping = waterDrag;
            rigidBody.angularDamping = waterAngularDrag;
        }
        else
        {
            rigidBody.linearDamping = originalDrag;
            rigidBody.angularDamping = originalAngularDrag;
        }
    }

    // ------------------------------------------------------------------
    // PHASE 2/3/4: damped spring, optionally with a smooth transition
    // band around the surface instead of a hard depth > 0 cutoff.
    // ------------------------------------------------------------------
    private void ApplyDampedBuoyancy(bool smoothTransition)
    {
        float totalWaterFactor = 0f;
        int validPoints = 0;

        for (int i = 0; i < floatingPoints.Length; i++)
        {
            Transform point = floatingPoints[i];
            if (point == null) continue;

            Vector3 pointPosition = point.position;
            if (!TryGetWaterHeight(i, pointPosition, out float waterHeight)) continue;

            validPoints++;
            float depth = waterHeight - pointPosition.y;

            // 0-1 factor describing how "in water" this point is.
            // Without smoothTransition, this behaves like a hard step
            // (0 or 1). With it, it ramps over surfaceTransitionBand.
            float submersionFactor = smoothTransition
                ? Mathf.Clamp01((depth + surfaceTransitionBand * 0.5f) / surfaceTransitionBand)
                : (depth > 0f ? 1f : 0f);

            totalWaterFactor += submersionFactor;

            if (submersionFactor <= 0f) continue;

            // Spring term (same as before, clamped so it can't explode).
            float springForce = Mathf.Min(Mathf.Max(depth, 0f) * buoyancyForcePerDepth, maxBuoyancyForce);

            // Damper term: oppose the point's own vertical velocity.
            // This is what actually kills the oscillation - a spring
            // alone will always overshoot and bounce back.
            Vector3 pointVelocity = rigidBody.GetPointVelocity(pointPosition);
            float verticalVelocity = pointVelocity.y;
            float dampingForce = -verticalVelocity * buoyancyDamping;

            // IMPORTANT: clamp the TOTAL force (spring + damping), not just
            // the spring term. Without this, a high-velocity moment (like
            // the fast oscillation you saw in Phase 1) produces a damping
            // spike far larger than maxBuoyancyForce, which - applied at an
            // off-center point like the bow or stern - creates a runaway
            // torque and spins the boat.
            float totalForce = (springForce + dampingForce) * submersionFactor;
            totalForce = Mathf.Clamp(totalForce, -maxBuoyancyForce, maxBuoyancyForce);

            rigidBody.AddForceAtPosition(Vector3.up * totalForce, pointPosition, ForceMode.Force);
        }

        // Smooth drag blend instead of an instant switch. Also smoothed
        // over time (Lerp) so a single point flickering across the
        // transition band doesn't itself cause a small step.
        float targetWaterFactor = validPoints > 0 ? totalWaterFactor / validPoints : 0f;
        smoothedWaterFactor = Mathf.Lerp(smoothedWaterFactor, targetWaterFactor, 0.5f);

        rigidBody.linearDamping = Mathf.Lerp(originalDrag, waterDrag, smoothedWaterFactor);
        rigidBody.angularDamping = Mathf.Lerp(originalAngularDrag, waterAngularDrag, smoothedWaterFactor);
    }

    private bool TryGetWaterHeight(int index, Vector3 worldPosition, out float waterHeight)
    {
        waterHeight = 0f;
        if (waterSurface == null) return false;

        ref WaterSearchParameters sp = ref searchParams[index];
        ref WaterSearchResult sr = ref searchResults[index];

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

    #region Debug Visualization
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

        // Draw the assumed/overridden center of mass for reference.
        if (overrideCenterOfMass)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.TransformPoint(centerOfMassLocal), gizmoRadius * 1.5f);
        }
    }
    #endregion
}