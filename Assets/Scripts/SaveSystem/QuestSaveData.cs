using System;
using System.Collections.Generic;

[Serializable]
public class QuestSaveData
{
    public string questID;
    public int currentStepIndex;
    public int currentAmount;
    public bool isStarted;
    public bool isCompleted;
}

[Serializable]
public class QuestSaveCollection
{
    public List<QuestSaveData> quests = new List<QuestSaveData>();
}