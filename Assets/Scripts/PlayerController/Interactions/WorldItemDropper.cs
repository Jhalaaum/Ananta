using UnityEngine;

public class WorldItemDropper : MonoBehaviour
{
    public static WorldItemDropper Instance;

    [Header("Drop")]
    public GameObject defaultWorldItemPrefab;
    public Transform dropPoint;
    public float forwardOffset = 1.5f;
    public float upwardOffset = 1.0f;
    public float throwForce = 2.0f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        Instance = this;
    }

    public void DropItem(ItemData itemData)
    {
        DropItem(itemData, 1);
    }

    public void DropItem(ItemData itemData, int amount)
    {
        if (itemData == null)
        {
            Debug.LogWarning("[WorldItemDropper] Cannot drop null item.");
            return;
        }

        if (amount <= 0)
        {
            amount = 1;
        }

        if (defaultWorldItemPrefab == null)
        {
            Debug.LogWarning("[WorldItemDropper] Missing default world item prefab.");
            return;
        }

        Vector3 spawnPosition = dropPoint != null
            ? dropPoint.position
            : transform.position + transform.forward * forwardOffset + Vector3.up * upwardOffset;

        GameObject spawned = Instantiate(defaultWorldItemPrefab, spawnPosition, Quaternion.identity);

        WorldDroppedItem droppedItem = spawned.GetComponent<WorldDroppedItem>();

        if (droppedItem != null)
        {
            droppedItem.SetItem(itemData, amount);
        }
        else
        {
            Debug.LogWarning("[WorldItemDropper] Spawned prefab has no WorldDroppedItem component.");
        }

        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            rb.AddForce(transform.forward * throwForce + Vector3.up * 1.0f, ForceMode.Impulse);
        }

        if (showDebugLogs)
        {
            Debug.Log($"[WorldItemDropper] Dropped {itemData.itemName} x{amount} at {spawnPosition}");
        }
    }
}