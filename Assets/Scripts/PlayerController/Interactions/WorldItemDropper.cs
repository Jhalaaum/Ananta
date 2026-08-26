using UnityEngine;

public class WorldItemDropper : MonoBehaviour
{
    public static WorldItemDropper Instance;

    [Header("Drop")]
    public GameObject defaultWorldItemPrefab;
    public Transform dropPoint;

    [Header("Position")]
    public float forwardOffset = 1.0f;
    public float upwardOffset = 0.7f;

    [Header("Physics")]
    public bool useThrowForce = false;
    public float throwForce = 0.75f;
    public float upwardForce = 0.5f;

    [Header("Ground Placement")]
    public bool raycastToGround = true;
    public float groundRaycastHeight = 2.0f;
    public float groundRaycastDistance = 5.0f;
    public LayerMask groundLayerMask = ~0;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool DropItem(ItemData itemData)
    {
        return DropItem(itemData, 1);
    }

    public bool DropItem(ItemData itemData, int amount)
    {
        if (itemData == null)
        {
            Debug.LogWarning(
                "[WorldItemDropper] Cannot drop null item."
            );

            return false;
        }

        if (amount <= 0)
        {
            Debug.LogWarning(
                $"[WorldItemDropper] Cannot drop {itemData.itemName}. " +
                $"Invalid amount: {amount}"
            );

            return false;
        }
        if (defaultWorldItemPrefab == null)
        {
            Debug.LogWarning(
                "[WorldItemDropper] Missing default world item prefab."
            );

            return false;
        }

        Vector3 spawnPosition = GetDropPosition();

        GameObject spawned = Instantiate(
            defaultWorldItemPrefab,
            spawnPosition,
            Quaternion.identity
        );

        if (spawned == null)
        {
            Debug.LogError(
                $"[WorldItemDropper] Failed to instantiate dropped item: " +
                $"{itemData.itemName}"
            );

            return false;
        }

        WorldDroppedItem droppedItem =
            spawned.GetComponent<WorldDroppedItem>();

        if (droppedItem == null)
        {
            Debug.LogError(
                "[WorldItemDropper] Spawned prefab has no " +
                "WorldDroppedItem component. Destroying invalid object."
            );

            Destroy(spawned);

            return false;
        }

        droppedItem.SetItem(
            itemData,
            amount
        );

        Rigidbody rb = spawned.GetComponent<Rigidbody>();

        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;

            rb.linearDamping = 8f;
            rb.angularDamping = 10f;

            if (useThrowForce)
            {
                Vector3 force =
                    transform.forward * throwForce +
                    Vector3.up * upwardForce;

                rb.AddForce(
                    force,
                    ForceMode.Impulse
                );
            }
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[WorldItemDropper] Dropped " +
                $"{itemData.itemName} x{amount} " +
                $"at {spawnPosition}"
            );
        }

        return true;
    }
    private Vector3 GetDropPosition()
    {
        Vector3 basePosition;

        if (dropPoint != null)
        {
            basePosition = dropPoint.position;
        }
        else
        {
            basePosition =
                transform.position +
                transform.forward * forwardOffset +
                Vector3.up * upwardOffset;
        }

        if (!raycastToGround)
        {
            return basePosition;
        }

        Vector3 rayStart =
            basePosition +
            Vector3.up * groundRaycastHeight;

        // Look downward for valid ground.
        if (Physics.Raycast(
                rayStart,
                Vector3.down,
                out RaycastHit hit,
                groundRaycastDistance,
                groundLayerMask,
                QueryTriggerInteraction.Ignore))
        {
            return hit.point + Vector3.up * 0.15f;
        }
        return basePosition;
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}