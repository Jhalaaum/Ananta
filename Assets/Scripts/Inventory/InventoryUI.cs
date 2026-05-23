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

    [Header("Details")]
    public TMP_Text itemNameText;
    public TMP_Text itemDescriptionText;
    public TMP_Text itemTypeText;
    public TMP_Text itemStatsText;
    public Image itemPreviewImage;

    [Header("Action Buttons")]
    public Button useItemButton;
    public Button dropItemButton;

    [Header("Empty Text")]
    public string noItemName = "No Item Selected";
    public string noItemDescription = "Select an item to view details.";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private int selectedSlotIndex = -1;

    private void OnEnable()
    {
        BuildOrRefreshGrid();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += BuildOrRefreshGrid;
        }

        if (useItemButton != null)
        {
            useItemButton.onClick.RemoveAllListeners();
            useItemButton.onClick.AddListener(UseSelectedItem);
        }

        if (dropItemButton != null)
        {
            dropItemButton.onClick.RemoveAllListeners();
            dropItemButton.onClick.AddListener(DropSelectedItem);
        }

        BuildOrRefreshGrid();
    }

    private void Update()
    {
        HandleNumberHotkeys();
    }

    private void OnDestroy()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= BuildOrRefreshGrid;
        }
    }

    private void HandleNumberHotkeys()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }

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
        else
        {
            RefreshExistingSlots(slots);
        }

        if (selectedSlotIndex >= 0 && selectedSlotIndex < slots.Count)
        {
            ShowDetails(selectedSlotIndex);
        }
        else
        {
            ClearDetails();
        }
    }

    private void RebuildGrid(List<InventorySlot> slots)
    {
        ClearGrid();

        if (inventoryGridContainer == null || inventorySlotPrefab == null)
        {
            Debug.LogWarning("[InventoryUI] Missing grid container or slot prefab.");
            return;
        }

        for (int i = 0; i < slots.Count; i++)
        {
            InventorySlotUI slotUI = Instantiate(inventorySlotPrefab, inventoryGridContainer);
            slotUI.gameObject.SetActive(true);
            slotUI.Setup(this, slots[i], i);

            slotUIs.Add(slotUI);
        }

        RefreshExistingSlots(slots);
    }

    private void RefreshExistingSlots(List<InventorySlot> slots)
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

        if (showDebugLogs)
        {
            Debug.Log($"[InventoryUI] Selected slot {slotIndex + 1}");
        }

        BuildOrRefreshGrid();
        ShowDetails(slotIndex);
    }

    public void QuickUseOrDrop(int slotIndex)
    {
        InventorySlot slot = InventoryManager.Instance != null
            ? InventoryManager.Instance.GetSlot(slotIndex)
            : null;

        if (slot == null || slot.IsEmpty())
        {
            return;
        }

        if (slot.itemData.canUse)
        {
            InventoryManager.Instance.UseSlot(slotIndex);
        }
        else if (slot.itemData.canDrop && !slot.itemData.isImportant)
        {
            InventoryManager.Instance.DropSlot(slotIndex);
        }
    }

    private void ShowDetails(int slotIndex)
    {
        InventorySlot slot = InventoryManager.Instance.GetSlot(slotIndex);

        if (slot == null || slot.IsEmpty())
        {
            ClearDetails();
            return;
        }

        ItemData item = slot.itemData;

        if (itemNameText != null)
        {
            itemNameText.text = item.itemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = item.itemDescription;
        }

        if (itemTypeText != null)
        {
            itemTypeText.text = $"Type: {item.itemType}";
        }

        if (itemStatsText != null)
        {
            itemStatsText.text =
                $"Amount: {slot.amount}\n" +
                $"Stackable: {(item.isStackable ? "Yes" : "No")}\n" +
                $"Important: {(item.isImportant ? "Yes" : "No")}";
        }

        if (itemPreviewImage != null)
        {
            itemPreviewImage.sprite = item.itemIcon;
            itemPreviewImage.gameObject.SetActive(item.itemIcon != null);
        }

        if (useItemButton != null)
        {
            useItemButton.gameObject.SetActive(item.canUse);
            useItemButton.interactable = item.canUse;
        }

        if (dropItemButton != null)
        {
            bool canDrop = item.canDrop && !item.isImportant;
            dropItemButton.gameObject.SetActive(canDrop);
            dropItemButton.interactable = canDrop;
        }
    }

    private void ClearDetails()
    {
        if (itemNameText != null)
        {
            itemNameText.text = noItemName;
        }

        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = noItemDescription;
        }

        if (itemTypeText != null)
        {
            itemTypeText.text = "Type: —";
        }

        if (itemStatsText != null)
        {
            itemStatsText.text = "";
        }

        if (itemPreviewImage != null)
        {
            itemPreviewImage.sprite = null;
            itemPreviewImage.gameObject.SetActive(false);
        }

        if (useItemButton != null)
        {
            useItemButton.gameObject.SetActive(false);
        }

        if (dropItemButton != null)
        {
            dropItemButton.gameObject.SetActive(false);
        }
    }

    private void UseSelectedItem()
    {
        if (selectedSlotIndex < 0)
        {
            return;
        }

        InventoryManager.Instance?.UseSlot(selectedSlotIndex);
    }

    private void DropSelectedItem()
    {
        if (selectedSlotIndex < 0)
        {
            return;
        }

        InventoryManager.Instance?.DropSlot(selectedSlotIndex);
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