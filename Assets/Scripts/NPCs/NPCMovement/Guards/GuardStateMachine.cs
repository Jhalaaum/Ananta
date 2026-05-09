using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(GuardVision))]
public class AdvancedGuardAIController : MonoBehaviour
{
    public enum GuardState
    {
        Patrol,
        LookAround,
        Suspicious,
        InvestigateSound,
        Chase,
        SearchArea,
        ReturnToPatrol,
        CapturePlayer
    }

    [Header("Current State")]
    public GuardState currentState = GuardState.Patrol;

    [Header("Patrol Route")]
    public Transform patrolRouteParent;
    public float patrolPointReachDistance = 0.7f;
    public float waitAtPatrolPointTime = 2f;
    public float lookAroundTurnAngle = 55f;
    public float lookAroundTurnSpeed = 90f;

    [Header("Movement Speeds")]
    public float patrolSpeed = 2.5f;
    public float investigateSpeed = 3.2f;
    public float chaseSpeed = 5.2f;
    public float returnSpeed = 2.8f;

    [Header("Suspicion")]
    [Range(0f, 100f)] public float suspicion = 0f;
    public float suspiciousThreshold = 35f;
    public float chaseThreshold = 100f;
    public float suspicionIncreasePerSecond = 35f;
    public float suspicionDecreasePerSecond = 18f;
    public float suspiciousLookTime = 1.3f;

    [Header("Vision Modifiers")]
    public float farVisionMultiplier = 0.5f;
    public float closeVisionMultiplier = 1.6f;
    public float closeDistance = 4f;

    [Header("Player Movement Modifiers")]
    public float silentVisionMultiplier = 0.75f;
    public float walkingVisionMultiplier = 1f;
    public float sprintingVisionMultiplier = 2.1f;
    public float crouchingVisionMultiplier = 0.45f;
    public float crawlingVisionMultiplier = 0.25f;
    public float airborneVisionMultiplier = 1.3f;

    [Header("Hearing")]
    public float minAudibilityToInvestigate = 0.12f;
    public float soundSuspicionBoost = 25f;

    [Header("Chase")]
    public float catchDistance = 1.4f;
    public float lostPlayerAfterSeconds = 2.5f;

    [Header("Search")]
    public float searchRadius = 6f;
    public int searchPointCount = 3;
    public float searchPointReachDistance = 0.8f;
    public float waitAtSearchPointTime = 1.2f;
    public int maxAttemptsToFindSearchPoint = 10;
    [Header("Smart Search")]
    public float firstSearchLookTime = 1.5f;
    public float forwardGuessDistance = 4f;
    public float sideSearchDistance = 3f;
    public float wideningSearchStep = 2.5f;

    private Vector3 previousKnownPlayerPosition;
    private Vector3 estimatedPlayerMoveDirection;
    private bool hasEstimatedMoveDirection;

    [Header("Visual Debug")]
    public Renderer guardRenderer;
    public GameObject alertIcon;

    public Color patrolColor = Color.white;
    public Color suspiciousColor = Color.yellow;
    public Color investigateColor = new Color(1f, 0.65f, 0f);
    public Color chaseColor = Color.red;
    public Color searchColor = new Color(1f, 0.35f, 0f);
    public Color returnColor = Color.gray;

    [Header("Debug")]
    public bool printStateChanges = true;

    [Header("Advanced Vision Feel")]
    public float centerVisionBonus = 1.35f;
    public float edgeVisionPenalty = 0.55f;
    public float suspicionMemoryTime = 1.2f;
    public float suspicionGraceDecayMultiplier = 0.35f;
    public float alertStateVisionMultiplier = 1.25f;
    public float suspicionStateVisionMultiplier = 1.1f;
    public float minimumVisionIncrease = 4f;

    private float recentlySawPlayerTimer;

    private NavMeshAgent agent;
    private GuardVision vision;
    private Transform player;
    private PlayerSoundEmitter playerSoundEmitter;

    private Transform[] patrolPoints;
    private int currentPatrolIndex;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 soundPosition;
    private Vector3 originalLookDirection;

    private float patrolWaitTimer;
    private float lookAroundTimer;
    private float lostPlayerTimer;
    private float searchWaitTimer;

    private bool isWaitingAtSearchPoint;
    private int searchPointsVisited;
    private Vector3 currentSearchPoint;

