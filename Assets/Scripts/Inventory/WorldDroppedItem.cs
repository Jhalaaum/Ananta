using UnityEngine;

public class WorldDroppedItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public ItemData itemData;
    public int amount = 1;

    [Header("Prompt")]
    public string pickupPrompt = "Press E to pick up";

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public GameObject fallbackVisual;

    [Header("Scale")]
    public float visualScale = 0.6f;

    private void Awake()
    {
        AutoFindVisuals();
        RefreshVisual();
    }

    private void AutoFindVisuals()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        }
    }

    public void SetItem(ItemData item, int amount)
    {
        itemData = item;
        this.amount = Mathf.Max(1, amount);

        RefreshVisual();
    }

    private void RefreshVisual()
    {
        if (spriteRenderer != null && itemData != null && itemData.itemIcon != null)
        {
            spriteRenderer.gameObject.SetActive(true);
            spriteRenderer.sprite = itemData.itemIcon;
            spriteRenderer.color = Color.white;
            spriteRenderer.transform.localScale = Vector3.one * visualScale;
        }

        if (fallbackVisual != null)
        {
            bool useFallback = itemData == null || itemData.itemIcon == null || spriteRenderer == null;
            fallbackVisual.SetActive(useFallback);
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null || spriteRenderer == null || !spriteRenderer.gameObject.activeSelf)
        {
            return;
        }

        Vector3 direction = spriteRenderer.transform.position - Camera.main.transform.position;

        if (direction.sqrMagnitude > 0.001f)
        {
            spriteRenderer.transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    public string GetPromptText()
    {
        if (itemData == null)
        {
            return pickupPrompt;
        }

        if (amount > 1)
        {
            return $"Press E to pick up {itemData.itemName} x{amount}";
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