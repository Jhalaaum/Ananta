using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Speeds")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 20f;

    [Header("Sprint Timing")]
    public float sprintBuildUpTime = 8f;
    public float sprintFullTime = 8f;
    public float sprintDecayTime = 10f;
    public float sprintCooldownTime = 10f;

    [Header("General")]
    public float gravity = -9.81f;

    private Vector3 velocity;
    private float currentSpeed;
    private float sprintTimer = 0f;
    private bool needsRepress = false; // New flag to force a stop

    private enum SprintState { Ready, Building, Full, Decaying, Cooldown }
    private SprintState sprintState = SprintState.Ready;

    void Update()
    {
        HandleSprintState();
        HandleMovement();
        HandleGravity();
    }

    void HandleSprintState()
    {
        bool shiftHeld = Keyboard.current.shiftKey.isPressed;
        bool isMoving = HasMovementInput();

        // If the player stops touching the keyboard, they've "repressed"
        if (!isMoving)
        {
            needsRepress = false;
        }

        switch (sprintState)
        {
            case SprintState.Ready:
                if (shiftHeld && isMoving) { sprintState = SprintState.Building; sprintTimer = 0; }
                break;

            case SprintState.Building:
                sprintTimer += Time.deltaTime;
                if (sprintTimer >= sprintBuildUpTime) { sprintState = SprintState.Full; sprintTimer = 0; }
                if (!shiftHeld || !isMoving) sprintState = SprintState.Ready;
                break;

            case SprintState.Full:
                sprintTimer += Time.deltaTime;
                // Transition to Decaying
                if (sprintTimer >= sprintFullTime || !shiftHeld || !isMoving) 
                { 
                    sprintState = SprintState.Decaying; 
                    sprintTimer = 0; 
                    TriggerHardStop(); // Force the player to stop
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
// ... inside the class ...

    void TriggerHardStop()
    {
        // Removed currentSpeed = 0; so it can actually decay
        needsRepress = true;
    }

    void HandleMovement()
    {
        // 1. Get Input
        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 1) move.Normalize();

        bool isMoving = move.magnitude > 0.1f;

        // 2. Logic to handle Decay vs Repress
        float targetSpeed = walkSpeed;

        if (sprintState == SprintState.Decaying)
        {
            // Calculate the decaying speed (dropping from sprint to walk)
            targetSpeed = Mathf.Lerp(sprintSpeed, walkSpeed, sprintTimer / sprintDecayTime);
            
            // If they let go of movement DURING decay, they now MUST repress to walk again
            if (!isMoving) needsRepress = true;
        }
        else if (sprintState == SprintState.Building || sprintState == SprintState.Full)
        {
            targetSpeed = sprintSpeed;
        }
        
        // 3. Apply the "Hard Stop" or Lock
        // If needsRepress is true, speed is 0. 
        // Note: We only check this if NOT in the decaying state to allow the decay to finish
        if (needsRepress && sprintState != SprintState.Decaying)
        {
            targetSpeed = 0f;
        }

        // 4. Apply movement
        float finalTarget = isMoving ? targetSpeed : 0f;
        
        // Acceleration/Deceleration feel
        currentSpeed = Mathf.MoveTowards(currentSpeed, finalTarget, 20f * Time.deltaTime);

        controller.Move(move * currentSpeed * Time.deltaTime);
    }
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