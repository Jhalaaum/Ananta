using System;
using System.Collections.Generic;
using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance;

    [Header("Inventory Size")]
    public int maxSlots = 32;

    [Header("Hotbar")]
    public int hotbarSlotCount = 9;
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

        for (int i = 0; i < maxSlots; i++)
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

        Log($"Selected hotbar slot {selectedHotbarIndex + 1}");

        OnSelectedHotbarChanged?.Invoke(selectedHotbarIndex);
        OnInventoryChanged?.Invoke();
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

                int space = slot.GetSpaceRemaining();
                int amountToAdd = Mathf.Min(space, remainingAmount);

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

            int amountForSlot = itemData.isStackable
                ? Mathf.Min(itemData.maxStackSize, remainingAmount)
                : 1;

            slot.itemData = itemData;
            slot.amount = amountForSlot;

            remainingAmount -= amountForSlot;

            if (remainingAmount <= 0)
            {
                NotifyItemAdded(itemData, amount);
                return true;
            }
        }

        Debug.LogWarning("[InventoryManager] Inventory full.");
        OnInventoryChanged?.Invoke();
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

                if (slot.CanStackWith(itemData))
                {
                    remainingAmount -= slot.GetSpaceRemaining();

                    if (remainingAmount <= 0)
                    {
                        return true;
                    }
                }
            }
        }

        foreach (InventorySlot slot in slots)
        {
            if (slot == null || !slot.IsEmpty())
            {
                continue;
            }

            int amountForSlot = itemData.isStackable
                ? Mathf.Min(itemData.maxStackSize, remainingAmount)
                : 1;

            remainingAmount -= amountForSlot;

            if (remainingAmount <= 0)
            {
                return true;
            }
        }

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

        if (item.itemType == ItemType.Consumable)
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

        if (!item.canDrop || item.isImportant)
        {
            Debug.Log($"[InventoryManager] Cannot drop important item: {item.itemName}");
            return;
        }

        RemoveFromSlot(slotIndex, 1);

        if (WorldItemDropper.Instance != null)
        {
            WorldItemDropper.Instance.DropItem(item);
        }

        Debug.Log($"[InventoryManager] Dropped item: {item.itemName}");
    }

    public void DropSelectedHotbarItem()
    {
        DropSlot(selectedHotbarIndex);
    }

    public void ClearInventory()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            slots[i].itemData = null;
            slots[i].amount = 0;
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
}