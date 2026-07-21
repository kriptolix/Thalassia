using UnityEngine;

// Anexar este script em qualquer objeto "gerente" da cena (pode ser um objeto vazio
// chamado "GerenciadorCamera", por exemplo). Arraste as duas câmeras nos campos abaixo.
public class CameraSwitcher : MonoBehaviour
{
    [Header("Câmeras")]
    [Tooltip("Pelo menos uma camera é obrigatória")]
    public Transform[] cameras;

    [Header("Tecla de atalho")]
    public KeyCode teclaAlternar = KeyCode.Tab;

    private int cameraAtual = 0;

    void Start()
    {
        // Garante um estado inicial consistente: começa na câmera do navio
        if (cameras == null || cameras.Length == 0)
            Debug.LogWarning($"{gameObject.name}: Nenhuma camera configurada!");

        AtivarCamera(cameraAtual);
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaAlternar))
        {
            cameraAtual = (cameraAtual + 1) % cameras.Length;
            AtivarCamera(cameraAtual);
        }
    }

    void AtivarCamera(int indice)
    {
        for (int i = 0; i < cameras.Length; i++)
        {
            cameras[i].gameObject.SetActive(i == indice);
        }
    }
    
}