    private GuardState previousState;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<GuardVision>();

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerSoundEmitter = playerObject.GetComponent<PlayerSoundEmitter>();
        }

        if (guardRenderer == null)
        {
            guardRenderer = GetComponentInChildren<Renderer>();
        }

        LoadPatrolPoints();
    }

    private void OnEnable()
    {
        PlayerSoundEmitter.OnSoundEmitted += HandlePlayerSound;
    }

    private void OnDisable()
    {
        PlayerSoundEmitter.OnSoundEmitted -= HandlePlayerSound;
    }

    private void Start()
    {
        EnterPatrol();
    }

    private void Update()
    {
        UpdateSuspicionFromVision();

        switch (currentState)
        {
            case GuardState.Patrol:
                UpdatePatrol();
                break;

            case GuardState.LookAround:
                UpdateLookAround();
                break;

            case GuardState.Suspicious:
                UpdateSuspicious();
                break;

            case GuardState.InvestigateSound:
                UpdateInvestigateSound();
                break;

            case GuardState.Chase:
                UpdateChase();
                break;

            case GuardState.SearchArea:
                UpdateSearchArea();
                break;

            case GuardState.ReturnToPatrol:
                UpdateReturnToPatrol();
                break;

            case GuardState.CapturePlayer:
                UpdateCapturePlayer();
                break;
        }

        DebugStateChange();
    }

    private void LoadPatrolPoints()
    {
        if (patrolRouteParent == null)
        {
            patrolPoints = new Transform[0];
            return;
        }

        int childCount = patrolRouteParent.childCount;
        patrolPoints = new Transform[childCount];

        for (int i = 0; i < childCount; i++)
        {
            patrolPoints[i] = patrolRouteParent.GetChild(i);
        }
    }

    private void UpdateSuspicionFromVision()
    {
        if (currentState == GuardState.CapturePlayer)
        {
            return;
        }

        bool canSeePlayer = vision != null && vision.CanSeePlayer && player != null;

        if (canSeePlayer)
        {
            recentlySawPlayerTimer = suspicionMemoryTime;
            previousKnownPlayerPosition = lastKnownPlayerPosition;
            lastKnownPlayerPosition = vision.LastKnownPlayerPosition;

            Vector3 movementDelta = lastKnownPlayerPosition - previousKnownPlayerPosition;
            movementDelta.y = 0f;

            if (movementDelta.sqrMagnitude > 0.1f)
            {
                estimatedPlayerMoveDirection = movementDelta.normalized;
                hasEstimatedMoveDirection = true;
            }

            float distanceToPlayer = Vector3.Distance(transform.position, player.position);

            float distanceMultiplier = GetDistanceSuspicionMultiplier(distanceToPlayer);
            float movementMultiplier = GetPlayerMovementVisionMultiplier();
            float angleMultiplier = GetVisionAngleMultiplier();
            float stateMultiplier = GetStateVisionMultiplier();

            float increaseAmount =
                suspicionIncreasePerSecond *
                distanceMultiplier *
                movementMultiplier *
                angleMultiplier *
                stateMultiplier *
                Time.deltaTime;

            increaseAmount = Mathf.Max(increaseAmount, minimumVisionIncrease * Time.deltaTime);

            suspicion += increaseAmount;
            suspicion = Mathf.Clamp(suspicion, 0f, 100f);

            FaceTarget(player.position);

            if (suspicion >= chaseThreshold)
            {
                EnterChase();
            }
            else if (suspicion >= suspiciousThreshold &&
                    currentState != GuardState.Suspicious &&
                    currentState != GuardState.InvestigateSound &&
                    currentState != GuardState.SearchArea)
            {
                EnterSuspicious();
            }

            return;
        }

        recentlySawPlayerTimer -= Time.deltaTime;

        bool shouldDecaySuspicion =
            currentState == GuardState.Patrol ||
            currentState == GuardState.LookAround ||
            currentState == GuardState.ReturnToPatrol;

        if (shouldDecaySuspicion)
        {
            float decayMultiplier = recentlySawPlayerTimer > 0f
                ? suspicionGraceDecayMultiplier
                : 1f;

            suspicion -= suspicionDecreasePerSecond * decayMultiplier * Time.deltaTime;
            suspicion = Mathf.Clamp(suspicion, 0f, 100f);
        }
    }
    private float GetDistanceSuspicionMultiplier(float distanceToPlayer)
    {
    float normalizedDistance = Mathf.Clamp01(distanceToPlayer / vision.viewDistance);

    float distanceMultiplier = Mathf.Lerp(closeVisionMultiplier, farVisionMultiplier, normalizedDistance);

    return distanceMultiplier;
    }

    private float GetVisionAngleMultiplier()
    {
        if (player == null || vision == null)
        {
            return 1f;
        }

        Vector3 directionToPlayer = player.position - transform.position;
        directionToPlayer.y = 0f;

        if (directionToPlayer.sqrMagnitude < 0.01f)
        {
            return centerVisionBonus;
        }

        float angleToPlayer = Vector3.Angle(transform.forward, directionToPlayer.normalized);

        float halfVisionAngle = vision.viewAngle * 0.5f;

        if (halfVisionAngle <= 0.01f)
        {
            return 1f;
        }

        float normalizedAngle = Mathf.Clamp01(angleToPlayer / halfVisionAngle);

        return Mathf.Lerp(centerVisionBonus, edgeVisionPenalty, normalizedAngle);
    }

    private float GetStateVisionMultiplier()
    {
        switch (currentState)
        {
            case GuardState.Chase:
                return alertStateVisionMultiplier;

            case GuardState.Suspicious:
            case GuardState.InvestigateSound:
            case GuardState.SearchArea:
                return suspicionStateVisionMultiplier;

            default:
                return 1f;
        }
    }
    private float GetPlayerMovementVisionMultiplier()
    {
        if (playerSoundEmitter == null)
        {
            return walkingVisionMultiplier;
        }

        PlayerSoundEmitter.MovementSoundType movementType =
            playerSoundEmitter.CurrentSoundField.movementType;

        switch (movementType)
        {
            case PlayerSoundEmitter.MovementSoundType.Silent:
                return silentVisionMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Walking:
                return walkingVisionMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Sprinting:
                return sprintingVisionMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Crouching:
                return crouchingVisionMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Crawling:
                return crawlingVisionMultiplier;

            case PlayerSoundEmitter.MovementSoundType.Airborne:
                return airborneVisionMultiplier;

            default:
                return walkingVisionMultiplier;
        }
    }

    private void HandlePlayerSound(PlayerSoundEmitter.SoundField sound)
    {
        if (currentState == GuardState.Chase || currentState == GuardState.CapturePlayer)
        {
            return;
        }

        float distance = Vector3.Distance(transform.position, sound.origin);

        if (distance > sound.radius)
        {
            return;
        }

        float distanceAudibility = 1f - Mathf.Clamp01(distance / sound.radius);
        float finalAudibility = distanceAudibility * sound.loudness;

        if (finalAudibility < minAudibilityToInvestigate)
        {
            return;
        }

        soundPosition = sound.origin;
        lastKnownPlayerPosition = sound.origin;

        suspicion += soundSuspicionBoost * finalAudibility;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        if (suspicion >= chaseThreshold && vision != null && vision.CanSeePlayer)
        {
            EnterChase();
        }
        else
        {
            EnterInvestigateSound();
        }
    }

    private void EnterPatrol()
    {
        currentState = GuardState.Patrol;
        agent.speed = patrolSpeed;
        agent.isStopped = false;

        SetVisualState(patrolColor, false);

        if (patrolPoints.Length > 0)
        {
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
    }

    private void UpdatePatrol()
    {
        if (patrolPoints.Length == 0)
        {
            return;
        }

        if (ReachedDestination(patrolPointReachDistance))
        {
            EnterLookAround();
        }
    }

    private void EnterLookAround()
    {
        currentState = GuardState.LookAround;
        agent.isStopped = true;
        patrolWaitTimer = waitAtPatrolPointTime;
        lookAroundTimer = 0f;
        originalLookDirection = transform.forward;

        SetVisualState(patrolColor, false);
    }

    private void UpdateLookAround()
    {
        patrolWaitTimer -= Time.deltaTime;
        lookAroundTimer += Time.deltaTime;

        float lookPhase = Mathf.Sin(lookAroundTimer * 2f);
        float targetAngle = lookPhase * lookAroundTurnAngle;

        Vector3 targetDirection = Quaternion.Euler(0f, targetAngle, 0f) * originalLookDirection;

        if (targetDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                targetRotation,
                lookAroundTurnSpeed * Time.deltaTime
            );
        }

        if (patrolWaitTimer <= 0f)
        {
            currentPatrolIndex++;

            if (currentPatrolIndex >= patrolPoints.Length)
            {
                currentPatrolIndex = 0;
            }

            agent.isStopped = false;
            EnterPatrol();
        }
    }

    private void EnterSuspicious()
    {
        currentState = GuardState.Suspicious;
        agent.isStopped = true;
        lookAroundTimer = suspiciousLookTime;

        SetVisualState(suspiciousColor, true);
    }

    private void UpdateSuspicious()
    {
        lookAroundTimer -= Time.deltaTime;

        if (player != null)
        {
            FaceTarget(player.position);
        }

        if (suspicion >= chaseThreshold)
        {
            EnterChase();
            return;
        }

        if (lookAroundTimer <= 0f)
        {
            EnterInvestigateLastKnownPosition();
        }
    }

    private void EnterInvestigateSound()
    {
        currentState = GuardState.InvestigateSound;
        agent.speed = investigateSpeed;
        agent.isStopped = false;
        agent.SetDestination(soundPosition);

        SetVisualState(investigateColor, true);
    }

    private void UpdateInvestigateSound()
    {
        FaceTarget(agent.steeringTarget);

        if (ReachedDestination(patrolPointReachDistance))
        {
            EnterSearchArea();
        }
    }

    private void EnterInvestigateLastKnownPosition()
    {
        currentState = GuardState.InvestigateSound;
        agent.speed = investigateSpeed;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPlayerPosition);

        SetVisualState(investigateColor, true);
    }

    private void EnterChase()
    {
        currentState = GuardState.Chase;
        agent.speed = chaseSpeed;
        agent.isStopped = false;
        lostPlayerTimer = lostPlayerAfterSeconds;

        SetVisualState(chaseColor, true);
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            EnterSearchArea();
            return;
        }

        if (vision != null && vision.CanSeePlayer)
        {
            lastKnownPlayerPosition = player.position;
            agent.SetDestination(player.position);
            lostPlayerTimer = lostPlayerAfterSeconds;
        }
        else
        {
            agent.SetDestination(lastKnownPlayerPosition);
            lostPlayerTimer -= Time.deltaTime;

            if (lostPlayerTimer <= 0f)
            {
                EnterSearchArea();
                return;
            }
        }

        FaceTarget(agent.steeringTarget);

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        if (distanceToPlayer <= catchDistance)
        {
            EnterCapturePlayer();
        }
    }

    private void EnterSearchArea()
    {
        currentState = GuardState.SearchArea;
        agent.speed = investigateSpeed;
        agent.isStopped = false;

        searchPointsVisited = 0;
        isWaitingAtSearchPoint = false;

        SetVisualState(searchColor, true);

        PickNextSmartSearchPoint();
    }
    private void UpdateSearchArea()
    {
        if (isWaitingAtSearchPoint)
        {
            searchWaitTimer -= Time.deltaTime;

            LookAroundDuringSearch();

            if (searchWaitTimer <= 0f)
            {
                isWaitingAtSearchPoint = false;
                searchPointsVisited++;

                if (searchPointsVisited >= searchPointCount)
                {
                    EnterReturnToPatrol();
                }
                else
                {
                    PickNextSmartSearchPoint();
                }
            }

            return;
        }

        FaceTarget(agent.steeringTarget);

        if (ReachedDestination(searchPointReachDistance))
        {
            agent.isStopped = true;
            isWaitingAtSearchPoint = true;

            searchWaitTimer = searchPointsVisited == 0
                ? firstSearchLookTime
                : waitAtSearchPointTime;
        }
    }
    private void PickNextSmartSearchPoint()
    {
        agent.isStopped = false;

        Vector3 searchTarget;

        switch (searchPointsVisited)
        {
            case 0:
                searchTarget = lastKnownPlayerPosition;
                break;

            case 1:
                if (hasEstimatedMoveDirection)
                {
                    searchTarget = lastKnownPlayerPosition + estimatedPlayerMoveDirection * forwardGuessDistance;
                }
                else
                {
                    searchTarget = lastKnownPlayerPosition + transform.forward * forwardGuessDistance;
                }
                break;

            case 2:
                searchTarget = GetSideSearchPoint(1f);
                break;

            case 3:
                searchTarget = GetSideSearchPoint(-1f);
                break;

            default:
                searchTarget = GetWideningSearchPoint();
                break;
        }

        if (TrySetSearchDestination(searchTarget))
        {
            return;
        }

        TrySetSearchDestination(GetWideningSearchPoint());
    }
    private void EnterReturnToPatrol()
    {
        currentState = GuardState.ReturnToPatrol;
        agent.speed = returnSpeed;
        agent.isStopped = false;

        suspicion = 0f;

        SetVisualState(returnColor, false);

        if (patrolPoints.Length > 0)
        {
            currentPatrolIndex = GetNearestPatrolPointIndex();
            agent.SetDestination(patrolPoints[currentPatrolIndex].position);
        }
        else
        {
            EnterPatrol();
        }
    }

    private void UpdateReturnToPatrol()
    {
        FaceTarget(agent.steeringTarget);

        if (ReachedDestination(patrolPointReachDistance))
        {
            EnterPatrol();
        }
    }

    private void EnterCapturePlayer()
    {
        currentState = GuardState.CapturePlayer;
        agent.isStopped = true;

        SetVisualState(chaseColor, true);

        Debug.Log("PLAYER CAUGHT by " + gameObject.name);

    }

    private void UpdateCapturePlayer()
    {
        if (player != null)
        {
            FaceTarget(player.position);
        }
    }

    private int GetNearestPatrolPointIndex()
    {
        int nearestIndex = 0;
        float nearestDistance = Mathf.Infinity;

        for (int i = 0; i < patrolPoints.Length; i++)
        {
            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
    }

    private bool ReachedDestination(float reachDistance)
    {
        if (agent.pathPending)
        {
            return false;
        }

        if (!agent.hasPath)
        {
            return false;
        }

        return agent.remainingDistance <= reachDistance;
    }

    private void FaceTarget(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            360f * Time.deltaTime
        );
    }

    private void SetVisualState(Color color, bool showAlertIcon)
    {
        if (guardRenderer != null)
        {
            guardRenderer.material.color = color;
        }

        if (alertIcon != null)
        {
            alertIcon.SetActive(showAlertIcon);
        }
    }

    private void DebugStateChange()
    {
        if (!printStateChanges)
        {
            return;
        }

        if (previousState != currentState)
        {
            Debug.Log(gameObject.name + " changed state to: " + currentState);
            previousState = currentState;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastKnownPlayerPosition, searchRadius);
    }
    private Vector3 GetSideSearchPoint(float side)
    {
        Vector3 baseDirection = hasEstimatedMoveDirection
            ? estimatedPlayerMoveDirection
            : transform.forward;

        Vector3 sideDirection = Vector3.Cross(Vector3.up, baseDirection).normalized * side;

        return lastKnownPlayerPosition + sideDirection * sideSearchDistance;
}

    private Vector3 GetWideningSearchPoint()
    {
        float currentRadius = searchRadius + searchPointsVisited * wideningSearchStep;

        Vector2 randomCircle = Random.insideUnitCircle.normalized * Random.Range(searchRadius * 0.5f, currentRadius);

        Vector3 randomPosition = new Vector3(
            lastKnownPlayerPosition.x + randomCircle.x,
            lastKnownPlayerPosition.y,
            lastKnownPlayerPosition.z + randomCircle.y
        );

        return randomPosition;
    }

    private bool TrySetSearchDestination(Vector3 targetPosition)
    {
        if (NavMesh.SamplePosition(targetPosition, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
        {
            currentSearchPoint = hit.position;
            agent.SetDestination(currentSearchPoint);
            return true;
        }

        return false;
    }

    private void LookAroundDuringSearch()
    {
        lookAroundTimer += Time.deltaTime;

        float lookPhase = Mathf.Sin(lookAroundTimer * 3f);
        float targetAngle = lookPhase * lookAroundTurnAngle;

        Vector3 baseDirection;

        if (hasEstimatedMoveDirection)
        {
            baseDirection = estimatedPlayerMoveDirection;
        }
        else
        {
            baseDirection = transform.forward;
        }

        if (baseDirection.sqrMagnitude < 0.01f)
        {
            return;
        }

        Vector3 targetDirection = Quaternion.Euler(0f, targetAngle, 0f) * baseDirection;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            lookAroundTurnSpeed * Time.deltaTime
        );
    }
}