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

    [Header("Guard Profile")]
    public GuardProfile guardProfile;

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
    public float catchPathExtraAllowance = 0.35f;
    public float playerNavMeshSampleDistance = 1.5f;

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

    [Header("Capture")]
    public float captureFreezeDelay = 0.15f;
    public bool disablePlayerOnCapture = true;
    public MonoBehaviour[] playerScriptsToDisableOnCapture;
    public Rigidbody playerRigidbody;
    public CharacterController playerCharacterController;
    public GameObject caughtUI;

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

    [Header("GDD Cover Identity / Zone Logic")]
    public bool useCoverIdentitySystem = true;
    public PlayerCoverIdentity playerCoverIdentity;
    public PlayerZoneTracker playerZoneTracker;

    [Header("GDD Behavioral Suspicion")]
    public float unauthorizedZoneMinimumSuspicion = 20f;
    public float unauthorizedZoneSuspicionPerSecond = 18f;
    public float allowedDisguiseSuspicionMultiplier = 0.35f;
    public float partialDisguiseSuspicionMultiplier = 0.65f;
    public float noDisguiseRestrictedMultiplier = 1.4f;
    public float wrongDisguiseRestrictedMultiplier = 1.8f;

    [Header("GDD Guard Response")]
    public bool warnBeforeChaseInRestrictedZone = true;
    public float warningSuspicionThreshold = 55f;
    public float warningDuration = 2f;

    [Header("Guard Questioning / Fast Talk")]
    public bool allowGuardQuestioning = true;
    public GuardQuestioningResponder questioningResponder;

    private bool hasGivenZoneWarning;
    private float warningTimer;
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

        if (questioningResponder == null)
        {
            questioningResponder = GetComponent<GuardQuestioningResponder>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

        if (playerObject != null)
        {
            player = playerObject.transform;
            playerSoundEmitter = playerObject.GetComponent<PlayerSoundEmitter>();

            if (playerCoverIdentity == null)
            {
                playerCoverIdentity = playerObject.GetComponent<PlayerCoverIdentity>();
            }

            if (playerZoneTracker == null)
            {
                playerZoneTracker = playerObject.GetComponent<PlayerZoneTracker>();
            }
        }

        if (playerCoverIdentity == null)
        {
            playerCoverIdentity = FindObjectOfType<PlayerCoverIdentity>();
        }

        if (playerZoneTracker == null)
        {
            playerZoneTracker = FindObjectOfType<PlayerZoneTracker>();
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
        ApplyGuardProfile();
        EnterPatrol();
    }

    private void Update()
    {
        if (questioningResponder != null && questioningResponder.IsQuestioning)
        {
            DebugStateChange();
            return;
        }

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

    private void ApplyGuardProfile()
    {
        if (guardProfile == null)
        {
            return;
        }

        patrolSpeed = guardProfile.patrolSpeed;
        investigateSpeed = guardProfile.investigateSpeed;
        chaseSpeed = guardProfile.chaseSpeed;
        returnSpeed = guardProfile.returnSpeed;

        suspiciousThreshold = guardProfile.suspiciousThreshold;
        chaseThreshold = guardProfile.chaseThreshold;
        suspicionIncreasePerSecond = guardProfile.suspicionIncreasePerSecond;
        suspicionDecreasePerSecond = guardProfile.suspicionDecreasePerSecond;
        soundSuspicionBoost = guardProfile.soundSuspicionBoost;
        minAudibilityToInvestigate = guardProfile.minAudibilityToInvestigate;

        searchPointCount = guardProfile.searchPointCount;
        searchRadius = guardProfile.searchRadius;

        if (vision != null)
        {
            vision.viewDistance = guardProfile.viewDistance;
            vision.viewAngle = guardProfile.viewAngle;
        }
    }

    public void ResetGuardForCheckpoint()
    {
        StopAllCoroutines();
        CancelInvoke();

        suspicion = 0f;
        recentlySawPlayerTimer = 0f;
        lostPlayerTimer = 0f;
        searchWaitTimer = 0f;
        patrolWaitTimer = 0f;
        lookAroundTimer = 0f;

        isWaitingAtSearchPoint = false;
        searchPointsVisited = 0;
        hasEstimatedMoveDirection = false;

        hasGivenZoneWarning = false;
        warningTimer = 0f;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        if (alertIcon != null)
        {
            alertIcon.SetActive(false);
        }

        currentPatrolIndex = GetNearestPatrolPointIndex();
        EnterPatrol();
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
            float coverIdentityMultiplier = GetCoverIdentitySuspicionMultiplier();
            float zoneLegalityMultiplier = GetZoneLegalitySuspicionMultiplier();

            float increaseAmount =
                suspicionIncreasePerSecond *
                distanceMultiplier *
                movementMultiplier *
                angleMultiplier *
                stateMultiplier *
                coverIdentityMultiplier *
                zoneLegalityMultiplier *
                Time.deltaTime;

            increaseAmount = Mathf.Max(increaseAmount, minimumVisionIncrease * Time.deltaTime);

            suspicion += increaseAmount;
            suspicion = Mathf.Clamp(suspicion, 0f, 100f);

            ApplyUnauthorizedZoneSuspicion(canSeePlayer);

            FaceTarget(player.position);

            if (TryStartQuestioningBeforeChase())
            {
                return;
            }

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

    private bool TryStartQuestioningBeforeChase()
    {
        if (!allowGuardQuestioning)
        {
            return false;
        }

        if (questioningResponder == null)
        {
            return false;
        }

        if (!questioningResponder.CanQuestion())
        {
            return false;
        }

        if (questioningResponder.questioningProfile == null)
        {
            return false;
        }

        if (suspicion < questioningResponder.questioningProfile.questioningThreshold)
        {
            return false;
        }

        if (!CanCurrentlySeePlayer())
        {
            return false;
        }

        questioningResponder.StartQuestioning();
        return true;
    }

    private float GetDistanceSuspicionMultiplier(float distanceToPlayer)
    {
        if (vision == null || vision.viewDistance <= 0.01f)
        {
            return 1f;
        }

        float normalizedDistance = Mathf.Clamp01(distanceToPlayer / vision.viewDistance);
        return Mathf.Lerp(closeVisionMultiplier, farVisionMultiplier, normalizedDistance);
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
        if (currentState == GuardState.Chase ||
            currentState == GuardState.CapturePlayer ||
            IsBeingControlledByQuestioning())
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

        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

        if (suspicion >= chaseThreshold && vision != null && vision.CanSeePlayer)
        {
            EnterChase();
        }
        else
        {
            EnterInvestigateSound();
        }
    }

    private bool IsBeingControlledByQuestioning()
    {
        return questioningResponder != null && questioningResponder.IsQuestioning;
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
        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

        if (hasGivenZoneWarning && warningTimer > 0f)
        {
            warningTimer -= Time.deltaTime;

            if (player != null)
            {
                FaceTarget(player.position);
            }

            if (TryStartQuestioningBeforeChase())
            {
                return;
            }

            if (suspicion >= chaseThreshold)
            {
                EnterChase();
                return;
            }

            if (warningTimer <= 0f)
            {
                RestrictedZone zone = GetCurrentPlayerZone();

                bool stillUnauthorized =
                    zone != null &&
                    !zone.IsPublicZone() &&
                    (playerCoverIdentity == null || !playerCoverIdentity.IsAllowedInZone(zone));

                if (stillUnauthorized)
                {
                    suspicion = Mathf.Max(suspicion, chaseThreshold);

                    if (TryStartQuestioningBeforeChase())
                    {
                        return;
                    }

                    EnterChase();
                    return;
                }
            }

            return;
        }

        lookAroundTimer -= Time.deltaTime;

        if (player != null)
        {
            FaceTarget(player.position);
        }

        if (TryStartQuestioningBeforeChase())
        {
            return;
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

        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

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

        if (CanCapturePlayer())
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
        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

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
        hasGivenZoneWarning = false;
        warningTimer = 0f;

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

        CatchPlayer();
    }

    private void CatchPlayer()
    {
        GuardCaughtReporter reporter = GetComponent<GuardCaughtReporter>();

        if (reporter != null)
        {
            reporter.ReportPlayerCaught();
        }
        else
        {
            Debug.LogWarning($"{gameObject.name} has no GuardCaughtReporter attached.");
        }
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
        if (patrolPoints == null || patrolPoints.Length == 0)
        {
            return 0;
        }

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
        if (agent == null)
        {
            return false;
        }

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

        Vector2 randomCircle = Random.insideUnitCircle.normalized *
                               Random.Range(searchRadius * 0.5f, currentRadius);

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

        Vector3 baseDirection = hasEstimatedMoveDirection
            ? estimatedPlayerMoveDirection
            : transform.forward;

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

    private bool CanCapturePlayer()
    {
        if (player == null || vision == null)
        {
            return false;
        }

        if (!vision.CanSeePlayer)
        {
            return false;
        }

        if (!CanReachPlayerByNavMesh(out float pathDistance))
        {
            return false;
        }

        return pathDistance <= catchDistance + catchPathExtraAllowance;
    }

    private bool CanReachPlayerByNavMesh(out float pathDistance)
    {
        pathDistance = Mathf.Infinity;

        if (agent == null || player == null)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(
                player.position,
                out NavMeshHit playerHit,
                playerNavMeshSampleDistance,
                NavMesh.AllAreas))
        {
            return false;
        }

        NavMeshPath path = new NavMeshPath();

        bool pathFound = NavMesh.CalculatePath(
            transform.position,
            playerHit.position,
            NavMesh.AllAreas,
            path
        );

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            return false;
        }

        pathDistance = GetPathDistance(path);
        return true;
    }

    private float GetPathDistance(NavMeshPath path)
    {
        if (path == null || path.corners == null || path.corners.Length < 2)
        {
            return 0f;
        }

        float distance = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return distance;
    }

    private float GetCoverIdentitySuspicionMultiplier()
    {
        if (!useCoverIdentitySystem)
        {
            return 1f;
        }

        if (playerCoverIdentity == null)
        {
            return 1f;
        }

        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone == null || zone.IsPublicZone())
        {
            return 1f;
        }

        string disguiseSet = playerCoverIdentity.GetBestDisguiseSetID();
        float completeness = playerCoverIdentity.GetBestDisguiseCompleteness();

        bool allowed = zone.IsDisguiseAllowed(disguiseSet, completeness);

        if (allowed)
        {
            if (completeness >= 1f)
            {
                return allowedDisguiseSuspicionMultiplier;
            }

            return partialDisguiseSuspicionMultiplier;
        }

        if (!string.IsNullOrWhiteSpace(disguiseSet))
        {
            return wrongDisguiseRestrictedMultiplier;
        }

        return noDisguiseRestrictedMultiplier;
    }

    private float GetZoneLegalitySuspicionMultiplier()
    {
        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone == null || zone.IsPublicZone())
        {
            return 1f;
        }

        if (playerCoverIdentity == null)
        {
            return noDisguiseRestrictedMultiplier;
        }

        bool allowed = playerCoverIdentity.IsAllowedInZone(zone);

        if (allowed)
        {
            return 0.55f;
        }

        return zone.unauthorizedVisionMultiplier;
    }

    private void ApplyUnauthorizedZoneSuspicion(bool canSeePlayer)
    {
        if (!canSeePlayer)
        {
            return;
        }

        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone == null || zone.IsPublicZone())
        {
            return;
        }

        if (playerCoverIdentity != null && playerCoverIdentity.IsAllowedInZone(zone))
        {
            return;
        }

        if (suspicion < unauthorizedZoneMinimumSuspicion)
        {
            suspicion = unauthorizedZoneMinimumSuspicion;
        }

        float zoneSuspicionRate = zone.unauthorizedSuspicionPerSecond > 0f
            ? zone.unauthorizedSuspicionPerSecond
            : unauthorizedZoneSuspicionPerSecond;

        suspicion += zoneSuspicionRate * Time.deltaTime;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

        if (warnBeforeChaseInRestrictedZone &&
            !hasGivenZoneWarning &&
            suspicion >= warningSuspicionThreshold &&
            suspicion < chaseThreshold)
        {
            EnterZoneWarning();
        }
    }

    private RestrictedZone GetCurrentPlayerZone()
    {
        if (playerZoneTracker != null)
        {
            return playerZoneTracker.GetCurrentZone();
        }

        if (PlayerZoneTracker.Instance != null)
        {
            return PlayerZoneTracker.Instance.GetCurrentZone();
        }

        return null;
    }

    private void EnterZoneWarning()
    {
        hasGivenZoneWarning = true;
        warningTimer = warningDuration;

        currentState = GuardState.Suspicious;
        agent.isStopped = true;

        SetVisualState(suspiciousColor, true);

        Debug.Log($"{gameObject.name}: You are not meant to be here.");
    }

    public float GetSuspicion()
    {
        return suspicion;
    }

    public void SetSuspicion(float value)
    {
        suspicion = Mathf.Clamp(value, 0f, 100f);
    }

    public bool CanCurrentlySeePlayer()
    {
        return vision != null && vision.CanSeePlayer && player != null;
    }

    public void ForceReturnToPatrol()
    {
        StopAllCoroutines();

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        EnterReturnToPatrol();
    }

    public void ForceChasePlayer()
    {
        StopAllCoroutines();

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        suspicion = 100f;
        EnterChase();
    }

    public void ForceCatchPlayer()
    {
        StopAllCoroutines();

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        EnterCapturePlayer();
    }
}