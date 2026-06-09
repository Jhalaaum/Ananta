using UnityEngine;

public class GuardQuestioningTrigger : MonoBehaviour
{
    [Header("References")]
    public AdvancedGuardAIController guardAI;
    public GuardQuestioningResponder responder;

    [Header("Settings")]
    public bool enableQuestioning = true;
    public bool onlyQuestionIfPlayerVisible = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (guardAI == null)
        {
            guardAI = GetComponent<AdvancedGuardAIController>();
        }

        if (responder == null)
        {
            responder = GetComponent<GuardQuestioningResponder>();
        }
    }

    private void Update()
    {
        if (!enableQuestioning)
        {
            return;
        }

        if (guardAI == null || responder == null || responder.questioningProfile == null)
        {
            return;
        }

        if (!responder.CanQuestion())
        {
            return;
        }

        if (guardAI.GetSuspicion() < responder.questioningProfile.questioningThreshold)
        {
            return;
        }

        if (onlyQuestionIfPlayerVisible && !guardAI.CanCurrentlySeePlayer())
        {
            return;
        }

        if (showDebugLogs)
        {
            Debug.Log("[GuardQuestioningTrigger] Questioning threshold reached.");
        }

        responder.StartQuestioning();
    }
}