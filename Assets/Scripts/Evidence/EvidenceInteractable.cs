using UnityEngine;

public class EvidenceInteractable :
    MonoBehaviour,
    IInteractable
{
    [Header("Evidence")]
    public EvidenceData evidence;

    [Header("Interaction")]
    public string prompt =
        "Press E to examine";

    public bool disableAfterDiscovery = true;

    [Header("Quest")]
    public bool progressQuest = false;

    public QuestObjectiveType objectiveType =
        QuestObjectiveType.PerformAction;

    public string questTargetID;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool discovered;

    public string GetPromptText()
    {
        return prompt;
    }

    public bool CanInteract(Transform interactor)
    {
        if (discovered)
        {
            return false;
        }

        if (evidence == null ||
            EvidenceManager.Instance == null)
        {
            return false;
        }

        if (EvidenceManager.Instance.HasEvidence(
            evidence.evidenceID))
        {
            return false;
        }

        return true;
    }
    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        bool success =
            EvidenceManager.Instance
                .DiscoverEvidence(evidence);

        if (!success)
        {
            return;
        }

        discovered = true;

        if (progressQuest &&
            QuestManager.Instance != null &&
            !string.IsNullOrWhiteSpace(questTargetID))
        {
            QuestManager.Instance.ProgressObjective(
                objectiveType,
                questTargetID
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[EvidenceInteractable] Found: " +
                evidence.displayName
            );
        }

        if (disableAfterDiscovery)
        {
            gameObject.SetActive(false);
        }
    }
}