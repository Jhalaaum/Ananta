using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler,
    IBeginDragHandler,
    IDragHandler,
    IEndDragHandler,
    IDropHandler
{
    [Header("UI References")]
    public Image backgroundImage;
    public Image itemIconImage;
    public TMP_Text amountText;
    public TMP_Text slotNumberText;

    [Header("Colors")]
    public Color normalColor;
    public Color hotbarColor;
    public Color selectedColor;
    public Color amountTextColor = Color.white;
    public Color slotNumberTextColor = new Color(0.75f, 0.68f, 0.55f, 1f);

    private InventoryUI inventoryUI;
    private InventorySlot slot;
    private int slotIndex;
    private bool isDragging;

    private void Awake()
    {
        AutoFindReferences();

        ColorUtility.TryParseHtmlString("#2E2417", out normalColor);
        ColorUtility.TryParseHtmlString("#3A2D18", out hotbarColor);
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);

        ForceVisualDefaults();
    }

    private void AutoFindReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (itemIconImage == null)
        {
            Transform icon = transform.Find("ItemIconImage");
            if (icon != null) itemIconImage = icon.GetComponent<Image>();
        }

        if (amountText == null)
        {
            Transform amount = transform.Find("ItemAmountText");
            if (amount != null) amountText = amount.GetComponent<TMP_Text>();
        }

        if (slotNumberText == null)
        {
            Transform number = transform.Find("SlotNumberText");
            if (number != null) slotNumberText = number.GetComponent<TMP_Text>();
        }
    }

    private void ForceVisualDefaults()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = true;
        }

        if (itemIconImage != null)
        {
            itemIconImage.raycastTarget = false;
            itemIconImage.preserveAspect = true;
            itemIconImage.color = Color.white;
        }

        if (amountText != null)
        {
            amountText.raycastTarget = false;
            amountText.color = amountTextColor;
            amountText.alpha = 1f;
            amountText.fontSize = 16;
            amountText.alignment = TextAlignmentOptions.BottomRight;
        }

        if (slotNumberText != null)
        {
            slotNumberText.raycastTarget = false;
            slotNumberText.color = slotNumberTextColor;
            slotNumberText.alpha = 1f;
            slotNumberText.fontSize = 12;
        }
    }

    public void Setup(InventoryUI inventoryUI, InventorySlot slot, int slotIndex)
    {
        this.inventoryUI = inventoryUI;
        this.slot = slot;
        this.slotIndex = slotIndex;

        AutoFindReferences();
        ForceVisualDefaults();
    }

    public void Refresh(bool selected)
    {
        AutoFindReferences();
        ForceVisualDefaults();

        bool isHotbar = InventoryManager.Instance != null &&
                        InventoryManager.Instance.IsHotbarSlot(slotIndex);

        if (backgroundImage != null)
        {
            backgroundImage.color = selected
                ? selectedColor
                : isHotbar ? hotbarColor : normalColor;
        }

        if (slotNumberText != null)
        {
            slotNumberText.gameObject.SetActive(isHotbar);
            slotNumberText.text = isHotbar ? (slotIndex + 1).ToString() : "";
        }

        if (slot == null || slot.IsEmpty())
        {
            SetEmpty();
            return;
        }

        ItemData item = slot.itemData;

        if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(item.itemIcon != null);
            itemIconImage.enabled = item.itemIcon != null;
            itemIconImage.sprite = item.itemIcon;
            itemIconImage.color = Color.white;
        }

        if (amountText != null)
        {
            bool showAmount = slot.amount > 1;
            amountText.gameObject.SetActive(showAmount);
            amountText.enabled = showAmount;
            amountText.text = showAmount ? slot.amount.ToString() : "";
        }
    }

    private void SetEmpty()
    {
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.enabled = false;
            itemIconImage.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.enabled = false;
            amountText.gameObject.SetActive(false);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventoryUI.SelectSlot(slotIndex);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryUI.DropInventorySlot(slotIndex);
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (inventoryUI == null || slot == null || slot.IsEmpty())
        {
            return;
        }

        isDragging = true;
        inventoryUI.BeginDragFromInventory(slotIndex, slot.itemData, slot.amount);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || inventoryUI == null)
        {
            return;
        }

        inventoryUI.UpdateDrag(eventData.position);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || inventoryUI == null)
        {
            return;
        }

        isDragging = false;
        inventoryUI.EndDrag();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            return;
        }

        inventoryUI.TryDropDraggedItemOnInventorySlot(slotIndex);
    }
}