using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HotbarSlotUI : MonoBehaviour
{
    [Header("UI")]
    public Image backgroundImage;
    public Image itemIconImage;
    public TMP_Text amountText;
    public TMP_Text slotNumberText;

    [Header("Colors")]
    public Color normalColor;
    public Color selectedColor;
    public Color amountColor = Color.white;
    public Color slotNumberColor = new Color(0.75f, 0.68f, 0.55f, 1f);

    private int slotIndex;

    private void Awake()
    {
        AutoFindReferences();

        ColorUtility.TryParseHtmlString("#2E2417", out normalColor);
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedColor);

        ForceDefaults();
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

            if (icon != null)
            {
                itemIconImage = icon.GetComponent<Image>();
            }
        }

        if (amountText == null)
        {
            Transform amount = transform.Find("ItemAmountText");

            if (amount != null)
            {
                amountText = amount.GetComponent<TMP_Text>();
            }
        }

        if (slotNumberText == null)
        {
            Transform number = transform.Find("SlotNumberText");

            if (number != null)
            {
                slotNumberText = number.GetComponent<TMP_Text>();
            }
        }
    }

    private void ForceDefaults()
    {
        if (backgroundImage != null)
        {
            backgroundImage.enabled = true;
            backgroundImage.raycastTarget = false;
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
            amountText.color = amountColor;
            amountText.alpha = 1f;
            amountText.fontSize = 16;
            amountText.alignment = TextAlignmentOptions.BottomRight;
        }

        if (slotNumberText != null)
        {
            slotNumberText.raycastTarget = false;
            slotNumberText.color = slotNumberColor;
            slotNumberText.alpha = 1f;
            slotNumberText.fontSize = 12;
        }
    }

    public void Setup(int slotIndex)
    {
        this.slotIndex = slotIndex;

        if (slotNumberText != null)
        {
            slotNumberText.gameObject.SetActive(true);
            slotNumberText.text = (slotIndex + 1).ToString();
        }
    }

    public void Refresh(InventorySlot slot, bool selected)
    {
        AutoFindReferences();
        ForceDefaults();

        if (backgroundImage != null)
        {
            backgroundImage.color = selected ? selectedColor : normalColor;
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
}