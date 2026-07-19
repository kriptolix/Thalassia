using UnityEngine;

/// <summary>
/// Escuta PlayerControlSystem.OnResetTest e reposiciona o barco para um
/// estado limpo (posição/rotação de spawn, velocidade linear e angular
/// zeradas). Necessário para testar isoladamente vela/leme sem herdar giro
/// ou velocidade de um teste anterior.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class ShipTestResetManager : MonoBehaviour
{
    [SerializeField] private PlayerControlSystem playerControlSystem;

    private Rigidbody _rigidBody;
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;

    private void Awake()
    {
        _rigidBody = GetComponent<Rigidbody>();
        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void OnEnable()
    {
        if (playerControlSystem != null)
            playerControlSystem.OnResetTest += ResetShip;
    }

    private void OnDisable()
    {
        if (playerControlSystem != null)
            playerControlSystem.OnResetTest -= ResetShip;
    }

    private void ResetShip()
    {
        _rigidBody.linearVelocity = Vector3.zero;
        _rigidBody.angularVelocity = Vector3.zero;
        transform.SetPositionAndRotation(_spawnPosition, _spawnRotation);
        Debug.Log("[ShipTestResetManager] Barco resetado para o spawn, velocidade e giro zerados.");
    }
}