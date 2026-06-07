using UnityEngine;

public enum InventoryItemRole
{
    General,
    Evidence,
    Disguise,
    Tool,
    Consumable,
    QuestItem,
    Key,
    Valuable,
    Document
}

public enum ItemSecurityType
{
    HardEvidence,     // usually not confiscated
    SoftItem,         // disguise/tools can be confiscated
    Normal
}

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
    public InventoryItemRole itemRole = InventoryItemRole.General;
    public ItemSecurityType securityType = ItemSecurityType.Normal;

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

    [Tooltip("Examples: guard, merchant, servant, monk, noble")]
    public string disguiseSetID;

    [Header("Disguise Quality")]
    [Range(0f, 1f)]
    public float disguiseQuality = 1f;

    [Tooltip("For guards later: suspicious if worn in wrong zone/faction.")]
    public string factionOrRoleID;
}