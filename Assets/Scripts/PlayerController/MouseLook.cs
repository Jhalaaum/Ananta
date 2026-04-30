using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 0.5f;
    public float leanAngle = 20f;
    public float leanDistance = 0.7f;
    public float leanSpeed = 10f;

    [Header("Collision Settings")]
    public float leanCheckRadius = 0.2f;
    public LayerMask obstacleLayers;
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
        float checkDir = targetLean > 0 ? -1f : 1f;
        if (targetLean == 0) checkDir = 0;

        float actualMaxDistance = leanDistance;

        if (targetLean != 0)
        {
            Vector3 rayOrigin = transform.parent.TransformPoint(new Vector3(cameraOffset.x, transform.localPosition.y, cameraOffset.z));
            Vector3 rayDirection = transform.parent.right * checkDir;

            if (Physics.SphereCast(rayOrigin, leanCheckRadius, rayDirection, out RaycastHit hit, leanDistance, obstacleLayers))
            {
                actualMaxDistance = Mathf.Max(0, hit.distance - 0.05f);
                
                if (actualMaxDistance < 0.1f) targetLean = 0;
            }
        }

        currentLean = Mathf.Lerp(currentLean, targetLean, Time.deltaTime * leanSpeed);

        float leanPercent = currentLean / leanAngle;
        float horizontalOffset = leanPercent * actualMaxDistance;

        transform.localPosition = new Vector3(
            cameraOffset.x - horizontalOffset, 
            transform.localPosition.y, 
            cameraOffset.z
        );
    }
}