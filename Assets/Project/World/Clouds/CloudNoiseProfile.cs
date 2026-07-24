using System;
using UnityEngine;

namespace Game.Weather
{
    /// <summary>
    /// Agrupa um CloudNoiseChannel por propriedade, conforme CloudSystem.md
    /// (seção 5.2.1): cada parâmetro pode possuir configuração independente
    /// de amplitude/frequência/velocidade.
    ///
    /// Os valores padrão de "coverage" replicam literalmente o exemplo do
    /// documento (Amplitude 0.05, Frequency 0.1, Speed 0.02). As demais
    /// propriedades vêm com amplitude 0 (ruído desativado) por padrão —
    /// ajuste no Inspector conforme necessário.
    /// </summary>
    [Serializable]
    public class CloudNoiseProfile
    {
        [Header("Cobertura e Densidade")]
        public CloudNoiseChannel coverage = new CloudNoiseChannel { amplitude = 0.05f, frequency = 0.1f, speed = 0.02f };
        public CloudNoiseChannel density = new CloudNoiseChannel { amplitude = 0.04f, frequency = 0.15f, speed = 0.03f };

        [Header("Forma e Erosão")]
        public CloudNoiseChannel erosion = new CloudNoiseChannel { amplitude = 0.02f, frequency = 0.1f, speed = 0.02f };
        public CloudNoiseChannel shapeFactor = new CloudNoiseChannel { amplitude = 0f, frequency = 0.1f, speed = 0.02f };
        public CloudNoiseChannel densityMultiplier = new CloudNoiseChannel { amplitude = 0f, frequency = 0.1f, speed = 0.02f };

        [Header("Luz e Espalhamento")]
        public CloudNoiseChannel ambientExposure = new CloudNoiseChannel { amplitude = 0f, frequency = 0.05f, speed = 0.01f };
        public CloudNoiseChannel powderEffect = new CloudNoiseChannel { amplitude = 0f, frequency = 0.1f, speed = 0.02f };
        public CloudNoiseChannel multiScattering = new CloudNoiseChannel { amplitude = 0f, frequency = 0.1f, speed = 0.02f };
        public CloudNoiseChannel cloudOpacity = new CloudNoiseChannel { amplitude = 0f, frequency = 0.1f, speed = 0.02f };

        [Header("Cor")]
        [Tooltip("Aplicado como pequena variação de brilho (HSV Value) sobre CloudColor, BottomTint e TopTint. Amplitude muito baixa, conforme seção 5.2.1 do CloudSystem.md, para evitar que a iluminação pareça pulsar.")]
        public CloudNoiseChannel colorBrightness = new CloudNoiseChannel { amplitude = 0.015f, frequency = 0.05f, speed = 0.01f };

        [Header("Geometria (quando aplicável)")]
        public CloudNoiseChannel altitude = new CloudNoiseChannel { amplitude = 0f, frequency = 0.05f, speed = 0.01f };
        public CloudNoiseChannel thickness = new CloudNoiseChannel { amplitude = 0f, frequency = 0.05f, speed = 0.01f };
    }
}
