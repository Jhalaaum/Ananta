public struct GuardSuspicionEvaluation
{
    public bool shouldIncreaseSuspicion;

    public GuardSuspicionReason reason;
    public string reasonText;

    public float multiplier;
    public bool isSerious;

    public float suspicionPerSecond;
    public float minimumSuspicion;

    public bool canTriggerQuestioning;
    public bool canTriggerChase;

    public float memoryAmount;

    public static GuardSuspicionEvaluation Ignore()
    {
        return new GuardSuspicionEvaluation
        {
            shouldIncreaseSuspicion = false,
            reason = GuardSuspicionReason.None,
            reasonText = "",
            multiplier = 0f,
            isSerious = false,
            suspicionPerSecond = 0f,
            minimumSuspicion = 0f,
            canTriggerQuestioning = false,
            canTriggerChase = false,
            memoryAmount = 0f
        };
    }

    public static GuardSuspicionEvaluation Suspicious(
        GuardSuspicionReason reason,
        string reasonText,
        float multiplier,
        bool isSerious
    )
    {
        return new GuardSuspicionEvaluation
        {
            shouldIncreaseSuspicion = true,
            reason = reason,
            reasonText = reasonText,
            multiplier = multiplier,
            isSerious = isSerious,
            suspicionPerSecond = 10f,
            minimumSuspicion = 0f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            memoryAmount = 5f
        };
    }
}