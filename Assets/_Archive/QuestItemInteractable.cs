using UnityEngine;

public class QuestItemInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to collect";

    [Header("Quest Target")]
    public QuestTarget questTarget;

    [Header("Item")]
    public string itemName = "Item";

    [TextArea(2, 4)]
    public string pickupMessage = "Item collected.";

    [Header("Settings")]
    public bool destroyOnPickup = true;
    public bool canInteract = true;

    private bool hasBeenCollected;

    private void Awake()
    {
        if (questTarget == null)
        {
            questTarget = GetComponentInParent<QuestTarget>();
        }
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        return canInteract && !hasBeenCollected && questTarget != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        hasBeenCollected = true;

        Debug.Log($"[QuestItemInteractable] Collected: {itemName}");

        if (!string.IsNullOrWhiteSpace(pickupMessage))
        {
            Debug.Log(pickupMessage);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.CollectItem,
                questTarget.targetID
            );
        }

        if (destroyOnPickup)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}