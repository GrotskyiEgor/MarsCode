
using UnityEngine;
using UnityEngine.InputSystem;

public class CameraMove : MonoBehaviour
{
    [Header("Movement Settings")]
    public bool canMove = true;

    public float edgeSpeed = 30f;
    public float dragSensitivity = 0.08f;
    public float edgeBorder = 20f;

    [Header("Camera Limits")]
    public float maxDistance = 50f;

    private Vector3 startPosition;

    private void Start()
    {
        startPosition = transform.position;
    }

    private void Update()
    {
        if (!canMove || Mouse.current == null)
            return;

        Vector3 movement = Vector3.zero;
        Vector2 mousePosition = Mouse.current.position.ReadValue();

        if (Mouse.current.rightButton.isPressed)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();

            movement.x -= mouseDelta.x * dragSensitivity;
            movement.z -= mouseDelta.y * dragSensitivity;
        }
        // else
        // {
        //     // Перемещение при наведении на края экрана

        //     if (mousePosition.x <= edgeBorder)
        //         movement.x -= edgeSpeed * Time.deltaTime;

        //     if (mousePosition.x >= Screen.width - edgeBorder)
        //         movement.x += edgeSpeed * Time.deltaTime;

        //     if (mousePosition.y <= edgeBorder)
        //         movement.z -= edgeSpeed * Time.deltaTime;

        //     if (mousePosition.y >= Screen.height - edgeBorder)
        //         movement.z += edgeSpeed * Time.deltaTime;
        // }

        Vector3 newPosition = transform.position + movement;

        newPosition.x = Mathf.Clamp(
            newPosition.x,
            startPosition.x - maxDistance,
            startPosition.x + maxDistance
        );

        newPosition.z = Mathf.Clamp(
            newPosition.z,
            startPosition.z - maxDistance,
            startPosition.z + maxDistance
        );

        transform.position = newPosition;
    }
}
