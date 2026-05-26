using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid")]
    public Transform inventoryGridContainer;
    public InventorySlotUI inventorySlotPrefab;

    [Header("Description Area")]
    public TMP_Text itemDescriptionHeaderText;
    public TMP_Text itemDescriptionText;
    public Button selectedItemActionButton;
    public TMP_Text selectedItemActionButtonText;

    [Header("Disguise / Equipment Preview")]
    public Image characterPreviewImage;
    public TMP_Text disguiseStatusText;

    [Header("Equipment Slots")]
    public EquipmentSlotUI headSlot;
    public EquipmentSlotUI bodySlot;
    public EquipmentSlotUI legsSlot;
    public EquipmentSlotUI feetSlot;
    public EquipmentSlotUI[] accessorySlots;

    [Header("Empty Text")]
    public string noItemHeader = "Description";
    public string noItemDescription = "Select an item to view details.";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private int selectedSlotIndex = -1;
    private EquipmentSlotUI selectedEquipmentSlot;

    private void OnEnable()
    {
        BuildOrRefreshGrid();
        RefreshEquipmentSlots();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += BuildOrRefreshGrid;
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.onClick.RemoveAllListeners();
            selectedItemActionButton.onClick.AddListener(UseSelectedItemAction);
        }

        SetupEquipmentSlots();
        BuildOrRefreshGrid();
        RefreshEquipmentSlots();
        ClearDetails();
    }

    private void Update()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

        HandleNumberHotkeys();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= BuildOrRefreshGrid;
        }
    }

    private void SetupEquipmentSlots()
    {
        if (headSlot != null)
        {
            headSlot.slotType = EquipmentSlotType.Head;
            headSlot.Setup(this);
        }

        if (bodySlot != null)
        {
            bodySlot.slotType = EquipmentSlotType.Body;
            bodySlot.Setup(this);
        }

        if (legsSlot != null)
        {
            legsSlot.slotType = EquipmentSlotType.Legs;
            legsSlot.Setup(this);
        }

        if (feetSlot != null)
        {
            feetSlot.slotType = EquipmentSlotType.Feet;
            feetSlot.Setup(this);
        }

        if (accessorySlots != null)
        {
            for (int i = 0; i < accessorySlots.Length; i++)
            {
                if (accessorySlots[i] == null)
                {
                    continue;
                }

                accessorySlots[i].slotType = EquipmentSlotType.Accessory;
                accessorySlots[i].accessoryIndex = i;
                accessorySlots[i].Setup(this);
            }
        }
    }

    private void HandleNumberHotkeys()
    {
        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) SelectSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) SelectSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) SelectSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) SelectSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) SelectSlot(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) SelectSlot(5);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) SelectSlot(6);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) SelectSlot(7);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) SelectSlot(8);
    }

    public void BuildOrRefreshGrid()
    {
        if (InventoryManager.Instance == null)
        {
            ClearDetails();
            return;
        }

        List<InventorySlot> slots = InventoryManager.Instance.GetSlots();

        if (slotUIs.Count != slots.Count)
        {
            RebuildGrid(slots);
        }

        RefreshSlots(slots);

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            ShowInventorySlotDetails(selectedSlotIndex);
        }
        else if (selectedEquipmentSlot != null)
        {
            ShowEquipmentSlotDetails(selectedEquipmentSlot);
        }
        else
        {
            ClearDetails();
        }
    }

    private void RebuildGrid(List<InventorySlot> slots)
    {
        ClearGrid();

        if (inventoryGridContainer == null)
        {
            Debug.LogWarning("[InventoryUI] Missing InventoryGridContainer.");
            return;
        }

        if (inventorySlotPrefab == null)
        {
            Debug.LogWarning("[InventoryUI] Missing InventorySlotPrefab.");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(inventorySlotPrefab, inventoryGridContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.Setup(this, slots[i], i);
            slotUIs.Add(slotUI);
        }
    }

    private void RefreshSlots(List<InventorySlot> slots)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            bool selected = i == selectedSlotIndex;

            if (slotUIs[i] != null)
            {
                slotUIs[i].Setup(this, slots[i], i);
                slotUIs[i].Refresh(selected);
            }
        }
    }

    public void SelectSlot(int slotIndex)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        InventorySlot slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null)
        {
            return;
        }

        selectedSlotIndex = slotIndex;
        selectedEquipmentSlot = null;

        if (showDebugLogs)
        {
            Debug.Log($"[InventoryUI] Selected inventory slot {slotIndex + 1}");
        }

        BuildOrRefreshGrid();
        ShowInventorySlotDetails(slotIndex);
    }

    private void ShowInventorySlotDetails(int slotIndex)
    {
        InventorySlot slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty())
        {
            ClearDetails();
            return;
        }

        ItemData item = slot.itemData;

        if (itemDescriptionHeaderText != null)
        {
            itemDescriptionHeaderText.text = item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                $"{item.itemDescription}\n\n" +
                $"Type: {item.itemType}\n" +
                $"Amount: {slot.amount}";
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.gameObject.SetActive(true);
        }

        if (selectedItemActionButtonText != null)
        {
            if (item.canEquip)
            {
                selectedItemActionButtonText.text = "Equip Item";
            }
            else if (item.canUse)
            {
                selectedItemActionButtonText.text = "Use Item";
            }
            else
            {
                selectedItemActionButtonText.text = "Select Item";
            }
        }
    }

    private void ClearDetails()
    {
        if (itemDescriptionHeaderText != null)
        {
            itemDescriptionHeaderText.text = noItemHeader;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = noItemDescription;
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.gameObject.SetActive(false);
        }
    }

    private void UseSelectedItemAction()
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        if (selectedSlotIndex < 0)
        {
            return;
        }

        InventorySlot slot = InventoryManager.Instance.GetSlot(selectedSlotIndex);

        if (slot == null || slot.IsEmpty())
        {
            return;
        }

        ItemData item = slot.itemData;

        if (item.canEquip)
        {
            EquipSelectedItem(item);
            return;
        }

        if (item.canUse)
        {
            InventoryManager.Instance.UseSlot(selectedSlotIndex);
            return;
        }

        Debug.Log($"[InventoryUI] Selected item: {item.itemName}");
    }

    private void EquipSelectedItem(ItemData item)
    {
        EquipmentSlotUI targetSlot = GetEquipmentSlotForItem(item);

        if (targetSlot == null)
        {
            Debug.LogWarning($"[InventoryUI] No equipment slot found for {item.itemName}");
            return;
        }

        if (!targetSlot.CanAccept(item))
        {
            Debug.LogWarning($"[InventoryUI] Slot cannot accept {item.itemName}");
            return;
        }

        targetSlot.SetItem(item);

        Debug.Log($"[InventoryUI] Equipped {item.itemName} to {item.equipmentSlotType}");

        RefreshEquipmentSlots();
    }

    private EquipmentSlotUI GetEquipmentSlotForItem(ItemData item)
    {
        if (item == null)
        {
            return null;
        }

        switch (item.equipmentSlotType)
        {
            case EquipmentSlotType.Head:
                return headSlot;

            case EquipmentSlotType.Body:
                return bodySlot;

            case EquipmentSlotType.Legs:
                return legsSlot;

            case EquipmentSlotType.Feet:
                return feetSlot;

            case EquipmentSlotType.Accessory:
                return GetFirstAvailableAccessorySlot();

            default:
                return null;
        }
    }

    private EquipmentSlotUI GetFirstAvailableAccessorySlot()
    {
        if (accessorySlots == null || accessorySlots.Length == 0)
        {
            return null;
        }

        foreach (EquipmentSlotUI slot in accessorySlots)
        {
            if (slot != null && slot.IsEmpty())
            {
                return slot;
            }
        }

        return accessorySlots[0];
    }

    public void SelectEquipmentSlot(EquipmentSlotUI equipmentSlot)
    {
        selectedEquipmentSlot = equipmentSlot;
        selectedSlotIndex = -1;

        ShowEquipmentSlotDetails(equipmentSlot);
        BuildOrRefreshGrid();
    }

    private void ShowEquipmentSlotDetails(EquipmentSlotUI equipmentSlot)
    {
        if (equipmentSlot == null)
        {
            ClearDetails();
            return;
        }

        ItemData item = equipmentSlot.GetItem();

        if (item == null)
        {
            if (itemDescriptionHeaderText != null)
            {
                itemDescriptionHeaderText.text = $"{equipmentSlot.slotType} Slot";
            }

            if (itemDescriptionText != null)
            {
                itemDescriptionText.text = $"No item equipped in this {equipmentSlot.slotType} slot.";
            }

            if (selectedItemActionButton != null)
            {
                selectedItemActionButton.gameObject.SetActive(false);
            }

            return;
        }

        if (itemDescriptionHeaderText != null)
        {
            itemDescriptionHeaderText.text = item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text =
                $"{item.itemDescription}\n\n" +
                $"Equipped Slot: {equipmentSlot.slotType}\n" +
                $"Disguise Set: {(string.IsNullOrWhiteSpace(item.disguiseSetID) ? "None" : item.disguiseSetID)}";
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.gameObject.SetActive(false);
        }
    }

    public void UnequipSlot(EquipmentSlotUI equipmentSlot)
    {
        if (equipmentSlot == null || equipmentSlot.IsEmpty())
        {
            return;
        }

        ItemData item = equipmentSlot.GetItem();
        equipmentSlot.SetItem(null);

        Debug.Log($"[InventoryUI] Unequipped {item.itemName}");

        RefreshEquipmentSlots();
        ClearDetails();
    }

    private void RefreshEquipmentSlots()
    {
        if (headSlot != null) headSlot.Refresh();
        if (bodySlot != null) bodySlot.Refresh();
        if (legsSlot != null) legsSlot.Refresh();
        if (feetSlot != null) feetSlot.Refresh();

        if (accessorySlots != null)
        {
            foreach (EquipmentSlotUI slot in accessorySlots)
            {
                if (slot != null)
                {
                    slot.Refresh();
                }
            }
        }

        RefreshDisguiseStatus();
    }

    private void RefreshDisguiseStatus()
    {
        if (disguiseStatusText == null)
        {
            return;
        }

        string disguiseSet = GetCurrentDisguiseSetID();

        if (string.IsNullOrWhiteSpace(disguiseSet))
        {
            disguiseStatusText.text = "Disguise: None";
        }
        else
        {
            disguiseStatusText.text = $"Disguise: {disguiseSet}";
        }
    }

    private string GetCurrentDisguiseSetID()
    {
        List<ItemData> equippedItems = new List<ItemData>();

        AddEquippedItem(equippedItems, headSlot);
        AddEquippedItem(equippedItems, bodySlot);
        AddEquippedItem(equippedItems, legsSlot);
        AddEquippedItem(equippedItems, feetSlot);

        if (accessorySlots != null)
        {
            foreach (EquipmentSlotUI slot in accessorySlots)
            {
                AddEquippedItem(equippedItems, slot);
            }
        }

        foreach (ItemData item in equippedItems)
        {
            if (item != null &&
                item.countsAsDisguisePiece &&
                !string.IsNullOrWhiteSpace(item.disguiseSetID))
            {
                return item.disguiseSetID;
            }
        }

        return "";
    }

    private void AddEquippedItem(List<ItemData> list, EquipmentSlotUI slot)
    {
        if (slot == null)
        {
            return;
        }

        ItemData item = slot.GetItem();

        if (item != null)
        {
            list.Add(item);
        }
    }

    public void QuickUseOrDrop(int slotIndex)
    {
        SelectSlot(slotIndex);
        UseSelectedItemAction();
    }

    private void ClearGrid()
    {
        foreach (InventorySlotUI slotUI in slotUIs)
        {
            if (slotUI != null)
            {
                Destroy(slotUI.gameObject);
            }
        }

        slotUIs.Clear();

        if (inventoryGridContainer == null)
        {
            return;
        }

        for (int i = inventoryGridContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(inventoryGridContainer.GetChild(i).gameObject);
        }
    }
}