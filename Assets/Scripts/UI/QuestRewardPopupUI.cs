using System.Collections;
using TMPro;
using UnityEngine;
using System.Collections.Generic;


public class QuestRewardPopupUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject rewardPanel;
    public TMP_Text rewardText;

    [Header("Timing")]
    public float displayTime = 2.2f;
    public float gapBetweenRewards = 0.25f;

    private Coroutine queueRoutine;
    private readonly Queue<string> messageQueue = new Queue<string>();

    private void Start()
    {
        if (QuestRewardManager.Instance != null)
        {
            QuestRewardManager.Instance.OnRewardGiven += HandleRewardGiven;
        }

        HideInstant();
    }

    private void OnDestroy()
    {
        if (QuestRewardManager.Instance != null)
        {
            QuestRewardManager.Instance.OnRewardGiven -= HandleRewardGiven;
        }
    }

    private void HandleRewardGiven(QuestRewardData reward)
    {
        if (reward == null || !reward.showMessage)
        {
            return;
        }

        string message = reward.rewardMessage;

        if (string.IsNullOrWhiteSpace(message))
        {
            message = BuildDefaultMessage(reward);
        }

        EnqueueMessage(message);
    }

    private void EnqueueMessage(string message)
    {
        messageQueue.Enqueue(message);

        if (queueRoutine == null)
        {
            queueRoutine = StartCoroutine(ProcessQueue());
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (messageQueue.Count > 0)
        {
            string message = messageQueue.Dequeue();

            ShowInstant(message);

            yield return new WaitForSeconds(displayTime);

            HideInstant();

            yield return new WaitForSeconds(gapBetweenRewards);
        }

        queueRoutine = null;
    }

    private void ShowInstant(string message)
    {
        if (rewardPanel == null || rewardText == null)
        {
            Debug.LogWarning("QuestRewardPopupUI is missing rewardPanel or rewardText.");
            return;
        }

        rewardPanel.SetActive(true);
        rewardText.text = message;
    }

    private void HideInstant()
    {
        if (rewardPanel != null)
        {
            rewardPanel.SetActive(false);
        }
    }

    private string BuildDefaultMessage(QuestRewardData reward)
    {
        switch (reward.rewardType)
        {
            case QuestRewardType.UnlockArea:
                return $"Area unlocked: {reward.rewardID}";

            case QuestRewardType.GiveItem:
                return $"Received item: {reward.rewardID} x{reward.amount}";

            case QuestRewardType.GiveCurrency:
                return $"Received {reward.amount} {reward.rewardID}";

            case QuestRewardType.GiveSkill:
                return $"Skill unlocked: {reward.rewardID}";

            case QuestRewardType.GiveInformation:
                return "New information discovered";

            case QuestRewardType.UnlockDialogue:
                return "New dialogue unlocked";

            case QuestRewardType.UnlockQuest:
                return "New quest unlocked";

            case QuestRewardType.IncreaseReputation:
                return $"Reputation increased: {reward.rewardID} +{reward.amount}";

            case QuestRewardType.DecreaseSuspicion:
                return $"Suspicion decreased: {reward.rewardID} -{reward.amount}";

            case QuestRewardType.GiveDisguise:
                return $"Disguise unlocked: {reward.rewardID}";

            case QuestRewardType.UnlockVendor:
                return $"Vendor unlocked: {reward.rewardID}";

            case QuestRewardType.AddJournalEntry:
                return "Journal updated";

            case QuestRewardType.EnableObject:
                return "Something changed nearby";

            case QuestRewardType.DisableObject:
                return "Something changed nearby";

            case QuestRewardType.Custom:
                return "Reward received";

            default:
                return "Reward received";
        }
    }
}