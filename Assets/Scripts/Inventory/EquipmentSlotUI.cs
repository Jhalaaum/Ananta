using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("Slot")]
    public EquipmentSlotType slotType;
    public int accessoryIndex = -1;

    [Header("UI")]
    public Image backgroundImage;
    public Image itemIconImage;
    public TMP_Text slotLabelText;

    [Header("Colors")]
    public Color normalColor = new Color(0.18f, 0.14f, 0.09f, 1f);
    public Color selectedColor;

    private ItemData equippedItem;
    private InventoryUI inventoryUI;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);
        Refresh();
    }

    public void Setup(InventoryUI ui)
    {
        inventoryUI = ui;
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
        if (item == null)
        {
            return false;
        }

        if (!item.canEquip)
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
        if (backgroundImage != null)
        {
            backgroundImage.color = normalColor;
        }

        if (itemIconImage != null)
        {
            if (equippedItem != null && equippedItem.itemIcon != null)
            {
                itemIconImage.gameObject.SetActive(true);
                itemIconImage.sprite = equippedItem.itemIcon;
            }
            else
            {
                itemIconImage.sprite = null;
                itemIconImage.gameObject.SetActive(false);
            }
        }

        if (slotLabelText != null)
        {
            if (equippedItem != null)
            {
                slotLabelText.text = "";
            }
            else
            {
                slotLabelText.text = GetShortLabel();
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

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            inventoryUI.UnequipSlot(this);
        }
    }
}