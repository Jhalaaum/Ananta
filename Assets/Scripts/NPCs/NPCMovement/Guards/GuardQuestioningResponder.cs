using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

public class GuardQuestioningResponder : MonoBehaviour
{
    [Header("Questioning")]
    public GuardQuestioningProfile questioningProfile;

    [Header("Reason Line / Questioning Start Indicator")]
    public bool showReasonLineBeforeConversation = true;
    public float minimumReasonLineDuration = 2.2f;
    public bool autoStartConversationAfterReasonLine = true;
    public bool allowSkipReasonLineAfterMinimumTime = true;
    public Key skipReasonLineKey = Key.Space;
    public string fallbackReasonLine = "Stop. Explain yourself.";

    [Header("Dialogue Countdown Indicator")]
    public bool showCountdownBeforeConversation = true;
    public bool refreshCountdownText = true;
    public float countdownRefreshInterval = 0.2f;
    public string reasonSpeakerName = "Guard";
    public string countdownFormat = "Questioning starts in {0:0.0}s";
    public string continuePromptText = "Press Space / E / Enter to respond.";

    [Header("Far Challenge Behaviour")]
    public bool hideReasonLineAfterFarChallenge = true;
    public bool showCountdownDuringFarChallenge = false;
    [Header("Far Challenge Timing")]
    public bool ensureReasonLineFullyVisibleBeforeClosing = true;

    [Tooltip("Used to estimate how long the typewriter needs to fully reveal the reason line.")]
    public float estimatedReasonLineCharactersPerSecond = 38f;

    [Tooltip("Extra time after the estimated full line reveal before the UI closes.")]
    public float reasonLineHoldAfterFullyVisible = 0.55f;

    [Tooltip("After the reason UI closes, questioning cannot start for this many seconds.")]
    public float postReasonLineQuestionGraceDuration = 2f;

