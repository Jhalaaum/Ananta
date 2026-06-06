using System;
using UnityEngine;

[Serializable]
public class DialogueCondition
{
    public DialogueConditionType conditionType;

    [Header("IDs")]
    public string flagID;
    public string factionID;
    public string itemID;
    public string questID;
    public string coverStoryID;

    [Header("Numbers")]
    public int requiredValue = 1;

    public bool IsMet()
    {
        switch (conditionType)
        {
            case DialogueConditionType.None:
                return true;

            case DialogueConditionType.RequiresFlag:
                return DialogueFlagManager.Instance != null &&
                       DialogueFlagManager.Instance.HasFlag(flagID);

            case DialogueConditionType.ForbidsFlag:
                return DialogueFlagManager.Instance == null ||
                       !DialogueFlagManager.Instance.HasFlag(flagID);

            case DialogueConditionType.RequiresFactionTrustAtLeast:
                return FactionTrustManager.Instance != null &&
                       FactionTrustManager.Instance.GetTrust(factionID) >= requiredValue;

            case DialogueConditionType.RequiresFactionTrustBelow:
                return FactionTrustManager.Instance != null &&
                       FactionTrustManager.Instance.GetTrust(factionID) < requiredValue;

            case DialogueConditionType.RequiresItem:
                return HasItem(itemID);

            case DialogueConditionType.RequiresQuestActive:
                return QuestManager.Instance != null &&
                       QuestManager.Instance.IsQuestActive(questID);

            case DialogueConditionType.RequiresQuestCompleted:
                return QuestManager.Instance != null &&
                       QuestManager.Instance.IsQuestCompleted(questID);

            case DialogueConditionType.RequiresCoverStory:
                return DialogueFlagManager.Instance != null &&
                       DialogueFlagManager.Instance.HasFlag("cover_" + coverStoryID);

            default:
                return true;
        }
    }

    private bool HasItem(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (InventoryManager.Instance == null)
        {
            return false;
        }

        foreach (InventorySlot slot in InventoryManager.Instance.GetSlots())
        {
            if (slot == null || slot.IsEmpty() || slot.itemData == null)
            {
                continue;
            }

            if (slot.itemData.itemID == id)
            {
                return true;
            }
        }

        return false;
    }
}