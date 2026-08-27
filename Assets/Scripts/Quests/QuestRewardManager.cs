using System;
using UnityEngine;

public class QuestRewardManager : MonoBehaviour
{
    public static QuestRewardManager Instance;

    public event Action<QuestRewardData> OnRewardGiven;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void GiveRewards(QuestRewardData[] rewards)
    {
        if (rewards == null || rewards.Length == 0)
        {
            return;
        }

        foreach (QuestRewardData reward in rewards)
        {
            GiveReward(reward);
        }
    }

    public bool GiveReward(QuestRewardData reward)
    {
        if (reward == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Cannot give null reward."
            );

            return false;
        }

        bool success = ApplyReward(reward);

        if (!success)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Reward FAILED. " +
                "Type: " + reward.rewardType +
                ", ID: " + reward.rewardID
            );

            return false;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[QuestRewardManager] Reward SUCCESS. " +
                "Type: " + reward.rewardType +
                ", ID: " + reward.rewardID +
                ", Amount: " + reward.amount
            );
        }

        OnRewardGiven?.Invoke(reward);

        return true;
    }

    private bool ApplyReward(QuestRewardData reward)
    {
        switch (reward.rewardType)
        {
            case QuestRewardType.GiveItem:
                return GiveItemReward(reward);

            case QuestRewardType.GiveDisguise:
                return GiveDisguiseReward(reward);

            case QuestRewardType.UnlockQuest:
                return UnlockQuestReward(reward);

            case QuestRewardType.EnableObject:
                return EnableObjectReward(reward);

            case QuestRewardType.DisableObject:
                return DisableObjectReward(reward);

            case QuestRewardType.UnlockArea:
                return UnlockAreaReward(reward);

            case QuestRewardType.GiveInformation:
                return GiveInformationReward(reward);

            case QuestRewardType.UnlockDialogue:
                return UnlockDialogueReward(reward);

            case QuestRewardType.Custom:
                return CustomReward(reward);

            case QuestRewardType.GiveCurrency:
            case QuestRewardType.GiveSkill:
            case QuestRewardType.IncreaseReputation:
            case QuestRewardType.DecreaseSuspicion:
            case QuestRewardType.UnlockVendor:
            case QuestRewardType.AddJournalEntry:
                Debug.LogWarning(
                    "[QuestRewardManager] Reward type not supported yet because " +
                    "its gameplay system has not been implemented: " +
                    reward.rewardType
                );

                return false;

            default:
                Debug.LogWarning(
                    "[QuestRewardManager] Unknown reward type: " +
                    reward.rewardType
                );

                return false;
        }
    }

    // --------------------------------------------------
    // GIVE ITEM
    // --------------------------------------------------

    private bool GiveItemReward(QuestRewardData reward)
    {
        if (InventoryManager.Instance == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] InventoryManager is missing."
            );

            return false;
        }

        int amount = Mathf.Max(1, reward.amount);

        ItemData item = reward.itemReward;

        if (item == null &&
            !string.IsNullOrWhiteSpace(reward.rewardID))
        {
            if (ItemDatabase.Instance == null)
            {
                Debug.LogWarning(
                    "[QuestRewardManager] ItemDatabase is missing."
                );

                return false;
            }

            item = ItemDatabase.Instance.GetItemByID(
                reward.rewardID.Trim()
            );
        }

        if (item == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] GiveItem has no valid ItemData."
            );

            return false;
        }

        return InventoryManager.Instance.AddItem(
            item,
            amount
        );
    }

    // --------------------------------------------------
    // GIVE DISGUISE
    //
    // Disguise pieces are normal ItemData assets in your
    // current architecture.
    // --------------------------------------------------

    private bool GiveDisguiseReward(QuestRewardData reward)
    {
        ItemData item = reward.itemReward;

        if (item == null &&
            !string.IsNullOrWhiteSpace(reward.rewardID) &&
            ItemDatabase.Instance != null)
        {
            item = ItemDatabase.Instance.GetItemByID(
                reward.rewardID.Trim()
            );
        }

        if (item == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] GiveDisguise has no valid ItemData."
            );

            return false;
        }

        if (!item.countsAsDisguisePiece)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Item '" +
                item.itemName +
                "' is not marked as a disguise piece."
            );

            return false;
        }

        if (InventoryManager.Instance == null)
        {
            return false;
        }

        int amount = Mathf.Max(1, reward.amount);

        return InventoryManager.Instance.AddItem(
            item,
            amount
        );
    }

    // --------------------------------------------------
    // UNLOCK QUEST
    // --------------------------------------------------

    private bool UnlockQuestReward(QuestRewardData reward)
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] QuestManager is missing."
            );

            return false;
        }

        if (QuestDatabase.Instance == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] QuestDatabase is missing."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(reward.rewardID))
        {
            Debug.LogWarning(
                "[QuestRewardManager] UnlockQuest has no quest ID."
            );

            return false;
        }

        string questID = reward.rewardID.Trim();

        if (QuestManager.Instance.IsQuestActive(questID))
        {
            return true;
        }

        if (QuestManager.Instance.IsQuestCompleted(questID))
        {
            return true;
        }

        QuestData questData =
            QuestDatabase.Instance.GetQuestByID(
                questID
            );

        if (questData == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] Quest not found: " +
                questID
            );

            return false;
        }

        QuestManager.Instance.StartQuest(
            questData
        );

        return true;
    }

    // --------------------------------------------------
    // ENABLE OBJECT
    // --------------------------------------------------

    private bool EnableObjectReward(QuestRewardData reward)
    {
        if (reward.targetObject == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] EnableObject has no targetObject."
            );

            return false;
        }

        reward.targetObject.SetActive(true);

        return true;
    }

    // --------------------------------------------------
    // DISABLE OBJECT
    // --------------------------------------------------

    private bool DisableObjectReward(QuestRewardData reward)
    {
        if (reward.targetObject == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] DisableObject has no targetObject."
            );

            return false;
        }

        reward.targetObject.SetActive(false);

        return true;
    }

    // --------------------------------------------------
    // UNLOCK AREA
    //
    // For now this uses a GameObject as the locked barrier/
    // entrance/object associated with an area.
    //
    // Example:
    // targetObject = invisible gate blocking Palace
    //
    // Completing quest disables that blocker.
    // --------------------------------------------------

    private bool UnlockAreaReward(QuestRewardData reward)
    {
        if (reward.targetObject == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] UnlockArea requires a targetObject."
            );

            return false;
        }

        reward.targetObject.SetActive(false);

        return true;
    }

    // --------------------------------------------------
    // GIVE INFORMATION
    //
    // There is no persistent knowledge/journal system yet.
    // We can still treat the successful reward as the
    // message/event itself.
    // --------------------------------------------------

    private bool GiveInformationReward(QuestRewardData reward)
    {
        if (string.IsNullOrWhiteSpace(reward.rewardMessage) &&
            string.IsNullOrWhiteSpace(reward.rewardID))
        {
            Debug.LogWarning(
                "[QuestRewardManager] GiveInformation has no message or ID."
            );

            return false;
        }

        // OnRewardGiven will cause QuestRewardPopupUI
        // to display the information message.
        return true;
    }

    // --------------------------------------------------
    // UNLOCK DIALOGUE
    //
    // Your current dialogue system has conditional dialogue,
    // but I do not see a global dialogue-unlock database yet.
    //
    // So, for now, use targetObject for dialogue-related
    // objects/components that should become available.
    // --------------------------------------------------

    private bool UnlockDialogueReward(QuestRewardData reward)
    {
        if (reward.targetObject == null)
        {
            Debug.LogWarning(
                "[QuestRewardManager] UnlockDialogue currently requires " +
                "a targetObject representing the unlocked dialogue/NPC."
            );

            return false;
        }

        reward.targetObject.SetActive(true);

        return true;
    }

    // --------------------------------------------------
    // CUSTOM
    //
    // Simple generic object-based custom reward.
    // --------------------------------------------------

    private bool CustomReward(QuestRewardData reward)
    {
        if (reward.targetObject != null)
        {
            reward.targetObject.SetActive(true);
            return true;
        }

        if (!string.IsNullOrWhiteSpace(reward.rewardMessage))
        {
            return true;
        }

        Debug.LogWarning(
            "[QuestRewardManager] Custom reward has no target or message."
        );

        return false;
    }
}