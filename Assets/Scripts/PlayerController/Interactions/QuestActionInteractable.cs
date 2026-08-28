using UnityEngine;

public class QuestActionInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to interact";

    [Header("Quest Target")]
    public QuestTarget questTarget;

    [Header("Objective")]
    public QuestObjectiveType objectiveType = QuestObjectiveType.PerformAction;

    [Header("Action")]
    public string actionName = "Action";

    [TextArea(2, 4)]
    public string actionMessage = "Action completed.";

    [Header("Settings")]
    public bool canOnlyUseOnce = true;
    public bool canInteract = true;

    [Header("Optional Object Changes")]
    public GameObject[] objectsToEnable;
    public GameObject[] objectsToDisable;
    private bool hasUsed;
    [Header("Persistence")]
    public bool saveUsedState = false;
    public string usedFlagID;

    private void Awake()
    {
        if (questTarget == null)
        {
            questTarget =
                GetComponentInParent<QuestTarget>();
        }

        if (saveUsedState &&
            StoryFlagManager.Instance != null &&
            !string.IsNullOrWhiteSpace(usedFlagID))
        {
            hasUsed =
                StoryFlagManager.Instance.GetFlag(
                    usedFlagID
                );
        }
    }
    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (canOnlyUseOnce && hasUsed)
        {
            return false;
        }

        return questTarget != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        hasUsed = true;
        if (saveUsedState &&
        StoryFlagManager.Instance != null &&
        !string.IsNullOrWhiteSpace(usedFlagID))
        {
            StoryFlagManager.Instance.SetFlag(
                usedFlagID,
                true
            );
        }

        Debug.Log($"[QuestActionInteractable] {actionName}: {actionMessage}");

        foreach (GameObject obj in objectsToEnable)
        {
            if (obj != null)
            {
                obj.SetActive(true);
            }
        }

        foreach (GameObject obj in objectsToDisable)
        {
            if (obj != null)
            {
                obj.SetActive(false);
            }
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressObjective(
                objectiveType,
                questTarget.targetID
            );
        }
    }
}