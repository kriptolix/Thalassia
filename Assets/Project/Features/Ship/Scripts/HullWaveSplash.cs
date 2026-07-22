using UnityEngine;
using UnityEngine.VFX;

/// <summary>
/// Splash/respingo visual quando uma onda "bate" contra o casco, usando VFX
/// Graph (GPU particles) em vez de Particle System - melhor performance com
/// vários impactos simultâneos e mais consistente com os outros efeitos de
/// água do projeto (Water Decal já é GPU-driven).
///
/// NÃO faz nenhuma busca de água própria - reaproveita os dados que o
/// BuoyancySystem já calcula uma vez por FixedUpdate (WaterHeights/Depths/
/// WaterSurfaceVerticalVelocity, um por floatingPoint).
///
/// DETECÇÃO: um ponto perto da superfície (|depth| pequeno) cuja água está
/// subindo rápido (WaterSurfaceVerticalVelocity alto) é tratado como
/// impacto de onda.
///
/// COMO USAR:
/// 1. Crie um Visual Effect Graph de respingo (droplets/spray). No grafo,
///    adicione um Custom Event (ex.: "Impact") ligado ao Spawn Context que
///    gera as partículas, e exponha no Blackboard uma propriedade float
///    (ex.: "Intensity") que o Spawn Context/Initialize leia pra escalar a
///    quantidade de partículas daquela rajada.
/// 2. Crie um prefab com um GameObject + VisualEffect usando esse grafo.
/// 3. Adicione este script no mesmo objeto do BuoyancySystem, atribua o
///    prefab em vfxPrefab, e confirme os nomes exatos (eventName/
///    intensityPropertyName) batendo com o que você criou no grafo -
///    ASSUMIDOS abaixo, NÃO CONFIRMADOS, já que dependem do seu grafo.
/// </summary>
public class HullWaveSplash : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Se vazio, tenta GetComponent neste mesmo objeto.")]
    [SerializeField] private BuoyancySystem buoyancy;
    [Tooltip("Prefab com um VisualEffect (VFX Graph) de respingo.")]
    [SerializeField] private VisualEffect vfxPrefab;

    [Header("Nomes no VFX Graph - CONFIRME no seu grafo")]
    [Tooltip("ASSUMIDO/NÃO CONFIRMADO - nome do Custom Event que dispara a rajada de partículas no grafo.")]
    [SerializeField] private string eventName = "Impact";
    [Tooltip("ASSUMIDO/NÃO CONFIRMADO - nome da propriedade float (Blackboard) que escala a intensidade/quantidade da rajada.")]
    [SerializeField] private string intensityPropertyName = "Intensity";

    [Header("Detecção de Impacto")]
    [Tooltip("Faixa (metros) ao redor da superfície em que um ponto é considerado 'perto o suficiente' pra gerar splash visível - filtra pontos profundamente submersos (onda passando por baixo, sem splash visível) ou muito acima da água (fora de alcance).")]
    [SerializeField] private float nearSurfaceBand = 0.6f;
    [Tooltip("Velocidade mínima (m/s) de subida da água nesse ponto pra considerar 'impacto de onda' - abaixo disso é só bobbing normal, sem splash.")]
    [SerializeField] private float minImpactVelocity = 1.5f;
    [Tooltip("Velocidade de subida (m/s) na qual o efeito atinge a intensidade MÁXIMA (1.0, escala a propriedade Intensity). Entre minImpactVelocity e este valor, escala proporcionalmente.")]
    [SerializeField] private float velocityForMaxImpact = 4f;
    [Tooltip("Tempo mínimo (segundos) entre dois disparos no MESMO ponto - evita disparar de novo a cada FixedUpdate enquanto a condição de impacto ainda estiver ativa.")]
    [SerializeField] private float perPointCooldownSeconds = 0.25f;

    private VisualEffect[] _pooledVfx;
    private float[] _cooldownTimers;

    private void Awake()
    {
        if (buoyancy == null) buoyancy = GetComponent<BuoyancySystem>();
    }

    private void Start()
    {
        if (buoyancy == null || buoyancy.floatingPoints == null)
        {
            Debug.LogWarning($"{nameof(HullWaveSplash)}: sem BuoyancySystem/floatingPoints - splash desativado.");
            enabled = false;
            return;
        }

        if (vfxPrefab == null)
        {
            Debug.LogWarning($"{nameof(HullWaveSplash)}: nenhum vfxPrefab atribuído - splash desativado.");
            enabled = false;
            return;
        }

        int count = buoyancy.floatingPoints.Length;
        _pooledVfx = new VisualEffect[count];
        _cooldownTimers = new float[count];

        Transform poolRoot = new GameObject("HullWaveSplash_VFXPool").transform;
        poolRoot.SetParent(transform, false);

        for (int i = 0; i < count; i++)
        {
            _pooledVfx[i] = Instantiate(vfxPrefab, poolRoot);
        }
    }

    private void FixedUpdate()
    {
        // Lê os dados já calculados pelo BuoyancySystem neste mesmo
        // FixedUpdate (a ordem de execução entre scripts no mesmo objeto
        // não é garantida pela Unity - se este script rodar ANTES do
        // BuoyancySystem no mesmo frame, os dados usados aqui são os do
        // frame anterior, aceitável pra esse efeito, só atrasa em 1 frame).
        for (int i = 0; i < buoyancy.floatingPoints.Length; i++)
        {
            if (_cooldownTimers[i] > 0f)
            {
                _cooldownTimers[i] -= Time.fixedDeltaTime;
                continue;
            }

            Transform point = buoyancy.floatingPoints[i];
            if (point == null) continue;

            float depth = buoyancy.Depths[i];
            float riseVelocity = buoyancy.WaterSurfaceVerticalVelocity[i];

            bool nearSurface = Mathf.Abs(depth) < nearSurfaceBand;
            bool risingFastEnough = riseVelocity > minImpactVelocity;

            if (!nearSurface || !risingFastEnough) continue;

            float intensity = Mathf.Clamp01((riseVelocity - minImpactVelocity) / Mathf.Max(0.01f, velocityForMaxImpact - minImpactVelocity));

            Vector3 splashPosition = new Vector3(point.position.x, buoyancy.WaterHeights[i], point.position.z);
            TriggerSplash(i, splashPosition, intensity);

            _cooldownTimers[i] = perPointCooldownSeconds;
        }
    }

    private void TriggerSplash(int index, Vector3 worldPosition, float intensity)
    {
        VisualEffect vfx = _pooledVfx[index];
        if (vfx == null) return;

        vfx.transform.position = worldPosition;
        if (vfx.HasFloat(intensityPropertyName))
        {
            vfx.SetFloat(intensityPropertyName, intensity);
        }
        vfx.SendEvent(eventName);
    }

    private void OnValidate()
    {
        if (nearSurfaceBand < 0f) nearSurfaceBand = 0f;
        if (velocityForMaxImpact < minImpactVelocity + 0.01f) velocityForMaxImpact = minImpactVelocity + 0.01f;
        if (perPointCooldownSeconds < 0f) perPointCooldownSeconds = 0f;
    }
}