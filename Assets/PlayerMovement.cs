using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Speeds")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 20f;
    public float acceleration = 10f; // How fast we reach target speed

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
                if (sprintTimer >= sprintFullTime || !shiftHeld || !isMoving) { sprintState = SprintState.Decaying; sprintTimer = 0; }
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
        // 1. Get Input
        float x = (Keyboard.current.dKey.isPressed ? 1 : 0) - (Keyboard.current.aKey.isPressed ? 1 : 0);
        float z = (Keyboard.current.wKey.isPressed ? 1 : 0) - (Keyboard.current.sKey.isPressed ? 1 : 0);

        Vector3 move = transform.right * x + transform.forward * z;
        if (move.magnitude > 1) move.Normalize();

        bool isMoving = move.magnitude > 0.1f;

        // 2. Determine if we are ACTUALLY sprinting right now
        // We only use sprint speed if we are in the Building or Full state.
        bool isEffectivelySprinting = (sprintState == SprintState.Building || sprintState == SprintState.Full);
        
        float targetSpeed = (isEffectivelySprinting && Keyboard.current.shiftKey.isPressed) 
            ? maxSprintingSpeed 
            : maxWalkingSpeed;

        // 3. Smoothly transition the speed
        // This replaces the complex sine/timer math that was causing the glitch.
        // Use a higher lerp value (like 5f) for responsive walking, 
        // or lower for "heavy" movement.
        float lerpSpeed = isEffectivelySprinting ? 2f : 5f; 
        
        if (isMoving)
        {
            currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * lerpSpeed);
        }
        else
        {
            currentSpeed = Mathf.Lerp(currentSpeed, 0, Time.deltaTime * 10f);
        }

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