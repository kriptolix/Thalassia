using UnityEngine;

public class WorldGridGizmo : MonoBehaviour
{
    [Header("Configuração da Grade")]
    public int chunksX = 5;
    public int chunksY = 5;
    public float chunkSize = 5000f; // 5 km em metros

    [Header("Visual")]
    public Color lineColor = Color.yellow;
    public float heightOffset = 50f; // altura acima da água, só para visualização

    private void OnDrawGizmos()
    {
        Gizmos.color = lineColor;

        float totalWidth = chunksX * chunkSize;
        float totalDepth = chunksY * chunkSize;
        float y = heightOffset;

        for (int x = 0; x <= chunksX; x++)
        {
            Vector3 start = new Vector3(x * chunkSize, y, 0);
            Vector3 end = new Vector3(x * chunkSize, y, totalDepth);
            Gizmos.DrawLine(start, end);
        }

        for (int y2 = 0; y2 <= chunksY; y2++)
        {
            Vector3 start = new Vector3(0, y, y2 * chunkSize);
            Vector3 end = new Vector3(totalWidth, y, y2 * chunkSize);
            Gizmos.DrawLine(start, end);
        }
    }
}