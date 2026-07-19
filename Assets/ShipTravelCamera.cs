using UnityEngine;

// Câmera panorâmica para trechos de viagem automática (leme não controlado pelo jogador).
// Anexar este script na CÂMERA da viagem automática (um objeto de Camera separado,
// desativado por padrão, e ativado só durante o trecho automático).
//
// A câmera NÃO é filha do navio. Em vez disso, ela segue no espaço do mundo (World Space)
// a posição/rotação que resulta da mistura entre 3 pontos fixos, que ESSES SIM
// devem ser filhos do navio (para se moverem e girarem junto com ele).
public class ShipTravelCamera : MonoBehaviour
{
    [Header("Pontos fixos de câmera (filhos do navio)")]
    [Tooltip("Ponto atrás e acima do navio (visão central).")]
    public Transform pontoCentro;
    [Tooltip("Ponto lateral esquerdo, mesma altura do ponto direito.")]
    public Transform pontoEsquerda;
    [Tooltip("Ponto lateral direito, mesma altura do ponto esquerdo.")]
    public Transform pontoDireita;

    [Header("Suavização")]
    [Tooltip("Quanto maior, mais rápido a câmera converge para o ângulo/posição alvo.")]
    public float velocidadeTransicao = 3f;

    void LateUpdate()
    {
        if (pontoCentro == null || pontoEsquerda == null || pontoDireita == null)
            return;

        // Posição horizontal do mouse na tela, normalizada de 0 (esquerda) a 1 (direita)
        float mouseXNormalizado = Mathf.Clamp01(Input.mousePosition.x / Screen.width);

        Vector3 posicaoAlvo;
        Quaternion rotacaoAlvo;

        if (mouseXNormalizado < 0.5f)
        {
            // Mistura entre esquerda (0) e centro (1)
            float t = Mathf.InverseLerp(0f, 0.5f, mouseXNormalizado);
            posicaoAlvo = Vector3.Lerp(pontoEsquerda.position, pontoCentro.position, t);
            rotacaoAlvo = Quaternion.Slerp(pontoEsquerda.rotation, pontoCentro.rotation, t);
        }
        else
        {
            // Mistura entre centro (0) e direita (1)
            float t = Mathf.InverseLerp(0.5f, 1f, mouseXNormalizado);
            posicaoAlvo = Vector3.Lerp(pontoCentro.position, pontoDireita.position, t);
            rotacaoAlvo = Quaternion.Slerp(pontoCentro.rotation, pontoDireita.rotation, t);
        }

        // Suaviza a própria transição da câmera até o alvo calculado acima,
        // evitando saltos bruscos quando o mouse se move rápido de um lado a outro.
        transform.position = Vector3.Lerp(transform.position, posicaoAlvo, velocidadeTransicao * Time.deltaTime);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotacaoAlvo, velocidadeTransicao * Time.deltaTime);
    }
}