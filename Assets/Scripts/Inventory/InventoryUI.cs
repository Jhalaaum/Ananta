using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryUI : MonoBehaviour
{
    [Header("Grid Containers")]
    public Transform hotbarGridContainer;
    public Transform inventoryGridContainer;
    public InventorySlotUI inventorySlotPrefab;

    [Header("Description Area")]
    public TMP_Text itemDescriptionHeaderText;
    public TMP_Text itemDescriptionText;
    public Button selectedItemActionButton;
    public TMP_Text selectedItemActionButtonText;

    [Header("Disguise UI")]
    public TMP_Text disguiseStatusText;
    public Image characterPreviewImage;

    [Header("Equipment Slots")]
    public EquipmentSlotUI headSlot;
    public EquipmentSlotUI bodySlot;
    public EquipmentSlotUI legsSlot;
    public EquipmentSlotUI feetSlot;
    public EquipmentSlotUI[] accessorySlots;

    [Header("Drag Visual")]
    public Canvas rootCanvas;
    public Image dragIconImage;

    [Header("Empty Text")]
    public string noItemHeader = "Description";
    public string noItemDescription = "Select an item to view details.";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<InventorySlotUI> slotUIs = new List<InventorySlotUI>();

    private int selectedSlotIndex = -1;
    private EquipmentSlotUI selectedEquipmentSlot;

    private int draggedInventorySlotIndex = -1;
    private ItemData draggedItem;
    private int draggedAmount;

    private void Awake()
    {
        if (rootCanvas == null)
        {
            rootCanvas = GetComponentInParent<Canvas>();
        }
    }

    private void OnEnable()
    {
        BuildOrRefreshGrid();
        RefreshEquipmentSlots();
        RefreshDisguiseStatus();
    }

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += BuildOrRefreshGrid;
        }

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged += HandleEquipmentChanged;
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.onClick.RemoveAllListeners();
            selectedItemActionButton.onClick.AddListener(UseSelectedItemAction);
        }

        CreateDragIconIfMissing();
        SetupEquipmentSlots();
        BuildOrRefreshGrid();
        RefreshEquipmentSlots();
        RefreshDisguiseStatus();
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

        if (EquipmentManager.Instance != null)
        {
            EquipmentManager.Instance.OnEquipmentChanged -= HandleEquipmentChanged;
        }
    }

    private void HandleEquipmentChanged()
    {
        RefreshEquipmentSlots();
        RefreshDisguiseStatus();
    }

    private void CreateDragIconIfMissing()
    {
        if (dragIconImage != null)
        {
            dragIconImage.gameObject.SetActive(false);
            return;
        }

        if (rootCanvas == null)
        {
            return;
        }

        GameObject dragObject = new GameObject("InventoryDragIcon");
        dragObject.transform.SetParent(rootCanvas.transform, false);
        dragObject.transform.SetAsLastSibling();

        dragIconImage = dragObject.AddComponent<Image>();
        dragIconImage.raycastTarget = false;
        dragIconImage.preserveAspect = true;

        RectTransform rect = dragIconImage.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(64, 64);

        dragIconImage.gameObject.SetActive(false);
    }

    private void SetupEquipmentSlots()
    {
        SetupEquipmentSlot(headSlot, EquipmentSlotType.Head, -1);
        SetupEquipmentSlot(bodySlot, EquipmentSlotType.Body, -1);
        SetupEquipmentSlot(legsSlot, EquipmentSlotType.Legs, -1);
        SetupEquipmentSlot(feetSlot, EquipmentSlotType.Feet, -1);

        if (accessorySlots != null)
        {
            for (int i = 0; i < accessorySlots.Length; i++)
            {
                SetupEquipmentSlot(accessorySlots[i], EquipmentSlotType.Accessory, i);
            }
        }
    }

    private void SetupEquipmentSlot(EquipmentSlotUI slot, EquipmentSlotType type, int accessoryIndex)
    {
        if (slot == null)
        {
            return;
        }

        slot.slotType = type;
        slot.accessoryIndex = accessoryIndex;
        slot.Setup(this);
    }

    private void HandleNumberHotkeys()
    {
        if (Keyboard.current == null || InventoryManager.Instance == null)
        {
            return;
        }

        if (Keyboard.current.digit1Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(0);
        if (Keyboard.current.digit2Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(1);
        if (Keyboard.current.digit3Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(2);
        if (Keyboard.current.digit4Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(3);
        if (Keyboard.current.digit5Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(4);
        if (Keyboard.current.digit6Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(5);
        if (Keyboard.current.digit7Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(6);
        if (Keyboard.current.digit8Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(7);
        if (Keyboard.current.digit9Key.wasPressedThisFrame) InventoryManager.Instance.SelectHotbarSlot(8);
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

        if (hotbarGridContainer == null)
        {
            Debug.LogWarning("[InventoryUI] Missing HotbarGridContainer.");
            return;
        }

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
            Transform parent = InventoryManager.Instance.IsHotbarSlot(i)
                ? hotbarGridContainer
                : inventoryGridContainer;

            InventorySlotUI slotUI = Instantiate(inventorySlotPrefab, parent);
            slotUI.gameObject.SetActive(true);
            slotUI.Setup(this, slots[i], i);
            slotUIs.Add(slotUI);
        }
    }

    private void RefreshSlots(List<InventorySlot> slots)
    {
        for (int i = 0; i < slotUIs.Count; i++)
        {
            bool selected =
                InventoryManager.Instance != null &&
                i == InventoryManager.Instance.selectedHotbarIndex &&
                InventoryManager.Instance.IsHotbarSlot(i);

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

        if (InventoryManager.Instance.IsHotbarSlot(slotIndex))
        {
            InventoryManager.Instance.SelectHotbarSlot(slotIndex);
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
                $"Role: {item.itemRole}\n" +
                $"Security: {item.securityType}\n" +
                $"Amount: {slot.amount}\n" +
                $"Location: {(InventoryManager.Instance.IsHotbarSlot(slotIndex) ? "Hotbar" : "Backpack")}\n" +
                GetDisguiseText(item);
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.gameObject.SetActive(true);
        }

        if (selectedItemActionButtonText != null)
        {
            if (item.canEquip)
            {
                selectedItemActionButtonText.text = "Equip";
            }
            else if (item.canUse)
            {
                selectedItemActionButtonText.text = "Use";
            }
            else if (item.canDrop && !item.isImportant)
            {
                selectedItemActionButtonText.text = "Drop";
            }
            else
            {
                selectedItemActionButtonText.text = "Inspect";
            }
        }
    }

    private string GetDisguiseText(ItemData item)
    {
        if (item == null || !item.canEquip)
        {
            return "";
        }

        return
            $"\nEquip Slot: {item.equipmentSlotType}\n" +
            $"Disguise Set: {(string.IsNullOrWhiteSpace(item.disguiseSetID) ? "None" : item.disguiseSetID)}\n" +
            $"Disguise Quality: {Mathf.RoundToInt(item.disguiseQuality * 100f)}%";
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
        if (InventoryManager.Instance == null || selectedSlotIndex < 0)
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
            EquipmentSlotUI targetSlot = GetEquipmentSlotForItem(item);
            TryEquipFromInventorySlot(selectedSlotIndex, targetSlot);
            return;
        }

        if (item.canUse)
        {
            InventoryManager.Instance.UseSlot(selectedSlotIndex);
            return;
        }

        if (item.canDrop && !item.isImportant)
        {
            DropInventorySlot(selectedSlotIndex);
        }
    }

    public void BeginDragFromInventory(int slotIndex, ItemData item, int amount)
    {
        if (item == null)
        {
            return;
        }

        draggedInventorySlotIndex = slotIndex;
        draggedItem = item;
        draggedAmount = amount;

        if (dragIconImage != null)
        {
            dragIconImage.sprite = item.itemIcon;
            dragIconImage.color = Color.white;
            dragIconImage.gameObject.SetActive(item.itemIcon != null);
            dragIconImage.transform.SetAsLastSibling();
        }
    }

    public void UpdateDrag(Vector2 screenPosition)
    {
        if (dragIconImage == null || !dragIconImage.gameObject.activeSelf || rootCanvas == null)
        {
            return;
        }

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rootCanvas.transform as RectTransform,
            screenPosition,
            rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera,
            out Vector2 localPoint
        );

        dragIconImage.rectTransform.anchoredPosition = localPoint;
    }

    public void EndDrag()
    {
        if (dragIconImage != null)
        {
            dragIconImage.gameObject.SetActive(false);
        }

        draggedInventorySlotIndex = -1;
        draggedItem = null;
        draggedAmount = 0;
    }

    public void TryDropDraggedItemOnInventorySlot(int targetSlotIndex)
    {
        if (InventoryManager.Instance == null)
        {
            EndDrag();
            return;
        }

        if (draggedInventorySlotIndex < 0)
        {
            EndDrag();
            return;
        }

        InventoryManager.Instance.MoveOrSwapSlots(draggedInventorySlotIndex, targetSlotIndex);
        EndDrag();
    }

    public void TryDropDraggedItemOnEquipmentSlot(EquipmentSlotUI equipmentSlot)
    {
        if (draggedItem == null || draggedInventorySlotIndex < 0 || equipmentSlot == null)
        {
            EndDrag();
            return;
        }

        TryEquipFromInventorySlot(draggedInventorySlotIndex, equipmentSlot);
        EndDrag();
    }

    private bool TryEquipFromInventorySlot(int inventorySlotIndex, EquipmentSlotUI targetSlot)
    {
        if (EquipmentManager.Instance == null || targetSlot == null)
        {
            return false;
        }

        bool equipped = EquipmentManager.Instance.EquipFromInventorySlot(
            inventorySlotIndex,
            targetSlot.slotType,
            targetSlot.accessoryIndex
        );

        if (!equipped)
        {
            return false;
        }

        selectedSlotIndex = -1;
        selectedEquipmentSlot = targetSlot;

        RefreshEquipmentSlots();
        RefreshDisguiseStatus();
        BuildOrRefreshGrid();
        ShowEquipmentSlotDetails(targetSlot);

        return true;
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

    public void DropInventorySlot(int slotIndex)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        InventoryManager.Instance.DropSlot(slotIndex);

        selectedSlotIndex = -1;
        BuildOrRefreshGrid();
        ClearDetails();
    }

    public void SelectEquipmentSlot(EquipmentSlotUI equipmentSlot)
    {
        selectedEquipmentSlot = equipmentSlot;
        selectedSlotIndex = -1;

        BuildOrRefreshGrid();
        ShowEquipmentSlotDetails(equipmentSlot);
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
                $"Disguise Set: {(string.IsNullOrWhiteSpace(item.disguiseSetID) ? "None" : item.disguiseSetID)}\n" +
                $"Quality: {Mathf.RoundToInt(item.disguiseQuality * 100f)}%\n\n" +
                $"Right-click this slot to unequip.";
        }

        if (selectedItemActionButton != null)
        {
            selectedItemActionButton.gameObject.SetActive(false);
        }
    }

    public void UnequipSlot(EquipmentSlotUI equipmentSlot)
    {
        if (equipmentSlot == null || equipmentSlot.IsEmpty() || EquipmentManager.Instance == null)
        {
            return;
        }

        EquipmentManager.Instance.UnequipToInventory(equipmentSlot.slotType, equipmentSlot.accessoryIndex);

        RefreshEquipmentSlots();
        RefreshDisguiseStatus();
        BuildOrRefreshGrid();
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
    }

    private void RefreshDisguiseStatus()
    {
        if (disguiseStatusText == null)
        {
            return;
        }

        if (EquipmentManager.Instance == null)
        {
            disguiseStatusText.text = "Disguise: None";
            return;
        }

        string setID = EquipmentManager.Instance.GetBestDisguiseSetID();
        float completeness = EquipmentManager.Instance.GetBestDisguiseCompleteness();

        if (string.IsNullOrWhiteSpace(setID))
        {
            disguiseStatusText.text = "Disguise: None";
            return;
        }

        disguiseStatusText.text =
            $"Disguise: {setID}\n" +
            $"Completeness: {Mathf.RoundToInt(completeness * 100f)}%";
    }

    public void QuickUseOrDrop(int slotIndex)
    {
        DropInventorySlot(slotIndex);
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

        ClearChildren(hotbarGridContainer);
        ClearChildren(inventoryGridContainer);
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null)
        {
            return;
        }

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }
}