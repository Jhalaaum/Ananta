using UnityEngine;

[System.Serializable]
public class QuestRewardData
{
    [Header("Reward")]
    public QuestRewardType rewardType;

    [Tooltip("Generic ID: item ID, area ID, skill ID, dialogue ID, quest ID, faction ID, journal ID, etc.")]
    public string rewardID;

    [Tooltip("Generic amount: coins, reputation amount, item count, suspicion change, etc.")]
    public int amount = 1;

    [Header("Optional Display")]
    public bool showMessage = true;

    [TextArea(2, 4)]
    public string rewardMessage;

    [Header("Optional Scene Object")]
    public GameObject targetObject;
}