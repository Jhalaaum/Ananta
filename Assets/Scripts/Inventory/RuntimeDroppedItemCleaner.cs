using UnityEngine;

public class RuntimeDroppedItemCleaner : MonoBehaviour
{
    public static RuntimeDroppedItemCleaner Instance;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        Instance = this;
    }

    public void ClearRuntimeDroppedItems()
    {
        WorldDroppedItem[] droppedItems = FindObjectsOfType<WorldDroppedItem>();

        int count = 0;

        foreach (WorldDroppedItem item in droppedItems)
        {
            if (item != null)
            {
                Destroy(item.gameObject);
                count++;
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[RuntimeDroppedItemCleaner] Cleared {count} runtime dropped items.");
        }
    }
}