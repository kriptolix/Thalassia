using UnityEngine;

public class FlyCamera : MonoBehaviour
{
    public float moveSpeed = 20f;
    public float lookSpeed = 2f;

    float rotationX = 0f;
    float rotationY = 0f;

    void Update()
    {
        if (Input.GetMouseButton(1))
        {
            rotationX += Input.GetAxis("Mouse X") * lookSpeed;
            rotationY -= Input.GetAxis("Mouse Y") * lookSpeed;
            rotationY = Mathf.Clamp(rotationY, -90f, 90f);

            transform.rotation = Quaternion.Euler(rotationY, rotationX, 0f);
        }

        Vector3 dir = new Vector3(
            Input.GetAxis("Horizontal"),
            0,
            Input.GetAxis("Vertical"));

        if (Input.GetKey(KeyCode.E)) dir.y += 1;
        if (Input.GetKey(KeyCode.Q)) dir.y -= 1;

        transform.Translate(dir * moveSpeed * Time.deltaTime, Space.Self);
    }
}