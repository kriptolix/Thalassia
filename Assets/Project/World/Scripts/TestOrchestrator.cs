using UnityEngine;

/// <summary>
/// Orquestrador de Testes - MonoBehaviour separado, NÃO faz parte dos
/// sistemas de gameplay (WindSystem/SailSystem/RudderSystem/ShipMovementSystem
/// não sabem que ele existe). Único propósito: fornecer vento CONSISTENTE e
/// reproduzível pra testar a física manualmente, sem depender da variação
/// automática do WindSystem (muda a cada 60-240s + rajadas, o que torna
/// difícil reproduzir um ponto de vela específico por tempo suficiente).
///
/// Uso básico: arraste a referência ao WindSystem da cena, ligue
/// "Override Ativo" no Inspector (em Play Mode) e ajuste windDirection/
/// windIntensity à vontade - aplica em tempo real via
/// WindSystem.SetManualOverride. Desligar o toggle libera o vento de volta
/// pra variação automática (WindSystem.ReleaseManualOverride).
///
/// Uso avançado (testar um PONTO DE VELA específico, ex: Bolina): atribua
/// shipToTestAgainst (o Transform do barco) e ajuste targetWindAngleFromBow -
/// em vez de uma direção de vento fixa no mundo, o vento é recalculado a
/// cada frame pra manter o ÂNGULO RELATIVO ao heading atual do barco. Assim
/// o teste continua válido mesmo se o barco guinar (inclusive guinadas
/// involuntárias tipo weather helm, que é justamente o que você quer
/// conseguir reproduzir de forma consistente).
/// </summary>
public class TestOrchestrator : MonoBehaviour
{
    [Header("Referência")]
    [SerializeField] private WindSystem windSystem;

    [Header("Override de Vento (só aplica com Override Ativo = true)")]
    [Tooltip("Liga/desliga o override - com true, ignora a variação automática do WindSystem e força os valores abaixo. Desligar libera de volta a variação automática, sem salto.")]
    [SerializeField] private bool overrideActive = false;
    [Tooltip("Direção do vento (graus, 0-360, mesma convenção do WindSystem: 0=Leste, 90=Norte...). Ignorado se shipToTestAgainst estiver atribuído - use targetWindAngleFromBow nesse caso.")]
    [Range(0f, 360f)]
    [SerializeField] private float windDirection = 90f;
    [Tooltip("Intensidade do vento, m/s internamente.")]
    [SerializeField] private float windIntensity = 12f;

    [Header("Atalho: Travar um Ponto de Vela (opcional)")]
    [Tooltip("Se atribuído, windDirection acima é recalculado a cada frame pra manter targetWindAngleFromBow constante em relação ao heading ATUAL deste Transform - útil pra testar um ponto de vela específico (ex: 50° = Bolina) mesmo que o barco guine durante o teste.")]
    [SerializeField] private Transform shipToTestAgainst;
    [Tooltip("Ângulo vento-proa desejado (0=vento na proa, 180=vento na popa - mesma definição do SailSystem/HUD). Só usado se shipToTestAgainst estiver atribuído.")]
    [Range(0f, 180f)]
    [SerializeField] private float targetWindAngleFromBow = 50f;
    [Tooltip("De que lado o vento bate: +1 = estibordo (direita), -1 = bombordo (esquerda). Só usado com shipToTestAgainst.")]
    [SerializeField] private int windSide = 1;

    private bool _wasOverrideActive = false;

    private void Update()
    {
        if (windSystem == null)
        {
            Debug.LogWarning($"{nameof(TestOrchestrator)}: WindSystem não atribuído.");
            return;
        }

        if (overrideActive)
        {
            float directionToApply = windDirection;

            if (shipToTestAgainst != null)
            {
                float shipHeadingDeg = NormalizeAngle(
                    Mathf.Atan2(shipToTestAgainst.forward.z, shipToTestAgainst.forward.x) * Mathf.Rad2Deg);

                // Inverte a fórmula do SailSystem (windAngleFromBow = 180 - |delta|)
                // pra achar a direção de vento que produz o ângulo desejado:
                // |delta| = 180 - windAngleFromBow, com o sinal escolhido por windSide.
                float deltaMagnitude = 180f - Mathf.Clamp(targetWindAngleFromBow, 0f, 180f);
                float delta = windSide >= 0 ? deltaMagnitude : -deltaMagnitude;
                directionToApply = NormalizeAngle(shipHeadingDeg + delta);

                windDirection = directionToApply; // reflete no Inspector, pra você conferir o valor absoluto resultante
            }

            windSystem.SetManualOverride(directionToApply, windIntensity);
        }
        else if (_wasOverrideActive)
        {
            windSystem.ReleaseManualOverride();
        }

        _wasOverrideActive = overrideActive;
    }

    private void OnDisable()
    {
        // Segurança: se este objeto for desativado/destruído com o override
        // ligado, libera o vento de volta pra variação automática em vez de
        // deixar o WindSystem travado num valor fixo pra sempre.
        if (_wasOverrideActive && windSystem != null)
        {
            windSystem.ReleaseManualOverride();
            _wasOverrideActive = false;
        }
    }

    private static float NormalizeAngle(float angle)
    {
        angle %= 360f;
        if (angle < 0f) angle += 360f;
        return angle;
    }
}