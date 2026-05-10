using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 0.5f;

    [Header("Lean Tilt")]
    public float leanAngle = 20f;
    public float leanSpeed = 10f;

    [Header("References")]
    public Transform playerBody;

    private float xRotation = 0f;
    private float currentLeanDirection = 0f;
    private float targetLeanDirection = 0f;

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update()
    {
        HandleRotation();
        HandleLeanTilt();
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        if (playerBody != null)
            playerBody.Rotate(Vector3.up * mouseX);
    }

    private void HandleLeanTilt()
    {
        if (Keyboard.current == null)
        {
            targetLeanDirection = 0f;
        }
        else if (Keyboard.current.qKey.isPressed)
        {
            targetLeanDirection = -1f;
        }
        else if (Keyboard.current.vKey.isPressed)
        {
            targetLeanDirection = 1f;
        }
        else
        {
            targetLeanDirection = 0f;
        }

        currentLeanDirection = Mathf.Lerp(
            currentLeanDirection,
            targetLeanDirection,
            Time.deltaTime * leanSpeed
        );

        float zRoll = -currentLeanDirection * leanAngle;

        transform.localRotation = Quaternion.Euler(
            xRotation,
            0f,
            zRoll
        );
    }
}