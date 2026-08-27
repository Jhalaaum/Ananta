using UnityEngine;
using UnityEngine.InputSystem;

public class MouseLook : MonoBehaviour
{
    [Header("Settings")]
    public float mouseSensitivity = 0.5f;

    [Header("Normal Look Limits")]
    public float normalLookDown = -90f;
    public float normalLookUp = 90f;

    [Header("Climbing Look Limits")]
    [Tooltip("Maximum camera yaw left/right while attached to a wall or ladder.")]
    public float climbingYawLimit = 55f;

    [Tooltip("How far downward the player can look while climbing.")]
    public float climbingLookDown = -55f;

    [Tooltip("How far upward the player can look while climbing.")]
    public float climbingLookUp = 70f;

    [Tooltip("How quickly camera yaw returns to center after leaving a climb.")]
    public float climbingYawReturnSpeed = 10f;

    [Header("Lean Tilt")]
    public float leanAngle = 20f;
    public float leanSpeed = 10f;

    [Header("References")]
    public Transform playerBody;
    public PlayerMovement playerMovement;

    private float xRotation = 0f;

    // Local camera yaw used only while climbing.
    private float climbingYaw = 0f;

    private float currentLeanDirection = 0f;
    private float targetLeanDirection = 0f;

    private void Awake()
    {
        if (playerMovement == null && playerBody != null)
        {
            playerMovement =
                playerBody.GetComponent<PlayerMovement>();
        }

        if (playerMovement == null)
        {
            playerMovement =
                GetComponentInParent<PlayerMovement>();
        }
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void Update()
    {
        HandleRotation();
        HandleLeanTilt();
        ApplyCameraRotation();
    }

    private void HandleRotation()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta =
            Mouse.current.delta.ReadValue();

        float mouseX =
            mouseDelta.x * mouseSensitivity;

        float mouseY =
            mouseDelta.y * mouseSensitivity;

        bool isClimbing =
            playerMovement != null &&
            (
                playerMovement.IsWallClimbing ||
                playerMovement.IsLadderClimbing
            );

        // ------------------------------
        // PITCH
        // ------------------------------

        xRotation -= mouseY;

        if (isClimbing)
        {
            xRotation =
                Mathf.Clamp(
                    xRotation,
                    climbingLookDown,
                    climbingLookUp
                );
        }
        else
        {
            xRotation =
                Mathf.Clamp(
                    xRotation,
                    normalLookDown,
                    normalLookUp
                );
        }

        // ------------------------------
        // YAW
        // ------------------------------

        if (isClimbing)
        {
            // Body stays facing wall.
            // Camera gets independent limited yaw.
            climbingYaw += mouseX;

            climbingYaw =
                Mathf.Clamp(
                    climbingYaw,
                    -climbingYawLimit,
                    climbingYawLimit
                );
        }
        else
        {
            // Normal FPS behaviour.
            if (playerBody != null)
            {
                playerBody.Rotate(
                    Vector3.up * mouseX
                );
            }

            // Return camera to body-forward after climbing.
            climbingYaw =
                Mathf.Lerp(
                    climbingYaw,
                    0f,
                    Time.deltaTime *
                    climbingYawReturnSpeed
                );

            if (Mathf.Abs(climbingYaw) < 0.05f)
            {
                climbingYaw = 0f;
            }
        }
    }

    private void HandleLeanTilt()
    {
        bool climbing =
            playerMovement != null &&
            (
                playerMovement.IsWallClimbing ||
                playerMovement.IsLadderClimbing ||
                playerMovement.IsMantling
            );

        // No leaning while climbing.
        if (climbing)
        {
            targetLeanDirection = 0f;
        }
        else if (Keyboard.current == null)
        {
            targetLeanDirection = 0f;
        }
        else if (Keyboard.current.bKey.isPressed)
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

        currentLeanDirection =
            Mathf.Lerp(
                currentLeanDirection,
                targetLeanDirection,
                Time.deltaTime *
                leanSpeed
            );
    }

    private void ApplyCameraRotation()
    {
        float zRoll =
            -currentLeanDirection *
            leanAngle;

        transform.localRotation =
            Quaternion.Euler(
                xRotation,
                climbingYaw,
                zRoll
            );
    }
}