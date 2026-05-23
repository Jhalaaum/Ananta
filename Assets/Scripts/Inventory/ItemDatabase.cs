using UnityEngine;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("All Items")]
    public ItemData[] allItems;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public ItemData GetItemByID(string itemID)
    {
        if (string.IsNullOrWhiteSpace(itemID))
        {
            return null;
        }

        string cleanID = itemID.Trim();

        foreach (ItemData item in allItems)
        {
            if (item == null)
            {
                continue;
            }

            if (item.itemID.Trim() == cleanID)
            {
                return item;
            }
        }

        Debug.LogWarning($"[ItemDatabase] Item not found: {itemID}");
        return null;
    }
}