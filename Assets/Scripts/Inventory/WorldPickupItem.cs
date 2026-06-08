using UnityEngine;

public class WorldPickupItem : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    public string worldItemID;

    [Header("Item")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Prompt")]
    public string promptText = "Press E to pick up";

    [Header("Visual / Interaction Objects")]
    public GameObject visualRoot;
    public Collider pickupCollider;

    [Header("Quest Optional")]
    public bool progressQuestOnPickup;
    public QuestObjectiveType objectiveType = QuestObjectiveType.CollectItem;
    public QuestTarget questTarget;

    [Header("State")]
    public bool isCollected;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (string.IsNullOrWhiteSpace(worldItemID))
        {
            worldItemID = gameObject.scene.name + "_" + gameObject.name;
        }

        if (pickupCollider == null)
        {
            pickupCollider = GetComponent<Collider>();
        }

        if (questTarget == null)
        {
            questTarget = GetComponentInParent<QuestTarget>();
        }

        WorldItemStateManager.Register(this);

        ApplyCollectedState();
    }

    private void OnDestroy()
    {
        WorldItemStateManager.Unregister(this);
    }

    public string GetPromptText()
    {
        if (itemData == null)
        {
            return promptText;
        }

        if (amount > 1)
        {
            return $"Press E to pick up {itemData.itemName} x{amount}";
        }

        return $"Press E to pick up {itemData.itemName}";
    }

    public bool CanInteract(Transform interactor)
    {
        return !isCollected &&
               itemData != null &&
               InventoryManager.Instance != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemData, amount);

        if (!added)
        {
            Debug.Log("[WorldPickupItem] Could not pick up item. Inventory may be full.");
            return;
        }

        if (progressQuestOnPickup && QuestManager.Instance != null)
        {
            string targetID = "";

            if (questTarget != null)
            {
                targetID = questTarget.targetID;
            }
            else if (itemData != null)
            {
                targetID = itemData.itemID;
            }

            QuestManager.Instance.ProgressObjective(
                objectiveType,
                targetID,
                amount
            );
        }

        SetCollected(true);
    }

    public void SetCollected(bool collected)
    {
        isCollected = collected;
        ApplyCollectedState();

        if (showDebugLogs)
        {
            Debug.Log($"[WorldPickupItem] {worldItemID} collected state = {isCollected}");
        }
    }

    private void ApplyCollectedState()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(!isCollected);
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled = !isCollected;
        }
    }
}