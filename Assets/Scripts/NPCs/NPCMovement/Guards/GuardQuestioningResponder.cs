using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class GuardQuestioningResponder : MonoBehaviour
{
    [Header("Questioning")]
    public GuardQuestioningProfile questioningProfile;

    [Header("References")]
    public DialogueManager dialogueManager;
    public Transform player;
    public NavMeshAgent agent;

    [Header("Optional Guard AI")]
    public AdvancedGuardAIController guardAI;

    [Header("State")]
    public bool IsQuestioning { get; private set; }
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

        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (agent == null)
        {
            agent = GetComponent<NavMeshAgent>();
        }

        if (guardAI == null)
        {
            guardAI = GetComponent<AdvancedGuardAIController>();
        }
    }

    private void Update()
    {
        if (ignorePlayerTimer > 0f)
        {
            ignorePlayerTimer -= Time.deltaTime;
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

        if (IsQuestioning)
        {
            return false;
        }

        if (ignorePlayerTimer > 0f)
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
        if (!CanQuestion())
        {
            return;
        }

        if (questioningRoutine != null)
        {
            StopCoroutine(questioningRoutine);
        }

        questioningRoutine = StartCoroutine(QuestioningRoutine());
    }

    private IEnumerator QuestioningRoutine()
    {
        IsQuestioning = true;
        lastQuestioningTime = Time.time;

        Log("Starting guard questioning.");

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

        GuardQuestioningOutcome outcome = GuardQuestioningOutcome.Failed;

        if (GuardQuestioningManager.Instance != null)
        {
            outcome = GuardQuestioningManager.Instance.EndQuestioningAndResolveOutcome();
        }

        ApplyOutcome(outcome);

        if (PlayerControlLock.Instance != null)
        {
            PlayerControlLock.Instance.UnlockPlayer();
        }

        IsQuestioning = false;
        questioningRoutine = null;
    }

    private void ApplyOutcome(GuardQuestioningOutcome outcome)
    {
        Log("Applying questioning outcome: " + outcome);

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
        ignorePlayerTimer = questioningProfile.ignorePlayerDurationAfterPassed;

        if (guardAI != null)
        {
            guardAI.SetSuspicion(questioningProfile.suspicionAfterPassed);
            guardAI.ForceReturnToPatrol();
        }
    }

    private void ApplyWarnedOutcome()
    {
        ignorePlayerTimer = questioningProfile.ignorePlayerDurationAfterWarning;

        if (guardAI != null)
        {
            guardAI.SetSuspicion(questioningProfile.suspicionAfterWarning);
            guardAI.ForceReturnToPatrol();
        }
    }

    private void ApplyFailedOutcome()
    {
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
        if (guardAI != null)
        {
            guardAI.ForceChasePlayer();
        }
    }

    private void ApplyCaughtOutcome()
    {
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