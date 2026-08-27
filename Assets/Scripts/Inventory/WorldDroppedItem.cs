using UnityEngine;

public class WorldDroppedItem : MonoBehaviour, IInteractable
{
    [Header("Item")]
    public ItemData itemData;

    [Min(1)]
    public int amount = 1;

    [Header("Prompt")]
    public string pickupPrompt =
        "Press E to pick up";

    [Header("Visuals")]
    public SpriteRenderer spriteRenderer;
    public GameObject fallbackVisual;

    [Header("Physics Stop")]
    public bool stopAfterSettling = true;
    public float settleDelay = 0.4f;
    public float stopSpeedThreshold = 0.15f;

    [Header("Scale")]
    public float visualScale = 0.6f;

    [Header("Debug")]
    public bool showDebugLogs = false;

    private Rigidbody rb;
    private float settleTimer;
    private bool pickupInProgress;

    private void Awake()
    {
        rb = GetComponent<Rigidbody>();

        amount = Mathf.Max(1, amount);

        AutoFindVisuals();
        RefreshVisual();

        settleTimer = settleDelay;
    }

    private void Update()
    {
        StopSlidingIfSettled();
    }

    public void SetItem(
        ItemData item,
        int newAmount)
    {
        itemData = item;

        amount =
            Mathf.Max(
                1,
                newAmount
            );

        pickupInProgress = false;

        RefreshVisual();
    }

    private void AutoFindVisuals()
    {
        if (spriteRenderer == null)
        {
            spriteRenderer =
                GetComponentInChildren<SpriteRenderer>();
        }
    }

    private void RefreshVisual()
    {
        if (spriteRenderer != null &&
            itemData != null &&
            itemData.itemIcon != null)
        {
            spriteRenderer.gameObject.SetActive(true);

            spriteRenderer.sprite =
                itemData.itemIcon;

            spriteRenderer.color =
                Color.white;

            spriteRenderer.transform.localScale =
                Vector3.one *
                visualScale;
        }

        if (fallbackVisual != null)
        {
            bool useFallback =
                itemData == null ||
                itemData.itemIcon == null ||
                spriteRenderer == null;

            fallbackVisual.SetActive(
                useFallback
            );
        }
    }

    private void StopSlidingIfSettled()
    {
        if (!stopAfterSettling ||
            rb == null)
        {
            return;
        }

        settleTimer -=
            Time.deltaTime;

        if (settleTimer > 0f)
            return;

        if (rb.linearVelocity.magnitude <=
            stopSpeedThreshold)
        {
            rb.linearVelocity =
                Vector3.zero;

            rb.angularVelocity =
                Vector3.zero;

            rb.Sleep();
        }
    }

    private void LateUpdate()
    {
        if (Camera.main == null ||
            spriteRenderer == null ||
            !spriteRenderer.gameObject.activeSelf)
        {
            return;
        }

        Vector3 direction =
            spriteRenderer.transform.position -
            Camera.main.transform.position;

        if (direction.sqrMagnitude >
            0.001f)
        {
            spriteRenderer.transform.rotation =
                Quaternion.LookRotation(
                    direction
                );
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

    public bool CanInteract(
        Transform interactor)
    {
        return
            !pickupInProgress &&
            itemData != null &&
            InventoryManager.Instance != null;
    }

    public void Interact(
        Transform interactor)
    {
        if (!CanInteract(interactor))
            return;

        pickupInProgress = true;

        InventoryManager inventory =
            InventoryManager.Instance;

        if (!inventory.CanAddItem(
                itemData,
                amount))
        {
            pickupInProgress = false;

            if (showDebugLogs)
            {
                Debug.Log(
                    "[WorldDroppedItem] Inventory full. " +
                    itemData.itemName +
                    " remains in world."
                );
            }

            return;
        }

        bool added =
            inventory.AddItem(
                itemData,
                amount
            );

        if (!added)
        {
            pickupInProgress = false;

            Debug.LogWarning(
                "[WorldDroppedItem] Failed to return dropped item to inventory."
            );

            return;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[WorldDroppedItem] Picked up " +
                itemData.itemName +
                " x" +
                amount
            );
        }

        Destroy(gameObject);
    }
}