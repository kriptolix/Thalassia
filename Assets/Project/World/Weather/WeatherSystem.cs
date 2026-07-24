using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Orquestrador central do sistema climático (ver Especificação de
    /// Arquitetura — WeatherSystem.md).
    ///
    /// Responsabilidades:
    ///   - Gerenciar os WeatherPresets disponíveis.
    ///   - Controlar a progressão lógica entre estados climáticos através de
    ///     uma tabela explícita de transições válidas (WeatherProgressionTable).
    ///   - Sinalizar mudanças de clima para os subsistemas através do evento
    ///     WeatherChanged.
    ///   - Suportar tanto progressão automática quanto controle forçado por
    ///     eventos de gameplay.
    ///
    /// O WeatherSystem é um orquestrador, não um executor: ele NUNCA aplica
    /// efeitos visuais ou físicos, nunca manipula HDRP Volumes, VFX Graph,
    /// Unity Water System, iluminação, etc. Ele também não controla a duração
    /// das transições visuais — apenas decide QUANDO uma mudança de clima
    /// ocorre. Essas responsabilidades pertencem exclusivamente aos
    /// subsistemas (CloudSystem, FogSystem, PrecipitationSystem, WaterSystem,
    /// DayNightSystem...), que devem se inscrever em WeatherChanged e
    /// interpretar apenas sua própria seção do WeatherPreset ativo.
    /// </summary>
    public class WeatherSystem : MonoBehaviour
    {
        public enum ControlMode
        {
            Automatic,
            Forced
        }

        [Header("Presets")]
        [Tooltip("Todos os WeatherPresets disponíveis para a progressão automática e para o controle forçado por tipo.")]
        [SerializeField] private List<WeatherPreset> availablePresets = new List<WeatherPreset>();

        [Tooltip("Tabela que define quais estados podem suceder o estado climático atual.")]
        [SerializeField] private WeatherProgressionTable progressionTable;

        [Tooltip("Preset usado ao iniciar o jogo. Se vazio, usa o primeiro item de 'Available Presets'.")]
        [SerializeField] private WeatherPreset initialPreset;

        [Header("Progressão Automática")]
        [Tooltip("Duração mínima (segundos) de um estado climático antes de uma nova progressão.")]
        [SerializeField] private float minStateDuration = 180f;

        [Tooltip("Duração máxima (segundos) de um estado climático antes de uma nova progressão.")]
        [SerializeField] private float maxStateDuration = 420f;

        [Header("Debug")]
        [SerializeField] private bool logWeatherChanges = false;

        /// <summary>Instância única para facilitar o acesso a partir dos subsistemas.</summary>
        public static WeatherSystem Instance { get; private set; }

        /// <summary>Preset climático atualmente ativo.</summary>
        public WeatherPreset CurrentPreset { get; private set; }

        /// <summary>Modo de controle atual (Automático ou Forçado).</summary>
        public ControlMode CurrentMode { get; private set; } = ControlMode.Automatic;

        public bool IsForced => CurrentMode == ControlMode.Forced;

        /// <summary>
        /// Emitido sempre que o clima muda, seja por progressão automática ou
        /// por controle forçado. Subsistemas devem se inscrever neste evento
        /// e consultar apenas os dados de que precisam em args.NewPreset.
        /// </summary>
        public event Action<WeatherChangedEventArgs> WeatherChanged;

        private readonly Dictionary<WeatherType, WeatherPreset> _presetLookup = new Dictionary<WeatherType, WeatherPreset>();
        private float _timer;
        private float _currentStateDuration;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[WeatherSystem] Já existe uma instância ativa nesta cena. Destruindo duplicata.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            BuildPresetLookup();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Start()
        {
            var startingPreset = initialPreset != null
                ? initialPreset
                : (availablePresets.Count > 0 ? availablePresets[0] : null);

            if (startingPreset == null)
            {
                Debug.LogError("[WeatherSystem] Nenhum WeatherPreset disponível. O sistema será desativado.");
                enabled = false;
                return;
            }

            CurrentMode = ControlMode.Automatic;
            SetPreset(startingPreset, isForced: false);
            RestartTimer();
        }

        private void Update()
        {
            if (CurrentMode != ControlMode.Automatic) return;

            _timer += Time.deltaTime;
            if (_timer >= _currentStateDuration)
            {
                AdvanceWeather();
            }
        }

        private void BuildPresetLookup()
        {
            _presetLookup.Clear();
            foreach (var preset in availablePresets)
            {
                if (preset == null) continue;

                if (_presetLookup.ContainsKey(preset.weatherType))
                {
                    Debug.LogWarning($"[WeatherSystem] Mais de um preset registrado para '{preset.weatherType}'. " +
                                      $"Mantendo '{_presetLookup[preset.weatherType].name}', ignorando '{preset.name}'.");
                    continue;
                }

                _presetLookup.Add(preset.weatherType, preset);
            }
        }

        /// <summary>
        /// Escolhe aleatoriamente o próximo preset entre os estados permitidos
        /// pela WeatherProgressionTable a partir do estado atual.
        /// </summary>
        private void AdvanceWeather()
        {
            if (progressionTable == null)
            {
                Debug.LogWarning("[WeatherSystem] Nenhuma WeatherProgressionTable configurada. Progressão automática pausada.");
                RestartTimer();
                return;
            }

            var allowedNext = progressionTable.GetAllowedNext(CurrentPreset.weatherType);
            var candidates = new List<WeatherPreset>(allowedNext.Count);

            foreach (var type in allowedNext)
            {
                if (_presetLookup.TryGetValue(type, out var preset))
                {
                    candidates.Add(preset);
                }
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning($"[WeatherSystem] Nenhum preset disponível para suceder '{CurrentPreset.weatherType}'. Mantendo o clima atual.");
                RestartTimer();
                return;
            }

            var next = candidates[UnityEngine.Random.Range(0, candidates.Count)];
            SetPreset(next, isForced: false);
            RestartTimer();
        }

        private void RestartTimer()
        {
            _timer = 0f;
            _currentStateDuration = UnityEngine.Random.Range(minStateDuration, maxStateDuration);
        }

        private void SetPreset(WeatherPreset newPreset, bool isForced)
        {
            if (newPreset == null) return;

            var previous = CurrentPreset;
            CurrentPreset = newPreset;

            if (logWeatherChanges)
            {
                var previousLabel = previous != null ? previous.weatherType.ToString() : "N/A";
                Debug.Log($"[WeatherSystem] Clima alterado: {previousLabel} -> {newPreset.weatherType} (Forçado: {isForced})");
            }

            WeatherChanged?.Invoke(new WeatherChangedEventArgs(previous, newPreset, isForced));
        }

        // ---------------------------------------------------------------
        // Controle Forçado
        // ---------------------------------------------------------------

        /// <summary>
        /// Interrompe a progressão automática e define explicitamente o clima
        /// ativo. Útil para missões, cutscenes ou eventos de gameplay.
        /// Enquanto o modo forçado estiver ativo, nenhuma progressão
        /// automática ocorre.
        /// </summary>
        public void ForceWeather(WeatherPreset preset)
        {
            if (preset == null)
            {
                Debug.LogError("[WeatherSystem] Tentativa de forçar um WeatherPreset nulo.");
                return;
            }

            CurrentMode = ControlMode.Forced;
            SetPreset(preset, isForced: true);
        }

        /// <summary>Sobrecarga que localiza o preset pelo WeatherType.</summary>
        public void ForceWeather(WeatherType type)
        {
            if (_presetLookup.TryGetValue(type, out var preset))
            {
                ForceWeather(preset);
            }
            else
            {
                Debug.LogError($"[WeatherSystem] Nenhum preset registrado para o tipo '{type}'. " +
                                "Verifique a lista 'Available Presets'.");
            }
        }

        /// <summary>
        /// Sai do modo forçado e retorna à progressão automática, utilizando o
        /// preset atual como ponto de partida (nenhuma transição imediata é
        /// disparada — a próxima progressão ocorre normalmente após o timer).
        /// </summary>
        public void ReleaseForce()
        {
            if (CurrentMode != ControlMode.Forced) return;

            CurrentMode = ControlMode.Automatic;
            RestartTimer();
        }

        // ---------------------------------------------------------------
        // API Pública Auxiliar
        // ---------------------------------------------------------------

        /// <summary>Tenta localizar o preset registrado para um WeatherType.</summary>
        public bool TryGetPreset(WeatherType type, out WeatherPreset preset) =>
            _presetLookup.TryGetValue(type, out preset);

        /// <summary>Lista somente leitura de todos os presets configurados.</summary>
        public IReadOnlyList<WeatherPreset> AvailablePresets => availablePresets;
    }
}
