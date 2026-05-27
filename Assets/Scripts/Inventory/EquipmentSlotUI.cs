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

    private ItemData equippedItem;
    private InventoryUI inventoryUI;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        AutoFindReferences();

        ColorUtility.TryParseHtmlString("#2E2417", out normalColor);
        ColorUtility.TryParseHtmlString("#3A2F1D", out occupiedColor);
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);

        ForceVisualDefaults();
    }

    private void AutoFindReferences()
    {
        if (itemIconImage == null)
        {
            Transform icon = transform.Find("EquipmentIconImage");

            if (icon != null)
            {
                itemIconImage = icon.GetComponent<Image>();
            }
        }

        if (slotLabelText == null)
        {
            Transform label = transform.Find("SlotLabelText");

            if (label != null)
            {
                slotLabelText = label.GetComponent<TMP_Text>();
            }
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

    public void Setup(InventoryUI ui)
    {
        inventoryUI = ui;
        AutoFindReferences();
        ForceVisualDefaults();
        Refresh();
    }

    public void SetItem(ItemData item)
    {
        equippedItem = item;
        Refresh();
    }

    public ItemData GetItem()
    {
        return equippedItem;
    }

    public bool IsEmpty()
    {
        return equippedItem == null;
    }

    public bool CanAccept(ItemData item)
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

    public void Refresh()
    {
        AutoFindReferences();
        ForceVisualDefaults();

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
            if (equippedItem == null)
            {
                slotLabelText.gameObject.SetActive(true);
                slotLabelText.text = GetShortLabel();
            }
            else
            {
                slotLabelText.text = "";
                slotLabelText.gameObject.SetActive(false);
            }
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