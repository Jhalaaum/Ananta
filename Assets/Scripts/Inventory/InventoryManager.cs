using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Size")]
    public int hotbarSlotCount = 9;
    public int backpackSlotCount = 24;

    [Header("Selected Hotbar")]
    public int selectedHotbarIndex = 0;

    [Header("Runtime Slots")]
    [SerializeField] private List<InventorySlot> slots = new List<InventorySlot>();

    [Header("Debug")]
    public bool showDebugLogs = true;

    public event Action OnInventoryChanged;
    public event Action<int> OnSelectedHotbarChanged;
    public event Action<ItemData, int> OnItemAdded;
    public event Action<ItemData, int> OnItemRemoved;
    public event Action<ItemData> OnItemUsed;

    public int TotalSlotCount => hotbarSlotCount + backpackSlotCount;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        InitializeSlots();
    }

    private void InitializeSlots()
    {
        slots.Clear();

        for (int i = 0; i < TotalSlotCount; i++)
        {
            slots.Add(new InventorySlot(null, 0));
        }

        selectedHotbarIndex = Mathf.Clamp(selectedHotbarIndex, 0, hotbarSlotCount - 1);
    }

    public List<InventorySlot> GetSlots()
    {
        return slots;
    }

    public InventorySlot GetSlot(int index)
    {
        if (index < 0 || index >= slots.Count)
        {
            return null;
        }

        return slots[index];
    }

    public bool IsHotbarSlot(int index)
    {
        return index >= 0 && index < hotbarSlotCount;
    }

    public InventorySlot GetSelectedHotbarSlot()
    {
        return GetSlot(selectedHotbarIndex);
    }

    public void SelectHotbarSlot(int index)
    {
        if (index < 0 || index >= hotbarSlotCount)
        {
            return;
        }

        selectedHotbarIndex = index;
        OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
        OnInventoryChanged?.Invoke();

        Log($"Selected hotbar slot {selectedHotbarIndex + 1}");
    }

    public void SelectNextHotbarSlot()
    {
        int next = selectedHotbarIndex + 1;

        if (next >= hotbarSlotCount)
        {
            next = 0;
        }

        SelectHotbarSlot(next);
    }

    public void SelectPreviousHotbarSlot()
    {
        int previous = selectedHotbarIndex - 1;

        if (previous < 0)
        {
            previous = hotbarSlotCount - 1;
        }

        SelectHotbarSlot(previous);
    }

    public bool AddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        // IMPORTANT:
        // Do not modify the inventory unless the ENTIRE requested amount fits.
        if (!CanAddItem(itemData, amount))
        {
            Debug.LogWarning(
                $"[InventoryManager] Cannot add {itemData.itemName} x{amount}. " +
                "Not enough inventory space."
            );

            return false;
        }

        int remainingAmount = amount;

        if (itemData.isStackable)
        {
            for (int i = 0; i < slots.Count; i++)
            {
                InventorySlot slot = slots[i];

                if (slot == null || slot.IsEmpty())
                {
                    continue;
                }

                if (!slot.CanStackWith(itemData))
                {
                    continue;
                }

                int spaceRemaining = slot.GetSpaceRemaining();

                if (spaceRemaining <= 0)
                {
                    continue;
                }

                int amountToAdd = Mathf.Min(
                    spaceRemaining,
                    remainingAmount
                );

                slot.amount += amountToAdd;
                remainingAmount -= amountToAdd;

                if (remainingAmount <= 0)
                {
                    NotifyItemAdded(itemData, amount);
                    return true;
                }
            }
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null || !slot.IsEmpty())
            {
                continue;
            }

            int amountForSlot;

            if (itemData.isStackable)
            {
                amountForSlot = Mathf.Min(
                    itemData.maxStackSize,
                    remainingAmount
                );
            }
            else
            {
                amountForSlot = 1;
            }

            slot.itemData = itemData;
            slot.amount = amountForSlot;

            remainingAmount -= amountForSlot;

            if (remainingAmount <= 0)
            {
                NotifyItemAdded(itemData, amount);
                return true;
            }
        }

        Debug.LogError(
            $"[InventoryManager] AddItem reached an unexpected state for " +
            $"{itemData.itemName} x{amount}."
        );

        return false;
    }
    public bool AddItemByID(string itemID, int amount = 1)
    {
        if (ItemDatabase.Instance == null)
        {
            Debug.LogWarning("[InventoryManager] No ItemDatabase found.");
            return false;
        }

        ItemData itemData = ItemDatabase.Instance.GetItemByID(itemID);

        if (itemData == null)
        {
            Debug.LogWarning($"[InventoryManager] Item not found: {itemID}");
            return false;
        }

        return AddItem(itemData, amount);
    }

    public bool CanAddItem(ItemData itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0)
        {
            return false;
        }

        int remainingAmount = amount;

        if (itemData.isStackable)
        {
            foreach (InventorySlot slot in slots)
            {
                if (slot == null || slot.IsEmpty())
                {
                    continue;
                }

                if (!slot.CanStackWith(itemData))
                {
                    continue;
                }

                int availableSpace = Mathf.Max(
                    0,
                    slot.GetSpaceRemaining()
                );

                remainingAmount -= availableSpace;

                if (remainingAmount <= 0)
                {
                    return true;
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty())
            {
                continue;
            }

            if (itemData.isStackable)
            {
                remainingAmount -= Mathf.Max(
                    1,
                    itemData.maxStackSize
                );
            }
            else
            {
                remainingAmount -= 1;
            }

            if (remainingAmount <= 0)
            {
                return true;
            }
        }

        // Not enough total capacity.
        return false;
    }
    public bool RemoveFromSlot(int slotIndex, int amount = 1)
    {
        InventorySlot slot = GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty() || amount <= 0)
        {
            return false;
        }

        ItemData removedItem = slot.itemData;
        int removedAmount = Mathf.Min(amount, slot.amount);

        slot.amount -= removedAmount;

        if (slot.amount <= 0)
        {
            slot.itemData = null;
            slot.amount = 0;
        }

        NotifyItemRemoved(removedItem, removedAmount);
        return true;
    }

    public bool MoveOrSwapSlots(int fromIndex, int toIndex)
    {
        if (fromIndex == toIndex)
        {
            return false;
        }

        InventorySlot fromSlot = GetSlot(fromIndex);
        InventorySlot toSlot = GetSlot(toIndex);

        if (fromSlot == null || toSlot == null || fromSlot.IsEmpty())
        {
            return false;
        }

        if (!toSlot.IsEmpty() &&
            fromSlot.itemData.itemID == toSlot.itemData.itemID &&
            fromSlot.itemData.isStackable)
        {
            int amountToMove = Mathf.Min(toSlot.GetSpaceRemaining(), fromSlot.amount);

            if (amountToMove > 0)
            {
                toSlot.amount += amountToMove;
                fromSlot.amount -= amountToMove;

                if (fromSlot.amount <= 0)
                {
                    fromSlot.itemData = null;
                    fromSlot.amount = 0;
                }

                OnInventoryChanged?.Invoke();
                return true;
            }
        }

        ItemData tempItem = toSlot.itemData;
        int tempAmount = toSlot.amount;

        toSlot.itemData = fromSlot.itemData;
        toSlot.amount = fromSlot.amount;

        fromSlot.itemData = tempItem;
        fromSlot.amount = tempAmount;

        OnInventoryChanged?.Invoke();
        return true;
    }

    public void UseSlot(int slotIndex)
    {
        InventorySlot slot = GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty())
        {
            return;
        }

        ItemData item = slot.itemData;

        if (!item.canUse)
        {
            Debug.Log($"[InventoryManager] {item.itemName} cannot be used.");
            return;
        }

        Debug.Log($"[InventoryManager] Used item: {item.itemName}");

        if (!string.IsNullOrWhiteSpace(item.useMessage))
        {
            Debug.Log(item.useMessage);
        }

        OnItemUsed?.Invoke(item);

        if (item.itemType == ItemType.Consumable || item.itemRole == InventoryItemRole.Consumable)
        {
            RemoveFromSlot(slotIndex, 1);
        }

        OnInventoryChanged?.Invoke();
    }

    public void UseSelectedHotbarItem()
    {
        UseSlot(selectedHotbarIndex);
    }

    public void DropSlot(int slotIndex)
    {
        InventorySlot slot = GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty())
        {
            return;
        }

        ItemData item = slot.itemData;

        if (item == null)
        {
            return;
        }

        if (!item.canDrop || item.isImportant)
        {
            Debug.Log(
                $"[InventoryManager] Cannot drop important item: {item.itemName}"
            );

            return;
        }

        if (WorldItemDropper.Instance == null)
        {
            Debug.LogWarning(
                $"[InventoryManager] Cannot drop {item.itemName}. " +
                "No WorldItemDropper exists."
            );

            return;
        }

        bool spawned =
            WorldItemDropper.Instance.DropItem(item, 1);

        if (!spawned)
        {
            Debug.LogWarning(
                $"[InventoryManager] Failed to drop {item.itemName}. " +
                "Item remains in inventory."
            );

            return;
        }

        bool removed = RemoveFromSlot(slotIndex, 1);

        if (!removed)
        {
            Debug.LogError(
                $"[InventoryManager] World item spawned, but failed " +
                $"to remove {item.itemName} from inventory."
            );

            return;
        }

        Debug.Log(
            $"[InventoryManager] Dropped item: {item.itemName}"
        );
    }
    public void DropSelectedHotbarItem()
    {
        DropSlot(selectedHotbarIndex);
    }

    public void ClearInventory()
    {
        foreach (InventorySlot slot in slots)
        {
            slot.itemData = null;
            slot.amount = 0;
        }

        OnInventoryChanged?.Invoke();
    }

    public void RemoveSoftItemsForArrest()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlot slot = slots[i];

            if (slot == null || slot.IsEmpty() || slot.itemData == null)
            {
                continue;
            }

            if (slot.itemData.securityType == ItemSecurityType.SoftItem)
            {
                slot.itemData = null;
                slot.amount = 0;
            }
        }

        OnInventoryChanged?.Invoke();
    }

    private void NotifyItemAdded(ItemData itemData, int amount)
    {
        Log($"Added {itemData.itemName} x{amount}");
        OnItemAdded?.Invoke(itemData, amount);
        OnInventoryChanged?.Invoke();
    }

    private void NotifyItemRemoved(ItemData itemData, int amount)
    {
        Log($"Removed {itemData.itemName} x{amount}");
        OnItemRemoved?.Invoke(itemData, amount);
        OnInventoryChanged?.Invoke();
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[InventoryManager] {message}");
        }
    }
    public InventorySaveData CaptureInventory()
    {
        InventorySaveData saveData = new InventorySaveData();
        saveData.selectedHotbarIndex = selectedHotbarIndex;

        foreach (InventorySlot slot in slots)
        {
            InventorySlotSaveData slotSaveData = new InventorySlotSaveData();

            if (slot != null && !slot.IsEmpty() && slot.itemData != null)
            {
                slotSaveData.itemID = slot.itemData.itemID;
                slotSaveData.amount = slot.amount;
            }
            else
            {
                slotSaveData.itemID = "";
                slotSaveData.amount = 0;
            }

            saveData.slots.Add(slotSaveData);
        }

        return saveData;
    }

    public void RestoreInventory(InventorySaveData saveData)
    {
        if (saveData == null)
        {
            ClearInventory();
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].itemData = null;
            slots[i].amount = 0;
        }

        for (int i = 0; i < saveData.slots.Count && i < slots.Count; i++)
        {
            InventorySlotSaveData slotSave = saveData.slots[i];

            if (slotSave == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(slotSave.itemID) || slotSave.amount <= 0)
            {
                continue;
            }

            ItemData item = ItemDatabase.Instance != null
                ? ItemDatabase.Instance.GetItemByID(slotSave.itemID)
                : null;

            if (item == null)
            {
                Debug.LogWarning($"[InventoryManager] Could not restore item: {slotSave.itemID}");
                continue;
            }

            slots[i].itemData = item;
            slots[i].amount = slotSave.amount;
        }

        selectedHotbarIndex = Mathf.Clamp(saveData.selectedHotbarIndex, 0, hotbarSlotCount - 1);

        OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
        OnInventoryChanged?.Invoke();
    }
}