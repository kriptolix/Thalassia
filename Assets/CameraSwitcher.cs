using UnityEngine;

// Anexar este script em qualquer objeto "gerente" da cena (pode ser um objeto vazio
// chamado "GerenciadorCamera", por exemplo). Arraste as duas câmeras nos campos abaixo.
public class CameraSwitcher : MonoBehaviour
{
    [Header("Câmeras")]
    [Tooltip("Câmera padrão, atrás do timão, controlada pelo mouse livre.")]
    public Camera cameraNavio;
    [Tooltip("Câmera panorâmica de viagem automática, com os 3 pontos fixos.")]
    public Camera cameraViagem;

    [Header("Tecla de atalho")]
    public KeyCode teclaAlternar = KeyCode.Tab;

    private bool viagemAtiva = false;

    void Start()
    {
        // Garante um estado inicial consistente: começa na câmera do navio
        AtivarCamera(cameraNavio, cameraViagem);
    }

    void Update()
    {
        if (Input.GetKeyDown(teclaAlternar))
        {
            viagemAtiva = !viagemAtiva;

            if (viagemAtiva)
                AtivarCamera(cameraViagem, cameraNavio);
            else
                AtivarCamera(cameraNavio, cameraViagem);
        }
    }

    private void AtivarCamera(Camera paraAtivar, Camera paraDesativar)
    {
        paraAtivar.gameObject.SetActive(true);
        paraDesativar.gameObject.SetActive(false);

        // Garante que só um AudioListener fique ativo por vez
        AudioListener listenerAtivar = paraAtivar.GetComponent<AudioListener>();
        AudioListener listenerDesativar = paraDesativar.GetComponent<AudioListener>();
        if (listenerAtivar != null) listenerAtivar.enabled = true;
        if (listenerDesativar != null) listenerDesativar.enabled = false;
    }
}