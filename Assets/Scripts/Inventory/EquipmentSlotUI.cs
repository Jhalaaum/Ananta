using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour,
    IPointerClickHandler,
    IDropHandler
{
    [Header("Slot")]
    public EquipmentSlotType slotType;
    public int accessoryIndex = -1;

    [Header("UI")]
    public Image backgroundImage;
    public Image itemIconImage;
    public TMP_Text slotLabelText;

    [Header("Colors")]
    public Color normalColor;
    public Color occupiedColor;
    public Color selectedColor;

    private InventoryUI inventoryUI;

    private void Awake()
    {
        AutoFindReferences();

        ColorUtility.TryParseHtmlString("#2E2417", out normalColor);
        ColorUtility.TryParseHtmlString("#3A2F1D", out occupiedColor);
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);

        ForceVisualDefaults();
    }

    public void Setup(InventoryUI ui)
    {
        inventoryUI = ui;
        AutoFindReferences();
        ForceVisualDefaults();
        Refresh();
    }

    private void AutoFindReferences()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        if (itemIconImage == null)
        {
            Transform icon = transform.Find("EquipmentIconImage");
            if (icon != null) itemIconImage = icon.GetComponent<Image>();
        }

        if (slotLabelText == null)
        {
            Transform label = transform.Find("SlotLabelText");
            if (label != null) slotLabelText = label.GetComponent<TMP_Text>();
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

        if (slotLabelText != null)
        {
            slotLabelText.raycastTarget = false;
            slotLabelText.color = new Color(0.85f, 0.78f, 0.62f, 1f);
            slotLabelText.alpha = 1f;
        }
    }

    public ItemData GetItem()
    {
        if (EquipmentManager.Instance == null)
        {
            return null;
        }

        return EquipmentManager.Instance.GetEquippedItem(slotType, accessoryIndex);
    }

    public bool IsEmpty()
    {
        return GetItem() == null;
    }

    public bool CanAccept(ItemData item)
    {
        if (EquipmentManager.Instance == null)
        {
            return false;
        }

        return EquipmentManager.Instance.CanAccept(item, slotType);
    }

    public void Refresh()
    {
        AutoFindReferences();
        ForceVisualDefaults();

        ItemData equippedItem = GetItem();

        if (backgroundImage != null)
        {
            backgroundImage.color = equippedItem == null ? normalColor : occupiedColor;
        }

        if (itemIconImage != null)
        {
            if (equippedItem != null && equippedItem.itemIcon != null)
            {
                itemIconImage.gameObject.SetActive(true);
                itemIconImage.enabled = true;
                itemIconImage.sprite = equippedItem.itemIcon;
                itemIconImage.color = Color.white;
            }
            else
            {
                itemIconImage.sprite = null;
                itemIconImage.enabled = false;
                itemIconImage.gameObject.SetActive(false);
            }
        }

        if (slotLabelText != null)
        {
            bool empty = equippedItem == null;
            slotLabelText.gameObject.SetActive(empty);
            slotLabelText.text = empty ? GetShortLabel() : "";
        }
    }

    private string GetShortLabel()
    {
        switch (slotType)
        {
            case EquipmentSlotType.Head:
                return "Head";
            case EquipmentSlotType.Body:
                return "Body";
            case EquipmentSlotType.Legs:
                return "Legs";
            case EquipmentSlotType.Feet:
                return "Feet";
            case EquipmentSlotType.Accessory:
                return "Acc";
            default:
                return "";
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            return;
        }

        inventoryUI.TryDropDraggedItemOnEquipmentSlot(this);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (inventoryUI == null)
        {
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            inventoryUI.SelectEquipmentSlot(this);
        }
        else if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryUI.UnequipSlot(this);
        }
    }
}