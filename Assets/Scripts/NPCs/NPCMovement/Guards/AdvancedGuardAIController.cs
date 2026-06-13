using System.Collections.Generic;
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
        QuestionApproach,
        InvestigateSound,
        Chase,
        SearchArea,
        ReturnToPatrol,
        CapturePlayer
    }

    private static readonly List<AdvancedGuardAIController> AllGuards =
        new List<AdvancedGuardAIController>();

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

    [Header("AA Movement Polish")]
    public bool configureAgentForPolish = true;
    public float agentAcceleration = 18f;
    public float agentAngularSpeed = 540f;
    public float patrolStoppingDistance = 0.15f;
    public float chaseStoppingDistance = 0.2f;
    public float investigateStoppingDistance = 0.3f;
    public float manualTurnSpeed = 540f;

    [Header("Suspicion")]
    [Range(0f, 100f)] public float suspicion = 0f;
    public float suspiciousThreshold = 35f;
    public float chaseThreshold = 90f;
    public float suspicionIncreasePerSecond = 35f;
    public float suspicionDecreasePerSecond = 18f;
    public float suspiciousLookTime = 1.3f;

    [Header("Vision Modifiers")]
    public float farVisionMultiplier = 0.5f;
    public float closeVisionMultiplier = 1.6f;

    [Header("Movement Vision Modifiers")]
    public float silentVisionMultiplier = 0.75f;
    public float walkingVisionMultiplier = 1f;
    public float sprintingVisionMultiplier = 2.1f;
    public float crouchingVisionMultiplier = 0.7f;
    public float crawlingVisionMultiplier = 0.5f;
    public float airborneVisionMultiplier = 1.3f;

    [Header("Hearing")]
    public float minAudibilityToInvestigate = 0.12f;
    public float soundSuspicionBoost = 25f;
    public bool allowHearingDuringChase = true;
    public bool chaseCanFollowPublicSounds = true;

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

    [Header("Smart Search")]
    public float firstSearchLookTime = 1.5f;
    public float forwardGuessDistance = 4f;
    public float sideSearchDistance = 3f;
    public float wideningSearchStep = 2.5f;

    [Header("Visual Debug")]
    public Renderer guardRenderer;
    public GameObject alertIcon;

    public Color patrolColor = Color.white;
    public Color suspiciousColor = Color.yellow;
    public Color investigateColor = new Color(1f, 0.65f, 0f);
    public Color chaseColor = Color.red;
    public Color searchColor = new Color(1f, 0.35f, 0f);
    public Color returnColor = Color.gray;

    [Header("Advanced Vision Feel")]
    public float centerVisionBonus = 1.35f;
    public float edgeVisionPenalty = 0.55f;
    public float suspicionMemoryTime = 1.2f;
    public float suspicionGraceDecayMultiplier = 0.35f;
    public float alertStateVisionMultiplier = 1.25f;
    public float suspicionStateVisionMultiplier = 1.1f;

    [Header("Cover Identity / Zone Logic")]
    public bool useCoverIdentitySystem = true;
    public PlayerCoverIdentity playerCoverIdentity;
    public PlayerZoneTracker playerZoneTracker;
    public PlayerMovementStateTracker playerMovementStateTracker;

    [Header("Restricted Zone Suspicion")]
    public float unauthorizedZoneMinimumSuspicion = 20f;
    public float unauthorizedZoneSuspicionPerSecond = 18f;
    public float allowedDisguiseSuspicionMultiplier = 0.35f;
    public float partialDisguiseSuspicionMultiplier = 0.65f;
    public float noDisguiseRestrictedMultiplier = 1.4f;
    public float wrongDisguiseRestrictedMultiplier = 1.8f;

    [Header("Reasoned Suspicion")]
    public GuardSuspicionReasoner suspicionReasoner;
    public GuardSuspicionReason currentSuspicionReason;
    public string currentSuspicionReasonText;
    public bool requireReasonToChase = true;

    [Header("Guard Questioning / Fast Talk")]
    public bool allowGuardQuestioning = true;
    public GuardQuestioningResponder questioningResponder;
    public bool guardSaysReasonBeforeQuestioning = true;

    [Header("Question Approach")]
    public bool guardMustBeCloseToQuestion = true;
    public float questioningStartDistance = 2.2f;
    public float questioningApproachStopDistance = 1.8f;
    public float questioningMaxApproachTime = 4.0f;
    public float minimumTimeBetweenQuestionAttempts = 3.0f;
    public bool returnToSearchIfQuestionApproachFails = true;

    private float questionApproachTimer;
    private float localQuestionAttemptCooldownTimer;

    [Header("Fast Talk Cooldown")]
    public float fastTalkCooldownTimer;
    public float fastTalkCooldownSuspicionMultiplier = 0.05f;
    public bool ignorePlayerDuringFastTalkCooldown = true;
    public bool blockQuestioningDuringFastTalkCooldown = true;
    public bool blockChaseDuringFastTalkCooldown = true;
    public bool showFastTalkCooldownDebug = true;

    [Header("Disguise Suspicion Relief")]
    public bool reduceSuspicionWhenDisguiseImproves = true;
    public float disguiseCheckInterval = 0.2f;
    public float validDisguiseSuspicionDropPerSecond = 30f;
    public float newlyValidDisguiseInstantDrop = 25f;
    public float validDisguiseSuspicionCap = 25f;
    public float validDisguiseAlertReduction = 8f;
    public bool allowDisguiseReliefOnlyBeforeChase = true;
    public bool printDisguiseReliefDebug = true;

    private float disguiseCheckTimer;
    private bool previousDisguiseAllowed;
    private string previousDisguiseSetID = "";
    private float previousDisguiseCompleteness;

    [Header("Realistic Sound Investigation")]
    public bool useRealisticSoundInvestigation = true;
    public float soundReactionDelay = 0.45f;
    public float minimumTimeBetweenSoundReactions = 0.6f;
    public float soundPositionUncertaintyAtLowConfidence = 5f;
    public float soundPositionUncertaintyAtHighConfidence = 0.8f;
    public float strongSoundAudibilityThreshold = 0.55f;
    public float weakSoundSuspicionMultiplier = 0.35f;
    public float strongSoundSuspicionMultiplier = 1f;
    public bool onlyQuestionSoundIfPlayerSeen = true;
    public bool showSoundDebug = true;

    private bool hasPendingSoundInvestigation;
    private Vector3 pendingSoundPosition;
    private float pendingSoundConfidence;
    private float pendingSoundTimer;
    private float lastSoundReactionTime = -999f;
    private float currentSearchRadiusMultiplier = 1f;
    private float currentSearchWaitMultiplier = 1f;

    [Header("Questioning Repeat Limiter")]
    public GuardQuestioningRepeatLimiter questioningRepeatLimiter;

    [Header("Guard Coordination")]
    public bool useGuardCoordination = true;
    public bool canCallForHelp = true;
    public bool canRespondToHelp = true;

    public bool callForHelpWhenChaseStarts = true;
    public bool callForHelpWhenStrongSoundHeard = true;

    public float helpCallRadius = 18f;
    public float helpCallCooldown = 6f;
    public float helpCallUrgencyOnChase = 1.2f;
    public float helpCallUrgencyOnSound = 0.65f;

    public float externalAlertSuspicionBoost = 18f;
    public float externalAlertInvestigationDelay = 0.2f;

    public bool respondingGuardCanOnlyChaseIfHeSeesPlayer = true;
    public bool ignoreExternalAlertsWhileChasing = true;
    public bool externalAlertCancelsPatrolWait = true;
    public bool showGuardCoordinationDebug = true;

    [Header("Call For Help Visual Feedback")]
    public GameObject callForHelpIndicator;
    public GameObject receivedHelpIndicator;
    public float callForHelpIndicatorDuration = 1.75f;
    public float receivedHelpIndicatorDuration = 1.25f;
    public bool hideIndicatorsOnStart = true;

    [Header("Call For Help Runtime Inspector")]
    public bool isCurrentlyCallingForHelp;
    public bool isCurrentlyRespondingToHelp;
    public float callForHelpIndicatorTimer;
    public float receivedHelpIndicatorTimer;
    public int lastHelpCallRecipientCount;
    [TextArea(2, 4)] public string lastHelpCallInspectorMessage;
    [TextArea(2, 4)] public string lastReceivedHelpInspectorMessage;

    private float lastHelpCallTime = -999f;
    private bool isRespondingToExternalAlert;
    private float externalAlertDelayTimer;
    private Vector3 pendingExternalAlertPosition;
    private string pendingExternalAlertReason;
    private float pendingExternalAlertUrgency;

    [Header("Debug")]
    public bool printStateChanges = true;
    public bool printSuspicionReasonChanges = true;

    private NavMeshAgent agent;
    private GuardVision vision;
    private Transform player;
    private PlayerSoundEmitter playerSoundEmitter;

    private Transform[] patrolPoints;
    private int currentPatrolIndex;

    private Vector3 lastKnownPlayerPosition;
    private Vector3 previousKnownPlayerPosition;
    private Vector3 estimatedPlayerMoveDirection;
    private bool hasEstimatedMoveDirection;

    private Vector3 soundPosition;
    private Vector3 originalLookDirection;
    private Vector3 currentSearchPoint;

    private float patrolWaitTimer;
    private float lookAroundTimer;
    private float lostPlayerTimer;
    private float searchWaitTimer;
    private float recentlySawPlayerTimer;

    private bool isWaitingAtSearchPoint;
    private int searchPointsVisited;

    private GuardState previousState;
    private GuardSuspicionReason previousSuspicionReason;
    private string previousSuspicionReasonText;

    private GuardSuspicionEvaluation currentSuspicionEvaluation;
    private string currentQuestioningSignature;

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        vision = GetComponent<GuardVision>();

        if (questioningResponder == null)
        {
            questioningResponder = GetComponent<GuardQuestioningResponder>();
        }

        if (suspicionReasoner == null)
        {
            suspicionReasoner = GetComponent<GuardSuspicionReasoner>();
        }

        if (questioningRepeatLimiter == null)
        {
            questioningRepeatLimiter = GetComponent<GuardQuestioningRepeatLimiter>();
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

            if (playerMovementStateTracker == null)
            {
                playerMovementStateTracker = playerObject.GetComponent<PlayerMovementStateTracker>();
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

        if (playerMovementStateTracker == null)
        {
            playerMovementStateTracker = FindObjectOfType<PlayerMovementStateTracker>();
        }

        if (guardRenderer == null)
        {
            guardRenderer = GetComponentInChildren<Renderer>();
        }

        LoadPatrolPoints();
    }

    private void OnEnable()
    {
        if (!AllGuards.Contains(this))
        {
            AllGuards.Add(this);
        }

        PlayerSoundEmitter.OnSoundEmitted += HandlePlayerSound;
    }

    private void OnDisable()
    {
        AllGuards.Remove(this);
        PlayerSoundEmitter.OnSoundEmitted -= HandlePlayerSound;
    }

    private void Start()
    {
        ApplyGuardProfile();
        ConfigureAgentForPolish();

        if (hideIndicatorsOnStart)
        {
            if (callForHelpIndicator != null)
            {
                callForHelpIndicator.SetActive(false);
            }

            if (receivedHelpIndicator != null)
            {
                receivedHelpIndicator.SetActive(false);
            }
        }

        EnterPatrol();
    }

    private void Update()
    {
        if (questioningResponder != null && questioningResponder.IsQuestioning)
        {
            DebugStateChange();
            return;
        }

        TickFastTalkCooldown();
        TickQuestionAttemptCooldown();
        UpdateCallForHelpFeedback();
        UpdateDisguiseSuspicionRelief();
        UpdatePendingSoundInvestigation();
        UpdateExternalGuardAlertResponse();

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

            case GuardState.QuestionApproach:
                UpdateQuestionApproach();
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
        DebugSuspicionReasonChange();
    }

    private void ConfigureAgentForPolish()
    {
        if (!configureAgentForPolish || agent == null)
        {
            return;
        }

        agent.acceleration = agentAcceleration;
        agent.angularSpeed = agentAngularSpeed;
        agent.updateRotation = false;
        agent.autoBraking = true;
    }

    private void TickFastTalkCooldown()
    {
        if (fastTalkCooldownTimer <= 0f)
        {
            return;
        }

        fastTalkCooldownTimer -= Time.deltaTime;

        if (fastTalkCooldownTimer < 0f)
        {
            fastTalkCooldownTimer = 0f;
        }
    }

    private void TickQuestionAttemptCooldown()
    {
        if (localQuestionAttemptCooldownTimer <= 0f)
        {
            return;
        }

        localQuestionAttemptCooldownTimer -= Time.deltaTime;

        if (localQuestionAttemptCooldownTimer < 0f)
        {
            localQuestionAttemptCooldownTimer = 0f;
        }
    }

    public bool IsInFastTalkCooldown()
    {
        return fastTalkCooldownTimer > 0f;
    }

    public void StartFastTalkCooldown(float duration, bool clearStealthToggles)
    {
        fastTalkCooldownTimer = Mathf.Max(fastTalkCooldownTimer, duration);

        if (clearStealthToggles && playerMovementStateTracker != null)
        {
            playerMovementStateTracker.ForceClearStealthToggles();
        }

        if (showFastTalkCooldownDebug)
        {
            Debug.Log($"[{gameObject.name}] Fast Talk cooldown started: {fastTalkCooldownTimer:0.0}s");
        }
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

        bool canSeePlayer = CanCurrentlySeePlayer();

        if (!canSeePlayer)
        {
            ClearSightOnlySuspicionReason();
            DecaySuspicionIfAllowed();
            recentlySawPlayerTimer -= Time.deltaTime;
            return;
        }

        if (IsInFastTalkCooldown() && ignorePlayerDuringFastTalkCooldown)
        {
            DecaySuspicionIfAllowed();
            FaceTarget(player.position);
            return;
        }

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

        RestrictedZone currentZone = GetCurrentPlayerZone();

        GuardSuspicionEvaluation evaluation = GuardSuspicionEvaluation.Suspicious(
            GuardSuspicionReason.SawPlayerNormally,
            "saw player",
            1f,
            false
        );

        if (suspicionReasoner != null)
        {
            evaluation = suspicionReasoner.EvaluateVision(
                currentZone,
                playerCoverIdentity,
                playerMovementStateTracker
            );
        }

        currentSuspicionEvaluation = evaluation;

        if (!evaluation.shouldIncreaseSuspicion)
        {
            currentSuspicionReason = GuardSuspicionReason.None;
            currentSuspicionReasonText = "";
            currentQuestioningSignature = "";

            if (questioningRepeatLimiter != null)
            {
                questioningRepeatLimiter.ObserveCurrentReason(
                    GuardSuspicionEvaluation.Ignore(),
                    ""
                );
            }

            DecaySuspicionIfAllowed();
            return;
        }

        currentSuspicionReason = evaluation.reason;
        currentSuspicionReasonText = evaluation.reasonText;
        currentQuestioningSignature = BuildQuestioningSignature(evaluation);

        if (questioningRepeatLimiter != null)
        {
            questioningRepeatLimiter.ObserveCurrentReason(
                evaluation,
                currentQuestioningSignature
            );
        }

        FaceTarget(player.position);

        string zoneID = GetCurrentZoneID();

        float memoryMultiplier = 1f;
        float memoryBonus = 0f;

        if (GuardMemoryManager.Instance != null)
        {
            memoryMultiplier = GuardMemoryManager.Instance.GetMemoryMultiplier(zoneID);
            memoryBonus = GuardMemoryManager.Instance.GetMemorySuspicionBonus(zoneID);
        }

        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        float distanceMultiplier = GetDistanceSuspicionMultiplier(distanceToPlayer);
        float movementMultiplier = GetPlayerMovementVisionMultiplier();
        float angleMultiplier = GetVisionAngleMultiplier();
        float stateMultiplier = GetStateVisionMultiplier();
        float coverIdentityMultiplier = GetCoverIdentitySuspicionMultiplier();
        float zoneLegalityMultiplier = GetZoneLegalitySuspicionMultiplier();
        float zoneAlertMultiplier = GetZoneAlertSuspicionMultiplier();

        float reasonSuspicionPerSecond = evaluation.suspicionPerSecond > 0f
            ? evaluation.suspicionPerSecond
            : suspicionIncreasePerSecond;

        float increaseAmount =
            reasonSuspicionPerSecond *
            distanceMultiplier *
            movementMultiplier *
            angleMultiplier *
            stateMultiplier *
            coverIdentityMultiplier *
            zoneLegalityMultiplier *
            evaluation.multiplier *
            memoryMultiplier *
            zoneAlertMultiplier *
            Time.deltaTime;

        if (IsInFastTalkCooldown())
        {
            increaseAmount *= fastTalkCooldownSuspicionMultiplier;
        }

        if (evaluation.minimumSuspicion > 0f && suspicion < evaluation.minimumSuspicion)
        {
            suspicion = evaluation.minimumSuspicion;
        }

        suspicion += increaseAmount;

        if (memoryBonus > 0f && suspicion < memoryBonus)
        {
            suspicion = memoryBonus;
        }

        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        if (suspicionReasoner != null)
        {
            suspicionReasoner.ReportMemoryIfNeeded(zoneID, evaluation);
        }

        ApplyUnauthorizedZoneSuspicion(canSeePlayer);

        if (TryStartQuestioningBeforeChase())
        {
            return;
        }

        if (suspicion >= chaseThreshold)
        {
            if (CanEscalateToChase())
            {
                EnterChase();
            }
            else
            {
                EnterSuspicious();
            }

            return;
        }

        if (suspicion >= suspiciousThreshold &&
            currentState != GuardState.Suspicious &&
            currentState != GuardState.QuestionApproach &&
            currentState != GuardState.InvestigateSound &&
            currentState != GuardState.SearchArea)
        {
            EnterSuspicious();
        }
    }

    private void ClearSightOnlySuspicionReason()
    {
        switch (currentSuspicionReason)
        {
            case GuardSuspicionReason.UnauthorizedZone:
            case GuardSuspicionReason.NoDisguiseInRestrictedZone:
            case GuardSuspicionReason.WrongDisguise:
            case GuardSuspicionReason.PartialDisguise:
            case GuardSuspicionReason.CrouchingInPublic:
            case GuardSuspicionReason.CrawlingInPublic:
            case GuardSuspicionReason.RunningNearGuard:
            case GuardSuspicionReason.SuspiciousMovementInPublic:
                currentSuspicionReason = GuardSuspicionReason.None;
                currentSuspicionReasonText = "";
                currentQuestioningSignature = "";
                currentSuspicionEvaluation = GuardSuspicionEvaluation.Ignore();
                break;
        }
    }

    private void DecaySuspicionIfAllowed()
    {
        bool shouldDecaySuspicion =
            currentState == GuardState.Patrol ||
            currentState == GuardState.LookAround ||
            currentState == GuardState.ReturnToPatrol;

        if (!shouldDecaySuspicion)
        {
            return;
        }

        float decayMultiplier = recentlySawPlayerTimer > 0f
            ? suspicionGraceDecayMultiplier
            : 1f;

        suspicion -= suspicionDecreasePerSecond * decayMultiplier * Time.deltaTime;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);
    }

    private bool TryStartQuestioningBeforeChase()
    {
        if (!allowGuardQuestioning)
        {
            return false;
        }

        if (currentState == GuardState.Chase ||
            currentState == GuardState.CapturePlayer)
        {
            return false;
        }

        if (currentState == GuardState.QuestionApproach)
        {
            return true;
        }

        if (localQuestionAttemptCooldownTimer > 0f)
        {
            return false;
        }

        if (IsInFastTalkCooldown() && blockQuestioningDuringFastTalkCooldown)
        {
            return false;
        }

        if (IsQuestioningOrResolving())
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

        if (!currentSuspicionEvaluation.canTriggerQuestioning)
        {
            return false;
        }

        if (suspicion < GetCurrentQuestioningThreshold())
        {
            return false;
        }

        if (!CanCurrentlySeePlayer())
        {
            return false;
        }

        string signature = BuildQuestioningSignature(currentSuspicionEvaluation);

        if (questioningRepeatLimiter != null &&
            questioningRepeatLimiter.IsBlocked(currentSuspicionReason, signature))
        {
            return false;
        }

        if (guardMustBeCloseToQuestion && !IsCloseEnoughToQuestion())
        {
            EnterQuestionApproach();
            return true;
        }

        return StartQuestioningNow(signature);
    }

    private bool StartQuestioningNow(string signature)
    {
        if (questioningResponder == null || !questioningResponder.CanQuestion())
        {
            return false;
        }

        localQuestionAttemptCooldownTimer = minimumTimeBetweenQuestionAttempts;

        string reasonLine = GetGuardQuestioningReasonLine();

        questioningResponder.StartQuestioning(reasonLine);

        if (questioningRepeatLimiter != null)
        {
            questioningRepeatLimiter.MarkQuestioned(
                currentSuspicionReason,
                signature,
                currentSuspicionReasonText
            );
        }

        return true;
    }

    private void EnterQuestionApproach()
    {
        if (currentState == GuardState.QuestionApproach)
        {
            return;
        }

        currentState = GuardState.QuestionApproach;
        questionApproachTimer = questioningMaxApproachTime;

        agent.speed = investigateSpeed;
        agent.stoppingDistance = questioningApproachStopDistance;
        agent.isStopped = false;

        if (player != null)
        {
            agent.SetDestination(player.position);
        }

        SetVisualState(suspiciousColor, true);
    }

    private void UpdateQuestionApproach()
    {
        if (player == null)
        {
            EnterSearchArea();
            return;
        }

        if (!CanCurrentlySeePlayer())
        {
            if (returnToSearchIfQuestionApproachFails)
            {
                EnterInvestigateLastKnownPosition();
            }
            else
            {
                EnterSuspicious();
            }

            return;
        }

        questionApproachTimer -= Time.deltaTime;

        agent.isStopped = false;
        agent.speed = investigateSpeed;
        agent.stoppingDistance = questioningApproachStopDistance;
        agent.SetDestination(player.position);

        FaceTarget(player.position);

        if (IsCloseEnoughToQuestion())
        {
            agent.isStopped = true;

            string signature = BuildQuestioningSignature(currentSuspicionEvaluation);
            StartQuestioningNow(signature);
            return;
        }

        if (questionApproachTimer <= 0f)
        {
            EnterSuspicious();
        }
    }

    private bool IsCloseEnoughToQuestion()
    {
        if (player == null)
        {
            return false;
        }

        return Vector3.Distance(transform.position, player.position) <= questioningStartDistance;
    }

    private string GetGuardQuestioningReasonLine()
    {
        if (!guardSaysReasonBeforeQuestioning)
        {
            return "";
        }

        if (!string.IsNullOrWhiteSpace(currentSuspicionReasonText))
        {
            switch (currentSuspicionReason)
            {
                case GuardSuspicionReason.HeardNoise:
                case GuardSuspicionReason.InvestigatedSound:
                    return $"Stop. I heard you {currentSuspicionReasonText}. Explain yourself.";

                default:
                    return $"Stop. I saw you {currentSuspicionReasonText}. Explain yourself.";
            }
        }

        return "Stop. You are acting suspiciously. Explain yourself.";
    }

    private bool CanEscalateToChase()
    {
        if (IsInFastTalkCooldown() && blockChaseDuringFastTalkCooldown)
        {
            return false;
        }

        if (!requireReasonToChase)
        {
            return true;
        }

        if (currentSuspicionEvaluation.canTriggerChase)
        {
            return true;
        }

        switch (currentSuspicionReason)
        {
            case GuardSuspicionReason.UnauthorizedZone:
            case GuardSuspicionReason.WrongDisguise:
            case GuardSuspicionReason.NoDisguiseInRestrictedZone:
            case GuardSuspicionReason.IgnoredWarning:
            case GuardSuspicionReason.FailedQuestioning:
            case GuardSuspicionReason.ContrabandSeen:
            case GuardSuspicionReason.LockpickingSeen:
            case GuardSuspicionReason.PickpocketingSeen:
            case GuardSuspicionReason.AttackedGuard:
                return true;

            case GuardSuspicionReason.CrawlingInPublic:
                return GuardMemoryManager.Instance != null &&
                       GuardMemoryManager.Instance.IsHostile(GetCurrentZoneID());

            default:
                return false;
        }
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
            case GuardState.QuestionApproach:
            case GuardState.InvestigateSound:
            case GuardState.SearchArea:
                return suspicionStateVisionMultiplier;

            default:
                return 1f;
        }
    }

    private float GetPlayerMovementVisionMultiplier()
    {
        if (playerMovementStateTracker == null)
        {
            return walkingVisionMultiplier;
        }

        switch (playerMovementStateTracker.GetCurrentState())
        {
            case PlayerMovementVisualState.Idle:
                return silentVisionMultiplier;

            case PlayerMovementVisualState.Walking:
                return walkingVisionMultiplier;

            case PlayerMovementVisualState.Running:
                return sprintingVisionMultiplier;

            case PlayerMovementVisualState.Crouching:
                return crouchingVisionMultiplier;

            case PlayerMovementVisualState.Crawling:
                return crawlingVisionMultiplier;

            case PlayerMovementVisualState.Jumping:
            case PlayerMovementVisualState.Falling:
                return airborneVisionMultiplier;

            default:
                return walkingVisionMultiplier;
        }
    }

    private void HandlePlayerSound(PlayerSoundEmitter.SoundField sound)
    {
        if (currentState == GuardState.CapturePlayer ||
            IsBeingControlledByQuestioning())
        {
            return;
        }

        if (player == null)
        {
            return;
        }

        if (IsInFastTalkCooldown() && ignorePlayerDuringFastTalkCooldown)
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

        if (ZoneAlertManager.Instance != null)
        {
            finalAudibility *= ZoneAlertManager.Instance.GetSoundSensitivityMultiplier(GetCurrentZoneID());
        }

        if (finalAudibility < minAudibilityToInvestigate)
        {
            return;
        }

        if (Time.time < lastSoundReactionTime + minimumTimeBetweenSoundReactions)
        {
            return;
        }

        RestrictedZone currentZone = GetCurrentPlayerZone();
        bool playerIsInPublic = currentZone == null || currentZone.IsPublicZone();
        bool guardIsChasing = currentState == GuardState.Chase;

        if (playerIsInPublic && !guardIsChasing)
        {
            if (showSoundDebug)
            {
                Debug.Log($"[{gameObject.name}] Ignored public sound because guard is not chasing.");
            }

            return;
        }

        if (guardIsChasing)
        {
            if (!allowHearingDuringChase)
            {
                return;
            }

            if (playerIsInPublic && !chaseCanFollowPublicSounds)
            {
                return;
            }
        }

        lastSoundReactionTime = Time.time;

        if (guardIsChasing)
        {
            HandleSoundDuringChase(sound.origin, finalAudibility, playerIsInPublic, currentZone);
            return;
        }

        HandleSuspiciousSoundOutsideChase(sound.origin, finalAudibility, currentZone);
    }

    private void HandleSoundDuringChase(
        Vector3 soundOrigin,
        float finalAudibility,
        bool playerIsInPublic,
        RestrictedZone currentZone
    )
    {
        Vector3 safePosition = GetSafeNavMeshPosition(soundOrigin, 3f);

        soundPosition = safePosition;
        lastKnownPlayerPosition = safePosition;
        lostPlayerTimer = lostPlayerAfterSeconds;

        currentSuspicionReason = GuardSuspicionReason.HeardNoise;

        if (playerIsInPublic)
        {
            currentSuspicionReasonText = "making noise while fleeing in public";
        }
        else
        {
            string zoneName = currentZone != null && !string.IsNullOrWhiteSpace(currentZone.zoneDisplayName)
                ? currentZone.zoneDisplayName
                : "a restricted area";

            currentSuspicionReasonText = $"making noise while fleeing inside {zoneName}";
        }

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = chaseSpeed;
            agent.stoppingDistance = chaseStoppingDistance;
            agent.SetDestination(lastKnownPlayerPosition);
        }

        if (showSoundDebug)
        {
            Debug.Log(
                $"[{gameObject.name}] Heard player during chase. " +
                $"Following sound. Audibility: {finalAudibility:0.00}, Position: {safePosition}"
            );
        }
    }

    private void HandleSuspiciousSoundOutsideChase(
        Vector3 soundOrigin,
        float finalAudibility,
        RestrictedZone currentZone
    )
    {
        if (currentZone == null || currentZone.IsPublicZone())
        {
            return;
        }

        string zoneName = !string.IsNullOrWhiteSpace(currentZone.zoneDisplayName)
            ? currentZone.zoneDisplayName
            : currentZone.zoneID;

        currentSuspicionReason = GuardSuspicionReason.HeardNoise;
        currentSuspicionReasonText = $"making noise inside {zoneName}";

        float soundSuspicionMultiplier = finalAudibility >= strongSoundAudibilityThreshold
            ? strongSoundSuspicionMultiplier
            : weakSoundSuspicionMultiplier;

        suspicion += soundSuspicionBoost * finalAudibility * soundSuspicionMultiplier;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        if (GuardMemoryManager.Instance != null)
        {
            GuardMemoryManager.Instance.ReportSuspicion(
                GetCurrentZoneID(),
                GuardSuspicionReason.HeardNoise,
                $"heard noise inside {zoneName}",
                finalAudibility >= strongSoundAudibilityThreshold ? 5f : 2f,
                false
            );
        }

        if (ZoneAlertManager.Instance != null && finalAudibility >= strongSoundAudibilityThreshold)
        {
            ZoneAlertManager.Instance.AddAlert(
                GetCurrentZoneID(),
                3f,
                $"guard heard a strong sound inside {zoneName}"
            );
        }

        if (callForHelpWhenStrongSoundHeard &&
            finalAudibility >= strongSoundAudibilityThreshold)
        {
            CallNearbyGuardsForHelpAtPosition(
                soundOrigin,
                $"heard a strong sound inside {zoneName}",
                helpCallUrgencyOnSound
            );
        }

        if (useRealisticSoundInvestigation)
        {
            StartPendingSoundInvestigation(soundOrigin, finalAudibility);
        }
        else
        {
            soundPosition = GetSafeNavMeshPosition(soundOrigin, 3f);
            lastKnownPlayerPosition = soundPosition;
            EnterInvestigateSound();
        }

        if (!onlyQuestionSoundIfPlayerSeen && TryStartQuestioningBeforeChase())
        {
            return;
        }

        if (CanCurrentlySeePlayer() &&
            suspicion >= chaseThreshold &&
            CanEscalateToChase())
        {
            EnterChase();
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
        agent.stoppingDistance = patrolStoppingDistance;
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

        FaceTarget(agent.steeringTarget);

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
        if (currentState == GuardState.Suspicious)
        {
            return;
        }

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

        lookAroundTimer -= Time.deltaTime;

        if (player != null && CanCurrentlySeePlayer())
        {
            FaceTarget(player.position);
        }
        else
        {
            LookAroundDuringSearch();
        }

        if (suspicion >= chaseThreshold && CanEscalateToChase())
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

        float alertSpeedMultiplier = 1f;

        if (ZoneAlertManager.Instance != null)
        {
            alertSpeedMultiplier = ZoneAlertManager.Instance.GetInvestigationSpeedMultiplier(GetCurrentZoneID());
        }

        agent.speed = investigateSpeed * alertSpeedMultiplier;
        agent.stoppingDistance = investigateStoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(soundPosition);

        SetVisualState(investigateColor, true);

        if (showSoundDebug)
        {
            Debug.Log($"[{gameObject.name}] Investigating sound at approximate position: {soundPosition}");
        }
    }

    private void UpdateInvestigateSound()
    {
        FaceTarget(agent.steeringTarget);

        if (!onlyQuestionSoundIfPlayerSeen)
        {
            if (TryStartQuestioningBeforeChase())
            {
                return;
            }
        }
        else if (CanCurrentlySeePlayer())
        {
            if (TryStartQuestioningBeforeChase())
            {
                return;
            }
        }

        if (ReachedDestination(patrolPointReachDistance))
        {
            currentSuspicionReason = GuardSuspicionReason.InvestigatedSound;
            currentSuspicionReasonText = "near the sound I investigated";

            EnterSearchArea();
        }
    }

    private void EnterInvestigateLastKnownPosition()
    {
        currentState = GuardState.InvestigateSound;
        agent.speed = investigateSpeed;
        agent.stoppingDistance = investigateStoppingDistance;
        agent.isStopped = false;
        agent.SetDestination(lastKnownPlayerPosition);

        SetVisualState(investigateColor, true);
    }

    private void EnterChase()
    {
        if (currentState == GuardState.Chase)
        {
            if (player != null && CanCurrentlySeePlayer())
            {
                lastKnownPlayerPosition = player.position;
            }

            return;
        }

        currentState = GuardState.Chase;
        agent.speed = chaseSpeed;
        agent.stoppingDistance = chaseStoppingDistance;
        agent.isStopped = false;
        lostPlayerTimer = lostPlayerAfterSeconds;

        if (player != null && CanCurrentlySeePlayer())
        {
            lastKnownPlayerPosition = player.position;
        }

        SetVisualState(chaseColor, true);

        Debug.Log($"[{gameObject.name}] Chasing player. Reason: {currentSuspicionReason} / {currentSuspicionReasonText}");

        if (callForHelpWhenChaseStarts)
        {
            Vector3 helpPosition = lastKnownPlayerPosition;

            if (player != null && CanCurrentlySeePlayer())
            {
                helpPosition = player.position;
            }

            CallNearbyGuardsForHelpAtPosition(
                helpPosition,
                $"chasing suspect because {currentSuspicionReasonText}",
                helpCallUrgencyOnChase
            );
        }
    }

    private void UpdateChase()
    {
        if (player == null)
        {
            EnterSearchArea();
            return;
        }

        if (CanCurrentlySeePlayer())
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
        agent.stoppingDistance = investigateStoppingDistance;
        agent.isStopped = false;

        searchPointsVisited = 0;
        isWaitingAtSearchPoint = false;

        SetVisualState(searchColor, true);

        PickNextSmartSearchPoint();
    }

    private void UpdateSearchArea()
    {
        if (CanCurrentlySeePlayer())
        {
            if (TryStartQuestioningBeforeChase())
            {
                return;
            }

            if (suspicion >= chaseThreshold && CanEscalateToChase())
            {
                EnterChase();
                return;
            }
        }

        if (isWaitingAtSearchPoint)
        {
            searchWaitTimer -= Time.deltaTime;
            LookAroundDuringSearch();

            if (searchWaitTimer <= 0f)
            {
                isWaitingAtSearchPoint = false;
                searchPointsVisited++;

                if (searchPointsVisited >= GetEffectiveSearchPointCount())
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

            float alertWaitMultiplier = 1f;

            if (ZoneAlertManager.Instance != null)
            {
                alertWaitMultiplier = ZoneAlertManager.Instance.GetSearchWaitMultiplier(GetCurrentZoneID());
            }

            float confidenceWaitMultiplier = currentSearchWaitMultiplier <= 0f
                ? 1f
                : currentSearchWaitMultiplier;

            searchWaitTimer = searchPointsVisited == 0
                ? firstSearchLookTime * alertWaitMultiplier * confidenceWaitMultiplier
                : waitAtSearchPointTime * alertWaitMultiplier * confidenceWaitMultiplier;
        }
    }

    private int GetEffectiveSearchPointCount()
    {
        int extra = 0;

        if (ZoneAlertManager.Instance != null)
        {
            extra = ZoneAlertManager.Instance.GetExtraSearchPoints(GetCurrentZoneID());
        }

        return Mathf.Max(1, searchPointCount + extra);
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
                searchTarget = hasEstimatedMoveDirection
                    ? lastKnownPlayerPosition + estimatedPlayerMoveDirection * forwardGuessDistance
                    : lastKnownPlayerPosition + transform.forward * forwardGuessDistance;
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
        agent.stoppingDistance = patrolStoppingDistance;
        agent.isStopped = false;

        suspicion = Mathf.Clamp(suspicion, 0f, 35f);

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

        Debug.Log($"PLAYER CAUGHT by {gameObject.name}. Reason: {currentSuspicionReason} / {currentSuspicionReasonText}");

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
            if (patrolPoints[i] == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, patrolPoints[i].position);

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearestIndex = i;
            }
        }

        return nearestIndex;
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
            manualTurnSpeed * Time.deltaTime
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
        float alertRadiusMultiplier = 1f;

        if (ZoneAlertManager.Instance != null)
        {
            alertRadiusMultiplier = ZoneAlertManager.Instance.GetSearchRadiusMultiplier(GetCurrentZoneID());
        }

        float confidenceRadiusMultiplier = currentSearchRadiusMultiplier <= 0f
            ? 1f
            : currentSearchRadiusMultiplier;

        float effectiveSearchRadius =
            searchRadius *
            alertRadiusMultiplier *
            confidenceRadiusMultiplier;

        float currentRadius = effectiveSearchRadius + searchPointsVisited * wideningSearchStep;

        Vector2 randomCircle = Random.insideUnitCircle.normalized *
                               Random.Range(effectiveSearchRadius * 0.5f, currentRadius);

        return new Vector3(
            lastKnownPlayerPosition.x + randomCircle.x,
            lastKnownPlayerPosition.y,
            lastKnownPlayerPosition.z + randomCircle.y
        );
    }

    private bool TrySetSearchDestination(Vector3 targetPosition)
    {
        Vector3 safePosition = GetSafeNavMeshPosition(targetPosition, 2.5f);

        if (NavMesh.SamplePosition(safePosition, out NavMeshHit hit, 2.5f, NavMesh.AllAreas))
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

        if (!CanCurrentlySeePlayer())
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
        if (!useCoverIdentitySystem || playerCoverIdentity == null)
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
        if (!canSeePlayer || !CanCurrentlySeePlayer())
        {
            return;
        }

        if (IsInFastTalkCooldown() && ignorePlayerDuringFastTalkCooldown)
        {
            return;
        }

        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone == null)
        {
            return;
        }

        if (zone.IsPublicZone())
        {
            return;
        }

        if (!zone.suspiciousIfUnauthorized)
        {
            return;
        }

        if (playerZoneTracker != null && !playerZoneTracker.IsCurrentlyInsideZone(zone))
        {
            return;
        }

        if (playerCoverIdentity != null && playerCoverIdentity.IsAllowedInZone(zone))
        {
            return;
        }

        currentSuspicionReason = GuardSuspicionReason.UnauthorizedZone;
        currentSuspicionReasonText = $"being somewhere you are not allowed in {zone.zoneDisplayName}";

        if (suspicion < unauthorizedZoneMinimumSuspicion)
        {
            suspicion = unauthorizedZoneMinimumSuspicion;
        }

        float zoneSuspicionRate = zone.unauthorizedSuspicionPerSecond > 0f
            ? zone.unauthorizedSuspicionPerSecond
            : unauthorizedZoneSuspicionPerSecond;

        suspicion += zoneSuspicionRate * Time.deltaTime;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);
    }

    private RestrictedZone GetCurrentPlayerZone()
    {
        if (playerZoneTracker != null)
        {
            RestrictedZone zone = playerZoneTracker.GetCurrentZone();

            if (zone != null)
            {
                return zone;
            }
        }

        if (PlayerZoneTracker.Instance != null)
        {
            RestrictedZone zone = PlayerZoneTracker.Instance.GetCurrentZone();

            if (zone != null)
            {
                return zone;
            }
        }

        ZoneTracker simpleZoneTracker = player != null
            ? player.GetComponent<ZoneTracker>()
            : null;

        if (simpleZoneTracker != null)
        {
            return simpleZoneTracker.GetCurrentRestrictedZone();
        }

        return null;
    }

    private string GetCurrentZoneID()
    {
        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone != null && !string.IsNullOrWhiteSpace(zone.zoneID))
        {
            return zone.zoneID.Trim();
        }

        ZoneTracker simpleZoneTracker = player != null
            ? player.GetComponent<ZoneTracker>()
            : null;

        if (simpleZoneTracker != null)
        {
            return simpleZoneTracker.GetCurrentZoneID();
        }

        return "public_city";
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
        fastTalkCooldownTimer = 0f;
        localQuestionAttemptCooldownTimer = 0f;
        questionApproachTimer = 0f;

        ClearGuardCoordinationRuntimeState();

        isWaitingAtSearchPoint = false;
        searchPointsVisited = 0;
        hasEstimatedMoveDirection = false;
        hasPendingSoundInvestigation = false;

        currentSuspicionReason = GuardSuspicionReason.None;
        currentSuspicionReasonText = "";
        currentSuspicionEvaluation = GuardSuspicionEvaluation.Ignore();
        currentQuestioningSignature = "";

        if (questioningRepeatLimiter != null)
        {
            questioningRepeatLimiter.ResetLimiter();
        }

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

        if (IsInFastTalkCooldown())
        {
            suspicion = Mathf.Clamp(suspicion, 0f, 10f);
            currentSuspicionReason = GuardSuspicionReason.None;
            currentSuspicionReasonText = "";
            currentSuspicionEvaluation = GuardSuspicionEvaluation.Ignore();
        }
        else
        {
            suspicion = Mathf.Clamp(suspicion, 0f, 35f);
        }

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
        currentSuspicionReason = GuardSuspicionReason.FailedQuestioning;
        currentSuspicionReasonText = "failing to explain yourself";

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

        currentSuspicionReason = GuardSuspicionReason.FailedQuestioning;
        currentSuspicionReasonText = "failing to explain yourself";

        EnterCapturePlayer();
    }

    public bool IsQuestioningOrResolving()
    {
        return questioningResponder != null &&
               (questioningResponder.IsQuestioning || questioningResponder.IsResolvingOutcome);
    }

    private float GetZoneAlertSuspicionMultiplier()
    {
        if (ZoneAlertManager.Instance == null)
        {
            return 1f;
        }

        return ZoneAlertManager.Instance.GetSuspicionMultiplier(GetCurrentZoneID());
    }

    private float GetCurrentQuestioningThreshold()
    {
        if (questioningResponder == null || questioningResponder.questioningProfile == null)
        {
            return 999f;
        }

        float baseThreshold = questioningResponder.questioningProfile.questioningThreshold;

        if (ZoneAlertManager.Instance == null)
        {
            return baseThreshold;
        }

        return baseThreshold + ZoneAlertManager.Instance.GetQuestioningThresholdModifier(GetCurrentZoneID());
    }

    private void UpdateDisguiseSuspicionRelief()
    {
        if (!reduceSuspicionWhenDisguiseImproves)
        {
            return;
        }

        if (playerCoverIdentity == null)
        {
            return;
        }

        if (currentState == GuardState.CapturePlayer)
        {
            return;
        }

        if (allowDisguiseReliefOnlyBeforeChase && currentState == GuardState.Chase)
        {
            return;
        }

        disguiseCheckTimer -= Time.deltaTime;

        if (disguiseCheckTimer > 0f)
        {
            return;
        }

        float reliefDelta = Mathf.Max(Time.deltaTime, disguiseCheckInterval);
        disguiseCheckTimer = disguiseCheckInterval;

        RestrictedZone zone = GetCurrentPlayerZone();

        if (zone == null || zone.IsPublicZone())
        {
            previousDisguiseAllowed = false;
            previousDisguiseSetID = "";
            previousDisguiseCompleteness = 0f;
            return;
        }

        string currentDisguiseSetID = playerCoverIdentity.GetBestDisguiseSetID();
        float currentCompleteness = playerCoverIdentity.GetBestDisguiseCompleteness();
        bool currentAllowed = playerCoverIdentity.IsAllowedInZone(zone);

        bool disguiseChanged =
            currentDisguiseSetID != previousDisguiseSetID ||
            Mathf.Abs(currentCompleteness - previousDisguiseCompleteness) > 0.01f;

        bool becameAllowed = !previousDisguiseAllowed && currentAllowed;

        if (currentAllowed)
        {
            suspicion -= validDisguiseSuspicionDropPerSecond * reliefDelta;
            suspicion = Mathf.Clamp(suspicion, 0f, 100f);

            if (suspicion > validDisguiseSuspicionCap)
            {
                suspicion = Mathf.MoveTowards(
                    suspicion,
                    validDisguiseSuspicionCap,
                    validDisguiseSuspicionDropPerSecond * reliefDelta
                );
            }

            if (becameAllowed || disguiseChanged)
            {
                suspicion -= newlyValidDisguiseInstantDrop;
                suspicion = Mathf.Clamp(suspicion, 0f, 100f);

                if (ZoneAlertManager.Instance != null)
                {
                    ZoneAlertManager.Instance.ReduceAlert(
                        GetCurrentZoneID(),
                        validDisguiseAlertReduction,
                        "player changed into a valid disguise"
                    );
                }

                currentSuspicionReason = GuardSuspicionReason.None;
                currentSuspicionReasonText = "valid disguise";

                if (printDisguiseReliefDebug)
                {
                    Debug.Log(
                        $"[{gameObject.name}] Disguise relief: {currentDisguiseSetID}, " +
                        $"Completeness: {currentCompleteness:0.00}, Suspicion: {suspicion:0.0}"
                    );
                }
            }
        }

        previousDisguiseAllowed = currentAllowed;
        previousDisguiseSetID = currentDisguiseSetID;
        previousDisguiseCompleteness = currentCompleteness;
    }

    private void StartPendingSoundInvestigation(Vector3 actualSoundPosition, float confidence)
    {
        hasPendingSoundInvestigation = true;
        pendingSoundConfidence = Mathf.Clamp01(confidence);
        pendingSoundTimer = soundReactionDelay;

        float uncertainty = Mathf.Lerp(
            soundPositionUncertaintyAtLowConfidence,
            soundPositionUncertaintyAtHighConfidence,
            pendingSoundConfidence
        );

        Vector2 randomOffset = Random.insideUnitCircle * uncertainty;

        pendingSoundPosition = new Vector3(
            actualSoundPosition.x + randomOffset.x,
            actualSoundPosition.y,
            actualSoundPosition.z + randomOffset.y
        );

        pendingSoundPosition = GetSafeNavMeshPosition(pendingSoundPosition, 3f);

        if (showSoundDebug)
        {
            Debug.Log(
                $"[{gameObject.name}] Heard sound. Confidence: {pendingSoundConfidence:0.00}, " +
                $"Uncertainty: {uncertainty:0.0}, Investigating approximate position."
            );
        }

        currentState = GuardState.Suspicious;
        agent.isStopped = true;
        lookAroundTimer = soundReactionDelay;
        SetVisualState(suspiciousColor, true);

        FaceTarget(pendingSoundPosition);
    }

    private void UpdatePendingSoundInvestigation()
    {
        if (!hasPendingSoundInvestigation)
        {
            return;
        }

        if (currentState == GuardState.Chase ||
            currentState == GuardState.CapturePlayer)
        {
            hasPendingSoundInvestigation = false;
            return;
        }

        pendingSoundTimer -= Time.deltaTime;

        FaceTarget(pendingSoundPosition);

        if (pendingSoundTimer > 0f)
        {
            return;
        }

        CommitPendingSoundInvestigation();
    }

    private void CommitPendingSoundInvestigation()
    {
        hasPendingSoundInvestigation = false;

        soundPosition = pendingSoundPosition;
        lastKnownPlayerPosition = pendingSoundPosition;

        currentSearchRadiusMultiplier = Mathf.Lerp(
            1.5f,
            0.75f,
            pendingSoundConfidence
        );

        currentSearchWaitMultiplier = Mathf.Lerp(
            1.35f,
            0.9f,
            pendingSoundConfidence
        );

        EnterInvestigateSound();
    }

    private string BuildQuestioningSignature(GuardSuspicionEvaluation evaluation)
    {
        string zoneID = GetCurrentZoneID();

        string movementState = "UnknownMovement";

        if (playerMovementStateTracker != null)
        {
            movementState = playerMovementStateTracker.GetCurrentState().ToString();
        }

        string disguiseSet = "NoDisguise";
        int disguiseCompletenessPercent = 0;

        if (playerCoverIdentity != null)
        {
            disguiseSet = playerCoverIdentity.GetBestDisguiseSetID();

            if (string.IsNullOrWhiteSpace(disguiseSet))
            {
                disguiseSet = "NoDisguise";
            }

            disguiseCompletenessPercent = Mathf.RoundToInt(
                playerCoverIdentity.GetBestDisguiseCompleteness() * 100f
            );
        }

        return
            $"Reason:{evaluation.reason}|" +
            $"Zone:{zoneID}|" +
            $"Movement:{movementState}|" +
            $"Disguise:{disguiseSet}|" +
            $"DisguiseComplete:{disguiseCompletenessPercent}";
    }

    private void CallNearbyGuardsForHelp(string reason, float urgency)
    {
        Vector3 alertPosition = lastKnownPlayerPosition;

        if (player != null && CanCurrentlySeePlayer())
        {
            alertPosition = player.position;
        }
        else if (alertPosition == Vector3.zero)
        {
            alertPosition = transform.position;
        }

        CallNearbyGuardsForHelpAtPosition(alertPosition, reason, urgency);
    }

    private void CallNearbyGuardsForHelpAtPosition(Vector3 alertPosition, string reason, float urgency)
    {
        if (!useGuardCoordination)
        {
            return;
        }

        if (!canCallForHelp)
        {
            return;
        }

        if (helpCallRadius <= 0f)
        {
            return;
        }

        if (Time.time < lastHelpCallTime + helpCallCooldown)
        {
            return;
        }

        lastHelpCallTime = Time.time;

        Vector3 safeAlertPosition = GetSafeNavMeshPosition(alertPosition, 3f);

        int recipients = 0;

        foreach (AdvancedGuardAIController guard in AllGuards)
        {
            if (guard == null)
            {
                continue;
            }

            if (guard == this)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, guard.transform.position);

            if (distance > helpCallRadius)
            {
                continue;
            }

            bool accepted = guard.ReceiveExternalGuardAlert(
                this,
                safeAlertPosition,
                urgency,
                reason
            );

            if (accepted)
            {
                recipients++;
            }
        }

        lastHelpCallRecipientCount = recipients;

        lastHelpCallInspectorMessage =
            $"CALLED FOR HELP\n" +
            $"Reason: {reason}\n" +
            $"Recipients: {recipients}\n" +
            $"Radius: {helpCallRadius:0.0}\n" +
            $"Position: {safeAlertPosition}";

        StartCallForHelpFeedback();

        if (showGuardCoordinationDebug)
        {
            Debug.Log(
                $"[{gameObject.name}] Called for help. " +
                $"Reason: {reason}, Recipients: {recipients}, Position: {safeAlertPosition}"
            );
        }
    }

    public bool ReceiveExternalGuardAlert(
        AdvancedGuardAIController caller,
        Vector3 alertPosition,
        float urgency,
        string reason
    )
    {
        if (!useGuardCoordination)
        {
            return false;
        }

        if (!canRespondToHelp)
        {
            return false;
        }

        if (caller == null || caller == this)
        {
            return false;
        }

        if (currentState == GuardState.CapturePlayer)
        {
            return false;
        }

        if (ignoreExternalAlertsWhileChasing && currentState == GuardState.Chase)
        {
            return false;
        }

        if (questioningResponder != null && questioningResponder.IsQuestioning)
        {
            return false;
        }

        if (IsInFastTalkCooldown() && ignorePlayerDuringFastTalkCooldown)
        {
            return false;
        }

        Vector3 safeAlertPosition = GetSafeNavMeshPosition(alertPosition, 3f);

        pendingExternalAlertPosition = safeAlertPosition;
        pendingExternalAlertReason = reason;
        pendingExternalAlertUrgency = Mathf.Max(0.1f, urgency);
        externalAlertDelayTimer = Mathf.Max(0f, externalAlertInvestigationDelay);
        isRespondingToExternalAlert = true;

        currentSuspicionReason = GuardSuspicionReason.InvestigatedSound;
        currentSuspicionReasonText = $"responding to another guard's call: {reason}";

        suspicion += externalAlertSuspicionBoost * pendingExternalAlertUrgency;
        suspicion = Mathf.Clamp(suspicion, 0f, 100f);

        if (externalAlertCancelsPatrolWait && agent != null)
        {
            agent.isStopped = false;
        }

        lastReceivedHelpInspectorMessage =
            $"RECEIVED HELP CALL\n" +
            $"From: {caller.gameObject.name}\n" +
            $"Reason: {reason}\n" +
            $"Urgency: {pendingExternalAlertUrgency:0.00}\n" +
            $"Position: {safeAlertPosition}";

        StartReceivedHelpFeedback();

        if (showGuardCoordinationDebug)
        {
            Debug.Log(
                $"[{gameObject.name}] Received guard alert from {caller.name}. " +
                $"Reason: {reason}, Position: {safeAlertPosition}, Urgency: {pendingExternalAlertUrgency:0.00}"
            );
        }

        return true;
    }

    private void UpdateExternalGuardAlertResponse()
    {
        if (!isRespondingToExternalAlert)
        {
            isCurrentlyRespondingToHelp = false;
            return;
        }

        isCurrentlyRespondingToHelp = true;

        if (currentState == GuardState.Chase ||
            currentState == GuardState.CapturePlayer)
        {
            isRespondingToExternalAlert = false;
            isCurrentlyRespondingToHelp = false;
            return;
        }

        externalAlertDelayTimer -= Time.deltaTime;

        if (externalAlertDelayTimer > 0f)
        {
            FaceTarget(pendingExternalAlertPosition);
            return;
        }

        isRespondingToExternalAlert = false;
        isCurrentlyRespondingToHelp = false;

        lastKnownPlayerPosition = pendingExternalAlertPosition;
        soundPosition = pendingExternalAlertPosition;

        if (respondingGuardCanOnlyChaseIfHeSeesPlayer)
        {
            if (CanCurrentlySeePlayer() && suspicion >= chaseThreshold && CanEscalateToChase())
            {
                EnterChase();
                return;
            }

            EnterInvestigateSound();
            return;
        }

        if (suspicion >= chaseThreshold && CanEscalateToChase())
        {
            EnterChase();
        }
        else
        {
            EnterInvestigateSound();
        }
    }

    private Vector3 GetSafeNavMeshPosition(Vector3 requestedPosition, float sampleDistance)
    {
        if (NavMesh.SamplePosition(
                requestedPosition,
                out NavMeshHit hit,
                sampleDistance,
                NavMesh.AllAreas))
        {
            return hit.position;
        }

        return requestedPosition;
    }

    private void StartCallForHelpFeedback()
    {
        isCurrentlyCallingForHelp = true;
        callForHelpIndicatorTimer = callForHelpIndicatorDuration;

        if (callForHelpIndicator != null)
        {
            callForHelpIndicator.SetActive(true);
        }
    }

    private void StartReceivedHelpFeedback()
    {
        isCurrentlyRespondingToHelp = true;
        receivedHelpIndicatorTimer = receivedHelpIndicatorDuration;

        if (receivedHelpIndicator != null)
        {
            receivedHelpIndicator.SetActive(true);
        }
    }

    private void UpdateCallForHelpFeedback()
    {
        if (callForHelpIndicatorTimer > 0f)
        {
            callForHelpIndicatorTimer -= Time.deltaTime;

            if (callForHelpIndicatorTimer <= 0f)
            {
                callForHelpIndicatorTimer = 0f;
                isCurrentlyCallingForHelp = false;

                if (callForHelpIndicator != null)
                {
                    callForHelpIndicator.SetActive(false);
                }
            }
        }
        else
        {
            isCurrentlyCallingForHelp = false;
        }

        if (receivedHelpIndicatorTimer > 0f)
        {
            receivedHelpIndicatorTimer -= Time.deltaTime;

            if (receivedHelpIndicatorTimer <= 0f)
            {
                receivedHelpIndicatorTimer = 0f;

                if (!isRespondingToExternalAlert)
                {
                    isCurrentlyRespondingToHelp = false;
                }

                if (receivedHelpIndicator != null)
                {
                    receivedHelpIndicator.SetActive(false);
                }
            }
        }
    }

    private void ClearGuardCoordinationRuntimeState()
    {
        isRespondingToExternalAlert = false;
        externalAlertDelayTimer = 0f;
        pendingExternalAlertPosition = Vector3.zero;
        pendingExternalAlertReason = "";
        pendingExternalAlertUrgency = 0f;
        lastHelpCallTime = -999f;

        isCurrentlyCallingForHelp = false;
        isCurrentlyRespondingToHelp = false;
        callForHelpIndicatorTimer = 0f;
        receivedHelpIndicatorTimer = 0f;
        lastHelpCallRecipientCount = 0;
        lastHelpCallInspectorMessage = "";
        lastReceivedHelpInspectorMessage = "";

        if (callForHelpIndicator != null)
        {
            callForHelpIndicator.SetActive(false);
        }

        if (receivedHelpIndicator != null)
        {
            receivedHelpIndicator.SetActive(false);
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

    private void DebugSuspicionReasonChange()
    {
        if (!printSuspicionReasonChanges)
        {
            return;
        }

        if (previousSuspicionReason != currentSuspicionReason ||
            previousSuspicionReasonText != currentSuspicionReasonText)
        {
            if (currentSuspicionReason != GuardSuspicionReason.None)
            {
                Debug.Log(
                    $"[{gameObject.name}] Suspicion reason: {currentSuspicionReason} / {currentSuspicionReasonText}"
                );
            }

            previousSuspicionReason = currentSuspicionReason;
            previousSuspicionReasonText = currentSuspicionReasonText;
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(lastKnownPlayerPosition, searchRadius);

        if (useGuardCoordination)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, helpCallRadius);

            if (pendingExternalAlertPosition != Vector3.zero)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawWireSphere(pendingExternalAlertPosition, 0.6f);
                Gizmos.DrawLine(transform.position, pendingExternalAlertPosition);
            }
        }

        if (guardMustBeCloseToQuestion)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, questioningStartDistance);
        }
    }
}