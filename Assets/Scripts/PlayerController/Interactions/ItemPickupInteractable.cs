using UnityEngine;

public class ItemPickupInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to pick up";

    [Header("Item")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Quest Optional")]
    public bool progressQuestOnPickup;
    public QuestObjectiveType objectiveType = QuestObjectiveType.CollectItem;
    public QuestTarget questTarget;

    [Header("Settings")]
    public bool destroyOnPickup = true;
    public bool canInteract = true;

    private bool hasPickedUp;

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
        return canInteract && !hasPickedUp && itemData != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning("[ItemPickupInteractable] No InventoryManager found.");
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemData, amount);

        if (!added)
        {
            Debug.Log("[ItemPickupInteractable] Could not pick up item. Inventory may be full.");
            return;
        }

        hasPickedUp = true;

        if (progressQuestOnPickup && QuestManager.Instance != null && questTarget != null)
        {
            QuestManager.Instance.ProgressObjective(
                objectiveType,
                questTarget.targetID,
                amount
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