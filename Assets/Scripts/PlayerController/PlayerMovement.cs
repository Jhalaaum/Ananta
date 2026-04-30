using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : MonoBehaviour
{
    public CharacterController controller;

    [Header("Speeds")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 20f;
    public float crouchSpeed = 3f;
    public float crawlSpeed = 1.5f;

    [Header("Sprint Timing")]
    public float sprintBuildUpTime = 4f;
    public float sprintFullTime = 5f;
    public float sprintDecayTime = 3f;
    public float sprintCooldownTime = 5f;

    [Header("Stance Heights")]
    public float standingHeight = 2.8f;
    public float crouchHeight = 2f;
    public float crawlHeight = 1f;
    public float transitionSpeed = 10f;

    [Header("General")]
    public float gravity = -19.81f;
    public LayerMask playerLayer;
    public Transform playerCamera;
    [Range(0, 1)] public float eyeLevelRatio = 0.9f;

    private Vector3 velocity;
    private float currentSpeed;
    private float sprintTimer = 0f;
    private bool needsRepress = false;

    private enum Stance { Standing, Crouching, Crawling }
    private Stance currentStance = Stance.Standing;

    private enum SprintState { Ready, Building, Full, Decaying, Cooldown }
    private SprintState sprintState = SprintState.Ready;

    private float targetCameraY;

    void Start()
    {
        targetCameraY = standingHeight * eyeLevelRatio;
    }

    void Update()
    {
        HandleStanceInput();
        HandleSprintState();
        UpdateDimensions();
        HandleMovement();
        HandleGravity();
        ApplyCameraSmoothing();
    }

    void HandleStanceInput()
    {
        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (currentStance == Stance.Crouching) TryStandUp();
            else currentStance = Stance.Crouching;
        }

        if (Keyboard.current.ctrlKey.wasPressedThisFrame)
        {
            if (currentStance == Stance.Crawling) TryStandUp();
            else currentStance = Stance.Crawling;
        }
    }

    void UpdateDimensions()
    {
        float targetH = standingHeight;
        if (currentStance == Stance.Crouching) targetH = crouchHeight;
        if (currentStance == Stance.Crawling) targetH = crawlHeight;

        controller.height = Mathf.Lerp(controller.height, targetH, Time.deltaTime * transitionSpeed);

        controller.center = new Vector3(0, controller.height / 2f, 0);

        targetCameraY = controller.height * eyeLevelRatio;
    }

    void TryStandUp()
    {
        Vector3 rayStart = transform.position + Vector3.up * controller.height;
        float distanceToCheck = standingHeight - controller.height;

        if (!Physics.Raycast(rayStart, Vector3.up, distanceToCheck, ~playerLayer))
        {
            currentStance = Stance.Standing;
        }
        else
        {
            Debug.Log("Ceiling detected! Stay low.");
        }
    }

    void HandleSprintState()
    {
        bool shiftHeld = Keyboard.current.shiftKey.isPressed;
        bool isMoving = HasMovementInput();

        if (!isMoving || sprintState == SprintState.Ready) needsRepress = false;

        switch (sprintState)
        {
            case SprintState.Ready:
                if (shiftHeld && isMoving) 
                { 
                    currentStance = Stance.Standing;
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

        float targetSpeed = walkSpeed;
        if (currentStance == Stance.Crouching) targetSpeed = crouchSpeed;
        if (currentStance == Stance.Crawling) targetSpeed = crawlSpeed;

        if (sprintState == SprintState.Building || sprintState == SprintState.Full)
        {
            targetSpeed = sprintSpeed;
        }
        else if (sprintState == SprintState.Decaying)
        {
            targetSpeed = Mathf.Lerp(sprintSpeed, walkSpeed, sprintTimer / sprintDecayTime);
        }
        else if (needsRepress) 
        {
            targetSpeed = walkSpeed * 0.5f; 
        }

        float finalTarget = move.magnitude > 0.1f ? targetSpeed : 0f;
        
        float accelerationValue = (sprintState == SprintState.Decaying) ? 5f : 30f; 
        currentSpeed = Mathf.MoveTowards(currentSpeed, finalTarget, accelerationValue * Time.deltaTime);

        controller.Move(move * currentSpeed * Time.deltaTime);
    }
    void ApplyCameraSmoothing()
    {
        Vector3 newPos = playerCamera.localPosition;
        newPos.y = Mathf.Lerp(newPos.y, targetCameraY, Time.deltaTime * transitionSpeed);
        playerCamera.localPosition = newPos;
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