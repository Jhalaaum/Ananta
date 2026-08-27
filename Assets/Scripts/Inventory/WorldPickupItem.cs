using UnityEngine;

public class WorldPickupItem : MonoBehaviour, IInteractable
{
    [Header("Identity")]
    [Tooltip(
        "Must be unique for every persistent placed item in the scene."
    )]
    public string worldItemID;

    [Header("Item")]
    public ItemData itemData;

    [Min(1)]
    public int amount = 1;

    [Header("Prompt")]
    public string promptText = "Press E to pick up";

    [Header("Visual / Interaction")]
    public GameObject visualRoot;
    public Collider pickupCollider;

    [Header("Quest Progress - Optional")]
    public bool progressQuestOnPickup = false;

    public QuestObjectiveType objectiveType =
        QuestObjectiveType.CollectItem;

    public QuestTarget questTarget;

    [Tooltip(
        "If QuestTarget is missing, use ItemData.itemID as the quest target."
    )]
    public bool fallbackToItemIDForQuest = true;

    [Header("Settings")]
    public bool canInteract = true;

    [Header("Runtime State")]
    public bool isCollected;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private bool pickupInProgress;

    private void Awake()
    {
        amount = Mathf.Max(1, amount);

        if (string.IsNullOrWhiteSpace(worldItemID))
        {
            worldItemID =
                gameObject.scene.name +
                "_" +
                gameObject.name;

            Debug.LogWarning(
                "[WorldPickupItem] '" +
                gameObject.name +
                "' has no explicit worldItemID. Generated fallback: " +
                worldItemID +
                ". Give persistent items a unique ID manually."
            );
        }

        worldItemID = worldItemID.Trim();

        if (pickupCollider == null)
        {
            pickupCollider =
                GetComponent<Collider>();
        }

        if (questTarget == null)
        {
            questTarget =
                GetComponentInParent<QuestTarget>();
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
            return
                "Press E to pick up " +
                itemData.itemName +
                " x" +
                amount;
        }

        return
            "Press E to pick up " +
            itemData.itemName;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
            return false;

        if (pickupInProgress)
            return false;

        if (isCollected)
            return false;

        if (itemData == null)
            return false;

        if (InventoryManager.Instance == null)
            return false;

        return true;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
            return;

        pickupInProgress = true;

        InventoryManager inventory =
            InventoryManager.Instance;

        if (inventory == null)
        {
            pickupInProgress = false;

            Debug.LogWarning(
                "[WorldPickupItem] No InventoryManager found."
            );

            return;
        }

        // ------------------------------------------------
        // PHASE 1: Verify full pickup can fit.
        // ------------------------------------------------

        if (!inventory.CanAddItem(itemData, amount))
        {
            pickupInProgress = false;

            if (showDebugLogs)
            {
                Debug.Log(
                    "[WorldPickupItem] Inventory cannot accept " +
                    itemData.itemName +
                    " x" +
                    amount +
                    ". Nothing was collected."
                );
            }

            return;
        }

        // ------------------------------------------------
        // PHASE 2: Add the item.
        // ------------------------------------------------

        bool added =
            inventory.AddItem(
                itemData,
                amount
            );

        if (!added)
        {
            pickupInProgress = false;

            Debug.LogWarning(
                "[WorldPickupItem] Inventory rejected " +
                itemData.itemName +
                " x" +
                amount +
                ". World item remains available."
            );

            return;
        }

        // ------------------------------------------------
        // PHASE 3: Quest progress.
        //
        // Only happens AFTER inventory succeeds.
        // ------------------------------------------------

        ProgressQuestIfNeeded();

        // ------------------------------------------------
        // PHASE 4: Persistent world state.
        // ------------------------------------------------

        SetCollected(true);

        pickupInProgress = false;

        if (showDebugLogs)
        {
            Debug.Log(
                "[WorldPickupItem] Picked up " +
                itemData.itemName +
                " x" +
                amount +
                " | World ID: " +
                worldItemID
            );
        }
    }

    private void ProgressQuestIfNeeded()
    {
        if (!progressQuestOnPickup)
            return;

        if (QuestManager.Instance == null)
            return;

        string targetID =
            GetQuestTargetID();

        if (string.IsNullOrWhiteSpace(targetID))
        {
            Debug.LogWarning(
                "[WorldPickupItem] Quest progress requested but no valid target ID exists for " +
                gameObject.name
            );

            return;
        }

        QuestManager.Instance.ProgressObjective(
            objectiveType,
            targetID,
            amount
        );

        if (showDebugLogs)
        {
            Debug.Log(
                "[WorldPickupItem] Quest progress sent. " +
                "Type: " +
                objectiveType +
                ", Target: " +
                targetID +
                ", Amount: " +
                amount
            );
        }
    }

    private string GetQuestTargetID()
    {
        if (questTarget != null &&
            !string.IsNullOrWhiteSpace(
                questTarget.targetID))
        {
            return questTarget.targetID.Trim();
        }

        if (fallbackToItemIDForQuest &&
            itemData != null &&
            !string.IsNullOrWhiteSpace(
                itemData.itemID))
        {
            return itemData.itemID.Trim();
        }

        return "";
    }

    public void SetCollected(bool collected)
    {
        isCollected = collected;
        pickupInProgress = false;

        ApplyCollectedState();

        if (showDebugLogs)
        {
            Debug.Log(
                "[WorldPickupItem] " +
                worldItemID +
                " collected state = " +
                isCollected
            );
        }
    }

    private void ApplyCollectedState()
    {
        if (visualRoot != null)
        {
            visualRoot.SetActive(
                !isCollected
            );
        }

        if (pickupCollider != null)
        {
            pickupCollider.enabled =
                !isCollected;
        }
    }

    private void OnValidate()
    {
        amount = Mathf.Max(1, amount);

        if (!string.IsNullOrWhiteSpace(worldItemID))
        {
            worldItemID =
                worldItemID.Trim();
        }
    }
}