using UnityEngine;

public class GuardQuestioningRepeatLimiter : MonoBehaviour
{
    [Header("Repeat Blocking")]
    public float sameReasonBlockDuration = 12f;
    public float globalQuestionBlockDuration = 4f;
    public float maxSameReasonMemoryDuration = 25f;

    [Header("Escalation Protection")]
    public int maxRepeatedQuestionsForSameSignature = 1;
    public float extendedBlockAfterRepeatedQuestioning = 20f;

    [Header("Runtime Inspector")]
    public GuardSuspicionReason lastQuestionedReason;
    public string lastQuestionedSignature;
    public string lastQuestionedReasonText;
    public int repeatedQuestionCount;
    public float sameReasonBlockTimer;
    public float globalBlockTimer;
    public float sameReasonMemoryTimer;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private GuardSuspicionReason observedReason;
    private string observedSignature;

    private void Update()
    {
        TickTimers();
    }

    private void TickTimers()
    {
        if (sameReasonBlockTimer > 0f)
        {
            sameReasonBlockTimer -= Time.deltaTime;
        }

        if (globalBlockTimer > 0f)
        {
            globalBlockTimer -= Time.deltaTime;
        }

        if (sameReasonMemoryTimer > 0f)
        {
            sameReasonMemoryTimer -= Time.deltaTime;

            if (sameReasonMemoryTimer <= 0f)
            {
                repeatedQuestionCount = 0;
                lastQuestionedSignature = "";
                lastQuestionedReasonText = "";
                lastQuestionedReason = GuardSuspicionReason.None;
            }
        }

        sameReasonBlockTimer = Mathf.Max(0f, sameReasonBlockTimer);
        globalBlockTimer = Mathf.Max(0f, globalBlockTimer);
        sameReasonMemoryTimer = Mathf.Max(0f, sameReasonMemoryTimer);
    }

    public void ObserveCurrentReason(GuardSuspicionEvaluation evaluation, string signature)
    {
        observedReason = evaluation.reason;
        observedSignature = signature;

        if (string.IsNullOrWhiteSpace(signature))
        {
            return;
        }

        if (signature != lastQuestionedSignature)
        {
            repeatedQuestionCount = 0;
        }
    }

    public bool IsBlocked(GuardSuspicionReason reason, string signature)
    {
        if (globalBlockTimer > 0f)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        bool sameSignature = signature == lastQuestionedSignature;
        bool sameReason = reason == lastQuestionedReason;

        if (sameSignature && sameReason && sameReasonBlockTimer > 0f)
        {
            return true;
        }

        if (sameSignature &&
            sameReason &&
            repeatedQuestionCount >= maxRepeatedQuestionsForSameSignature)
        {
            return true;
        }

        return false;
    }

    public void MarkQuestioned(
        GuardSuspicionReason reason,
        string signature,
        string reasonText
    )
    {
        if (string.IsNullOrWhiteSpace(signature))
        {
            signature = reason.ToString();
        }

        bool sameSignature = signature == lastQuestionedSignature;
        bool sameReason = reason == lastQuestionedReason;

        if (sameSignature && sameReason)
        {
            repeatedQuestionCount++;
        }
        else
        {
            repeatedQuestionCount = 1;
        }

        lastQuestionedReason = reason;
        lastQuestionedSignature = signature;
        lastQuestionedReasonText = reasonText;

        globalBlockTimer = globalQuestionBlockDuration;

        sameReasonBlockTimer = repeatedQuestionCount > maxRepeatedQuestionsForSameSignature
            ? extendedBlockAfterRepeatedQuestioning
            : sameReasonBlockDuration;

        sameReasonMemoryTimer = maxSameReasonMemoryDuration;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardQuestioningRepeatLimiter] Marked questioned. " +
                $"Reason: {reason}, Count: {repeatedQuestionCount}, Signature: {signature}"
            );
        }
    }

    public void ResetLimiter()
    {
        lastQuestionedReason = GuardSuspicionReason.None;
        lastQuestionedSignature = "";
        lastQuestionedReasonText = "";
        repeatedQuestionCount = 0;
        sameReasonBlockTimer = 0f;
        globalBlockTimer = 0f;
        sameReasonMemoryTimer = 0f;
        observedReason = GuardSuspicionReason.None;
        observedSignature = "";
    }
}