using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class PlayerMovementStateTracker : MonoBehaviour
{
    public static PlayerMovementStateTracker Instance;

    [Header("Authoritative Movement")]
    public PlayerMovement playerMovement;

    [Header("Runtime State")]
    public PlayerMovementVisualState currentState =
        PlayerMovementVisualState.Idle;

    public float currentHorizontalSpeed;
    public bool isGrounded;
    public bool isSprintingInput;
    public bool isCrouchingInput;
    public bool isCrawlingInput;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private PlayerMovementVisualState previousState;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;

        if (playerMovement == null)
        {
            playerMovement =
                GetComponent<PlayerMovement>();
        }
    }

    private void Update()
    {
        if (playerMovement == null)
        {
            return;
        }

        UpdateCompatibilityValues();
        ResolveVisualState();
        DebugStateChange();
    }

    private void UpdateCompatibilityValues()
    {
        Vector3 horizontalVelocity =
            playerMovement.CurrentHorizontalVelocity;

        horizontalVelocity.y = 0f;

        currentHorizontalSpeed =
            horizontalVelocity.magnitude;

        isGrounded =
            playerMovement.IsGroundedPublic;

        isSprintingInput =
            playerMovement.IsSprinting;

        isCrouchingInput =
            playerMovement.IsCrouching;

        isCrawlingInput =
            playerMovement.IsCrawling;
    }

    private void ResolveVisualState()
    {
        switch (playerMovement.CurrentMovementState)
        {
            case PlayerMovement.MovementState.Idle:
                currentState =
                    PlayerMovementVisualState.Idle;
                break;

            case PlayerMovement.MovementState.Walk:
            case PlayerMovement.MovementState.SlowWalk:
                currentState =
                    PlayerMovementVisualState.Walking;
                break;

            case PlayerMovement.MovementState.Sprint:
                currentState =
                    PlayerMovementVisualState.Running;
                break;

            case PlayerMovement.MovementState.Crouch:
                currentState =
                    PlayerMovementVisualState.Crouching;
                break;

            case PlayerMovement.MovementState.Crawl:
                currentState =
                    PlayerMovementVisualState.Crawling;
                break;

            case PlayerMovement.MovementState.Jump:
                currentState =
                    PlayerMovementVisualState.Jumping;
                break;

            case PlayerMovement.MovementState.Falling:
                currentState =
                    PlayerMovementVisualState.Falling;
                break;

            case PlayerMovement.MovementState.ClimbMantle:
            case PlayerMovement.MovementState.WallClimb:
            case PlayerMovement.MovementState.LadderClimb:
                // Existing visual enum has no Climbing state.
                // Jumping is the safest compatibility state for now.
                currentState =
                    PlayerMovementVisualState.Jumping;
                break;

            case PlayerMovement.MovementState.Interact:
            case PlayerMovement.MovementState.Hide:
                currentState =
                    PlayerMovementVisualState.Idle;
                break;

            default:
                currentState =
                    PlayerMovementVisualState.Idle;
                break;
        }
    }

    public PlayerMovementVisualState GetCurrentState()
    {
        return currentState;
    }

    public bool IsNormalPublicMovement()
    {
        return currentState ==
                   PlayerMovementVisualState.Idle ||
               currentState ==
                   PlayerMovementVisualState.Walking;
    }

    public bool IsVisuallySuspiciousInPublic()
    {
        if (playerMovement != null &&
            (playerMovement.IsMantling ||
             playerMovement.IsWallClimbing ||
             playerMovement.IsLadderClimbing))
        {
            return true;
        }

        return
            currentState ==
                PlayerMovementVisualState.Running ||
            currentState ==
                PlayerMovementVisualState.Crouching ||
            currentState ==
                PlayerMovementVisualState.Crawling ||
            currentState ==
                PlayerMovementVisualState.Jumping ||
            currentState ==
                PlayerMovementVisualState.Falling;
    }

    // Retained for old callers.
    // PlayerMovement now owns crouch/crawl state,
    // so this no longer manipulates keyboard toggles.
    public void ForceClearStealthToggles()
    {
        if (playerMovement != null)
        {
            playerMovement.ForceStandIfPossible();
        }
    }

    private void DebugStateChange()
    {
        if (!showDebugLogs)
        {
            return;
        }

        if (previousState == currentState)
        {
            return;
        }

        Debug.Log(
            "[PlayerMovementStateTracker] State: " +
            currentState +
            " | PlayerMovement: " +
            playerMovement.CurrentMovementState +
            " | Speed: " +
            currentHorizontalSpeed.ToString("0.00")
        );

        previousState = currentState;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}