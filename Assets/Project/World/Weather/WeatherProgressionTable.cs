using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Define, para cada WeatherType, quais estados podem sucedê-lo.
    ///
    /// O WeatherSystem escolhe aleatoriamente apenas entre os estados permitidos
    /// por esta tabela, evitando transições ilógicas (ex.: Clear -> Storm),
    /// conforme descrito em WeatherSystem.md ("Progressão Climática").
    /// </summary>
    [CreateAssetMenu(fileName = "New Weather Progression Table", menuName = "Weather/Progression Table", order = 1)]
    public class WeatherProgressionTable : ScriptableObject
    {
        [Serializable]
        public class TransitionRule
        {
            public WeatherType from;
            public List<WeatherType> allowedNext = new List<WeatherType>();
        }

        [SerializeField] private List<TransitionRule> rules = new List<TransitionRule>();

        private Dictionary<WeatherType, List<WeatherType>> _lookup;

        private void BuildLookupIfNeeded()
        {
            if (_lookup != null) return;

            _lookup = new Dictionary<WeatherType, List<WeatherType>>();
            foreach (var rule in rules)
            {
                if (rule == null) continue;

                if (_lookup.ContainsKey(rule.from))
                {
                    Debug.LogWarning($"[WeatherProgressionTable] Regra duplicada para '{rule.from}'. " +
                                      "A primeira definição encontrada será utilizada.");
                    continue;
                }

                _lookup.Add(rule.from, rule.allowedNext ?? new List<WeatherType>());
            }
        }

        /// <summary>
        /// Retorna os estados permitidos a partir do estado atual.
        /// Retorna uma lista vazia caso não exista regra definida para ele.
        /// </summary>
        public IReadOnlyList<WeatherType> GetAllowedNext(WeatherType current)
        {
            BuildLookupIfNeeded();
            return _lookup.TryGetValue(current, out var next) ? next : Array.Empty<WeatherType>();
        }

        private void OnValidate()
        {
            // Invalida o cache sempre que a tabela for editada no Inspector.
            _lookup = null;
        }
    }
}
