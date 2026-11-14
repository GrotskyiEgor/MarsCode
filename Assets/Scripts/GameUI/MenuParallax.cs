using UnityEngine;
using UnityEngine.InputSystem;

public class MenuParallax : MonoBehaviour
{
    [SerializeField] public float offsetMultiplier = 1f;
    [SerializeField] public float smoothTime = .3f;

    private Vector3 startPosition;
    private Vector3 velocity;
    private bool isStartPositionSaved = false;

    void Start()
    {

    }

    void Update()
    {
        if (!isStartPositionSaved)
        {
            startPosition = transform.position;
            isStartPositionSaved = true;
        }

        Vector2 mouseViewportPos = Camera.main.ScreenToViewportPoint(Mouse.current.position.ReadValue());
        Vector2 offset = mouseViewportPos - new Vector2(0.5f, 0.5f);

        Vector3 targetPosition = startPosition + ((Vector3)offset * offsetMultiplier);
        
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
    }
}
