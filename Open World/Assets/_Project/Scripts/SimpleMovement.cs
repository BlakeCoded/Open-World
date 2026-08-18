using UnityEngine;
using UnityEngine.InputSystem;

public class SimpleMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;

        if (keyboard == null)
            return;

        // Rotate
        if (keyboard.aKey.isPressed)
        {
            transform.Rotate(Vector3.up, -rotationSpeed * Time.deltaTime);
        }
        else if (keyboard.dKey.isPressed)
        {
            transform.Rotate(Vector3.up, rotationSpeed * Time.deltaTime);
        }

        // Move
        if (keyboard.wKey.isPressed)
        {
            transform.position += transform.forward * moveSpeed * Time.deltaTime;
            //rb.AddForce(transform.forward * moveSpeed * Time.deltaTime, ForceMode.Impulse);
        }
        else if (keyboard.sKey.isPressed)
        {
            transform.position -= transform.forward * moveSpeed * Time.deltaTime;
            //rb.AddForce(-transform.forward * moveSpeed * Time.deltaTime, ForceMode.Impulse);
        }
    }
}