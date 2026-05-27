using UnityEngine;

public class WorldDroppedItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Prompt")]
    public string pickupPrompt = "Press E to pick up";

    [Header("Visual")]
    public SpriteRenderer spriteRenderer;

    private void Awake()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }

        RefreshVisual();
    }

    public void SetItem(ItemData item, int amount)
    {
        itemData = item;
        this.amount = amount;
        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer != null && itemData != null)
        {
            spriteRenderer.sprite = itemData.itemIcon;
        }
    }

    public string GetPromptText()
    {
        if (itemData == null)
        {
            return pickupPrompt;
        }

        return $"Press E to pick up {itemData.itemName}";
    }

    public bool CanInteract(Transform interactor)
    {
        return itemData != null && InventoryManager.Instance != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        bool added = InventoryManager.Instance.AddItem(itemData, amount);

        if (added)
        {
            Destroy(gameObject);
        }
    }
}