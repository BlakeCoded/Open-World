using UnityEngine;
using UnityEngine.InputSystem;
using WorldGen.Terrain;

public class SimpleMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotationSpeed = 180f;

    private Rigidbody rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    float timer;

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

        if(keyboard.spaceKey.isPressed)
        {
            transform.position += transform.up * moveSpeed * Time.deltaTime;
        }
        else if (keyboard.shiftKey.isPressed)
        {
            transform.position -= transform.up * moveSpeed * Time.deltaTime;
        }

        if (keyboard.tKey.isPressed)
        {
            if (timer <= 0)
            {
                ChunkManager.Instance.OnReload();
                gameObject.transform.SetPositionAndRotation(new Vector3(Random.Range(-100000, 100000), 30f, Random.Range(-100000, 100000)), Quaternion.identity);
                timer = 5f;
            }
        }

        timer -= Time.deltaTime;
    }
}