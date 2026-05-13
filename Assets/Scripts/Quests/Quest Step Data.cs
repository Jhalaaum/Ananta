using UnityEngine;

[System.Serializable]
public class QuestStepData
{
    [Header("Step Info")]
    public string stepName;

    [TextArea(2, 4)]
    public string objectiveDescription;

    [Header("Objective")]
    public QuestObjectiveType objectiveType;

    [Tooltip("Unique ID of NPC, item, location, guard zone, or action target.")]
    public string targetID;

    [Tooltip("How many times this objective must be completed. Usually 1.")]
    public int requiredAmount = 1;

    [Header("Optional Polish")]
    public bool showObjectiveMarker = true;

    [TextArea(2, 4)]
    public string stepStartMessage;

    [TextArea(2, 4)]
    public string stepCompleteMessage;

    [Header("Step Rewards")]
    public QuestRewardData[] rewards;
}