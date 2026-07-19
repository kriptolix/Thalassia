using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Representa um único Ponto de Flutuação do casco.
/// Não é um MonoBehaviour — é um contêiner de configuração + estado runtime,
/// referenciado pela lista de pontos do BoatFloater.
///
/// Os campos WaterSearchParameters/WaterSearchResult são reutilizados a cada
/// FixedUpdate (são structs, não geram alocação) para permitir que a busca
/// iterativa da altura da água comece a partir do último resultado conhecido,
/// como recomendado pela documentação da HDRP.
/// </summary>
[System.Serializable]
public class FloatPoint
{
    [Header("Referência")]
    [Tooltip("Transform do ponto de flutuação (filho da embarcação).")]
    public Transform point;

    [Header("Multiplicadores")]
    [Tooltip("Multiplicador individual de empuxo aplicado a este ponto.")]
    public float buoyancyMultiplier = 1f;

    [Tooltip("Multiplicador individual de amortecimento vertical aplicado a este ponto.")]
    public float dampingMultiplier = 1f;

    [Header("Submersão")]
    [Tooltip("Limite máximo de submersão específico deste ponto (metros). Se <= 0, usa o valor global (Max Submersion) do BoatFloater.")]
    public float maxSubmersionOverride = 0f;

    // ---------------------------------------------------------------------
    // Estado em tempo de execução — somente leitura, usado para gizmos/debug.
    // Não deve ser editado manualmente no Inspector.
    // ---------------------------------------------------------------------
    [System.NonSerialized] public bool isSubmerged;
    [System.NonSerialized] public float currentSubmersion;
    [System.NonSerialized] public float waterHeight;
    [System.NonSerialized] public Vector3 waterNormal = Vector3.up;
    [System.NonSerialized] public Vector3 appliedForce;
    [System.NonSerialized] public Vector3 worldPosition;

    // Aproximação (derivada numérica) da velocidade vertical da superfície.
    // A API do HDRP não expõe velocidade de superfície diretamente; isto é
    // apenas informativo (gizmo/debug) e NÃO participa do cálculo de força.
    [System.NonSerialized] public float estimatedSurfaceVelocity;
    [System.NonSerialized] public float previousWaterHeight;

    // Buffers de consulta à água — reutilizados a cada frame, sem alocação.
    [System.NonSerialized] public WaterSearchParameters searchParams;
    [System.NonSerialized] public WaterSearchResult searchResult;
    [System.NonSerialized] public bool hasSearchResult;
}