    [Header("Far Challenge Runtime")]
    public bool isPostReasonLineGraceActive;
    public float postReasonLineGraceTimer;
    [Header("Reason Line Runtime")]
    public bool isShowingReasonLine;
    public bool IsPreparingQuestioning { get; private set; }
    public float reasonLineCountdownTimer;
    [TextArea(2, 5)] public string currentReasonLineDebug;

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
    private Coroutine reasonLineRoutine;

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
            if (ignorePlayerTimer < 0f)
            {
                ignorePlayerTimer = 0f;
            }
        }

        if (localQuestionCooldownTimer > 0f)
        {
            localQuestionCooldownTimer -= Time.deltaTime;
            if (localQuestionCooldownTimer < 0f)
            {
                localQuestionCooldownTimer = 0f;
            }
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

        if (IsPreparingQuestioning || IsQuestioning || IsResolvingOutcome)
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

    public bool CanStartConversationNow()
    {
        if (questioningProfile == null)
        {
            return false;
        }

        if (questioningProfile.questioningConversation == null)
        {
            return false;
        }

        if (IsPreparingQuestioning)
        {
            return false;
        }

        if (isShowingReasonLine)
        {
            return false;
        }

        if (isPostReasonLineGraceActive || postReasonLineGraceTimer > 0f)
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

        if (guardAI != null && guardAI.IsInFastTalkCooldown())
        {
            return false;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return false;
        }

        return true;
    }
    public void ShowReasonLineOnly(string reasonLine)
    {
        if (IsQuestioning || IsResolvingOutcome)
        {
            return;
        }

        if (reasonLineRoutine != null)
        {
            StopCoroutine(reasonLineRoutine);
        }

        reasonLineRoutine = StartCoroutine(ReasonLineOnlyRoutine(reasonLine));
    }

    private IEnumerator ReasonLineOnlyRoutine(string reasonLine)
    {
        IsPreparingQuestioning = true;

        string finalReasonLine = !string.IsNullOrWhiteSpace(reasonLine)
            ? reasonLine
            : fallbackReasonLine;

        isShowingReasonLine = true;
        isPostReasonLineGraceActive = false;
        postReasonLineGraceTimer = 0f;

        float displayDuration = Mathf.Max(0f, minimumReasonLineDuration);

        if (ensureReasonLineFullyVisibleBeforeClosing)
        {
            float characterRevealTime = finalReasonLine.Length /
                                        Mathf.Max(1f, estimatedReasonLineCharactersPerSecond);

            float estimatedFullDisplayTime = characterRevealTime + reasonLineHoldAfterFullyVisible;

            displayDuration = Mathf.Max(displayDuration, estimatedFullDisplayTime);
        }

        reasonLineCountdownTimer = displayDuration;

        if (dialogueUI != null)
        {
            dialogueUI.ForceHidePrompt();

            if (showCountdownDuringFarChallenge)
            {
                dialogueUI.ShowLine(
                    reasonSpeakerName,
                    BuildReasonLineDisplay(finalReasonLine, reasonLineCountdownTimer)
                );
            }
            else
            {
                dialogueUI.ShowLine(reasonSpeakerName, finalReasonLine);
                currentReasonLineDebug = finalReasonLine;
            }
        }

        float refreshTimer = 0f;

        while (reasonLineCountdownTimer > 0f)
        {
            FacePlayer();

            reasonLineCountdownTimer -= Time.unscaledDeltaTime;

            if (reasonLineCountdownTimer < 0f)
            {
                reasonLineCountdownTimer = 0f;
            }

            refreshTimer -= Time.unscaledDeltaTime;

            if (dialogueUI != null &&
                showCountdownDuringFarChallenge &&
                refreshCountdownText &&
                refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.05f, countdownRefreshInterval);

                dialogueUI.ShowLine(
                    reasonSpeakerName,
                    BuildReasonLineDisplay(finalReasonLine, reasonLineCountdownTimer)
                );
            }

            yield return null;
        }

        isShowingReasonLine = false;
        reasonLineCountdownTimer = 0f;
        currentReasonLineDebug = "";

        if (hideReasonLineAfterFarChallenge && dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.HidePrompt();
        }

        // Important:
        // The reason has finished and the UI is now closed.
        // Now give the player a grace period before real questioning can begin.
        isPostReasonLineGraceActive = true;
        postReasonLineGraceTimer = Mathf.Max(0f, postReasonLineQuestionGraceDuration);

        while (postReasonLineGraceTimer > 0f)
        {
            FacePlayer();

            postReasonLineGraceTimer -= Time.deltaTime;

            if (postReasonLineGraceTimer < 0f)
            {
                postReasonLineGraceTimer = 0f;
            }

            yield return null;
        }

        isPostReasonLineGraceActive = false;
        postReasonLineGraceTimer = 0f;
        IsPreparingQuestioning = false;

        reasonLineRoutine = null;
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

        questioningRoutine = StartCoroutine(LegacyQuestioningRoutine(reasonLine));
    }

    private IEnumerator LegacyQuestioningRoutine(string reasonLine)
    {
        IsPreparingQuestioning = true;

        if (showReasonLineBeforeConversation && dialogueUI != null)
        {
            yield return ShowReasonLineBlockingRoutine(reasonLine);
        }

        IsPreparingQuestioning = false;

        if (guardAI != null &&
            (guardAI.currentState == AdvancedGuardAIController.GuardState.Chase ||
             guardAI.currentState == AdvancedGuardAIController.GuardState.CapturePlayer))
        {
            questioningRoutine = null;
            yield break;
        }

        yield return ConversationRoutine();
    }

    public bool StartQuestioningConversationOnly(string reasonLine)
    {
        if (!CanStartConversationNow())
        {
            return false;
        }

        if (reasonLineRoutine != null)
        {
            StopCoroutine(reasonLineRoutine);
            reasonLineRoutine = null;
        }

        if (questioningRoutine != null)
        {
            StopCoroutine(questioningRoutine);
        }

        isShowingReasonLine = false;
        reasonLineCountdownTimer = 0f;
        currentReasonLineDebug = "";
        IsPreparingQuestioning = false;

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.HidePrompt();
        }

        questioningRoutine = StartCoroutine(ConversationRoutine());
        return true;
    }

    private IEnumerator ShowReasonLineBlockingRoutine(string reasonLine)
    {
        string finalReasonLine = !string.IsNullOrWhiteSpace(reasonLine)
            ? reasonLine
            : fallbackReasonLine;

        isShowingReasonLine = true;
        reasonLineCountdownTimer = Mathf.Max(0f, minimumReasonLineDuration);

        if (dialogueUI != null)
        {
            dialogueUI.ForceHidePrompt();
            dialogueUI.ShowLine(
                reasonSpeakerName,
                BuildReasonLineDisplay(finalReasonLine, reasonLineCountdownTimer)
            );
        }

        float refreshTimer = 0f;

        while (reasonLineCountdownTimer > 0f)
        {
            FacePlayer();

            reasonLineCountdownTimer -= Time.unscaledDeltaTime;

            if (reasonLineCountdownTimer < 0f)
            {
                reasonLineCountdownTimer = 0f;
            }

            refreshTimer -= Time.unscaledDeltaTime;

            if (dialogueUI != null &&
                showCountdownBeforeConversation &&
                refreshCountdownText &&
                refreshTimer <= 0f)
            {
                refreshTimer = Mathf.Max(0.05f, countdownRefreshInterval);
                dialogueUI.ShowLine(
                    reasonSpeakerName,
                    BuildReasonLineDisplay(finalReasonLine, reasonLineCountdownTimer)
                );
            }

            yield return null;
        }

        isShowingReasonLine = false;
        reasonLineCountdownTimer = 0f;
        currentReasonLineDebug = "";

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        yield return null;
    }

    private IEnumerator ConversationRoutine()
    {
        IsPreparingQuestioning = false;
        IsQuestioning = true;
        IsResolvingOutcome = false;
        lastQuestioningTime = Time.time;

        Log("Questioning conversation started.");

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

    private string BuildReasonLineDisplay(string reasonLine, float secondsRemaining)
    {
        string safeReason = !string.IsNullOrWhiteSpace(reasonLine)
            ? reasonLine
            : fallbackReasonLine;

        if (!showCountdownBeforeConversation)
        {
            currentReasonLineDebug = safeReason;
            return safeReason;
        }

        string countdownText = string.Format(
            countdownFormat,
            Mathf.Max(0f, secondsRemaining)
        );

        string displayText = safeReason + "\n\n" + countdownText;

        currentReasonLineDebug = displayText;
        return displayText;
    }

    public void CancelPendingQuestioning(string reason)
    {
        if (reasonLineRoutine != null)
        {
            StopCoroutine(reasonLineRoutine);
            reasonLineRoutine = null;
        }

        if (IsPreparingQuestioning)
        {
            IsPreparingQuestioning = false;
        }
        isPostReasonLineGraceActive = false;
        postReasonLineGraceTimer = 0f;
        isShowingReasonLine = false;
        reasonLineCountdownTimer = 0f;
        currentReasonLineDebug = "";

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
            dialogueUI.HidePrompt();
        }

        Log("Pending questioning cancelled: " + reason);
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
                ZoneAlertManager.Instance.AddAlert(zoneID, passedAlertIncrease, "Fast Talk passed");
                break;

            case GuardQuestioningOutcome.Warned:
                ZoneAlertManager.Instance.AddAlert(zoneID, warnedAlertIncrease, "Guard issued warning");
                break;

            case GuardQuestioningOutcome.Failed:
                ZoneAlertManager.Instance.AddAlert(zoneID, failedAlertIncrease, "Player failed questioning");
                break;

            case GuardQuestioningOutcome.Chase:
                ZoneAlertManager.Instance.AddAlert(zoneID, chaseAlertIncrease, "Guard began chase");
                break;

            case GuardQuestioningOutcome.Caught:
                ZoneAlertManager.Instance.AddAlert(zoneID, caughtAlertIncrease, "Player was caught");
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

    public void ResetQuestioningForCheckpoint()
    {
        StopAllCoroutines();

        IsPreparingQuestioning = false;
        IsQuestioning = false;
        IsResolvingOutcome = false;

        ignorePlayerTimer = 0f;
        localQuestionCooldownTimer = 0f;

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        if (PlayerControlLock.Instance != null)
        {
            PlayerControlLock.Instance.UnlockPlayer();
        }

        if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.HidePrompt();
            dialogueUI.HideDialogue();
        }
        isPostReasonLineGraceActive = false;
        postReasonLineGraceTimer = 0f;
        IsPreparingQuestioning = false;
        isShowingReasonLine = false;
        reasonLineCountdownTimer = 0f;
        currentReasonLineDebug = "";

        questioningRoutine = null;
        reasonLineRoutine = null;

        Log("Reset for checkpoint.");
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[GuardQuestioningResponder] " + message);
        }
    }
}