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

    public void GiveReward(QuestRewardData reward)
    {
        if (reward == null)
        {
            return;
        }

        ApplySimpleBuiltInReward(reward);

        OnRewardGiven?.Invoke(reward);

        if (showDebugLogs)
        {
            Debug.Log(BuildDebugMessage(reward));
        }
    }

    private void ApplySimpleBuiltInReward(QuestRewardData reward)
    {
        switch (reward.rewardType)
        {
            case QuestRewardType.EnableObject:
                if (reward.targetObject != null)
                {
                    reward.targetObject.SetActive(true);
                }
                break;

            case QuestRewardType.DisableObject:
                if (reward.targetObject != null)
                {
                    reward.targetObject.SetActive(false);
                }
                break;

            default:
                break;
        }
    }

    private string BuildDebugMessage(QuestRewardData reward)
    {
        return $"[QuestReward] Type: {reward.rewardType}, ID: {reward.rewardID}, Amount: {reward.amount}";
    }
}