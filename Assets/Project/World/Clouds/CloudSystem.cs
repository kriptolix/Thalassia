using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

namespace Game.Weather
{
    /// <summary>
    /// CloudSystem — controla toda a representação visual das nuvens
    /// (ver Especificação de Arquitetura — CloudSystem.md).
    ///
    /// Não decide o clima, não inicia chuva, não controla vento nem
    /// iluminação. Sua única responsabilidade é interpretar os parâmetros
    /// recebidos do WeatherSystem (via WeatherPreset.clouds), gerar pequenas
    /// variações naturais (CloudNoise) e aplicá-las de forma suave e
    /// contínua ao HDRP Volumetric Clouds e ao Cloud Layer (fazendo o papel
    /// de Background Clouds).
    ///
    /// Fluxo (arquitetura, seção 4/11):
    /// WeatherPreset.clouds -> CloudStateGenerator (+ CloudNoise) -> alvo
    ///   -> CloudTransitionController (interpola) -> estado atual
    ///   -> CloudHDRPAdapter (Volumetric Clouds)
    ///   -> CloudLayerAdapter (Background Clouds / Cloud Layer)
    /// </summary>
    [RequireComponent(typeof(Volume))]
    public class CloudSystem : MonoBehaviour, IWeatherSubsystem
    {
        [Header("Referências")]
        [Tooltip("Volume HDRP contendo os overrides Volumetric Clouds e Cloud Layer. Se vazio, usa o Volume neste mesmo GameObject.")]
        [SerializeField] private Volume targetVolume;

        [Tooltip("WeatherSystem a ser observado. Se vazio, usa WeatherSystem.Instance.")]
        [SerializeField] private WeatherSystem weatherSystem;

        [Header("Background Clouds (Cloud Layer)")]
        [Tooltip("Desative caso ainda não exista um override Cloud Layer configurado no Volume.")]
        [SerializeField] private bool applyToCloudLayer = true;

        [Tooltip("Ative se o Cloud Layer estiver configurado como Double Layer (layerA = nuvens inferiores, layerB = nuvens superiores).")]
        [SerializeField] private bool cloudLayerUsesDoubleLayer = false;

        [Tooltip("Canal do cloud map (R/G/B/A) usado para representar a cobertura. Ajustar assim que o cloud map final for definido.")]
        [SerializeField] private CloudLayerAdapter.CoverageChannel cloudLayerCoverageChannel = CloudLayerAdapter.CoverageChannel.All;

        [Header("Ruído (variações naturais)")]
        [SerializeField] private CloudNoiseProfile noiseProfile = new CloudNoiseProfile();

        [Header("Transição")]
        [Tooltip("Velocidade de suavização ao seguir o estado alvo. Maior = reage mais rápido a mudanças de clima.")]
        [SerializeField] private float transitionSpeed = 0.15f;

        [Header("Reprodutibilidade")]
        [Tooltip("Offset de semente aplicado ao ruído. Sementes iguais produzem o mesmo padrão de variação.")]
        [SerializeField] private float seed = 0f;

        private CloudHDRPAdapter _hdrpAdapter;
        private CloudLayerAdapter _cloudLayerAdapter;
        private CloudTransitionController _transitionController;
        private CloudSettings _baseTarget;
        private float _time;

        /// <summary>API pública simples para consulta do estado atual (CloudSystem.md, seção 2).</summary>
        public CloudSettings CurrentState => _transitionController?.Current;

        private void Awake()
        {
            if (targetVolume == null) targetVolume = GetComponent<Volume>();
            SetupAdapters();
        }

        private void SetupAdapters()
        {
            if (targetVolume == null || targetVolume.profile == null)
            {
                Debug.LogError("[CloudSystem] Nenhum Volume/Profile HDRP configurado. O CloudSystem não poderá aplicar valores.");
                return;
            }

            if (targetVolume.profile.TryGet<VolumetricClouds>(out var volumetricClouds))
            {
                _hdrpAdapter = new CloudHDRPAdapter(volumetricClouds);
            }
            else
            {
                Debug.LogWarning("[CloudSystem] Nenhum override 'Volumetric Clouds' encontrado no Volume informado.");
            }

            if (applyToCloudLayer)
            {
                if (targetVolume.profile.TryGet<CloudLayer>(out var cloudLayer))
                {
                    _cloudLayerAdapter = new CloudLayerAdapter(cloudLayer, cloudLayerCoverageChannel, cloudLayerUsesDoubleLayer);
                }
                else
                {
                    Debug.LogWarning("[CloudSystem] Nenhum override 'Cloud Layer' encontrado no Volume informado.");
                }
            }
        }

        private void OnEnable()
        {
            var ws = ResolveWeatherSystem();
            if (ws == null)
            {
                Debug.LogWarning("[CloudSystem] Nenhum WeatherSystem encontrado para se inscrever no evento WeatherChanged.");
                return;
            }

            ws.WeatherChanged += HandleWeatherChanged;

            // Se o clima já estiver ativo (CloudSystem habilitado após o WeatherSystem
            // já ter iniciado), inicializa imediatamente com o preset atual.
            if (ws.CurrentPreset != null && _transitionController == null)
            {
                InitializeFromPreset(ws.CurrentPreset);
            }
        }

        private void OnDisable()
        {
            var ws = ResolveWeatherSystem();
            if (ws != null) ws.WeatherChanged -= HandleWeatherChanged;
        }

        private WeatherSystem ResolveWeatherSystem() => weatherSystem != null ? weatherSystem : WeatherSystem.Instance;

        /// <summary>Implementação do contrato opcional IWeatherSubsystem (delegando para o mesmo handler do evento).</summary>
        void IWeatherSubsystem.OnWeatherChanged(WeatherChangedEventArgs args) => HandleWeatherChanged(args);

        private void HandleWeatherChanged(WeatherChangedEventArgs args)
        {
            if (args.NewPreset == null) return;

            if (_transitionController == null)
            {
                InitializeFromPreset(args.NewPreset);
                return;
            }

            // Apenas atualiza o alvo-base; o TransitionController se encarrega de
            // caminhar até lá suavemente nos próximos frames (Update()), evitando
            // saltos abruptos mesmo em trocas de clima.
            _baseTarget = args.NewPreset.clouds;
        }

        private void InitializeFromPreset(WeatherPreset preset)
        {
            _baseTarget = preset.clouds;
            _transitionController = new CloudTransitionController(preset.clouds.Clone());
        }

        private void Update()
        {
            if (_baseTarget == null || _transitionController == null) return;

            _time += Time.deltaTime;

            var noisyTarget = CloudStateGenerator.GenerateTarget(_baseTarget, noiseProfile, _time + seed);
            _transitionController.Tick(noisyTarget, transitionSpeed, Time.deltaTime);

            _hdrpAdapter?.Apply(_transitionController.Current);

            if (applyToCloudLayer) _cloudLayerAdapter?.Apply(_transitionController.Current);
        }
    }
}
