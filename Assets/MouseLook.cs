using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 0.5f;
    public float leanAngle = 20f;
    public float leanDistance = 0.5f;
    public float leanSpeed = 10f;

    [Header("References")]
    public Transform playerBody;
    
    private float xRotation = 0f;
    private float currentLean = 0f;
    private float targetLean = 0f;
    private Vector3 cameraOffset;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        // Save the camera's starting position relative to the player
        cameraOffset = transform.localPosition;
    }

    void Update()
    {
        HandleRotation();
        HandleLeaning();
    }

    void HandleRotation()
    {
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        float mouseX = mouseDelta.x * mouseSensitivity;
        float mouseY = mouseDelta.y * mouseSensitivity;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);

        // Apply vertical rotation to camera and horizontal to body
        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentLean);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleLeaning()
    {
        // Check keys (In a real project, use Input Action Assets for this!)
        if (Keyboard.current.qKey.isPressed)
            targetLean = leanAngle;
        else if (Keyboard.current.eKey.isPressed)
            targetLean = -leanAngle;
        else
            targetLean = 0f;

        // Smoothly interpolate the lean angle
        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        // Move camera position slightly to the side while leaning
        float horizontalOffset = (currentLean / leanAngle) * leanDistance;
        transform.localPosition = new Vector3(cameraOffset.x - horizontalOffset, cameraOffset.y, cameraOffset.z);
    }
}