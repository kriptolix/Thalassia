using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Espuma de ESTEIRA (wake foam) local do casco, usando o WaterFoamGenerator
/// nativo do Water System da HDRP.
///
/// IMPORTANTE - isso NÃO é o mesmo que o script de exemplo original
/// (WaterSurface.GetFoamBuffer): aquele método faz uma LEITURA do buffer de
/// espuma já existente na superfície (útil pra sampleá-lo em outro material/
/// shader), mas não ADICIONA espuma nova em lugar nenhum. Quem efetivamente
/// injeta espuma localmente na água é o componente WaterFoamGenerator (o
/// mesmo usado nas cenas de exemplo da Unity para espuma de esteira de barco
/// e de arrebentação em praia) - é isso que este script configura e anima.
///
/// COMO USAR: adicionar este componente no MESMO GameObject do mesh do
/// casco (ou informar manualmente um Renderer/MeshFilter de outro objeto em
/// hullRenderer). O WaterFoamGenerator é adicionado automaticamente
/// (RequireComponent) e se comporta como um decal: a região de espuma
/// acompanha a posição/rotação deste Transform sozinha, frame a frame, sem
/// nenhum código extra pra "seguir" o casco - por isso este script só
/// precisa (1) dimensionar a região uma vez a partir do bounds do mesh e (2)
/// modular a intensidade da espuma conforme a velocidade do barco.
///
/// Requer o pacote HDRP com o Water System habilitado (Water Decals/Water
/// Foam habilitados no HDRP Asset) e uma WaterSurface na cena - o
/// WaterFoamGenerator não referencia a WaterSurface diretamente, ele afeta
/// qualquer superfície de água compatível dentro da sua região.
/// </summary>
[RequireComponent(typeof(WaterFoamGenerator))]
public class ShipWakeFoam : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Usado só para AUTO-DIMENSIONAR a região de espuma a partir do bounds local do mesh (largura/comprimento do casco). Se vazio, tenta pegar um Renderer neste GameObject ou nos filhos automaticamente no Awake.")]
    [SerializeField] private Renderer hullRenderer;
    [Tooltip("Opcional - se atribuído, a intensidade da espuma escala com a velocidade do barco (CurrentSpeed, m/s). Se vazio, tenta encontrar um ShipMovementSystem num objeto pai automaticamente; se ainda assim não achar, usa intensidade fixa (maxSurfaceFoamDimmer/maxDeepFoamDimmer).")]
    [SerializeField] private ShipMovementSystem shipMovement;

    [Header("Dimensionamento (automático, a partir do mesh do casco)")]
    [Tooltip("Multiplicador aplicado ao comprimento do casco (eixo Z local) para estender a região PARA TRÁS, cobrindo a esteira além da popa - sem isso a região cobriria só o footprint do próprio casco, e a esteira nunca apareceria atrás do barco.")]
    [SerializeField] private float wakeLengthMultiplier = 2.5f;
    [Tooltip("Multiplicador aplicado à largura do casco (eixo X local). 1 = largura real do casco: normalmente já é suficiente, aumente só se quiser uma esteira visivelmente mais larga que o próprio casco.")]
    [SerializeField] private float wakeWidthMultiplier = 1.2f;
    [Tooltip("ASSUMIDO: eixo local considerado 'comprimento' (proa-popa) do casco, usado para calcular o tamanho e o alongamento da esteira para trás. Troque se o seu mesh usar outro eixo como frente.")]
    [SerializeField] private Axis hullForwardAxis = Axis.Z;

    [Header("Resposta à Velocidade do Barco")]
    [Tooltip("Velocidade (m/s) na qual a espuma atinge a intensidade MÁXIMA (maxSurfaceFoamDimmer/maxDeepFoamDimmer). Abaixo disso, a intensidade cai proporcionalmente até minFoamAtZeroSpeed.")]
    [SerializeField] private float speedForMaxFoam = 8f;
    [Tooltip("Intensidade mínima (fração de max*Dimmer) mesmo com o barco parado - um casco grande sempre desloca alguma água. 0 = espuma some completamente parado.")]
    [Range(0f, 1f)]
    [SerializeField] private float minFoamAtZeroSpeed = 0f;
    [Tooltip("Constante de tempo (segundos) de suavização da intensidade - evita que a espuma 'pisque' com pequenas variações de velocidade (rajadas, ondulação). Não afeta o tempo de reação a mudanças reais e sustentadas de velocidade.")]
    [SerializeField] private float foamSmoothingSeconds = 0.4f;

    [Header("Intensidade Máxima (tetos)")]
    [Tooltip("Teto de surfaceFoamDimmer na velocidade máxima (speedForMaxFoam) - equivale ao valor que você configuraria direto no WaterFoamGenerator, mas escalado dinamicamente pela velocidade em vez de fixo.")]
    [Range(0f, 1f)]
    [SerializeField] private float maxSurfaceFoamDimmer = 1f;
    [Tooltip("Teto de deepFoamDimmer na velocidade máxima (speedForMaxFoam).")]
    [Range(0f, 1f)]
    [SerializeField] private float maxDeepFoamDimmer = 1f;

    private enum Axis { X, Z }

    private WaterFoamGenerator _foamGenerator;
    private float _smoothedIntensity;

    private void Awake()
    {
        _foamGenerator = GetComponent<WaterFoamGenerator>();
        _foamGenerator.type = WaterFoamGeneratorType.Rectangle;
        // ScaleInvariant: o regionSize já é calculado em METROS reais a
        // partir do bounds do mesh - não queremos que a escala do Transform
        // (ex.: se o casco pai tiver uma escala != 1) distorça isso de novo.
        _foamGenerator.scaleMode = DecalScaleMode.ScaleInvariant;

        if (hullRenderer == null) hullRenderer = GetComponent<Renderer>();
        if (hullRenderer == null) hullRenderer = GetComponentInChildren<Renderer>();

        if (shipMovement == null) shipMovement = GetComponentInParent<ShipMovementSystem>();
    }

    private void Start()
    {
        AutoSizeFromHullBounds();
    }

    /// <summary>
    /// Recalcula regionSize a partir do bounds LOCAL do mesh do casco
    /// (MeshFilter.sharedMesh.bounds se disponível - bounds puro do mesh,
    /// não afetado pela rotação/heel atual do barco; cai para
    /// hullRenderer.bounds convertido pra espaço local só como fallback,
    /// caso não haja MeshFilter, ex.: SkinnedMeshRenderer). Pode ser
    /// chamado de novo manualmente (ex.: pelo menu de contexto) se o mesh
    /// do casco mudar em runtime.
    /// </summary>
    [ContextMenu("Recalcular Tamanho a partir do Casco")]
    public void AutoSizeFromHullBounds()
    {
        if (_foamGenerator == null) _foamGenerator = GetComponent<WaterFoamGenerator>();

        if (hullRenderer == null)
        {
            Debug.LogWarning($"{nameof(ShipWakeFoam)}: nenhum Renderer encontrado (neste objeto ou nos filhos) para auto-dimensionar a região de espuma - configure regionSize manualmente no WaterFoamGenerator.");
            return;
        }

        Vector3 localSize = GetLocalMeshSize(hullRenderer);

        float hullWidth = hullForwardAxis == Axis.Z ? localSize.x : localSize.z;
        float hullLength = hullForwardAxis == Axis.Z ? localSize.z : localSize.x;

        float regionWidth = hullWidth * wakeWidthMultiplier;
        float regionLength = hullLength * wakeLengthMultiplier;
        _foamGenerator.regionSize = new Vector2(regionWidth, regionLength);
    }

    // Bounds LOCAL do mesh (não world) - preferencialmente do MeshFilter
    // (sharedMesh.bounds, exato e independente da rotação/heel atual),
    // com fallback pro bounds WORLD do Renderer convertido pra espaço local
    // deste Transform (aproximação: correto só quando o casco não está
    // rotacionado/adernado no instante da medição).
    private Vector3 GetLocalMeshSize(Renderer rend)
    {
        MeshFilter meshFilter = rend.GetComponent<MeshFilter>();
        if (meshFilter != null && meshFilter.sharedMesh != null)
        {
            return meshFilter.sharedMesh.bounds.size;
        }

        Debug.LogWarning($"{nameof(ShipWakeFoam)}: sem MeshFilter em '{rend.name}' (ex.: SkinnedMeshRenderer) - usando Renderer.bounds (WORLD) como aproximação, só correta se o casco estiver sem rotação/heel no momento da medição.");
        Bounds worldBounds = rend.bounds;
        Vector3 halfExtentsLocal = transform.InverseTransformVector(worldBounds.extents);
        return new Vector3(Mathf.Abs(halfExtentsLocal.x), Mathf.Abs(halfExtentsLocal.y), Mathf.Abs(halfExtentsLocal.z)) * 2f;
    }

    private void Update()
    {
        float targetIntensity = minFoamAtZeroSpeed;

        if (shipMovement != null)
        {
            float speedRatio = Mathf.Clamp01(shipMovement.CurrentSpeed / Mathf.Max(0.01f, speedForMaxFoam));
            targetIntensity = Mathf.Lerp(minFoamAtZeroSpeed, 1f, speedRatio);
        }
        else
        {
            targetIntensity = 1f; // sem referência de velocidade - intensidade fixa no teto
        }

        float smoothingAlpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, foamSmoothingSeconds));
        _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, targetIntensity, smoothingAlpha);

        _foamGenerator.surfaceFoamDimmer = _smoothedIntensity * maxSurfaceFoamDimmer;
        _foamGenerator.deepFoamDimmer = _smoothedIntensity * maxDeepFoamDimmer;
    }

    private void OnValidate()
    {
        if (wakeLengthMultiplier < 0f) wakeLengthMultiplier = 0f;
        if (wakeWidthMultiplier < 0f) wakeWidthMultiplier = 0f;
        if (speedForMaxFoam < 0.01f) speedForMaxFoam = 0.01f;
        if (foamSmoothingSeconds < 0.01f) foamSmoothingSeconds = 0.01f;
    }
}