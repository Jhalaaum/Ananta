using System;
using UnityEngine;

[Serializable]
public class DialogueEffect
{
    public DialogueEffectType effectType;

    [Header("IDs")]
    public string flagID;
    public string factionID;
    public string itemID;
    public string infoID;
    public string coverStoryID;

    [Header("Quest")]
    public QuestData questToStart;
    public QuestObjectiveType questObjectiveType;
    public string questTargetID;

    [Header("Numbers")]
    public int amount = 1;

    public void Apply()
    {
        switch (effectType)
        {
            case DialogueEffectType.None:
                return;

            case DialogueEffectType.SetFlag:
                DialogueFlagManager.Instance?.SetFlag(flagID);
                return;

            case DialogueEffectType.ClearFlag:
                DialogueFlagManager.Instance?.ClearFlag(flagID);
                return;

            case DialogueEffectType.AddFactionTrust:
                FactionTrustManager.Instance?.AddTrust(factionID, amount);
                return;

            case DialogueEffectType.RemoveFactionTrust:
                FactionTrustManager.Instance?.AddTrust(factionID, -amount);
                return;

            case DialogueEffectType.StartQuest:
                if (QuestManager.Instance != null && questToStart != null)
                {
                    QuestManager.Instance.StartQuest(questToStart);
                }
                return;

            case DialogueEffectType.ProgressQuestObjective:
                if (QuestManager.Instance != null)
                {
                    QuestManager.Instance.ProgressObjective(
                        questObjectiveType,
                        questTargetID,
                        Mathf.Max(1, amount)
                    );
                }
                return;

            case DialogueEffectType.GiveItem:
                if (InventoryManager.Instance != null)
                {
                    InventoryManager.Instance.AddItemByID(itemID, Mathf.Max(1, amount));
                }
                return;

            case DialogueEffectType.RemoveItem:
                RemoveItem(itemID, Mathf.Max(1, amount));
                return;

            case DialogueEffectType.UnlockInformation:
                DialogueFlagManager.Instance?.SetFlag("info_" + infoID);
                return;

            case DialogueEffectType.AddCoverStory:
                DialogueFlagManager.Instance?.SetFlag("cover_" + coverStoryID);
                return;

            case DialogueEffectType.EndConversation:
                DialogueManager.Instance?.EndDialogue();
                return;
        }
    }

    private void RemoveItem(string id, int amountToRemove)
    {
        if (InventoryManager.Instance == null)
        {
            return;
        }

        int remaining = amountToRemove;

        for (int i = 0; i < InventoryManager.Instance.GetSlots().Count; i++)
        {
            InventorySlot slot = InventoryManager.Instance.GetSlot(i);

            if (slot == null || slot.IsEmpty() || slot.itemData == null)
            {
                continue;
            }

            if (slot.itemData.itemID != id)
            {
                continue;
            }

            int removeNow = Mathf.Min(slot.amount, remaining);
            InventoryManager.Instance.RemoveFromSlot(i, removeNow);
            remaining -= removeNow;

            if (remaining <= 0)
            {
                return;
            }
        }
    }
}