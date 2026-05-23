using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Inventory/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identity")]
    public string itemID;
    public string itemName;

    [TextArea(3, 6)]
    public string itemDescription;

    [Header("Classification")]
    public ItemType itemType = ItemType.Misc;

    [Header("Icon / Visual")]
    public Sprite itemIcon;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 99;

    [Header("Usage")]
    public bool canUse;
    public bool canDrop = true;

    [TextArea(2, 4)]
    public string useMessage = "Used item.";

    [Header("Quest / Story")]
    public bool isImportant;
}