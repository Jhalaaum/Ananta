using UnityEngine;

[RequireComponent(typeof(Collider))]
public class QuestLocationTrigger : MonoBehaviour
{
    [Header("Quest Location")]
    public QuestTarget questTarget;

    [Header("Objective")]
    public QuestObjectiveType objectiveType = QuestObjectiveType.ReachLocation;

    [Header("Settings")]
    public bool onlyTriggerOnce = true;
    public bool requirePlayerTag = true;
    public string playerTag = "Player";

    private bool hasTriggered;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered && onlyTriggerOnce)
        {
            return;
        }

        if (requirePlayerTag && !other.CompareTag(playerTag))
        {
            return;
        }

        if (questTarget == null)
        {
            Debug.LogWarning($"{name} has no QuestTarget assigned.");
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("No QuestManager found in scene.");
            return;
        }

        QuestManager.Instance.ProgressObjective(objectiveType, questTarget.targetID);

        hasTriggered = true;
    }
}