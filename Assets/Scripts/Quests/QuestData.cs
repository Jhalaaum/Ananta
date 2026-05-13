using UnityEngine;

[CreateAssetMenu(fileName = "New Quest", menuName = "Quests/Quest Data")]
public class QuestData : ScriptableObject
{
    [Header("Quest Identity")]
    public string questID;
    public string questName;

    [TextArea(3, 6)]
    public string questDescription;

    [Header("Quest Steps")]
    public QuestStepData[] steps;

    [Header("Quest Completion")]
    [TextArea(2, 4)]
    public string completionMessage;

    [Header("Quest Completion Rewards")]
    public QuestRewardData[] completionRewards;

    [Header("Optional Next Quest")]
    public QuestData nextQuestToUnlock;
}