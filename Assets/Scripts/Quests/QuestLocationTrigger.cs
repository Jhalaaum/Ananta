using UnityEngine;

public class QuestLocationTrigger : MonoBehaviour
{
    [Header("Objective")]
    public QuestObjectiveType objectiveType = QuestObjectiveType.ReachLocation;
    public string targetID = "ledger_house_entry";
    public int amount = 1;

    [Header("Settings")]
    public bool triggerOnlyOnce = true;
    public bool requirePlayerTag = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool hasTriggered;

    private void OnTriggerEnter(Collider other)
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            return;
        }

        if (requirePlayerTag && !other.CompareTag("Player"))
        {
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("[QuestLocationTrigger] No QuestManager found.");
            return;
        }

        hasTriggered = true;

        QuestManager.Instance.ProgressObjective(
            objectiveType,
            targetID,
            amount
        );

        if (showDebugLogs)
        {
            Debug.Log($"[QuestLocationTrigger] Progressed {objectiveType}: {targetID}");
        }
    }
}