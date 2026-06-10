using UnityEngine;

public class GuardQuestioningRepeatLimiter : MonoBehaviour
{
    [Header("Repeat Prevention")]
    public bool preventRepeatedQuestioningForSameAction = true;

    [Tooltip("If true, the same reason can only be questioned again after the player stops doing that action first.")]
    public bool requireActionToStopBeforeRepeat = true;

    [Tooltip("Optional emergency reset. Set to 0 or below to disable time-based reset.")]
    public float emergencyResetAfterSeconds = 0f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool hasLockedReason;
    private GuardSuspicionReason lockedReason;
    private string lockedSignature;
    private string lockedReasonText;
    private float lockedTimer;

    public bool IsBlocked(GuardSuspicionReason reason, string signature)
    {
        if (!preventRepeatedQuestioningForSameAction)
        {
            return false;
        }

        if (!hasLockedReason)
        {
            return false;
        }

        if (reason == GuardSuspicionReason.None)
        {
            return false;
        }

        if (lockedReason != reason)
        {
            return false;
        }

        if (lockedSignature != signature)
        {
            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardQuestioningRepeatLimiter] Blocked repeat questioning for: {lockedReason} / {lockedReasonText}"
            );
        }

        return true;
    }

    public void MarkQuestioned(
        GuardSuspicionReason reason,
        string signature,
        string reasonText
    )
    {
        if (!preventRepeatedQuestioningForSameAction)
        {
            return;
        }

        if (reason == GuardSuspicionReason.None)
        {
            return;
        }

        hasLockedReason = true;
        lockedReason = reason;
        lockedSignature = signature;
        lockedReasonText = reasonText;
        lockedTimer = 0f;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardQuestioningRepeatLimiter] Locked reason: {lockedReason} / {lockedReasonText} / Signature: {lockedSignature}"
            );
        }
    }

    public void ObserveCurrentReason(
        GuardSuspicionEvaluation evaluation,
        string currentSignature
    )
    {
        if (!hasLockedReason)
        {
            return;
        }

        lockedTimer += Time.deltaTime;

        if (emergencyResetAfterSeconds > 0f && lockedTimer >= emergencyResetAfterSeconds)
        {
            ClearLock("emergency timeout");
            return;
        }

        if (!requireActionToStopBeforeRepeat)
        {
            return;
        }

        if (!evaluation.shouldIncreaseSuspicion ||
            evaluation.reason == GuardSuspicionReason.None)
        {
            ClearLock("player stopped suspicious action");
            return;
        }

        if (evaluation.reason != lockedReason)
        {
            ClearLock("suspicion reason changed");
            return;
        }

        if (currentSignature != lockedSignature)
        {
            ClearLock("suspicious action signature changed");
            return;
        }
    }

    public void ClearLock(string reason)
    {
        if (!hasLockedReason)
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardQuestioningRepeatLimiter] Cleared lock because {reason}. Previous: {lockedReason} / {lockedReasonText}"
            );
        }

        hasLockedReason = false;
        lockedReason = GuardSuspicionReason.None;
        lockedSignature = "";
        lockedReasonText = "";
        lockedTimer = 0f;
    }

    public string GetDebugText()
    {
        if (!hasLockedReason)
        {
            return "No questioning lock.";
        }

        return
            $"Locked Reason: {lockedReason}\n" +
            $"Text: {lockedReasonText}\n" +
            $"Signature: {lockedSignature}\n" +
            $"Timer: {lockedTimer:0.0}s";
    }
}