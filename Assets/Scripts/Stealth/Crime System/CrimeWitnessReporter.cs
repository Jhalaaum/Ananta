using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class CrimeWitnessReporter : MonoBehaviour
{
    [Header("Witness")]
    public bool canWitnessCrimes = true;
    public bool autoSetCivilianOrGuard = true;

    public bool isCivilian = true;
    public bool isGuard = false;

    public float witnessDistance = 10f;
    [Range(1f, 180f)] public float witnessAngle = 100f;
    public float eyeHeight = 1.5f;

    [Header("Line Of Sight")]
    public bool requireLineOfSight = true;
    public LayerMask lineOfSightMask = ~0;

    [Header("Reporting Chance")]
    public bool useRandomReportingChance = true;
    public float minimumSuspicionToReport = 20f;
    public float alwaysReportAtSuspicion = 80f;

    [Range(0f, 1f)] public float baseReportChance = 0.35f;
    [Range(0f, 1f)] public float suspicionChanceWeight = 0.45f;
    [Range(0f, 1f)] public float zoneAlertChanceWeight = 0.15f;
    [Range(0f, 1f)] public float immediateChaseBonus = 0.25f;

    [Range(0f, 1f)] public float civilianCourage = 0.65f;
    [Range(0f, 1f)] public float civilianFear = 0.15f;

    [Header("Smart Guard Search")]
    public float nearestGuardSearchRadius = 28f;

    [Tooltip("If true, civilian goes to a visible guard directly. If not visible, they use patrol points.")]
    public bool goDirectlyToVisibleGuard = true;

    [Tooltip("If the guard is not visible, civilian walks to smart patrol-route meet points.")]
    public bool useGuardPatrolPointsAsMeetPoints = true;

    public float guardSightDistance = 18f;
    [Range(1f, 180f)] public float guardSightAngle = 125f;
    public float meetPointRefreshInterval = 0.5f;

    [Header("Reporting Movement")]
    public NavMeshAgent agent;
    public float civilianWalkSpeed = 2.4f;
    public float reportStartDelay = 0.45f;
    public float reportHandoffDistance = 2.25f;
    public float maxTimeToFindGuard = 18f;
    public float waypointReachDistance = 1.2f;

    [Header("Conversation And Lead")]
    public float conversationDuration = 1.25f;
    public float leadToCrimeStoppingDistance = 1.5f;
    public float maxLeadToCrimeTime = 18f;
    public float turnSpeed = 540f;

    [Header("After Reporting")]
    public bool returnToStartAfterReporting = true;
    public float returnStoppingDistance = 0.45f;

    [Header("Runtime Debug")]
    public bool hasPendingReport;
    public bool isGoingToFindGuard;
    public bool isTalkingToGuard;
    public bool isLeadingGuardToCrime;
    public bool isReturningToStart;

    public float currentReportChance;
    public float currentReportRoll;

    [TextArea(3, 8)] public string witnessDebugStatus;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private AdvancedGuardAIController ownGuardAI;
    private AdvancedGuardAIController targetGuard;

    private CrimeReport pendingReport;
    private Vector3 startPosition;
    private Vector3 currentMeetPoint;
    private Vector3 crimeSpot;

    private Coroutine reportRoutine;
    private float meetPointRefreshTimer;

    private void Awake()
    {
        ownGuardAI = GetComponent<AdvancedGuardAIController>();

        if (autoSetCivilianOrGuard)
        {
            if (ownGuardAI != null)
            {
                isGuard = true;
                isCivilian = false;
            }
            else
            {
                isCivilian = true;
                isGuard = false;
            }
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        startPosition = transform.position;
    }

    private void OnEnable()
    {
        CrimeManager.OnCrimeCommitted += HandleCrimeCommitted;
    }

    private void OnDisable()
    {
        CrimeManager.OnCrimeCommitted -= HandleCrimeCommitted;
    }

    private void Update()
    {
        UpdateReturnToStart();
    }

    private void HandleCrimeCommitted(CrimeReport report)
    {
        if (!canWitnessCrimes || report == null)
        {
            return;
        }

        if (isCivilian && !report.canBeReportedByCivilians)
        {
            return;
        }

        if (!CanWitnessCrime(report))
        {
            return;
        }

        CrimeReport witnessedReport = CopyReport(report);
        witnessedReport.reporter = transform;

        witnessDebugStatus =
            $"Witnessed crime: {witnessedReport.crimeType}\n" +
            $"Description: {witnessedReport.crimeDescription}\n" +
            $"Position: {witnessedReport.crimePosition}";

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Witnessed crime: {witnessedReport.crimeType}");
        }

        if (isGuard && ownGuardAI != null)
        {
            ownGuardAI.ReceiveCrimeReport(witnessedReport);
            return;
        }

        if (isCivilian)
        {
            TryStartCivilianReport(witnessedReport);
        }
    }

    private void TryStartCivilianReport(CrimeReport report)
    {
        if (hasPendingReport || reportRoutine != null)
        {
            return;
        }

        if (!ShouldCivilianReport(report))
        {
            witnessDebugStatus +=
                $"\nDecision: NOT reporting\n" +
                $"Chance: {currentReportChance:0.00}\n" +
                $"Roll: {currentReportRoll:0.00}";

            if (showDebugLogs)
            {
                Debug.Log(
                    $"[{gameObject.name}] Chose not to report. " +
                    $"Chance: {currentReportChance:0.00}, Roll: {currentReportRoll:0.00}"
                );
            }

            return;
        }

        pendingReport = report;
        crimeSpot = report.crimePosition;
        hasPendingReport = true;

        reportRoutine = StartCoroutine(CivilianReportRoutine());
    }

    private bool ShouldCivilianReport(CrimeReport report)
    {
        if (report.suspicionAmount < minimumSuspicionToReport)
        {
            currentReportChance = 0f;
            currentReportRoll = 1f;
            return false;
        }

        if (report.suspicionAmount >= alwaysReportAtSuspicion)
        {
            currentReportChance = 1f;
            currentReportRoll = 0f;
            return true;
        }

        if (!useRandomReportingChance)
        {
            currentReportChance = 1f;
            currentReportRoll = 0f;
            return true;
        }

        float suspicionFactor = Mathf.Clamp01(report.suspicionAmount / 100f);
        float zoneAlertFactor = Mathf.Clamp01(report.zoneAlertAmount / 50f);

        currentReportChance =
            baseReportChance +
            suspicionFactor * suspicionChanceWeight +
            zoneAlertFactor * zoneAlertChanceWeight +
            civilianCourage * 0.2f -
            civilianFear * 0.25f;

        if (report.immediateChaseIfSeen)
        {
            currentReportChance += immediateChaseBonus;
        }

        currentReportChance = Mathf.Clamp01(currentReportChance);
        currentReportRoll = Random.value;

        return currentReportRoll <= currentReportChance;
    }

    private IEnumerator CivilianReportRoutine()
    {
        yield return new WaitForSeconds(reportStartDelay);

        targetGuard = FindBestGuard(out currentMeetPoint);

        if (targetGuard == null)
        {
            witnessDebugStatus += "\nWanted to report, but no guard was found.";
            ClearReportRuntime();
            yield break;
        }

        isGoingToFindGuard = true;
        isTalkingToGuard = false;
        isLeadingGuardToCrime = false;
        isReturningToStart = false;

        if (agent == null || !agent.isOnNavMesh)
        {
            DeliverReportImmediatelyAsFallback();
            ClearReportRuntime();
            yield break;
        }

        agent.isStopped = false;
        agent.speed = civilianWalkSpeed;
        agent.stoppingDistance = reportHandoffDistance;

        float findGuardTimer = maxTimeToFindGuard;
        meetPointRefreshTimer = 0f;

        while (findGuardTimer > 0f)
        {
            if (targetGuard == null || pendingReport == null)
            {
                ClearReportRuntime();
                yield break;
            }

            findGuardTimer -= Time.deltaTime;
            meetPointRefreshTimer -= Time.deltaTime;

            bool canSeeGuard = CanSeeGuard(targetGuard);

            if (canSeeGuard && goDirectlyToVisibleGuard)
            {
                currentMeetPoint = targetGuard.transform.position;
            }
            else if (meetPointRefreshTimer <= 0f)
            {
                meetPointRefreshTimer = meetPointRefreshInterval;
                currentMeetPoint = GetSmartMeetPointForGuard(targetGuard);
            }

            TrySetAgentDestination(currentMeetPoint);

            float distanceToGuard = Vector3.Distance(
                transform.position,
                targetGuard.transform.position
            );

            witnessDebugStatus =
                $"Reporting crime\n" +
                $"State: Finding guard\n" +
                $"Target Guard: {targetGuard.gameObject.name}\n" +
                $"Can See Guard: {canSeeGuard}\n" +
                $"Distance To Guard: {distanceToGuard:0.00}\n" +
                $"Meet Point: {currentMeetPoint}\n" +
                $"Chance: {currentReportChance:0.00}, Roll: {currentReportRoll:0.00}";

            if (distanceToGuard <= reportHandoffDistance)
            {
                yield return ConversationThenLeadGuardRoutine();
                yield break;
            }

            yield return null;
        }

        witnessDebugStatus = "Civilian failed to find a guard in time.";
        ClearReportRuntime();
    }

    private IEnumerator ConversationThenLeadGuardRoutine()
    {
        isGoingToFindGuard = false;
        isTalkingToGuard = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        bool guardAcceptedLead = false;

        if (targetGuard != null && pendingReport != null)
        {
            guardAcceptedLead = targetGuard.AcceptCivilianCrimeLead(
                pendingReport,
                transform,
                crimeSpot,
                conversationDuration
            );
        }

        float timer = Mathf.Max(0f, conversationDuration);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            if (targetGuard != null)
            {
                FacePosition(targetGuard.transform.position);
            }

            witnessDebugStatus =
                $"Reporting crime\n" +
                $"State: Talking to guard\n" +
                $"Guard: {(targetGuard != null ? targetGuard.gameObject.name : "None")}\n" +
                $"Conversation Time Left: {timer:0.00}";

            yield return null;
        }

        isTalkingToGuard = false;

        if (!guardAcceptedLead)
        {
            DeliverReportImmediatelyAsFallback();
            ClearReportRuntime();
            yield break;
        }

        yield return LeadGuardToCrimeSpotRoutine();
    }

    private IEnumerator LeadGuardToCrimeSpotRoutine()
    {
        isLeadingGuardToCrime = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = civilianWalkSpeed;
            agent.stoppingDistance = leadToCrimeStoppingDistance;
        }

        float timer = maxLeadToCrimeTime;

        while (timer > 0f)
        {
            timer -= Time.deltaTime;

            TrySetAgentDestination(crimeSpot);

            float distanceToCrime = Vector3.Distance(transform.position, crimeSpot);

            witnessDebugStatus =
                $"Reporting crime\n" +
                $"State: Leading guard to crime spot\n" +
                $"Guard: {(targetGuard != null ? targetGuard.gameObject.name : "None")}\n" +
                $"Distance To Crime: {distanceToCrime:0.00}";

            if (distanceToCrime <= leadToCrimeStoppingDistance)
            {
                break;
            }

            yield return null;
        }

        isLeadingGuardToCrime = false;

        if (returnToStartAfterReporting)
        {
            StartReturnToStart();
        }
        else
        {
            ClearReportRuntime();
        }
    }

    private void DeliverReportImmediatelyAsFallback()
    {
        if (targetGuard == null || pendingReport == null)
        {
            return;
        }

        pendingReport.reporter = transform;
        targetGuard.ReceiveCrimeReport(pendingReport);

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Fallback: delivered report immediately to {targetGuard.gameObject.name}");
        }
    }

    private AdvancedGuardAIController FindBestGuard(out Vector3 meetPoint)
    {
        meetPoint = transform.position;

        AdvancedGuardAIController[] guards = FindObjectsOfType<AdvancedGuardAIController>();

        AdvancedGuardAIController bestGuard = null;
        float bestScore = Mathf.Infinity;
        Vector3 bestMeetPoint = transform.position;

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            if (guard.currentState == AdvancedGuardAIController.GuardState.CapturePlayer)
            {
                continue;
            }

            float straightDistance = Vector3.Distance(transform.position, guard.transform.position);

            if (straightDistance > nearestGuardSearchRadius)
            {
                continue;
            }

            Vector3 candidateMeetPoint;

            bool canSeeGuard = CanSeeGuard(guard);

            if (canSeeGuard && goDirectlyToVisibleGuard)
            {
                candidateMeetPoint = guard.transform.position;
            }
            else
            {
                candidateMeetPoint = GetSmartMeetPointForGuard(guard);
            }

            float pathDistance = GetApproxPathDistance(transform.position, candidateMeetPoint);
            float guardDistanceFromMeetPoint = Vector3.Distance(candidateMeetPoint, guard.transform.position);

            float score = pathDistance + guardDistanceFromMeetPoint * 0.35f;

            if (canSeeGuard)
            {
                score -= 8f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                bestGuard = guard;
                bestMeetPoint = candidateMeetPoint;
            }
        }

        meetPoint = bestMeetPoint;
        return bestGuard;
    }

    private Vector3 GetSmartMeetPointForGuard(AdvancedGuardAIController guard)
    {
        if (guard == null)
        {
            return transform.position;
        }

        if (CanSeeGuard(guard) && goDirectlyToVisibleGuard)
        {
            return guard.transform.position;
        }

        if (!useGuardPatrolPointsAsMeetPoints || guard.patrolRouteParent == null)
        {
            return guard.transform.position;
        }

        Vector3 bestPoint = guard.transform.position;
        float bestScore = Mathf.Infinity;

        for (int i = 0; i < guard.patrolRouteParent.childCount; i++)
        {
            Transform patrolPoint = guard.patrolRouteParent.GetChild(i);

            if (patrolPoint == null)
            {
                continue;
            }

            float pathDistance = GetApproxPathDistance(transform.position, patrolPoint.position);
            float distanceFromGuard = Vector3.Distance(patrolPoint.position, guard.transform.position);

            float score = pathDistance + distanceFromGuard * 0.35f;

            if (score < bestScore)
            {
                bestScore = score;
                bestPoint = patrolPoint.position;
            }
        }

        return bestPoint;
    }

    private bool CanSeeGuard(AdvancedGuardAIController guard)
    {
        if (guard == null)
        {
            return false;
        }

        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 targetPosition = guard.transform.position + Vector3.up * eyeHeight;

        Vector3 directionToGuard = targetPosition - eyePosition;
        float distance = directionToGuard.magnitude;

        if (distance > guardSightDistance)
        {
            return false;
        }

        if (distance <= 0.01f)
        {
            return true;
        }

        Vector3 direction = directionToGuard.normalized;
        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > guardSightAngle * 0.5f)
        {
            return false;
        }

        if (Physics.Raycast(
                eyePosition,
                direction,
                out RaycastHit hit,
                distance,
                lineOfSightMask,
                QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null)
            {
                return true;
            }

            if (hit.collider.transform == guard.transform ||
                hit.collider.transform.IsChildOf(guard.transform))
            {
                return true;
            }

            return false;
        }

        return true;
    }

    private bool CanWitnessCrime(CrimeReport report)
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 crimePosition = report.crimePosition;

        Vector3 directionToCrime = crimePosition - eyePosition;
        float distance = directionToCrime.magnitude;

        if (distance > witnessDistance)
        {
            return false;
        }

        if (distance <= 0.01f)
        {
            return true;
        }

        Vector3 direction = directionToCrime.normalized;
        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > witnessAngle * 0.5f)
        {
            return false;
        }

        if (!requireLineOfSight)
        {
            return true;
        }

        if (Physics.Raycast(
                eyePosition,
                direction,
                out RaycastHit hit,
                distance,
                lineOfSightMask,
                QueryTriggerInteraction.Ignore))
        {
            if (hit.collider == null)
            {
                return true;
            }

            if (report.crimeObject != null)
            {
                if (hit.collider.gameObject == report.crimeObject ||
                    hit.collider.transform.IsChildOf(report.crimeObject.transform))
                {
                    return true;
                }
            }

            if (report.suspect != null)
            {
                if (hit.collider.transform == report.suspect ||
                    hit.collider.transform.IsChildOf(report.suspect))
                {
                    return true;
                }
            }

            return false;
        }

        return true;
    }

    private bool TrySetAgentDestination(Vector3 destination)
    {
        if (agent == null || !agent.isOnNavMesh)
        {
            return false;
        }

        if (!NavMesh.SamplePosition(
                destination,
                out NavMeshHit hit,
                2.5f,
                NavMesh.AllAreas))
        {
            return false;
        }

        agent.SetDestination(hit.position);
        return true;
    }

    private float GetApproxPathDistance(Vector3 from, Vector3 to)
    {
        if (!NavMesh.SamplePosition(from, out NavMeshHit fromHit, 2.5f, NavMesh.AllAreas))
        {
            return Vector3.Distance(from, to);
        }

        if (!NavMesh.SamplePosition(to, out NavMeshHit toHit, 2.5f, NavMesh.AllAreas))
        {
            return Vector3.Distance(from, to);
        }

        NavMeshPath path = new NavMeshPath();

        bool pathFound = NavMesh.CalculatePath(
            fromHit.position,
            toHit.position,
            NavMesh.AllAreas,
            path
        );

        if (!pathFound || path.status != NavMeshPathStatus.PathComplete)
        {
            return Vector3.Distance(from, to) * 2f;
        }

        float distance = 0f;

        for (int i = 1; i < path.corners.Length; i++)
        {
            distance += Vector3.Distance(path.corners[i - 1], path.corners[i]);
        }

        return distance;
    }

    private void StartReturnToStart()
    {
        ClearReportRuntimeButKeepReturn();

        isReturningToStart = true;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
            agent.speed = civilianWalkSpeed;
            agent.stoppingDistance = returnStoppingDistance;
            TrySetAgentDestination(startPosition);
        }
    }

    private void UpdateReturnToStart()
    {
        if (!isReturningToStart)
        {
            return;
        }

        if (agent == null || !agent.isOnNavMesh)
        {
            isReturningToStart = false;
            return;
        }

        if (agent.pathPending)
        {
            return;
        }

        if (agent.remainingDistance <= returnStoppingDistance)
        {
            isReturningToStart = false;
            agent.isStopped = true;
            witnessDebugStatus = "Returned after reporting.";
        }
    }

    private void FacePosition(Vector3 targetPosition)
    {
        Vector3 direction = targetPosition - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction.normalized, Vector3.up);

        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            turnSpeed * Time.deltaTime
        );
    }

    private CrimeReport CopyReport(CrimeReport source)
    {
        return new CrimeReport
        {
            crimeType = source.crimeType,
            suspect = source.suspect,
            reporter = source.reporter,
            crimePosition = source.crimePosition,
            crimeDescription = source.crimeDescription,
            suspicionAmount = source.suspicionAmount,
            zoneAlertAmount = source.zoneAlertAmount,
            immediateChaseIfSeen = source.immediateChaseIfSeen,
            canBeReportedByCivilians = source.canBeReportedByCivilians,
            createsContraband = source.createsContraband,
            crimeObject = source.crimeObject
        };
    }

    private void ClearReportRuntime()
    {
        hasPendingReport = false;
        isGoingToFindGuard = false;
        isTalkingToGuard = false;
        isLeadingGuardToCrime = false;

        pendingReport = null;
        targetGuard = null;
        reportRoutine = null;

        if (agent != null && agent.isOnNavMesh)
        {
            agent.isStopped = false;
        }
    }

    private void ClearReportRuntimeButKeepReturn()
    {
        hasPendingReport = false;
        isGoingToFindGuard = false;
        isTalkingToGuard = false;
        isLeadingGuardToCrime = false;

        pendingReport = null;
        targetGuard = null;
        reportRoutine = null;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isCivilian ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, witnessDistance);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, nearestGuardSearchRadius);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(currentMeetPoint, 0.35f);

        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(crimeSpot, 0.35f);
    }
}