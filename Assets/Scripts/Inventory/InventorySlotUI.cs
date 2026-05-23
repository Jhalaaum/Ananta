using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    public Image backgroundImage;
    public Image itemIconImage;
    public TMP_Text amountText;
    public TMP_Text slotNumberText;

    [Header("Colors")]
    public Color normalColor = new Color(0.18f, 0.14f, 0.09f, 1f);
    public Color hoverColor = new Color(0.28f, 0.22f, 0.14f, 1f);
    public Color selectedColor;

    private InventoryUI inventoryUI;
    private InventorySlot slot;
    private int slotIndex;

    private void Awake()
    {
        if (backgroundImage == null)
        {
            backgroundImage = GetComponent<Image>();
        }

        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);
    }

    public void Setup(InventoryUI inventoryUI, InventorySlot slot, int slotIndex)
    {
        this.inventoryUI = inventoryUI;
        this.slot = slot;
        this.slotIndex = slotIndex;

        Refresh(false);
    }

    public void Refresh(bool selected)
    {
        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
        }

        if (slotNumberText != null)
        {
            slotNumberText.text = (slotIndex + 1).ToString();
        }

        if (slot == null || slot.IsEmpty())
        {
            SetEmpty();
            return;
        }

        if (itemIconImage != null)
        {
            itemIconImage.gameObject.SetActive(true);
            itemIconImage.sprite = slot.itemData.itemIcon;
            itemIconImage.enabled = slot.itemData.itemIcon != null;
        }

        if (amountText != null)
        {
            bool showAmount = slot.itemData.isStackable && slot.amount > 1;
            amountText.gameObject.SetActive(showAmount);
            amountText.text = showAmount ? slot.amount.ToString() : "";
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
            inventoryUI.QuickUseOrDrop(slotIndex);
        }
    }

    private void SetEmpty()
    {
        if (itemIconImage != null)
        {
            itemIconImage.sprite = null;
            itemIconImage.gameObject.SetActive(false);
        }

        if (amountText != null)
        {
            amountText.text = "";
            amountText.gameObject.SetActive(false);
        }
    }
}