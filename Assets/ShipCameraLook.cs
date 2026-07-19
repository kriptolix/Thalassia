using UnityEngine;

// Anexar este script ao "CameraPivot" (objeto vazio, filho do navio,
// posicionado atrás do timão). A câmera fica filha desse pivot.
public class ShipCameraLook : MonoBehaviour
{
    [Header("Sensibilidade do Mouse")]
    public float sensibilidadeMouseX = 2f;
    public float sensibilidadeMouseY = 2f;

    [Header("Limite vertical (graus)")]
    public float anguloMinimo = -30f; // olhar para baixo
    public float anguloMaximo = 50f;  // olhar para cima

    [Header("Suavização")]
    [Tooltip("Tempo (em segundos) para a câmera alcançar a posição alvo. Quanto maior, mais suave/lento.")]
    public float tempoSuavizacao = 0.08f;

    // Alvo bruto vindo do mouse (sem suavização)
    private float rotacaoXAlvo; // yaw alvo (horizontal, sem limite)
    private float rotacaoYAlvo; // pitch alvo (vertical, limitado)

    // Valor atual, suavizado, aplicado de fato na rotação
    private float rotacaoXAtual;
    private float rotacaoYAtual;

    // Velocidades internas usadas pelo SmoothDampAngle
    private float velocidadeX;
    private float velocidadeY;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        Vector3 anguloAtual = transform.localEulerAngles;
        rotacaoXAlvo = rotacaoXAtual = anguloAtual.y;
        rotacaoYAlvo = rotacaoYAtual = anguloAtual.x;
    }

    void Update()
    {
        float mouseX = Input.GetAxis("Mouse X") * sensibilidadeMouseX;
        float mouseY = Input.GetAxis("Mouse Y") * sensibilidadeMouseY;

        rotacaoXAlvo += mouseX;                       // horizontal: gira livremente 360°
        rotacaoYAlvo -= mouseY;                        // vertical: invertido p/ mouse "natural"
        rotacaoYAlvo = Mathf.Clamp(rotacaoYAlvo, anguloMinimo, anguloMaximo);

        // Suaviza a transição do valor atual até o alvo, evitando giros bruscos
        rotacaoXAtual = Mathf.SmoothDampAngle(rotacaoXAtual, rotacaoXAlvo, ref velocidadeX, tempoSuavizacao);
        rotacaoYAtual = Mathf.SmoothDampAngle(rotacaoYAtual, rotacaoYAlvo, ref velocidadeY, tempoSuavizacao);

        transform.localRotation = Quaternion.Euler(rotacaoYAtual, rotacaoXAtual, 0f);

        // Esc libera o cursor (útil para testar/abrir menus)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            bool travado = Cursor.lockState == CursorLockMode.Locked;
            Cursor.lockState = travado ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = travado;
        }
    }
}