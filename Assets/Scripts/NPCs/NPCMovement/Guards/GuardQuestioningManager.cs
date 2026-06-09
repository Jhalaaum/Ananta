using UnityEngine;

public class GuardQuestioningManager : MonoBehaviour
{
    public static GuardQuestioningManager Instance;

    [Header("Current Questioning")]
    public bool IsQuestioningActive { get; private set; }
    public GuardQuestioningOutcome CurrentOutcome { get; private set; }

    [Header("Outcome Flags")]
    public string passedFlag = "guard_question_passed";
    public string warnedFlag = "guard_question_warned";
    public string failedFlag = "guard_question_failed";
    public string chaseFlag = "guard_question_chase";
    public string caughtFlag = "guard_question_caught";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BeginQuestioning()
    {
        IsQuestioningActive = true;
        CurrentOutcome = GuardQuestioningOutcome.None;
        ClearOutcomeFlags();

        Log("Questioning started.");
    }

    public GuardQuestioningOutcome EndQuestioningAndResolveOutcome()
    {
        CurrentOutcome = ReadOutcomeFromFlags();
        IsQuestioningActive = false;

        Log("Questioning ended with outcome: " + CurrentOutcome);

        ClearOutcomeFlags();

        return CurrentOutcome;
    }

    public void ForceOutcome(GuardQuestioningOutcome outcome)
    {
        CurrentOutcome = outcome;
        IsQuestioningActive = false;
        ClearOutcomeFlags();

        Log("Forced questioning outcome: " + outcome);
    }

    private GuardQuestioningOutcome ReadOutcomeFromFlags()
    {
        if (DialogueFlagManager.Instance == null)
        {
            return GuardQuestioningOutcome.Failed;
        }

        if (DialogueFlagManager.Instance.HasFlag(caughtFlag))
        {
            return GuardQuestioningOutcome.Caught;
        }

        if (DialogueFlagManager.Instance.HasFlag(chaseFlag))
        {
            return GuardQuestioningOutcome.Chase;
        }

        if (DialogueFlagManager.Instance.HasFlag(failedFlag))
        {
            return GuardQuestioningOutcome.Failed;
        }

        if (DialogueFlagManager.Instance.HasFlag(warnedFlag))
        {
            return GuardQuestioningOutcome.Warned;
        }

        if (DialogueFlagManager.Instance.HasFlag(passedFlag))
        {
            return GuardQuestioningOutcome.Passed;
        }

        return GuardQuestioningOutcome.Failed;
    }

    private void ClearOutcomeFlags()
    {
        if (DialogueFlagManager.Instance == null)
        {
            return;
        }

        DialogueFlagManager.Instance.ClearFlag(passedFlag);
        DialogueFlagManager.Instance.ClearFlag(warnedFlag);
        DialogueFlagManager.Instance.ClearFlag(failedFlag);
        DialogueFlagManager.Instance.ClearFlag(chaseFlag);
        DialogueFlagManager.Instance.ClearFlag(caughtFlag);
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[GuardQuestioningManager] " + message);
        }
    }
}