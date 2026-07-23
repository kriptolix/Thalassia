using UnityEngine;

public class CompassNeedle : MonoBehaviour
{
    public Transform needle;
    [SerializeField] public ShipMovementSystem shipMovementSystem;

    public float smooth = 8f;
    public float offset = 0f;

    void Update()
    {
        float heading = shipMovementSystem.CurrentShipHeadingDeg;

        float targetAngle = heading + offset;

        Quaternion targetRotation = Quaternion.Euler(
            targetAngle,
            0,
            0
        );

        needle.localRotation = Quaternion.Slerp(
            needle.localRotation,
            targetRotation,
            Time.deltaTime * smooth
        );
    }
}