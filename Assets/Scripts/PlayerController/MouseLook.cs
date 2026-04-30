// using UnityEngine;
// using UnityEngine.InputSystem;

// public class MouseLook : MonoBehaviour
// {
//     [Header("Settings")]
//     public float mouseSensitivity = 0.5f;
//     public float leanAngle = 20f;
//     public float leanDistance = 0.5f;
//     public float leanSpeed = 10f;

//     [Header("References")]
//     public Transform playerBody;
    
//     private float xRotation = 0f;
//     private float currentLean = 0f;
//     private float targetLean = 0f;
//     private Vector3 cameraOffset;

//     void Start()
//     {
//         Cursor.lockState = CursorLockMode.Locked;
//         // Save the camera's starting position relative to the player
//         cameraOffset = transform.localPosition;
//     }

//     void Update()
//     {
//         HandleRotation();
//         HandleLeaning();
//     }

//     void HandleRotation()
//     {
//         Vector2 mouseDelta = Mouse.current.delta.ReadValue();

//         float mouseX = mouseDelta.x * mouseSensitivity;
//         float mouseY = mouseDelta.y * mouseSensitivity;

//         xRotation -= mouseY;
//         xRotation = Mathf.Clamp(xRotation, -90f, 90f);

//         // Apply vertical rotation to camera and horizontal to body
//         transform.localRotation = Quaternion.Euler(xRotation, 0f, currentLean);
//         playerBody.Rotate(Vector3.up * mouseX);
//     }
// }
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

        transform.localRotation = Quaternion.Euler(xRotation, 0f, currentLean);
        playerBody.Rotate(Vector3.up * mouseX);
    }

    void HandleLeaning()
    {
        if (Keyboard.current.qKey.isPressed)
            targetLean = leanAngle;
        else if (Keyboard.current.eKey.isPressed)
            targetLean = -leanAngle;
        else
            targetLean = 0f;

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        float horizontalOffset = (currentLean / leanAngle) * leanDistance;

        // FIX: Instead of using cameraOffset.y, keep the CURRENT y position 
        // set by the PlayerMovement script.
        transform.localPosition = new Vector3(
            cameraOffset.x - horizontalOffset, 
            transform.localPosition.y, // This is the magic line
            cameraOffset.z
        );
}}