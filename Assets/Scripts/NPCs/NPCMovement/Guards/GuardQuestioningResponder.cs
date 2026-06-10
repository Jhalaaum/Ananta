using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GuardQuestioningResponder : MonoBehaviour
{
    [Header("Questioning")]
    public GuardQuestioningProfile questioningProfile;

    [Header("Reason Line")]
    public bool showReasonLineBeforeConversation = true;
    public float minimumReasonLineDuration = 2.2f;
    public bool allowSkipReasonLineAfterMinimumTime = true;
    public Key skipReasonLineKey = Key.Space;
    public string fallbackReasonLine = "Stop. Explain yourself.";

    [Header("Fast Talk Cooldown")]
    public float passedFastTalkCooldown = 7f;
    public float warnedFastTalkCooldown = 5f;
    public bool clearStealthTogglesAfterSuccessfulFastTalk = true;

    [Header("Zone Alert Amounts")]
    public float passedAlertIncrease = 3f;
    public float warnedAlertIncrease = 12f;
    public float failedAlertIncrease = 25f;
    public float chaseAlertIncrease = 35f;
    public float caughtAlertIncrease = 45f;

    [Header("References")]
    public DialogueManager dialogueManager;
    public DialogueUI dialogueUI;
    public Transform player;
    public NavMeshAgent agent;
    public AdvancedGuardAIController guardAI;

    [Header("Runtime State")]
    public bool IsQuestioning { get; private set; }
    public bool IsResolvingOutcome { get; private set; }
    public float localQuestionCooldownTimer;
    public float ignorePlayerTimer;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private float lastQuestioningTime = -999f;
    private Coroutine questioningRoutine;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (guardAI == null)
        {
            guardAI = GetComponent<AdvancedGuardAIController>();
        }

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }
    }

    private void Update()
    {
        if (ignorePlayerTimer > 0f)
        {
            ignorePlayerTimer -= Time.deltaTime;
        }

        if (localQuestionCooldownTimer > 0f)
        {
            localQuestionCooldownTimer -= Time.deltaTime;
        }
    }

    public bool CanQuestion()
    {
        if (questioningProfile == null)
        {
            return false;
        }

        if (questioningProfile.questioningConversation == null)
        {
            return false;
        }

        if (IsQuestioning || IsResolvingOutcome)
        {
            return false;
        }

        if (ignorePlayerTimer > 0f)
        {
            return false;
        }

        if (localQuestionCooldownTimer > 0f)
        {
            return false;
        }

        if (guardAI != null && guardAI.IsInFastTalkCooldown())
        {
            return false;
        }

        if (Time.time < lastQuestioningTime + questioningProfile.minTimeBetweenQuestioning)
        {
            return false;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return false;
        }

        return true;
    }

    public void StartQuestioning()
    {
        StartQuestioning("");
    }

    public void StartQuestioning(string reasonLine)
    {
        if (!CanQuestion())
        {
            return;
        }

        if (questioningRoutine != null)
        {
            StopCoroutine(questioningRoutine);
        }

        questioningRoutine = StartCoroutine(QuestioningRoutine(reasonLine));
    }

    private IEnumerator QuestioningRoutine(string reasonLine)
    {
        IsQuestioning = true;
        IsResolvingOutcome = false;
        lastQuestioningTime = Time.time;

        Log("Questioning started.");

        if (agent != null)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }

        FacePlayer();

        if (PlayerControlLock.Instance != null)
        {
            PlayerControlLock.Instance.LockPlayer();
        }

        if (GuardQuestioningManager.Instance != null)
        {
            GuardQuestioningManager.Instance.BeginQuestioning();
        }

        if (showReasonLineBeforeConversation && dialogueUI != null)
        {
            yield return ShowReasonLineRoutine(reasonLine);
        }

        if (dialogueManager != null && questioningProfile.questioningConversation != null)
        {
            dialogueManager.StartDialogue(questioningProfile.questioningConversation, this, player);
        }

        while (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            FacePlayer();
            yield return null;
        }

        IsResolvingOutcome = true;

        GuardQuestioningOutcome outcome = GuardQuestioningOutcome.Failed;

        if (GuardQuestioningManager.Instance != null)
        {
            outcome = GuardQuestioningManager.Instance.EndQuestioningAndResolveOutcome();
        }

        ReportQuestioningOutcomeToMemory(outcome);
        ReportOutcomeToZoneAlert(outcome);
        ApplyOutcome(outcome);

        if (PlayerControlLock.Instance != null)
        {
            PlayerControlLock.Instance.UnlockPlayer();
        }

        IsQuestioning = false;
        IsResolvingOutcome = false;
        questioningRoutine = null;

        Log("Questioning finished.");
    }

    private IEnumerator ShowReasonLineRoutine(string reasonLine)
    {
        string finalReasonLine = !string.IsNullOrWhiteSpace(reasonLine)
            ? reasonLine
            : fallbackReasonLine;

        dialogueUI.ForceHidePrompt();
        dialogueUI.ShowLine("Guard", finalReasonLine);

        float timer = 0f;

        while (timer < minimumReasonLineDuration)
        {
            FacePlayer();
            timer += Time.unscaledDeltaTime;
            yield return null;
        }

        if (allowSkipReasonLineAfterMinimumTime)
        {
            bool continuePressed = false;

            while (!continuePressed)
            {
                FacePlayer();

                if (Keyboard.current == null)
                {
                    break;
                }

                if (Keyboard.current[skipReasonLineKey].wasPressedThisFrame ||
                    Keyboard.current.eKey.wasPressedThisFrame ||
                    Keyboard.current.enterKey.wasPressedThisFrame)
                {
                    continuePressed = true;
                }

                yield return null;
            }
        }

        dialogueUI.HideDialogue();
        yield return null;
    }

    private void ApplyOutcome(GuardQuestioningOutcome outcome)
    {
        Log("Applying outcome: " + outcome);

        switch (outcome)
        {
            case GuardQuestioningOutcome.Passed:
                ApplyPassedOutcome();
                break;

            case GuardQuestioningOutcome.Warned:
                ApplyWarnedOutcome();
                break;

            case GuardQuestioningOutcome.Chase:
                ApplyChaseOutcome();
                break;

            case GuardQuestioningOutcome.Caught:
                ApplyCaughtOutcome();
                break;

            case GuardQuestioningOutcome.Failed:
            case GuardQuestioningOutcome.None:
            default:
                ApplyFailedOutcome();
                break;
        }
    }

    private void ApplyPassedOutcome()
    {
        float cooldown = Mathf.Max(
            passedFastTalkCooldown,
            questioningProfile.ignorePlayerDurationAfterPassed
        );

        ignorePlayerTimer = cooldown;
        localQuestionCooldownTimer = cooldown;

        if (guardAI != null)
        {
            guardAI.SetSuspicion(questioningProfile.suspicionAfterPassed);
            guardAI.StartFastTalkCooldown(cooldown, clearStealthTogglesAfterSuccessfulFastTalk);
            guardAI.ForceReturnToPatrol();
        }
    }

    private void ApplyWarnedOutcome()
    {
        float cooldown = Mathf.Max(
            warnedFastTalkCooldown,
            questioningProfile.ignorePlayerDurationAfterWarning
        );

        ignorePlayerTimer = cooldown;
        localQuestionCooldownTimer = cooldown;

        if (guardAI != null)
        {
            guardAI.SetSuspicion(questioningProfile.suspicionAfterWarning);
            guardAI.StartFastTalkCooldown(cooldown, clearStealthTogglesAfterSuccessfulFastTalk);
            guardAI.ForceReturnToPatrol();
        }
    }

    private void ApplyFailedOutcome()
    {
        ignorePlayerTimer = 0f;
        localQuestionCooldownTimer = 0f;

        if (questioningProfile.chaseOnFailed)
        {
            ApplyChaseOutcome();
            return;
        }

        if (guardAI != null)
        {
            guardAI.ForceReturnToPatrol();
        }
    }

    private void ApplyChaseOutcome()
    {
        ignorePlayerTimer = 0f;
        localQuestionCooldownTimer = 0f;

        if (guardAI != null)
        {
            guardAI.ForceChasePlayer();
        }
    }

    private void ApplyCaughtOutcome()
    {
        ignorePlayerTimer = 0f;
        localQuestionCooldownTimer = 0f;

        if (questioningProfile.useCaughtSystemOnCaught)
        {
            if (guardAI != null)
            {
                guardAI.ForceCatchPlayer();
            }

            return;
        }

        ApplyChaseOutcome();
    }

    private string GetCurrentZoneID()
    {
        if (PlayerZoneTracker.Instance != null &&
            PlayerZoneTracker.Instance.GetCurrentZone() != null &&
            !string.IsNullOrWhiteSpace(PlayerZoneTracker.Instance.GetCurrentZone().zoneID))
        {
            return PlayerZoneTracker.Instance.GetCurrentZone().zoneID.Trim();
        }

        return "public_city";
    }

    private void ReportOutcomeToZoneAlert(GuardQuestioningOutcome outcome)
    {
        if (ZoneAlertManager.Instance == null)
        {
            return;
        }

        string zoneID = GetCurrentZoneID();

        switch (outcome)
        {
            case GuardQuestioningOutcome.Passed:
                ZoneAlertManager.Instance.AddAlert(
                    zoneID,
                    passedAlertIncrease,
                    "Fast Talk passed"
                );
                break;

            case GuardQuestioningOutcome.Warned:
                ZoneAlertManager.Instance.AddAlert(
                    zoneID,
                    warnedAlertIncrease,
                    "Guard issued warning"
                );
                break;

            case GuardQuestioningOutcome.Failed:
                ZoneAlertManager.Instance.AddAlert(
                    zoneID,
                    failedAlertIncrease,
                    "Player failed questioning"
                );
                break;

            case GuardQuestioningOutcome.Chase:
                ZoneAlertManager.Instance.AddAlert(
                    zoneID,
                    chaseAlertIncrease,
                    "Guard began chase"
                );
                break;

            case GuardQuestioningOutcome.Caught:
                ZoneAlertManager.Instance.AddAlert(
                    zoneID,
                    caughtAlertIncrease,
                    "Player was caught"
                );
                break;
        }
    }

    private void ReportQuestioningOutcomeToMemory(GuardQuestioningOutcome outcome)
    {
        if (GuardMemoryManager.Instance == null)
        {
            return;
        }

        string zoneID = GetCurrentZoneID();

        switch (outcome)
        {
            case GuardQuestioningOutcome.Passed:
                return;

            case GuardQuestioningOutcome.Warned:
                GuardMemoryManager.Instance.ReportSuspicion(
                    zoneID,
                    GuardSuspicionReason.IgnoredWarning,
                    "was warned by guard",
                    10f,
                    false
                );
                break;

            case GuardQuestioningOutcome.Failed:
            case GuardQuestioningOutcome.Chase:
            case GuardQuestioningOutcome.Caught:
                GuardMemoryManager.Instance.ReportSuspicion(
                    zoneID,
                    GuardSuspicionReason.FailedQuestioning,
                    "failed guard questioning",
                    25f,
                    true
                );
                break;
        }
    }

    private void FacePlayer()
    {
        if (player == null)
        {
            return;
        }

        Vector3 direction = player.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            10f * Time.deltaTime
        );
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[GuardQuestioningResponder] " + message);
        }
    }
}