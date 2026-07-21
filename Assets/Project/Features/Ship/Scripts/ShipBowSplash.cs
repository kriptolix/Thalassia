using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Onda/espuma de PROA (bow splash), usando o mesmo componente "Water
/// Decal", com o material configurado com um deformador tipo Bow Wave.
///
/// amplitude/surfaceFoamDimmer/deepFoamDimmer são campos diretos do
/// componente (mesma confiança do ShipWakeFoam). A ELEVAÇÃO do Bow Wave,
/// porém, é um parâmetro dentro do MATERIAL (shader graph) - por isso ainda
/// precisa do nome da propriedade como string, com o mesmo aviso de sempre:
/// NÃO CONFIRMADO, é um palpite. Abra o Shader Graph do seu material,
/// confira o "Reference" da propriedade de Elevação no Blackboard, e
/// corrija o campo abaixo se vier diferente.
/// </summary>
[RequireComponent(typeof(WaterDecal))]
public class ShipBowSplash : MonoBehaviour
{
    [Header("Referência (opcional)")]
    [Tooltip("Se atribuído, a intensidade escala com a velocidade do barco (CurrentSpeed, m/s). Se vazio, tenta encontrar um ShipMovementSystem num objeto pai automaticamente; se ainda assim não achar, usa intensidade fixa nos tetos abaixo.")]
    [SerializeField] private ShipMovementSystem shipMovement;

    [Header("Propriedade de Elevação no material (Shader Graph) - CONFIRME")]
    [Tooltip("ASSUMIDO/NÃO CONFIRMADO - nome (Reference) da propriedade de Elevação do deformador Bow Wave, no Blackboard do seu Shader Graph.")]
    [SerializeField] private string elevationProperty = "_Elevation";

    [Header("Resposta à Velocidade do Barco")]
    [Tooltip("Velocidade (m/s) na qual o splash atinge a intensidade MÁXIMA. Tipicamente mais baixo que o da esteira - a proa já perturba água em velocidade moderada.")]
    [SerializeField] private float speedForMaxSplash = 5f;
    [Tooltip("Intensidade mínima (fração dos tetos) mesmo parado. 0 = recomendado (sem velocidade, não há proa 'batendo' em nada).")]
    [Range(0f, 1f)]
    [SerializeField] private float minSplashAtZeroSpeed = 0f;
    [Tooltip("Constante de tempo (segundos) de suavização.")]
    [SerializeField] private float splashSmoothingSeconds = 0.3f;

    [Header("Intensidade Máxima (tetos, na velocidade máxima)")]
    [Tooltip("Teto de amplitude (m) - sinal define se o deslocamento é pra cima (+) ou pra baixo (-).")]
    [SerializeField] private float maxAmplitude = 0.4f;
    [Tooltip("Teto da elevação (m) do deformador Bow Wave (propriedade do material).")]
    [SerializeField] private float maxElevation = 0.8f;
    [Range(0f, 1f)]
    [SerializeField] private float maxSurfaceFoamDimmer = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float maxDeepFoamDimmer = 1f;

    private WaterDecal _waterDecal;
    private Material _materialInstance;
    private float _smoothedIntensity;

    private void Awake()
    {
        _waterDecal = GetComponent<WaterDecal>();

        // Instancia o material - só necessário por causa da propriedade de
        // Elevação (SetFloat no shader graph). Se setássemos direto no
        // asset compartilhado, qualquer outro Water Decal usando o mesmo
        // material seria afetado junto (e permanentemente, no Editor).
        if (_waterDecal.material != null)
        {
            _materialInstance = new Material(_waterDecal.material);
            _waterDecal.material = _materialInstance;
        }
        else
        {
            Debug.LogWarning($"{nameof(ShipBowSplash)}: Water Decal sem material atribuído - a elevação do Bow Wave não poderá ser controlada por script (amplitude/foam continuam funcionando normalmente).");
        }

        if (shipMovement == null) shipMovement = GetComponentInParent<ShipMovementSystem>();
    }

    private void Update()
    {
        float targetIntensity;
        if (shipMovement != null)
        {
            float speedRatio = Mathf.Clamp01(shipMovement.CurrentSpeed / Mathf.Max(0.01f, speedForMaxSplash));
            targetIntensity = Mathf.Lerp(minSplashAtZeroSpeed, 1f, speedRatio);
        }
        else
        {
            targetIntensity = 1f;
        }

        float smoothingAlpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, splashSmoothingSeconds));
        _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, targetIntensity, smoothingAlpha);

        _waterDecal.amplitude = _smoothedIntensity * maxAmplitude;
        _waterDecal.surfaceFoamDimmer = _smoothedIntensity * maxSurfaceFoamDimmer;
        _waterDecal.deepFoamDimmer = _smoothedIntensity * maxDeepFoamDimmer;

        if (_materialInstance != null)
        {
            _materialInstance.SetFloat(elevationProperty, _smoothedIntensity * maxElevation);
        }
    }

    private void OnValidate()
    {
        if (speedForMaxSplash < 0.01f) speedForMaxSplash = 0.01f;
        if (splashSmoothingSeconds < 0.01f) splashSmoothingSeconds = 0.01f;
    }
}