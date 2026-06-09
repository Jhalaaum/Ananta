using UnityEngine;

[CreateAssetMenu(fileName = "New Guard Questioning Profile", menuName = "Guards/Questioning Profile")]
public class GuardQuestioningProfile : ScriptableObject
{
    [Header("Identity")]
    public string profileID;
    public string displayName;

    [Header("Dialogue")]
    public DialogueConversation questioningConversation;

    [Header("Suspicion")]
    public float questioningThreshold = 65f;
    public float minTimeBetweenQuestioning = 8f;

    [Header("After Passed")]
    public float suspicionAfterPassed = 15f;
    public float ignorePlayerDurationAfterPassed = 8f;

    [Header("After Warning")]
    public float suspicionAfterWarning = 45f;
    public float ignorePlayerDurationAfterWarning = 3f;

    [Header("After Failed")]
    public bool chaseOnFailed = true;

    [Header("After Caught")]
    public bool useCaughtSystemOnCaught = true;
}