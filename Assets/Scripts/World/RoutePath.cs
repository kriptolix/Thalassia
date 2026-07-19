using UnityEngine;

public class RoutePath : MonoBehaviour
{
    [Header("Identificação")]
    public string routeId = "Route_A";

    [Header("Visual")]
    public Color lineColor = Color.cyan;

    private void OnDrawGizmos()
    {
        Gizmos.color = lineColor;

        // Pega todos os filhos deste objeto (os waypoints) na ordem em que aparecem na Hierarchy
        Transform[] waypoints = new Transform[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            waypoints[i] = transform.GetChild(i);
        }

        // Desenha uma linha conectando cada waypoint ao próximo
        for (int i = 0; i < waypoints.Length - 1; i++)
        {
            Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
            Gizmos.DrawSphere(waypoints[i].position, 30f); // bolinha em cada ponto
        }

        // Desenha a bolinha do último ponto também
        if (waypoints.Length > 0)
            Gizmos.DrawSphere(waypoints[waypoints.Length - 1].position, 30f);
    }
}