using UnityEngine;

public class WindFlag : MonoBehaviour
{
    public Transform flag;
    public float smooth = 3f;
    public float offset = 0f;

    // Valor recebido do sistema de vento
    [SerializeField] public SailSystem sailSystem;

    void Update()
    {
        float angle = 0;
        //float angle = sailSystem.windAngleFromBow + offset;

        Quaternion target = Quaternion.Euler(
            0,
            angle,
            0
        );

        flag.localRotation = Quaternion.Slerp(
            flag.localRotation,
            target,
            Time.deltaTime * smooth
        );
    }
}