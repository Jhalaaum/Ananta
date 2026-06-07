using System;
using System.Collections.Generic;
using UnityEngine;

public class EquipmentManager : MonoBehaviour
{
    public static EquipmentManager Instance;

    [Header("Equipped Clothing")]
    public ItemData headItem;
    public ItemData bodyItem;
    public ItemData legsItem;
    public ItemData feetItem;

    [Header("Accessories")]
    public ItemData[] accessoryItems = new ItemData[4];

    [Header("Disguise Rules")]
    public int expectedMainPieces = 4;

    [Header("Debug")]
    public bool showDebugLogs = true;

    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (accessoryItems == null || accessoryItems.Length != 4)
        {
            accessoryItems = new ItemData[4];
        }
    }

    public bool CanAccept(ItemData item, EquipmentSlotType slotType)
    {
        if (item == null || !item.canEquip)
        {
            return false;
        }

        if (slotType == EquipmentSlotType.Accessory)
        {
            return item.equipmentSlotType == EquipmentSlotType.Accessory;
        }

        return item.equipmentSlotType == slotType;
    }

    public ItemData GetEquippedItem(EquipmentSlotType slotType, int accessoryIndex = -1)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                return headItem;

            case EquipmentSlotType.Body:
                return bodyItem;

            case EquipmentSlotType.Legs:
                return legsItem;

            case EquipmentSlotType.Feet:
                return feetItem;

            case EquipmentSlotType.Accessory:
                if (accessoryIndex >= 0 && accessoryIndex < accessoryItems.Length)
                {
                    return accessoryItems[accessoryIndex];
                }

                return null;

            default:
                return null;
        }
    }

    public bool EquipFromInventorySlot(int inventorySlotIndex, EquipmentSlotType slotType, int accessoryIndex = -1)
    {
        if (InventoryManager.Instance == null)
        {
            return false;
        }

        InventorySlot inventorySlot = InventoryManager.Instance.GetSlot(inventorySlotIndex);

        if (inventorySlot == null || inventorySlot.IsEmpty())
        {
            return false;
        }

        ItemData itemToEquip = inventorySlot.itemData;

        if (!CanAccept(itemToEquip, slotType))
        {
            Debug.LogWarning($"[EquipmentManager] {itemToEquip.itemName} cannot go into {slotType}.");
            return false;
        }

        ItemData oldItem = GetEquippedItem(slotType, accessoryIndex);

        if (oldItem != null)
        {
            if (!InventoryManager.Instance.CanAddItem(oldItem, 1))
            {
                Debug.LogWarning("[EquipmentManager] Cannot equip because inventory has no room for old item.");
                return false;
            }
        }

        bool removed = InventoryManager.Instance.RemoveFromSlot(inventorySlotIndex, 1);

        if (!removed)
        {
            return false;
        }

        if (oldItem != null)
        {
            InventoryManager.Instance.AddItem(oldItem, 1);
        }

        SetEquippedItem(slotType, itemToEquip, accessoryIndex);

        Log($"Equipped {itemToEquip.itemName} to {slotType}");
        OnEquipmentChanged?.Invoke();

        return true;
    }

    public bool UnequipToInventory(EquipmentSlotType slotType, int accessoryIndex = -1)
    {
        if (InventoryManager.Instance == null)
        {
            return false;
        }

        ItemData item = GetEquippedItem(slotType, accessoryIndex);

        if (item == null)
        {
            return false;
        }

        if (!InventoryManager.Instance.CanAddItem(item, 1))
        {
            Debug.LogWarning("[EquipmentManager] Cannot unequip. Inventory is full.");
            return false;
        }

        SetEquippedItem(slotType, null, accessoryIndex);
        InventoryManager.Instance.AddItem(item, 1);

        Log($"Unequipped {item.itemName}");
        OnEquipmentChanged?.Invoke();

        return true;
    }

    private void SetEquippedItem(EquipmentSlotType slotType, ItemData item, int accessoryIndex = -1)
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                headItem = item;
                break;

            case EquipmentSlotType.Body:
                bodyItem = item;
                break;

            case EquipmentSlotType.Legs:
                legsItem = item;
                break;

            case EquipmentSlotType.Feet:
                feetItem = item;
                break;

            case EquipmentSlotType.Accessory:
                if (accessoryIndex >= 0 && accessoryIndex < accessoryItems.Length)
                {
                    accessoryItems[accessoryIndex] = item;
                }
                break;
        }
    }

    public string GetBestDisguiseSetID()
    {
        Dictionary<string, float> scores = new Dictionary<string, float>();

        AddDisguiseScore(scores, headItem);
        AddDisguiseScore(scores, bodyItem);
        AddDisguiseScore(scores, legsItem);
        AddDisguiseScore(scores, feetItem);

        foreach (ItemData accessory in accessoryItems)
        {
            AddDisguiseScore(scores, accessory);
        }

        string bestSet = "";
        float bestScore = 0f;

        foreach (KeyValuePair<string, float> pair in scores)
        {
            if (pair.Value > bestScore)
            {
                bestSet = pair.Key;
                bestScore = pair.Value;
            }
        }

        return bestSet;
    }

    public float GetBestDisguiseCompleteness()
    {
        string bestSet = GetBestDisguiseSetID();

        if (string.IsNullOrWhiteSpace(bestSet))
        {
            return 0f;
        }

        float mainPieceScore = 0f;

        if (CountsForSet(headItem, bestSet)) mainPieceScore += headItem.disguiseQuality;
        if (CountsForSet(bodyItem, bestSet)) mainPieceScore += bodyItem.disguiseQuality;
        if (CountsForSet(legsItem, bestSet)) mainPieceScore += legsItem.disguiseQuality;
        if (CountsForSet(feetItem, bestSet)) mainPieceScore += feetItem.disguiseQuality;

        return Mathf.Clamp01(mainPieceScore / expectedMainPieces);
    }

    public List<ItemData> GetAllEquippedItems()
    {
        List<ItemData> result = new List<ItemData>();

        AddIfNotNull(result, headItem);
        AddIfNotNull(result, bodyItem);
        AddIfNotNull(result, legsItem);
        AddIfNotNull(result, feetItem);

        foreach (ItemData accessory in accessoryItems)
        {
            AddIfNotNull(result, accessory);
        }

        return result;
    }

    public void RemoveSoftEquipmentForArrest()
    {
        if (headItem != null && headItem.securityType == ItemSecurityType.SoftItem) headItem = null;
        if (bodyItem != null && bodyItem.securityType == ItemSecurityType.SoftItem) bodyItem = null;
        if (legsItem != null && legsItem.securityType == ItemSecurityType.SoftItem) legsItem = null;
        if (feetItem != null && feetItem.securityType == ItemSecurityType.SoftItem) feetItem = null;

        for (int i = 0; i < accessoryItems.Length; i++)
        {
            if (accessoryItems[i] != null && accessoryItems[i].securityType == ItemSecurityType.SoftItem)
            {
                accessoryItems[i] = null;
            }
        }

        OnEquipmentChanged?.Invoke();
    }

    private void AddDisguiseScore(Dictionary<string, float> scores, ItemData item)
    {
        if (item == null || !item.countsAsDisguisePiece || string.IsNullOrWhiteSpace(item.disguiseSetID))
        {
            return;
        }

        string cleanID = item.disguiseSetID.Trim();

        if (!scores.ContainsKey(cleanID))
        {
            scores[cleanID] = 0f;
        }

        scores[cleanID] += Mathf.Max(0.1f, item.disguiseQuality);
    }

    private bool CountsForSet(ItemData item, string disguiseSetID)
    {
        if (item == null || !item.countsAsDisguisePiece || string.IsNullOrWhiteSpace(item.disguiseSetID))
        {
            return false;
        }

        return item.disguiseSetID.Trim() == disguiseSetID.Trim();
    }

    private void AddIfNotNull(List<ItemData> list, ItemData item)
    {
        if (item != null)
        {
            list.Add(item);
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[EquipmentManager] {message}");
        }
    }
}