using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class PlayerMovement : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CharacterController controller;
    [SerializeField] private Transform playerCamera;
    [SerializeField] private Camera cameraComponent;

    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 6f;
    [SerializeField] private float sprintSpeed = 18f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float crawlSpeed = 2f;

    [Header("Acceleration")]
    [SerializeField] private float groundedAcceleration = 34f;
    [SerializeField] private float groundedDeceleration = 42f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float airDeceleration = 4f;
    [SerializeField] private float airControlMultiplier = 0.45f;

    [Tooltip("How quickly the player cancels old movement when pressing the opposite direction.")]
    [SerializeField] private float directionChangeBoost = 2.4f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -45f;
    [SerializeField] private float groundedStickForce = -4f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallMultiplier = 1.25f;

    [Tooltip("Small delay between auto-jumps while Space is held. Lower value = faster repeated jumping.")]
    [SerializeField] private float heldJumpRepeatDelay = 0.05f;

    [Header("Stance Heights")]
    [SerializeField] private float standingHeight = 2.8f;
    [SerializeField] private float crouchHeight = 1.8f;
    [SerializeField] private float crawlHeight = 1.0f;
    [SerializeField] private float stanceTransitionSpeed = 12f;

    [Range(0.5f, 1f)]
    [SerializeField] private float eyeLevelRatio = 0.9f;

    [Header("Sprint System")]
    [SerializeField] private float sprintBuildTime = 4f;
    [SerializeField] private float maxSprintDuration = 6f;
    [SerializeField] private float sprintRecoveryTime = 6f;
    [SerializeField] private float exhaustedSpeedMultiplier = 0.65f;

    [Tooltip("How long it takes sprint speed to smoothly slow down before stamina recovery starts.")]
    [SerializeField] private float sprintSlowdownTime = 4.0f;

    [Tooltip("After sprint speed has slowed down, wait this long before stamina starts recovering.")]
    [SerializeField] private float sprintRecoveryDelayAfterSlowdown = 0.6f;

    [Tooltip("Sprint blend must fall below this before stamina recovery can begin.")]
    [SerializeField] private float sprintRecoveryStartThreshold = 0.05f;

    [Header("Camera Polish")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobAmount = 0.035f;
    [SerializeField] private float sprintBobAmount = 0.065f;
    [SerializeField] private float crouchBobAmount = 0.02f;
    [SerializeField] private float bobFrequency = 10f;

    [SerializeField] private bool enableFovKick = true;
    [SerializeField] private float normalFov = 70f;
    [SerializeField] private float sprintFov = 78f;
    [SerializeField] private float fovSmoothSpeed = 8f;

    [Header("Lean Position")]
    [SerializeField] private float leanDistance = 0.7f;
    [SerializeField] private float leanSpeed = 10f;
    [SerializeField] private float leanCheckRadius = 0.2f;

    [Tooltip("Layers that block leaning. Usually Environment/Ground/World, but NOT Player.")]
    [SerializeField] private LayerMask leanObstacleMask;

    [Header("Collision Checks")]
    [Tooltip("Layers that block the player from standing up. Usually this should include Ground/World/Environment, but NOT Player.")]
    [SerializeField] private LayerMask obstructionMask;

    [SerializeField] private float standCheckRadiusPadding = 0.05f;

    [Tooltip("Prevents the stand-up check from detecting the floor as an obstruction.")]
    [SerializeField] private float standCheckFloorOffset = 0.08f;

    private enum Stance
    {
        Standing,
        Crouching,
        Crawling
    }

    private Stance currentStance = Stance.Standing;

    private Vector2 rawInput;
    private Vector3 horizontalVelocity;
    private Vector3 verticalVelocity;

    private float targetHeight;
    private float targetCameraY;

    private float baseCameraLocalX;
    private float baseCameraLocalZ;

    private float coyoteTimer;
    private float jumpBufferTimer;
    private float heldJumpTimer;

    private float sprintEnergy = 1f;
    private float sprintBlend;
    private bool isExhausted;
    private float sprintRecoveryDelayTimer;

    private float bobTimer;

    private float currentLeanOffset;
    private float targetLeanOffset;

    private readonly Collider[] standCheckResults = new Collider[12];
    private readonly RaycastHit[] leanHits = new RaycastHit[8];

    private bool IsGrounded => controller != null && controller.isGrounded;

    private void Reset()
    {
        controller = GetComponent<CharacterController>();
    }

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<CharacterController>();

        if (cameraComponent == null && playerCamera != null)
            cameraComponent = playerCamera.GetComponent<Camera>();

        targetHeight = standingHeight;

        if (playerCamera != null)
        {
            baseCameraLocalX = playerCamera.localPosition.x;
            baseCameraLocalZ = playerCamera.localPosition.z;
            targetCameraY = standingHeight * eyeLevelRatio;
        }

        if (cameraComponent != null)
            cameraComponent.fieldOfView = normalFov;

        if (obstructionMask.value == 0)
        {
            obstructionMask = ~LayerMask.GetMask("Player");
        }

        if (leanObstacleMask.value == 0)
        {
            leanObstacleMask = obstructionMask;
        }
    }

    private void Update()
    {
        ReadInput();

        UpdateGroundedTimers();
        HandleStanceInput();
        HandleJumpInput();
        HandleSprint();
        UpdateStanceDimensions();

        MovePlayer();

        HandleLeanPosition();
        UpdateCamera();
    }

    private void ReadInput()
    {
        if (Keyboard.current == null)
        {
            rawInput = Vector2.zero;
            return;
        }

        float x = 0f;
        float z = 0f;

        if (Keyboard.current.aKey.isPressed) x -= 1f;
        if (Keyboard.current.dKey.isPressed) x += 1f;
        if (Keyboard.current.sKey.isPressed) z -= 1f;
        if (Keyboard.current.wKey.isPressed) z += 1f;

        rawInput = new Vector2(x, z);
        rawInput = Vector2.ClampMagnitude(rawInput, 1f);
    }

    private void UpdateGroundedTimers()
    {
        if (IsGrounded)
            coyoteTimer = coyoteTime;
        else
            coyoteTimer -= Time.deltaTime;

        if (jumpBufferTimer > 0f)
            jumpBufferTimer -= Time.deltaTime;

        if (heldJumpTimer > 0f)
            heldJumpTimer -= Time.deltaTime;
    }

    private void HandleStanceInput()
    {
        if (Keyboard.current == null)
            return;

        if (Keyboard.current.cKey.wasPressedThisFrame)
        {
            if (currentStance == Stance.Crouching)
            {
                TryStandUp();
            }
            else if (currentStance == Stance.Crawling)
            {
                currentStance = Stance.Crouching;
            }
            else
            {
                currentStance = Stance.Crouching;
            }
        }

        if (Keyboard.current.xKey.wasPressedThisFrame)
        {
            if (currentStance == Stance.Crawling)
            {
                TryStandUp();
            }
            else
            {
                currentStance = Stance.Crawling;
            }
        }
    }

    private void HandleJumpInput()
    {
        if (Keyboard.current == null)
            return;

        bool spacePressedThisFrame = Keyboard.current.spaceKey.wasPressedThisFrame;
        bool spaceHeld = Keyboard.current.spaceKey.isPressed;

        if (spacePressedThisFrame)
            jumpBufferTimer = jumpBufferTime;

        if (spaceHeld && IsGrounded && heldJumpTimer <= 0f)
            jumpBufferTimer = jumpBufferTime;

        if (jumpBufferTimer <= 0f)
            return;

        if (currentStance != Stance.Standing)
        {
            if (TryStandUp())
                jumpBufferTimer = 0f;

            return;
        }

        if (coyoteTimer > 0f)
        {
            verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            heldJumpTimer = heldJumpRepeatDelay;
        }
    }

    private void HandleSprint()
    {
        if (Keyboard.current == null)
            return;

        bool wantsSprint = Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed;
        bool hasMovementInput = rawInput.sqrMagnitude > 0.01f;
        bool canSprint = currentStance == Stance.Standing && IsGrounded && hasMovementInput && !isExhausted;

        if (wantsSprint && canSprint)
        {
            sprintBlend = Mathf.MoveTowards(
                sprintBlend,
                1f,
                Time.deltaTime / sprintBuildTime
            );

            sprintEnergy -= Time.deltaTime / maxSprintDuration;

            // Keep recovery blocked while actively sprinting.
            sprintRecoveryDelayTimer = sprintRecoveryDelayAfterSlowdown;

            if (sprintEnergy <= 0f)
            {
                sprintEnergy = 0f;
                isExhausted = true;

                // Important:
                // Do NOT instantly set sprintBlend to 0 here.
                // The else block will smoothly slow sprintBlend down first.
            }
        }
        else
        {
            sprintBlend = Mathf.MoveTowards(
                sprintBlend,
                0f,
                Time.deltaTime / sprintSlowdownTime
            );

            bool hasSlowedDown = sprintBlend <= sprintRecoveryStartThreshold;

            if (!hasSlowedDown)
            {
                // While the player is still slowing down from sprint,
                // stamina should not recover yet.
                sprintRecoveryDelayTimer = sprintRecoveryDelayAfterSlowdown;
            }
            else
            {
                if (sprintRecoveryDelayTimer > 0f)
                {
                    sprintRecoveryDelayTimer -= Time.deltaTime;
                }
                else
                {
                    bool canRecover = !wantsSprint || !hasMovementInput || !IsGrounded || isExhausted;

                    if (canRecover)
                    {
                        sprintEnergy += Time.deltaTime / sprintRecoveryTime;
                    }
                }
            }

            if (sprintEnergy >= 1f)
            {
                sprintEnergy = 1f;
                isExhausted = false;
            }
        }

        if (sprintBlend > 0.05f)
            TryStandUp();
    }

    private void UpdateStanceDimensions()
    {
        switch (currentStance)
        {
            case Stance.Standing:
                targetHeight = standingHeight;
                break;

            case Stance.Crouching:
                targetHeight = crouchHeight;
                break;

            case Stance.Crawling:
                targetHeight = crawlHeight;
                break;
        }

        float previousHeight = controller.height;

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * stanceTransitionSpeed
        );

        float heightDifference = controller.height - previousHeight;

        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        if (IsGrounded && Mathf.Abs(heightDifference) > 0.001f)
        {
            transform.position += Vector3.up * heightDifference * 0.5f;
        }

        targetCameraY = controller.height * eyeLevelRatio;
    }

    private void MovePlayer()
    {
        Vector3 inputDirection = transform.right * rawInput.x + transform.forward * rawInput.y;
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        float targetSpeed = GetTargetSpeed();
        Vector3 targetHorizontalVelocity = inputDirection * targetSpeed;

        float acceleration;

        if (IsGrounded)
        {
            bool hasInput = rawInput.sqrMagnitude > 0.01f;
            acceleration = hasInput ? groundedAcceleration : groundedDeceleration;
        }
        else
        {
            bool hasInput = rawInput.sqrMagnitude > 0.01f;
            acceleration = hasInput ? airAcceleration * airControlMultiplier : airDeceleration;
        }

        if (IsChangingDirection(targetHorizontalVelocity))
        {
            acceleration *= directionChangeBoost;
        }

        horizontalVelocity = Vector3.MoveTowards(
            horizontalVelocity,
            targetHorizontalVelocity,
            acceleration * Time.deltaTime
        );

        ApplyGravity();

        Vector3 finalVelocity = horizontalVelocity + verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);
    }

    private bool IsChangingDirection(Vector3 targetHorizontalVelocity)
    {
        Vector3 currentFlat = new Vector3(horizontalVelocity.x, 0f, horizontalVelocity.z);
        Vector3 targetFlat = new Vector3(targetHorizontalVelocity.x, 0f, targetHorizontalVelocity.z);

        if (currentFlat.sqrMagnitude < 0.1f)
            return false;

        if (targetFlat.sqrMagnitude < 0.1f)
            return false;

        float dot = Vector3.Dot(currentFlat.normalized, targetFlat.normalized);

        return dot < -0.2f;
    }

    private float GetTargetSpeed()
    {
        float baseSpeed = walkSpeed;

        if (currentStance == Stance.Crouching)
            baseSpeed = crouchSpeed;
        else if (currentStance == Stance.Crawling)
            baseSpeed = crawlSpeed;

        if (isExhausted && currentStance == Stance.Standing)
            baseSpeed *= exhaustedSpeedMultiplier;

        return Mathf.Lerp(baseSpeed, sprintSpeed, sprintBlend);
    }

    private void ApplyGravity()
    {
        if (IsGrounded && verticalVelocity.y < 0f)
        {
            verticalVelocity.y = groundedStickForce;
        }

        float gravityToApply = gravity;

        if (verticalVelocity.y < 0f)
            gravityToApply *= fallMultiplier;

        verticalVelocity.y += gravityToApply * Time.deltaTime;
    }

    private void HandleLeanPosition()
    {
        if (Keyboard.current == null || playerCamera == null)
        {
            targetLeanOffset = 0f;
            currentLeanOffset = Mathf.Lerp(currentLeanOffset, 0f, Time.deltaTime * leanSpeed);
            return;
        }

        float leanInput = 0f;

        if (Keyboard.current.qKey.isPressed)
            leanInput = -1f;
        else if (Keyboard.current.eKey.isPressed)
            leanInput = 1f;

        float allowedDistance = leanDistance;

        if (leanInput != 0f)
        {
            allowedDistance = GetAllowedLeanDistance(leanInput);
        }

        targetLeanOffset = leanInput * allowedDistance;

        currentLeanOffset = Mathf.Lerp(
            currentLeanOffset,
            targetLeanOffset,
            Time.deltaTime * leanSpeed
        );
    }

    private float GetAllowedLeanDistance(float leanInput)
    {
        Vector3 origin = playerCamera.position;
        Vector3 direction = transform.right * leanInput;

        int hitCount = Physics.SphereCastNonAlloc(
            origin,
            leanCheckRadius,
            direction,
            leanHits,
            leanDistance,
            leanObstacleMask,
            QueryTriggerInteraction.Ignore
        );

        float closestDistance = leanDistance;

        for (int i = 0; i < hitCount; i++)
        {
            RaycastHit hit = leanHits[i];

            if (hit.collider == null)
                continue;

            if (hit.collider.transform == transform || hit.collider.transform.IsChildOf(transform))
                continue;

            closestDistance = Mathf.Min(closestDistance, hit.distance);
        }

        return Mathf.Max(0f, closestDistance - 0.05f);
    }

    private bool TryStandUp()
    {
        if (HasRoomForHeight(standingHeight))
        {
            currentStance = Stance.Standing;
            return true;
        }

        return false;
    }

    private bool HasRoomForHeight(float desiredHeight)
    {
        float radius = Mathf.Max(0.05f, controller.radius - standCheckRadiusPadding);

        Vector3 worldCenter = transform.TransformPoint(controller.center);

        float bottomY = transform.position.y + radius + standCheckFloorOffset;
        float topY = transform.position.y + desiredHeight - radius;

        Vector3 bottom = new Vector3(worldCenter.x, bottomY, worldCenter.z);
        Vector3 top = new Vector3(worldCenter.x, topY, worldCenter.z);

        int hits = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            standCheckResults,
            obstructionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits; i++)
        {
            Collider hit = standCheckResults[i];

            if (hit == null)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }

    private void UpdateCamera()
    {
        if (playerCamera == null)
            return;

        UpdateCameraHeightBobAndLean();
        UpdateCameraFov();
    }

    private void UpdateCameraHeightBobAndLean()
    {
        Vector3 localPos = playerCamera.localPosition;

        float bobOffset = 0f;
        bool isMoving = rawInput.sqrMagnitude > 0.01f;
        bool shouldBob = enableHeadBob && IsGrounded && isMoving;

        if (shouldBob)
        {
            float speedRatio = Mathf.InverseLerp(0f, sprintSpeed, horizontalVelocity.magnitude);
            float stanceBobAmount = walkBobAmount;

            if (currentStance == Stance.Crouching || currentStance == Stance.Crawling)
                stanceBobAmount = crouchBobAmount;

            float bobAmount = Mathf.Lerp(stanceBobAmount, sprintBobAmount, sprintBlend);
            float frequency = Mathf.Lerp(bobFrequency, bobFrequency * 1.45f, speedRatio);

            bobTimer += Time.deltaTime * frequency;
            bobOffset = Mathf.Sin(bobTimer) * bobAmount;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 6f);
        }

        localPos.x = baseCameraLocalX + currentLeanOffset;
        localPos.z = baseCameraLocalZ;

        localPos.y = Mathf.Lerp(
            localPos.y,
            targetCameraY + bobOffset,
            Time.deltaTime * stanceTransitionSpeed
        );

        playerCamera.localPosition = localPos;
    }

    private void UpdateCameraFov()
    {
        if (!enableFovKick || cameraComponent == null)
            return;

        float targetFov = Mathf.Lerp(normalFov, sprintFov, sprintBlend);

        cameraComponent.fieldOfView = Mathf.Lerp(
            cameraComponent.fieldOfView,
            targetFov,
            Time.deltaTime * fovSmoothSpeed
        );
    }
}