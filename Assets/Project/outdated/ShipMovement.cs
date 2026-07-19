using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class ShipMovement : MonoBehaviour
{
    [Header("Configurações de Movimento")]
    public float velocidadeMaxima = 10f;
    public float aceleracao = 5f;
    public float desaceleracao = 2f;
    public float velocidadeRotacao = 30f; // graus por segundo, ao girar o leme

    private Rigidbody rb;
    private float velocidadeAtual;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        // Não mexemos em useGravity, linearDamping ou angularDamping aqui:
        // isso fica por conta do script de flutuação/ondas e do Inspector,
        // já que a gravidade precisa continuar ativa para as ondas funcionarem.
    }

    void FixedUpdate()
    {
        float inputVertical = Input.GetAxis("Vertical");     // W/S ou setas cima/baixo
        float inputHorizontal = Input.GetAxis("Horizontal"); // A/D ou setas esquerda/direita

        // Acelera suavemente para frente/trás
        if (Mathf.Abs(inputVertical) > 0.01f)
        {
            velocidadeAtual += inputVertical * aceleracao * Time.fixedDeltaTime;
        }
        else
        {
            velocidadeAtual = Mathf.MoveTowards(velocidadeAtual, 0f, desaceleracao * Time.fixedDeltaTime);
        }

        velocidadeAtual = Mathf.Clamp(velocidadeAtual, -velocidadeMaxima * 0.5f, velocidadeMaxima);

        // Aplica velocidade apenas no plano horizontal (frente/trás do navio),
        // preservando o componente vertical (Y) da velocidade, que é controlado
        // pelo script de flutuação/ondas e pela gravidade.
        Vector3 direcaoMovimento = transform.forward * velocidadeAtual;
        Vector3 velocidadeAtualRB = rb.linearVelocity;
        rb.linearVelocity = new Vector3(direcaoMovimento.x, velocidadeAtualRB.y, direcaoMovimento.z);

        // Gira o navio (leme) em torno do eixo Y local, sem afetar o roll/pitch
        // que o script de ondas aplica para inclinar o navio.
        float fatorGiro = inputHorizontal * velocidadeRotacao * Time.fixedDeltaTime;
        rb.MoveRotation(rb.rotation * Quaternion.Euler(0f, fatorGiro, 0f));
    }
}