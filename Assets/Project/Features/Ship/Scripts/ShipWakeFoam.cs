using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

/// <summary>
/// Espuma de ESTEIRA (wake foam) local, usando o componente atual "Water
/// Decal" da HDRP (substituiu WaterFoamGenerator/WaterDeformer).
///
/// NOME DA CLASSE NÃO CONFIRMADO por documentação - usando "WaterDecal" por
/// ser como aparece no seu Inspector ("Water Decal (Script)"). Se o nome
/// real da classe C# for diferente, a Unity vai acusar erro de compilação
/// bem específico na linha do RequireComponent/GetComponent abaixo - troque
/// pelo nome correto nesse caso.
///
/// amplitude/surfaceFoamDimmer/deepFoamDimmer agora são campos DIRETOS do
/// componente (não mais propriedades de shader/material) - controlados
/// aqui com total confiança, sem precisar adivinhar nomes de propriedade.
///
/// Posicionamento/dimensionamento (Transform, resolution, material de
/// máscara) continuam manuais, feitos por você no Editor - este script só
/// modula a intensidade pela velocidade do barco.
/// </summary>
[RequireComponent(typeof(WaterDecal))]
public class ShipWakeFoam : MonoBehaviour
{
    [Header("Referência (opcional)")]
    [Tooltip("Se atribuído, a intensidade escala com a velocidade do barco (CurrentSpeed, m/s). Se vazio, tenta encontrar um ShipMovementSystem num objeto pai automaticamente; se ainda assim não achar, usa intensidade fixa nos tetos abaixo.")]
    [SerializeField] private ShipMovementSystem shipMovement;

    [Header("Resposta à Velocidade do Barco")]
    [Tooltip("Velocidade (m/s) na qual a espuma/deformação atinge a intensidade MÁXIMA (tetos abaixo). Abaixo disso, cai proporcionalmente até minIntensityAtZeroSpeed.")]
    [SerializeField] private float speedForMaxEffect = 8f;
    [Tooltip("Intensidade mínima (fração dos tetos) mesmo com o barco parado. 0 = efeito some completamente parado.")]
    [Range(0f, 1f)]
    [SerializeField] private float minIntensityAtZeroSpeed = 0f;
    [Tooltip("Constante de tempo (segundos) de suavização - evita 'piscar' com pequenas variações de velocidade.")]
    [SerializeField] private float smoothingSeconds = 0.4f;

    [Header("Intensidade Máxima (tetos, na velocidade máxima)")]
    [Tooltip("Teto de amplitude (m) - sinal define se a deformação da esteira vai pra cima (+) ou pra baixo (-). Ambos os sinais geram espuma.")]
    [SerializeField] private float maxAmplitude = -0.3f;
    [Range(0f, 1f)]
    [SerializeField] private float maxSurfaceFoamDimmer = 1f;
    [Range(0f, 1f)]
    [SerializeField] private float maxDeepFoamDimmer = 1f;

    private WaterDecal _waterDecal;
    private float _smoothedIntensity;

    private void Awake()
    {
        _waterDecal = GetComponent<WaterDecal>();
        if (shipMovement == null) shipMovement = GetComponentInParent<ShipMovementSystem>();
    }

    private void Update()
    {
        float targetIntensity;
        if (shipMovement != null)
        {
            float speedRatio = Mathf.Clamp01(shipMovement.CurrentSpeed / Mathf.Max(0.01f, speedForMaxEffect));
            targetIntensity = Mathf.Lerp(minIntensityAtZeroSpeed, 1f, speedRatio);
        }
        else
        {
            targetIntensity = 1f;
        }

        float smoothingAlpha = 1f - Mathf.Exp(-Time.deltaTime / Mathf.Max(0.01f, smoothingSeconds));
        _smoothedIntensity = Mathf.Lerp(_smoothedIntensity, targetIntensity, smoothingAlpha);

        _waterDecal.amplitude = _smoothedIntensity * maxAmplitude;
        _waterDecal.surfaceFoamDimmer = _smoothedIntensity * maxSurfaceFoamDimmer;
        _waterDecal.deepFoamDimmer = _smoothedIntensity * maxDeepFoamDimmer;
    }

    private void OnValidate()
    {
        if (speedForMaxEffect < 0.01f) speedForMaxEffect = 0.01f;
        if (smoothingSeconds < 0.01f) smoothingSeconds = 0.01f;
    }
}