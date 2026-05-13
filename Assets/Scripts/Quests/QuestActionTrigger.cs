using UnityEngine;

public class QuestActionTrigger : MonoBehaviour
{
    [Header("Action Target")]
    public QuestTarget questTarget;

    [Header("Action Info")]
    public string actionName = "Inspect";

    [TextArea(2, 4)]
    public string actionMessage = "Action completed.";

    [Header("Settings")]
    public bool canOnlyUseOnce = true;

    private bool hasUsed;

    public void PerformAction()
    {
        if (hasUsed && canOnlyUseOnce)
        {
            return;
        }

        if (questTarget == null)
        {
            Debug.LogWarning($"{name} has no QuestTarget assigned.");
            return;
        }

        hasUsed = true;

        Debug.Log($"{actionName}: {actionMessage}");

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.PerformAction,
                questTarget.targetID
            );
        }
    }
}