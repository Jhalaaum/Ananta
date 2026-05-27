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

    [Header("Icon")]
    public Sprite itemIcon;

    [Header("Stacking")]
    public bool isStackable = true;
    public int maxStackSize = 99;

    [Header("Use / Drop")]
    public bool canUse;
    public bool canDrop = true;

    [TextArea(2, 4)]
    public string useMessage = "Used item.";

    [Header("Important")]
    public bool isImportant;

    [Header("Equipment / Disguise")]
    public bool canEquip;
    public EquipmentSlotType equipmentSlotType;
    public bool countsAsDisguisePiece;
    public string disguiseSetID;
}