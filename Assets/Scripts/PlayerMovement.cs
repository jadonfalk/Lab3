using UnityEngine;

#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private Camera gameCamera;
    [SerializeField] private float moveSpeed = 6f;

    // Set this to approximately half the player's visible width.
    [SerializeField] private float edgePadding = 0.5f;

    private void Awake()
    {
        if (gameCamera == null)
            gameCamera = Camera.main;
    }

    private void Update()
    {
        if (gameCamera == null)
            return;

        float horizontal = 0f;

#if ENABLE_INPUT_SYSTEM
        Keyboard keyboard = Keyboard.current;

        if (keyboard != null)
        {
            if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed)
                horizontal -= 1f;

            if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed)
                horizontal += 1f;
        }
#else
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            horizontal -= 1f;

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            horizontal += 1f;
#endif

        // Change only X, preserving the player's Y and Z.
        Vector3 position = transform.position;
        position.x += horizontal * moveSpeed * Time.deltaTime;

        // Orthographic size is half the camera's visible height.
        float halfWidth = gameCamera.orthographicSize * gameCamera.aspect;
        float availableWidth = Mathf.Max(0f, halfWidth - edgePadding);

        float leftEdge = gameCamera.transform.position.x - availableWidth;
        float rightEdge = gameCamera.transform.position.x + availableWidth;

        position.x = Mathf.Clamp(position.x, leftEdge, rightEdge);

        transform.position = position;
    }
}