using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

/// <summary>
/// Player Control System — camada intermediária entre o jogador e os sistemas
/// físicos. Não possui lógica de física/movimentação/simulação: apenas lê o
/// Unity Input System (via o asset PlayerControls.inputactions) e distribui
/// comandos.
///
/// Comandos contínuos (leme, vela, câmera) são enviados diretamente por
/// chamada de método aos sistemas consumidores (SailSystem, RudderSystem) a
/// cada frame, e também expostos publicamente para HUD/depuração.
///
/// Comandos sem sistema consumidor ainda (F1-F5, F de interação) são expostos
/// via evento C# (Action) e UnityEvent, para sistemas futuros assinarem.
/// </summary>
[DisallowMultipleComponent]
public class PlayerControlSystem : MonoBehaviour
{
    [Header("Input Actions")]
    [Tooltip("Asset gerado (PlayerControls.inputactions), com os mapas 'Gameplay' e 'Debug'.")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Referências — comandos contínuos enviados diretamente")]
    [SerializeField] private SailSystem sailSystem;
    [SerializeField] private RudderSystem rudderSystem;

    [Header("Câmera — Limites (graus)")]
    [Tooltip("A spec descreve limites de câmera dentro deste próprio sistema; como o Camera System ainda não existe, este script acumula e limita o ângulo internamente, além de expor o LookInput bruto (delta por frame) conforme a interface pública original.")]
    [SerializeField] private float lookHorizontalMin = -180f;
    [SerializeField] private float lookHorizontalMax = 180f;
    [SerializeField] private float lookVerticalMin = -60f;
    [SerializeField] private float lookVerticalMax = 60f;

    [Header("Eventos — Interação e Depuração (sem sistema consumidor ainda)")]
    [SerializeField] private UnityEvent onInteractUnityEvent;
    [SerializeField] private UnityEvent onPauseToggleUnityEvent;
    [SerializeField] private UnityEvent onNormalSpeedUnityEvent;
    [SerializeField] private UnityEvent onToggleHUDUnityEvent;
    [SerializeField] private UnityEvent onDebugModeToggleUnityEvent;
    [SerializeField] private UnityEvent onResetTestUnityEvent;

    public event Action OnInteract;
    public event Action OnPauseToggle;
    public event Action OnNormalSpeed;
    public event Action OnToggleHUD;
    public event Action OnDebugModeToggle;
    public event Action OnResetTest;

    private InputAction _rudderAction;
    private InputAction _sailAngleAction;
    private InputAction _sailOpenAction;
    private InputAction _lookAction;
    private InputAction _interactAction;
    private InputAction _pauseAction;
    private InputAction _normalSpeedAction;
    private InputAction _toggleHudAction;
    private InputAction _debugModeAction;
    private InputAction _resetTestAction;

    // --- Interface Pública (valores normalizados) ---
    public float RudderInput { get; private set; }
    public float SailAngleInput { get; private set; }
    public float SailOpenInput { get; private set; }
    public Vector2 LookInput { get; private set; }

    /// <summary>Ângulos de câmera acumulados e limitados (X=horizontal, Y=vertical). Ver tooltip de "Câmera — Limites".</summary>
    public Vector2 CurrentLookAngles { get; private set; }

    public bool DebugModeActive { get; private set; }
    public bool IsPaused { get; private set; }

    private void Awake()
    {
        if (inputActions == null)
        {
            Debug.LogError($"{nameof(PlayerControlSystem)}: nenhum InputActionAsset atribuído no Inspector.");
            return;
        }

        InputActionMap gameplay = inputActions.FindActionMap("Gameplay", throwIfNotFound: true);
        _rudderAction = gameplay.FindAction("RudderInput");
        _sailAngleAction = gameplay.FindAction("SailAngleInput");
        _sailOpenAction = gameplay.FindAction("SailOpenInput");
        _lookAction = gameplay.FindAction("LookInput");
        _interactAction = gameplay.FindAction("Interact");

        InputActionMap debugMap = inputActions.FindActionMap("Debug", throwIfNotFound: true);
        _pauseAction = debugMap.FindAction("PauseToggle");
        _normalSpeedAction = debugMap.FindAction("NormalSpeed");
        _toggleHudAction = debugMap.FindAction("ToggleHUD");
        _debugModeAction = debugMap.FindAction("DebugMode");
        _resetTestAction = debugMap.FindAction("ResetTest");

        _interactAction.performed += _ => RaiseInteract();
        _pauseAction.performed += _ => RaisePauseToggle();
        _normalSpeedAction.performed += _ => RaiseNormalSpeed();
        _toggleHudAction.performed += _ => RaiseToggleHUD();
        _debugModeAction.performed += _ => RaiseDebugModeToggle();
        _resetTestAction.performed += _ => RaiseResetTest();
    }

    private void OnEnable()
    {
        inputActions?.Enable();
    }

    private void OnDisable()
    {
        inputActions?.Disable();
    }

    private void Update()
    {
        if (inputActions == null) return;

        RudderInput = _rudderAction.ReadValue<float>();
        SailAngleInput = _sailAngleAction.ReadValue<float>();
        SailOpenInput = _sailOpenAction.ReadValue<float>();
        LookInput = _lookAction.ReadValue<Vector2>();

        Vector2 accumulated = CurrentLookAngles + LookInput;
        accumulated.x = Mathf.Clamp(accumulated.x, lookHorizontalMin, lookHorizontalMax);
        accumulated.y = Mathf.Clamp(accumulated.y, lookVerticalMin, lookVerticalMax);
        CurrentLookAngles = accumulated;

        // Envio direto dos comandos contínuos aos sistemas consumidores.
        if (sailSystem != null)
        {
            // SetSailModeInput (antes SetSailAngleInput): o SailSystem foi
            // revisado para usar MODOS DE NAVEGAÇÃO (Contra o Vento, Bolina,
            // Través, Largo, Popa) em vez de ângulo em graus - o mesmo sinal
            // -1/0/+1 de SailAngleInput agora seleciona entre os modos.
            sailSystem.SetSailModeInput(SailAngleInput);
            sailSystem.SetSailOpenInput(SailOpenInput);
        }

        if (rudderSystem != null)
        {
            rudderSystem.SetRudderInput(RudderInput);
        }
    }

    private void RaiseInteract()
    {
        OnInteract?.Invoke();
        onInteractUnityEvent?.Invoke();
    }

    private void RaisePauseToggle()
    {
        IsPaused = !IsPaused;
        Time.timeScale = IsPaused ? 0f : 1f;
        OnPauseToggle?.Invoke();
        onPauseToggleUnityEvent?.Invoke();
    }

    private void RaiseNormalSpeed()
    {
        IsPaused = false;
        Time.timeScale = 1f;
        OnNormalSpeed?.Invoke();
        onNormalSpeedUnityEvent?.Invoke();
    }

    private void RaiseToggleHUD()
    {
        OnToggleHUD?.Invoke();
        onToggleHUDUnityEvent?.Invoke();
    }

    private void RaiseDebugModeToggle()
    {
        DebugModeActive = !DebugModeActive;
        OnDebugModeToggle?.Invoke();
        onDebugModeToggleUnityEvent?.Invoke();
    }

    private void RaiseResetTest()
    {
        // Este sistema apenas SINALIZA a intenção de reset (não altera posição do barco
        // diretamente, conforme a spec). Um script de teste/gerenciador deve assinar
        // OnResetTest (ou o UnityEvent) e reposicionar o Rigidbody da embarcação.
        OnResetTest?.Invoke();
        onResetTestUnityEvent?.Invoke();
    }

    private void OnValidate()
    {
        if (lookHorizontalMin > lookHorizontalMax) lookHorizontalMax = lookHorizontalMin;
        if (lookVerticalMin > lookVerticalMax) lookVerticalMax = lookVerticalMin;
    }
}