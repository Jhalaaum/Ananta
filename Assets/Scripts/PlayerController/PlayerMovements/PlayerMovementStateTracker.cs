using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovementStateTracker : MonoBehaviour
{
    public static PlayerMovementStateTracker Instance;

    [Header("Movement References")]
    public CharacterController characterController;
    public Rigidbody playerRigidbody;

    [Header("Input Mode")]
    public bool crouchIsToggle = true;
    public bool crawlIsToggle = true;

    [Header("Input Keys")]
    public Key sprintKey = Key.LeftShift;
    public Key crouchKey = Key.C;
    public Key crawlKey = Key.X;

    [Header("Speed Thresholds")]
    public float idleSpeedThreshold = 0.08f;
    public float runningSpeedThreshold = 4.5f;

    [Header("Ground Check")]
    public bool useCharacterControllerGrounded = true;
    public bool assumeGroundedIfNoController = true;

    [Header("Runtime Toggle State")]
    public bool crouchToggleActive;
    public bool crawlToggleActive;

    [Header("Runtime State")]
    public PlayerMovementVisualState currentState = PlayerMovementVisualState.Idle;
    public float currentHorizontalSpeed;
    public bool isGrounded;
    public bool isSprintingInput;
    public bool isCrouchingInput;
    public bool isCrawlingInput;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private PlayerMovementVisualState previousState;

    private void Awake()
    {
        Instance = this;

        if (characterController == null)
        {
            characterController = GetComponent<CharacterController>();
        }

        if (playerRigidbody == null)
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }
    }

    private void Update()
    {
        ReadInputs();
        UpdateMovementValues();
        ResolveState();
        DebugStateChange();
    }

    private void ReadInputs()
    {
        isSprintingInput = false;
        isCrouchingInput = false;
        isCrawlingInput = false;

        if (Keyboard.current == null)
        {
            return;
        }

        isSprintingInput = Keyboard.current[sprintKey].isPressed;

        if (crawlIsToggle)
        {
            if (Keyboard.current[crawlKey].wasPressedThisFrame)
            {
                crawlToggleActive = !crawlToggleActive;

                if (crawlToggleActive)
                {
                    crouchToggleActive = false;
                }
            }

            isCrawlingInput = crawlToggleActive;
        }
        else
        {
            isCrawlingInput = Keyboard.current[crawlKey].isPressed;
        }

        if (crouchIsToggle)
        {
            if (Keyboard.current[crouchKey].wasPressedThisFrame)
            {
                crouchToggleActive = !crouchToggleActive;

                if (crouchToggleActive)
                {
                    crawlToggleActive = false;
                }
            }

            isCrouchingInput = crouchToggleActive;
        }
        else
        {
            isCrouchingInput = Keyboard.current[crouchKey].isPressed;
        }

        // Sprinting should usually cancel stealth posture.
        if (isSprintingInput)
        {
            crouchToggleActive = false;
            crawlToggleActive = false;
            isCrouchingInput = false;
            isCrawlingInput = false;
        }
    }

    private void UpdateMovementValues()
    {
        Vector3 velocity = Vector3.zero;

        if (characterController != null)
        {
            velocity = characterController.velocity;

            isGrounded = useCharacterControllerGrounded
                ? characterController.isGrounded
                : true;
        }
        else if (playerRigidbody != null)
        {
            velocity = playerRigidbody.linearVelocity;
            isGrounded = assumeGroundedIfNoController;
        }
        else
        {
            isGrounded = assumeGroundedIfNoController;
        }

        Vector3 horizontalVelocity = new Vector3(velocity.x, 0f, velocity.z);
        currentHorizontalSpeed = horizontalVelocity.magnitude;
    }

    private void ResolveState()
    {
        if (!isGrounded)
        {
            if (characterController != null && characterController.velocity.y > 0.1f)
            {
                currentState = PlayerMovementVisualState.Jumping;
            }
            else
            {
                currentState = PlayerMovementVisualState.Falling;
            }

            return;
        }

        if (isCrawlingInput)
        {
            currentState = PlayerMovementVisualState.Crawling;
            return;
        }

        if (isCrouchingInput)
        {
            currentState = PlayerMovementVisualState.Crouching;
            return;
        }

        if (currentHorizontalSpeed <= idleSpeedThreshold)
        {
            currentState = PlayerMovementVisualState.Idle;
            return;
        }

        if (isSprintingInput || currentHorizontalSpeed >= runningSpeedThreshold)
        {
            currentState = PlayerMovementVisualState.Running;
            return;
        }

        currentState = PlayerMovementVisualState.Walking;
    }

    public PlayerMovementVisualState GetCurrentState()
    {
        return currentState;
    }

    public void ForceClearStealthToggles()
    {
        crouchToggleActive = false;
        crawlToggleActive = false;
        isCrouchingInput = false;
        isCrawlingInput = false;
    }

    public bool IsNormalPublicMovement()
    {
        return currentState == PlayerMovementVisualState.Idle ||
               currentState == PlayerMovementVisualState.Walking;
    }

    public bool IsVisuallySuspiciousInPublic()
    {
        return currentState == PlayerMovementVisualState.Running ||
               currentState == PlayerMovementVisualState.Crouching ||
               currentState == PlayerMovementVisualState.Crawling ||
               currentState == PlayerMovementVisualState.Jumping ||
               currentState == PlayerMovementVisualState.Falling;
    }

    private void DebugStateChange()
    {
        if (!showDebugLogs)
        {
            return;
        }

        if (previousState != currentState)
        {
            Debug.Log(
                $"[PlayerMovementStateTracker] State: {currentState}, " +
                $"Speed: {currentHorizontalSpeed}, " +
                $"CrouchToggle: {crouchToggleActive}, " +
                $"CrawlToggle: {crawlToggleActive}"
            );

            previousState = currentState;
        }
    }
}