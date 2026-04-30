using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Speeds")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 20f;
    public float crouchSpeed = 3f;

    [Header("Sprint Timing")]
    public float sprintBuildUpTime = 4f; // Reduced for better gameplay feel
    public float sprintFullTime = 5f;
    public float sprintDecayTime = 3f;
    public float sprintCooldownTime = 5f;

    [Header("Crouch Settings")]
    public float crouchHeight = 1f;
    public float standingHeight = 2f;
    public Transform playerCamera;
    public float crouchTransitionSpeed = 10f;
    [Range(0, 1)] public float eyeLevelRatio = 0.9f; 

    [Header("General")]
    public float gravity = -19.81f; // Slightly stronger gravity feels "snappier"
    public LayerMask playerLayer;

    private Vector3 velocity;
    private float currentSpeed;
    private float sprintTimer = 0f;
    private bool needsRepress = false;
    private bool isCrouching;
    private float targetCameraY;

    private enum SprintState { Ready, Building, Full, Decaying, Cooldown }
    private SprintState sprintState = SprintState.Ready;

    void Start()
    {
        targetCameraY = standingHeight * eyeLevelRatio;
    }

    void Update()
    {
        HandleCrouch();
        HandleSprintState();
        HandleMovement();
        HandleGravity();
        
        // Smoothly lerp the camera position every frame
        ApplyCameraSmoothing();
    }

    void HandleSprintState()
    {
        bool shiftHeld = Keyboard.current.shiftKey.isPressed;
        bool isMoving = HasMovementInput();

        // Fix: Reset needsRepress if we stop moving OR if we are back to Ready
        if (!isMoving || sprintState == SprintState.Ready) needsRepress = false;

        switch (sprintState)
        {
            case SprintState.Ready:
                if (shiftHeld && isMoving) { 
                    isCrouching = false; // Auto-stand when sprinting
                    sprintState = SprintState.Building; 
                    sprintTimer = 0; 
                }
                break;
            case SprintState.Building:
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= sprintBuildUpTime) { sprintState = SprintState.Full; sprintTimer = 0; }
                if (!shiftHeld || !isMoving) sprintState = SprintState.Ready;
                break;
            case SprintState.Full:
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= sprintFullTime || !shiftHeld || !isMoving) 
                { 
                    sprintState = SprintState.Decaying; 
                    sprintTimer = 0; 
                    needsRepress = true;
                }
                break;
            case SprintState.Decaying:
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= sprintDecayTime) { sprintState = SprintState.Cooldown; sprintTimer = 0; }
                break;
            case SprintState.Cooldown:
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= sprintCooldownTime) { sprintState = SprintState.Ready; sprintTimer = 0; }
                break;
        }
    }

    void HandleMovement()
    {
        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 1) move.Normalize();

        float targetSpeed = isCrouching ? crouchSpeed : walkSpeed;

        if (sprintState == SprintState.Decaying)
            targetSpeed = Mathf.Lerp(sprintSpeed, walkSpeed, sprintTimer / sprintDecayTime);
        else if (sprintState == SprintState.Building || sprintState == SprintState.Full)
            targetSpeed = sprintSpeed;
        
        // If exhausted, force speed to walk speed (or 0 if you want a penalty)
        if (needsRepress) targetSpeed = walkSpeed * 0.5f; 

        float finalTarget = move.magnitude > 0.1f ? targetSpeed : 0f;
        currentSpeed = Mathf.MoveTowards(currentSpeed, finalTarget, 30f * Time.deltaTime);

        controller.Move(move * currentSpeed * Time.deltaTime);
    }

    void HandleCrouch()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (isCrouching) StandUp();
            else CrouchDown();
        }

        // --- THE FIX ---
        // We calculate the target height once per frame for the Lerp
        float targetHeight = isCrouching ? crouchHeight : standingHeight;
        
        // Smoothly change the controller height
        controller.height = Mathf.Lerp(controller.height, targetHeight, Time.deltaTime * crouchTransitionSpeed);
        
        // Keep the center consistent so the "feet" stay at the pivot (y=0)
        controller.center = new Vector3(0, controller.height / 2f, 0);
        
        // Update the target for the camera based on the physical height we just calculated
        targetCameraY = controller.height * eyeLevelRatio;
    }

    void StandUp()
    {
        // Calculate the starting point of the ray: the current top of the head
        // We add a tiny offset (0.1f) so the ray starts just outside the controller
        Vector3 rayStart = transform.position + Vector3.up * controller.height;
        float distanceToCheck = standingHeight - controller.height;

        // If there is NO obstacle above us, we can stand up
        if (!Physics.Raycast(rayStart, Vector3.up, distanceToCheck, ~playerLayer))
        {
            isCrouching = false;
        }
        else 
        {
            Debug.Log("Ceiling detected! Cannot stand up.");
        }
    }    
    void ApplyCameraSmoothing()
    {
        Vector3 newPos = playerCamera.localPosition;
        newPos.y = Mathf.Lerp(newPos.y, targetCameraY, Time.deltaTime * crouchTransitionSpeed);
        playerCamera.localPosition = newPos;
    }

    void CrouchDown() => isCrouching = true;
    void HandleGravity()
    {
        if (controller.isGrounded && velocity.y < 0) velocity.y = -2f;
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    bool HasMovementInput() => 
        Keyboard.current.wKey.isPressed || Keyboard.current.aKey.isPressed || 
        Keyboard.current.sKey.isPressed || Keyboard.current.dKey.isPressed;
}