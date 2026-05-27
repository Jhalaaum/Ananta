using UnityEngine;

public class WorldItemDropper : MonoBehaviour
{
    public static WorldItemDropper Instance;

    [Header("Drop")]
    public GameObject defaultWorldItemPrefab;
    public Transform dropPoint;
    public float forwardOffset = 1.2f;
    public float upwardOffset = 0.4f;
    public float throwForce = 2.5f;

    private void Awake()
    {
        Instance = this;
    }

    public void DropItem(ItemData itemData)
    {
        if (itemData == null)
        {
            return;
        }

        if (defaultWorldItemPrefab == null)
        {
            Debug.LogWarning("[WorldItemDropper] Missing default world item prefab.");
            return;
        }

        Vector3 spawnPosition;

        if (dropPoint != null)
        {
            spawnPosition = dropPoint.position;
        }
        else
        {
            spawnPosition = transform.position + transform.forward * forwardOffset + Vector3.up * upwardOffset;
        }

        GameObject spawned = Instantiate(defaultWorldItemPrefab, spawnPosition, Quaternion.identity);

        WorldDroppedItem droppedItem = spawned.GetComponent<WorldDroppedItem>();

        if (droppedItem != null)
        {
            droppedItem.SetItem(itemData, 1);
        }

        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.AddForce(transform.forward * throwForce + Vector3.up * 1.2f, ForceMode.Impulse);
        }

        Debug.Log($"[WorldItemDropper] Dropped world item: {itemData.itemName}");
    }
}