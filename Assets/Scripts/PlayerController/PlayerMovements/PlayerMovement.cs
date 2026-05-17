using System.Collections;
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
    [SerializeField] private float slowWalkSpeed = 3.2f;
    [SerializeField] private float crouchSpeed = 3f;
    [SerializeField] private float crawlSpeed = 2f;

    [Header("Acceleration")]
    [SerializeField] private float groundedAcceleration = 34f;
    [SerializeField] private float groundedDeceleration = 42f;
    [SerializeField] private float airAcceleration = 8f;
    [SerializeField] private float airDeceleration = 4f;
    [SerializeField] private float airControlMultiplier = 0.45f;
    [SerializeField] private float directionChangeBoost = 2.4f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 1f;
    [SerializeField] private float gravity = -45f;
    [SerializeField] private float groundedStickForce = -4f;
    [SerializeField] private float coyoteTime = 0.12f;
    [SerializeField] private float jumpBufferTime = 0.12f;
    [SerializeField] private float fallMultiplier = 1.25f;
    [SerializeField] private float heldJumpRepeatDelay = 0.05f;

    [Header("Landing Polish")]
    [SerializeField] private float hardLandingVelocity = -12f;
    [SerializeField] private float landingCameraDip = 0.12f;
    [SerializeField] private float landingCameraReturnSpeed = 12f;

    [Header("Stance Heights")]
    [SerializeField] private float standingHeight = 2.8f;
    [SerializeField] private float crouchHeight = 1.8f;
    [SerializeField] private float crawlHeight = 1.0f;
    [SerializeField] private float stanceTransitionSpeed = 12f;

    [Range(0.5f, 1f)]
    [SerializeField] private float eyeLevelRatio = 0.9f;

    [Header("Step Offset")]
    [SerializeField] private float standingStepOffset = 0.35f;
    [SerializeField] private float crouchStepOffset = 0.22f;
    [SerializeField] private float crawlStepOffset = 0.08f;

    [Header("Sprint System")]
    [SerializeField] private float sprintBuildTime = 4f;
    [SerializeField] private float maxSprintDuration = 6f;
    [SerializeField] private float sprintRecoveryTime = 6f;
    [SerializeField] private float exhaustedSpeedMultiplier = 0.65f;
    [SerializeField] private float sprintSlowdownTime = 4.0f;
    [SerializeField] private float sprintRecoveryDelayAfterSlowdown = 0.6f;
    [SerializeField] private float sprintRecoveryStartThreshold = 0.05f;

    [Header("Mantle / Climb Over")]
    [SerializeField] private bool enableMantle = true;
    [SerializeField] private float mantleCheckDistance = 1.15f;
    [SerializeField] private float mantleMinHeight = 0.45f;
    [SerializeField] private float mantleMaxHeight = 1.65f;
    [SerializeField] private float mantleForwardOffset = 0.65f;
    [SerializeField] private float mantleDuration = 0.35f;
    [SerializeField] private LayerMask mantleMask;

    [Header("Wall Climbing")]
    [SerializeField] private bool enableWallClimb = true;
    [SerializeField] private LayerMask climbableWallMask;
    [SerializeField] private float wallCheckDistance = 0.9f;
    [SerializeField] private float wallCheckRadius = 0.45f;
    [SerializeField] private float climbUpSpeed = 3.2f;
    [SerializeField] private float climbDownSpeed = 2.8f;
    [SerializeField] private float climbSideSpeed = 2.4f;
    [SerializeField] private float climbStickToWallForce = 4f;
    [SerializeField] private float climbWallOffset = 0.65f;
    [SerializeField] private float climbDetachPush = 2.5f;
    [SerializeField] private float climbTopProbeHeight = 4.5f;
    [SerializeField] private float climbTopForwardOffset = 2.2f;
    [SerializeField] private float climbTopExitDuration = 0.38f;
    [SerializeField] private float climbMinimumStartHeight = 0.7f;
    [SerializeField] private float climbCameraBobAmount = 0.012f;
    [SerializeField] private float climbCameraBobFrequency = 5f;
    [SerializeField] private float climbTopStandForwardOffset = 2.4f;
    [SerializeField] private float climbTopStandUpOffset = 0.18f;
    [SerializeField] private float climbExitGroundedGraceTime = 0.35f;
    [SerializeField] private float climbTopSearchStart = 0.2f;
    [SerializeField] private float climbTopSearchEnd = 2.2f;
    [SerializeField] private float climbTopSearchStep = 0.15f;
    [SerializeField] private float climbTopEdgeInset = 0.12f;
    [SerializeField] private float climbTopGroundCheckDistance = 0.6f;
    [SerializeField] private float climbTopRequiredSupport = 0.75f;
    private float climbExitGroundedGraceTimer;

    [Header("Camera Polish")]
    [SerializeField] private bool enableHeadBob = true;
    [SerializeField] private float walkBobAmount = 0.035f;
    [SerializeField] private float sprintBobAmount = 0.065f;
    [SerializeField] private float crouchBobAmount = 0.02f;
    [SerializeField] private float crawlBobAmount = 0.012f;
    [SerializeField] private float bobFrequency = 10f;

    [SerializeField] private bool enableFovKick = true;
    [SerializeField] private float normalFov = 70f;
    [SerializeField] private float sprintFov = 78f;
    [SerializeField] private float fovSmoothSpeed = 8f;

    [Header("Lean Position")]
    [SerializeField] private float leanDistance = 0.7f;
    [SerializeField] private float leanSpeed = 10f;
    [SerializeField] private float leanCheckRadius = 0.2f;
    [SerializeField] private LayerMask leanObstacleMask;

    [Header("Collision Checks")]
    [SerializeField] private LayerMask obstructionMask;
    [SerializeField] private float standCheckRadiusPadding = 0.05f;
    [SerializeField] private float standCheckFloorOffset = 0.08f;

    public enum MovementState
    {
        Idle,
        Walk,
        SlowWalk,
        Sprint,
        Crouch,
        Crawl,
        Jump,
        Falling,
        ClimbMantle,
        WallClimb,
        Interact,
        Hide
    }

    private enum Stance
    {
        Standing,
        Crouching,
        Crawling
    }

    public MovementState CurrentMovementState { get; private set; }
    public bool IsSprinting => CurrentMovementState == MovementState.Sprint;
    public bool IsCrouching => currentStance == Stance.Crouching;
    public bool IsCrawling => currentStance == Stance.Crawling;
    public bool IsMantling => isMantling;
    public bool IsWallClimbing => isWallClimbing;
    public bool IsMovementLocked => isInteracting || isHidden || isMantling || isWallClimbing;

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

    private float landingOffset;

    private bool isInteracting;
    private bool isHidden;
    private bool isMantling;
    private bool isWallClimbing;

    private Vector3 climbWallNormal;
    private Vector3 climbWallPoint;
    private float climbStartY;

    private readonly Collider[] standCheckResults = new Collider[12];
    private readonly Collider[] mantleCheckResults = new Collider[12];
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
            obstructionMask = ~LayerMask.GetMask("Player");

        if (leanObstacleMask.value == 0)
            leanObstacleMask = obstructionMask;

        if (mantleMask.value == 0)
            mantleMask = obstructionMask;

        if (climbableWallMask.value == 0)
            climbableWallMask = mantleMask;

        controller.stepOffset = standingStepOffset;
    }

    private void Update()
    {
        ReadInput();
        UpdateGroundedTimers();

        if (isWallClimbing)
        {
            HandleWallClimb();
        }
        else if (!isMantling)
        {
            HandleStanceInput();
            HandleJumpInput();
            HandleSprint();
            UpdateStanceDimensions();
            MovePlayer();
        }

        HandleLeanPosition();
        UpdateCamera();
        UpdateMovementState();
    }

    private void ReadInput()
    {
        if (Keyboard.current == null || isInteracting || isHidden || isMantling)
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

        rawInput = Vector2.ClampMagnitude(new Vector2(x, z), 1f);
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
                TrySetStance(Stance.Crouching);
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
        {
            if (TryStartWallClimb())
                return;

            if (TryStartMantle())
                return;

            jumpBufferTimer = jumpBufferTime;
        }

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
            sprintBlend = Mathf.MoveTowards(sprintBlend, 1f, Time.deltaTime / sprintBuildTime);
            sprintEnergy -= Time.deltaTime / maxSprintDuration;
            sprintRecoveryDelayTimer = sprintRecoveryDelayAfterSlowdown;

            if (sprintEnergy <= 0f)
            {
                sprintEnergy = 0f;
                isExhausted = true;
            }
        }
        else
        {
            sprintBlend = Mathf.MoveTowards(sprintBlend, 0f, Time.deltaTime / sprintSlowdownTime);

            bool hasSlowedDown = sprintBlend <= sprintRecoveryStartThreshold;

            if (!hasSlowedDown)
            {
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
                        sprintEnergy += Time.deltaTime / sprintRecoveryTime;
                }
            }

            if (sprintEnergy >= 1f)
            {
                sprintEnergy = 1f;
                isExhausted = false;
            }
        }

        if (wantsSprint && sprintBlend > 0.05f)
            TryStandUp();
    }

    private void UpdateStanceDimensions()
    {
        if (controller == null || !controller.enabled)
            return;
        targetHeight = GetHeightForStance(currentStance);

        controller.height = Mathf.Lerp(
            controller.height,
            targetHeight,
            Time.deltaTime * stanceTransitionSpeed
        );

        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);

        // FIX: Prevent PhysX crashes by zeroing stepOffset in the air
        if (!IsGrounded)
        {
            controller.stepOffset = 0f;
        }
        else
        {
            if (currentStance == Stance.Standing)
                controller.stepOffset = standingStepOffset;
            else if (currentStance == Stance.Crouching)
                controller.stepOffset = crouchStepOffset;
            else
                controller.stepOffset = crawlStepOffset;
        }

        targetCameraY = controller.height * eyeLevelRatio;
    }
    private void MovePlayer()
    {
        if (controller == null || !controller.enabled || IsMovementLocked)
            return;
        bool wasGrounded = IsGrounded;
        float previousVerticalVelocity = verticalVelocity.y;

        Vector3 inputDirection = transform.right * rawInput.x + transform.forward * rawInput.y;
        inputDirection = Vector3.ClampMagnitude(inputDirection, 1f);

        float targetSpeed = GetTargetSpeed();
        Vector3 targetHorizontalVelocity = inputDirection * targetSpeed;

        bool hasInput = rawInput.sqrMagnitude > 0.01f;

        if (IsGrounded)
        {
            if (!hasInput)
            {
                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    Vector3.zero,
                    groundedDeceleration * Time.deltaTime
                );

                if (horizontalVelocity.sqrMagnitude < 0.0025f)
                    horizontalVelocity = Vector3.zero;
            }
            else
            {
                if (IsChangingDirection(targetHorizontalVelocity))
                {
                    horizontalVelocity = Vector3.MoveTowards(
                        horizontalVelocity,
                        Vector3.zero,
                        groundedDeceleration * directionChangeBoost * Time.deltaTime
                    );
                }

                horizontalVelocity = Vector3.MoveTowards(
                    horizontalVelocity,
                    targetHorizontalVelocity,
                    groundedAcceleration * directionChangeBoost * Time.deltaTime
                );
            }
        }
        else
        {
            float acceleration = hasInput
                ? airAcceleration * airControlMultiplier
                : airDeceleration;

            horizontalVelocity = Vector3.MoveTowards(
                horizontalVelocity,
                targetHorizontalVelocity,
                acceleration * Time.deltaTime
            );
        }

        ApplyGravity();

        Vector3 finalVelocity = horizontalVelocity + verticalVelocity;
        controller.Move(finalVelocity * Time.deltaTime);

        if (!wasGrounded && IsGrounded && previousVerticalVelocity <= hardLandingVelocity)
        {
            float landingStrength = Mathf.InverseLerp(
                hardLandingVelocity,
                hardLandingVelocity * 2f,
                previousVerticalVelocity
            );

            landingOffset = -landingCameraDip * Mathf.Clamp01(landingStrength);
        }
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

        return dot < 0.35f;
    }

    private float GetTargetSpeed()
    {
        float baseSpeed = walkSpeed;

        if (currentStance == Stance.Crouching)
            return crouchSpeed;

        if (currentStance == Stance.Crawling)
            return crawlSpeed;

        if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed)
            baseSpeed = slowWalkSpeed;

        if (isExhausted)
            baseSpeed *= exhaustedSpeedMultiplier;

        return Mathf.Lerp(baseSpeed, sprintSpeed, sprintBlend);
    }

    private void ApplyGravity()
    {
        if (climbExitGroundedGraceTimer > 0f)
        {
            climbExitGroundedGraceTimer -= Time.deltaTime;
            verticalVelocity.y = groundedStickForce;
            return;
        }

        if (IsGrounded && verticalVelocity.y < 0f)
            verticalVelocity.y = groundedStickForce;

        float gravityToApply = gravity;

        if (verticalVelocity.y < 0f)
            gravityToApply *= fallMultiplier;

        verticalVelocity.y += gravityToApply * Time.deltaTime;
    }
    private bool TryStartWallClimb()
    {
        if (!enableWallClimb)
            return false;

        if (isWallClimbing || isMantling || isInteracting || isHidden)
            return false;

        if (currentStance != Stance.Standing)
            return false;

        if (!TryFindClimbableWall(out RaycastHit wallHit))
            return false;

        if (wallHit.point.y - transform.position.y < climbMinimumStartHeight)
            return false;

        isWallClimbing = true;

        climbWallNormal = wallHit.normal;
        climbWallPoint = wallHit.point;
        climbStartY = transform.position.y;

        sprintBlend = 0f;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;

        currentStance = Stance.Standing;
        controller.stepOffset = 0f;

        SnapToClimbWall();

        return true;
    }

    private bool TryFindClimbableWall(out RaycastHit wallHit)
    {
        Vector3 origin = transform.position + Vector3.up * Mathf.Max(1f, controller.height * 0.55f);

        return Physics.SphereCast(
            origin,
            wallCheckRadius,
            transform.forward,
            out wallHit,
            wallCheckDistance,
            climbableWallMask,
            QueryTriggerInteraction.Ignore
        );
    }

    private void HandleWallClimb()
    {
        if (Keyboard.current == null)
        {
            StopWallClimb(false);
            return;
        }

        bool wantsDrop =
            Keyboard.current.xKey.wasPressedThisFrame ||
            Keyboard.current.leftCtrlKey.wasPressedThisFrame ||
            Keyboard.current.rightCtrlKey.wasPressedThisFrame;

        if (wantsDrop)
        {
            StopWallClimb(true);
            return;
        }

        if (rawInput.y > 0.1f && TryExitClimbAtTop())
            return;

        bool foundWall = TryFindClimbableWall(out RaycastHit wallHit);

        if (!foundWall)
        {
            if (rawInput.y > 0.1f && TryExitClimbAtTop())
                return;

            StopWallClimb(false);
            return;
        }

        climbWallNormal = wallHit.normal;
        climbWallPoint = wallHit.point;

        Vector3 wallRight = Vector3.Cross(Vector3.up, climbWallNormal).normalized;

        float verticalInput = rawInput.y;
        float sideInput = rawInput.x;

        float verticalSpeed = verticalInput >= 0f ? climbUpSpeed : climbDownSpeed;

        Vector3 climbMovement =
            Vector3.up * verticalInput * verticalSpeed +
            wallRight * sideInput * climbSideSpeed +
            -climbWallNormal * climbStickToWallForce;

        controller.Move(climbMovement * Time.deltaTime);

        if (rawInput.y > 0.1f && TryExitClimbAtTop())
            return;

        SnapToClimbWall();

        verticalVelocity = Vector3.zero;
        horizontalVelocity = Vector3.zero;
        sprintBlend = 0f;
    }
    private void SnapToClimbWall()
    {
        Vector3 desiredPosition = climbWallPoint + climbWallNormal * climbWallOffset;
        Vector3 flatCorrection = desiredPosition - transform.position;
        flatCorrection.y = 0f;

        if (flatCorrection.magnitude > 0.02f)
            controller.Move(flatCorrection * 0.45f);
    }

    private bool TryExitClimbAtTop()
    {
        if (rawInput.y <= 0.1f)
            return false;

        if (!TryFindReliableClimbTopPosition(out Vector3 targetPosition, out Collider groundCollider))
            return false;

        if (!HasRoomAtPositionIgnoringCollider(targetPosition, standingHeight, groundCollider))
            return false;

        StartCoroutine(ClimbTopExitRoutine(targetPosition));
        return true;
    }
    private bool TryFindReliableClimbTopPosition(out Vector3 targetPosition, out Collider groundCollider)
    {
        targetPosition = Vector3.zero;
        groundCollider = null;

        Vector3 overWallDirection = -climbWallNormal;
        overWallDirection.y = 0f;

        if (overWallDirection.sqrMagnitude < 0.01f)
            return false;

        overWallDirection.Normalize();

        Vector3 right = Vector3.Cross(Vector3.up, overWallDirection).normalized;

        float bestSupport = 0f;
        Vector3 bestPosition = Vector3.zero;
        Collider bestCollider = null;

        for (float forward = climbTopSearchStart; forward <= climbTopSearchEnd; forward += climbTopSearchStep)
        {
            Vector3 probeStart =
                transform.position +
                Vector3.up * climbTopProbeHeight +
                overWallDirection * forward;

            bool foundTop = Physics.Raycast(
                probeStart,
                Vector3.down,
                out RaycastHit topHit,
                climbTopProbeHeight + 3f,
                obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            if (!foundTop)
                continue;

            if (Vector3.Dot(topHit.normal, Vector3.up) < 0.7f)
                continue;

            if (topHit.point.y < transform.position.y + 0.25f)
                continue;

            Vector3 candidate = topHit.point;

            candidate += overWallDirection * climbTopEdgeInset;
            candidate += Vector3.up * climbTopStandUpOffset;

            float support = GetLandingSupportAmount(candidate, overWallDirection, right);

            if (support > bestSupport)
            {
                bestSupport = support;
                bestPosition = candidate;
                bestCollider = topHit.collider;
            }

            if (support >= climbTopRequiredSupport)
            {
                targetPosition = candidate;
                groundCollider = topHit.collider;
                return true;
            }
        }

        if (bestSupport >= climbTopRequiredSupport)
        {
            targetPosition = bestPosition;
            groundCollider = bestCollider;
            return true;
        }

        return false;
    }
    private float GetLandingSupportAmount(Vector3 feetPosition, Vector3 forward, Vector3 right)
    {
        float radius = Mathf.Max(0.05f, controller.radius * 0.85f);

        Vector3[] checkPoints =
        {
            feetPosition,
            feetPosition + forward * radius,
            feetPosition - forward * radius,
            feetPosition + right * radius,
            feetPosition - right * radius
        };

        int supportedPoints = 0;

        for (int i = 0; i < checkPoints.Length; i++)
        {
            Vector3 rayStart = checkPoints[i] + Vector3.up * 0.25f;

            bool hasGround = Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                climbTopGroundCheckDistance,
                obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            if (!hasGround)
                continue;

            if (Vector3.Dot(hit.normal, Vector3.up) < 0.7f)
                continue;

            supportedPoints++;
        }

        return supportedPoints / (float)checkPoints.Length;
    }
    private bool TryFindClimbTopPosition(out Vector3 targetPosition, out Collider groundCollider)
    {
        targetPosition = Vector3.zero;
        groundCollider = null;

        Vector3 overWallDirection = -climbWallNormal;

        float[] forwardChecks =
        {
            0.15f,
            0.25f,
            0.35f,
            0.45f,
            0.6f,
            0.8f,
            1.0f
        };

        for (int i = 0; i < forwardChecks.Length; i++)
        {
            Vector3 probeStart =
                transform.position +
                Vector3.up * climbTopProbeHeight +
                overWallDirection * forwardChecks[i];

            bool foundTop = Physics.Raycast(
                probeStart,
                Vector3.down,
                out RaycastHit topHit,
                climbTopProbeHeight + 3f,
                obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            if (!foundTop)
                continue;

            if (Vector3.Dot(topHit.normal, Vector3.up) < 0.7f)
                continue;

            if (topHit.point.y < transform.position.y + 0.2f)
                continue;

            Vector3 candidate = topHit.point;

            candidate += Vector3.up * climbTopStandUpOffset;

            Vector3 groundCheckStart = candidate + Vector3.up * 0.25f;

            bool stillHasGround = Physics.Raycast(
                groundCheckStart,
                Vector3.down,
                out RaycastHit confirmHit,
                0.7f,
                obstructionMask,
                QueryTriggerInteraction.Ignore
            );

            if (!stillHasGround)
                continue;

            if (Vector3.Dot(confirmHit.normal, Vector3.up) < 0.7f)
                continue;

            targetPosition = candidate;
            groundCollider = topHit.collider;
            return true;
        }

        return false;
    }
    private bool HasRoomAtPositionIgnoringCollider(Vector3 position, float desiredHeight, Collider ignoredCollider)
    {
        float radius = Mathf.Max(0.05f, controller.radius - standCheckRadiusPadding);

        Vector3 bottom = position + Vector3.up * (radius + standCheckFloorOffset);
        Vector3 top = position + Vector3.up * (desiredHeight - radius);

        int hits = Physics.OverlapCapsuleNonAlloc(
            bottom,
            top,
            radius,
            mantleCheckResults,
            obstructionMask,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hits; i++)
        {
            Collider hit = mantleCheckResults[i];

            if (hit == null)
                continue;

            if (hit == ignoredCollider)
                continue;

            if (hit.transform == transform || hit.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }
    private IEnumerator ClimbTopExitRoutine(Vector3 targetPosition)
    {
        isWallClimbing = false;
        isMantling = true;
        CurrentMovementState = MovementState.ClimbMantle;

        rawInput = Vector2.zero;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        sprintBlend = 0f;

        Vector3 startPosition = transform.position;
        float timer = 0f;

        controller.enabled = false;

        while (timer < climbTopExitDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / climbTopExitDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

            yield return null;
        }

        transform.position = targetPosition;

        controller.height = standingHeight;
        controller.center = new Vector3(0f, controller.height * 0.5f, 0f);
        controller.stepOffset = standingStepOffset;

        currentStance = Stance.Standing;

        controller.enabled = true;

        controller.Move(Vector3.down * 0.05f);

        verticalVelocity = Vector3.zero;
        verticalVelocity.y = groundedStickForce;

        horizontalVelocity = Vector3.zero;
        sprintBlend = 0f;

        coyoteTimer = coyoteTime;
        jumpBufferTimer = 0f;
        climbExitGroundedGraceTimer = climbExitGroundedGraceTime;

        isWallClimbing = false;
        isMantling = false;
    }
    private void StopWallClimb(bool pushAwayFromWall)
    {
        isWallClimbing = false;

        controller.stepOffset = standingStepOffset;

        verticalVelocity.y = 0f;

        if (pushAwayFromWall)
        {
            horizontalVelocity = climbWallNormal * climbDetachPush;
        }
        else
        {
            horizontalVelocity = Vector3.zero;
        }
    }

    private bool TryStartMantle()
    {
        if (!enableMantle)
            return false;

        if (currentStance != Stance.Standing)
            return false;

        if (isMantling || isInteracting || isHidden || isWallClimbing)
            return false;

        Vector3 forward = transform.forward;
        
        Vector3 lowerOrigin = transform.position + Vector3.up * mantleMinHeight;
        Vector3 upperOrigin = transform.position + Vector3.up * mantleMaxHeight;

        bool hasLowerHit = Physics.Raycast(
            lowerOrigin,
            forward,
            out RaycastHit wallHit,
            mantleCheckDistance,
            mantleMask,
            QueryTriggerInteraction.Ignore
        );

        if (!hasLowerHit)
            return false;

        bool upperBlocked = Physics.Raycast(
            upperOrigin,
            forward,
            mantleCheckDistance,
            mantleMask,
            QueryTriggerInteraction.Ignore
        );

        if (upperBlocked)
            return false;

        Vector3 ledgeProbeStart = wallHit.point + forward * 0.1f + Vector3.up * (mantleMaxHeight + 0.5f);

        bool foundTop = Physics.Raycast(
            ledgeProbeStart,
            Vector3.down,
            out RaycastHit topHit,
            mantleMaxHeight + 1.0f,
            mantleMask,
            QueryTriggerInteraction.Ignore
        );

        if (!foundTop)
            return false;

        float mantleHeight = topHit.point.y - transform.position.y;

        if (mantleHeight < mantleMinHeight || mantleHeight > mantleMaxHeight)
            return false;

        Vector3 targetPosition = topHit.point + forward * (controller.radius + 0.05f);
        targetPosition.y += 0.05f;

        if (!HasRoomAtPosition(targetPosition, standingHeight))
            return false;

        StartCoroutine(MantleRoutine(targetPosition));
        return true;
    }
    private IEnumerator MantleRoutine(Vector3 targetPosition)
    {
        isMantling = true;
        CurrentMovementState = MovementState.ClimbMantle;

        sprintBlend = 0f;
        horizontalVelocity = Vector3.zero;
        verticalVelocity = Vector3.zero;
        rawInput = Vector2.zero;

        Vector3 startPosition = transform.position;
        float timer = 0f;

        controller.enabled = false;

        while (timer < mantleDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / mantleDuration);
            float smoothT = t * t * (3f - 2f * t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, smoothT);

            yield return null;
        }

        transform.position = targetPosition;

        controller.enabled = true;

        verticalVelocity.y = groundedStickForce;
        coyoteTimer = coyoteTime;
        jumpBufferTimer = 0f;

        isMantling = false;
    }

    private bool HasRoomAtPosition(Vector3 position, float desiredHeight)
    {
        float radius = Mathf.Max(0.05f, controller.radius - standCheckRadiusPadding);
        
        Vector3 bottom = position + Vector3.up * (radius + standCheckFloorOffset);
        Vector3 top = position + Vector3.up * (desiredHeight - radius);

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
            if (hit == null) continue;

            if (hit == controller || hit.transform.IsChildOf(transform))
                continue;

            return false;
        }

        return true;
    }
    private void HandleLeanPosition()
    {
        if (Keyboard.current == null || playerCamera == null || isInteracting || isHidden || isMantling || isWallClimbing)
        {
            targetLeanOffset = 0f;
            currentLeanOffset = Mathf.Lerp(currentLeanOffset, 0f, Time.deltaTime * leanSpeed);
            return;
        }

        float leanInput = 0f;

        if (Keyboard.current.qKey.isPressed)
            leanInput = -1f;
        else if (Keyboard.current.vKey.isPressed)
            leanInput = 1f;

        float allowedDistance = leanInput != 0f ? GetAllowedLeanDistance(leanInput) : leanDistance;

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
        return TrySetStance(Stance.Standing);
    }

    private bool TrySetStance(Stance targetStance)
    {
        float desiredHeight = GetHeightForStance(targetStance);

        if (desiredHeight > controller.height && !HasRoomForHeight(desiredHeight))
            return false;

        currentStance = targetStance;
        return true;
    }

    private float GetHeightForStance(Stance stance)
    {
        switch (stance)
        {
            case Stance.Standing:
                return standingHeight;

            case Stance.Crouching:
                return crouchHeight;

            case Stance.Crawling:
                return crawlHeight;

            default:
                return standingHeight;
        }
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
        bool shouldBob = enableHeadBob && IsGrounded && isMoving && !isMantling && !isWallClimbing;

        if (shouldBob)
        {
            float speedRatio = Mathf.InverseLerp(0f, sprintSpeed, horizontalVelocity.magnitude);
            float stanceBobAmount = walkBobAmount;

            if (currentStance == Stance.Crouching)
                stanceBobAmount = crouchBobAmount;
            else if (currentStance == Stance.Crawling)
                stanceBobAmount = crawlBobAmount;

            float bobAmount = Mathf.Lerp(stanceBobAmount, sprintBobAmount, sprintBlend);
            float frequency = Mathf.Lerp(bobFrequency, bobFrequency * 1.45f, speedRatio);

            if (currentStance == Stance.Crawling)
                frequency *= 0.65f;

            bobTimer += Time.deltaTime * frequency;
            bobOffset = Mathf.Sin(bobTimer) * bobAmount;
        }
        else if (isWallClimbing && enableHeadBob && isMoving)
        {
            bobTimer += Time.deltaTime * climbCameraBobFrequency;
            bobOffset = Mathf.Sin(bobTimer) * climbCameraBobAmount;
        }
        else
        {
            bobTimer = Mathf.Lerp(bobTimer, 0f, Time.deltaTime * 6f);
        }

        landingOffset = Mathf.Lerp(
            landingOffset,
            0f,
            Time.deltaTime * landingCameraReturnSpeed
        );

        localPos.x = baseCameraLocalX + currentLeanOffset;
        localPos.z = baseCameraLocalZ;

        localPos.y = Mathf.Lerp(
            localPos.y,
            targetCameraY + bobOffset + landingOffset,
            Time.deltaTime * stanceTransitionSpeed
        );

        playerCamera.localPosition = localPos;
    }

    private void UpdateCameraFov()
    {
        if (!enableFovKick || cameraComponent == null)
            return;

        float targetFov = isWallClimbing ? normalFov : Mathf.Lerp(normalFov, sprintFov, sprintBlend);

        cameraComponent.fieldOfView = Mathf.Lerp(
            cameraComponent.fieldOfView,
            targetFov,
            Time.deltaTime * fovSmoothSpeed
        );
    }

    private void UpdateMovementState()
    {
        if (isHidden)
        {
            CurrentMovementState = MovementState.Hide;
            return;
        }

        if (isInteracting)
        {
            CurrentMovementState = MovementState.Interact;
            return;
        }

        if (isMantling)
        {
            CurrentMovementState = MovementState.ClimbMantle;
            return;
        }

        if (isWallClimbing)
        {
            CurrentMovementState = MovementState.WallClimb;
            return;
        }

        if (!IsGrounded)
        {
            CurrentMovementState = verticalVelocity.y > 0f
                ? MovementState.Jump
                : MovementState.Falling;

            return;
        }

        if (rawInput.sqrMagnitude <= 0.01f)
        {
            CurrentMovementState = MovementState.Idle;
            return;
        }

        if (currentStance == Stance.Crawling)
        {
            CurrentMovementState = MovementState.Crawl;
            return;
        }

        if (currentStance == Stance.Crouching)
        {
            CurrentMovementState = MovementState.Crouch;
            return;
        }

        if (sprintBlend > 0.25f)
        {
            CurrentMovementState = MovementState.Sprint;
            return;
        }

        if (Keyboard.current != null && Keyboard.current.leftAltKey.isPressed)
        {
            CurrentMovementState = MovementState.SlowWalk;
            return;
        }

        CurrentMovementState = MovementState.Walk;
    }

    public void SetInteractMovementLocked(bool locked)
    {
        isInteracting = locked;

        if (locked)
        {
            rawInput = Vector2.zero;
            sprintBlend = 0f;
            horizontalVelocity = Vector3.zero;

            if (isWallClimbing)
                StopWallClimb(false);
        }
    }

    public void SetHidden(bool hidden)
    {
        isHidden = hidden;

        if (hidden)
        {
            rawInput = Vector2.zero;
            sprintBlend = 0f;
            horizontalVelocity = Vector3.zero;
            verticalVelocity = Vector3.zero;
            currentLeanOffset = 0f;
            targetLeanOffset = 0f;

            if (isWallClimbing)
                StopWallClimb(false);
        }
    }

    public void ForceStandIfPossible()
    {
        TryStandUp();
    }
}