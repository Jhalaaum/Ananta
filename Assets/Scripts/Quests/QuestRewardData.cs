using UnityEngine;

[System.Serializable]
public class QuestRewardData
{
    [Header("Reward")]
    public QuestRewardType rewardType;

    [Tooltip(
        "Generic ID used for quests, areas, dialogue, etc. " +
        "For item rewards, prefer assigning Item Reward below."
    )]
    public string rewardID;

    [Tooltip(
        "Generic amount: item count, coins, reputation, etc."
    )]
    public int amount = 1;

    [Header("Item Reward")]
    [Tooltip(
        "For GiveItem or GiveDisguise, drag the ItemData asset here."
    )]
    public ItemData itemReward;

    [Header("Optional Display")]
    public bool showMessage = true;

    [TextArea(2, 4)]
    public string rewardMessage;

    [Header("Optional Scene Object")]
    public GameObject targetObject;
}