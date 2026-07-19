using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class ShipBuoyancy : MonoBehaviour
{
    public WaterSurface waterSurface;
    public Transform[] floatPoints; // 4+ pontos nas quinas do casco
    public float buoyancyForce = 10f;
    public float damping = 1f;

    Rigidbody rb;
    WaterSearchParameters searchParams = new WaterSearchParameters();
    WaterSearchResult searchResult = new WaterSearchResult();

    void Start() => rb = GetComponent<Rigidbody>();

    void FixedUpdate()
    {
        if (waterSurface == null) return;

        foreach (var point in floatPoints)
        {
            searchParams.startPositionWS = point.position;
            searchParams.targetPositionWS = point.position;
            searchParams.error = 0.01f;
            searchParams.maxIterations = 8;

            if (waterSurface.ProjectPointOnWaterSurface(searchParams, out searchResult))
            {
                float submersion = searchResult.projectedPositionWS.y - point.position.y;
                if (submersion > 0)
                {
                    Vector3 force = Vector3.up * submersion * buoyancyForce;
                    rb.AddForceAtPosition(force, point.position, ForceMode.Acceleration);
                }
            }
        }

        // damping simples pra evitar oscilação infinita
        rb.linearVelocity *= (1f - damping * Time.fixedDeltaTime);
        rb.angularVelocity *= (1f - damping * Time.fixedDeltaTime);
    }
}