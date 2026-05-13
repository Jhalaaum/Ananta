using System;

[Serializable]
public class QuestState
{
    public QuestData questData;
    public int currentStepIndex;
    public int currentAmount;
    public bool isStarted;
    public bool isCompleted;

    public QuestState(QuestData data)
    {
        questData = data;
        currentStepIndex = 0;
        currentAmount = 0;
        isStarted = true;
        isCompleted = false;
    }

    public QuestStepData GetCurrentStep()
    {
        if (questData == null || questData.steps == null)
        {
            return null;
        }

        if (currentStepIndex < 0 || currentStepIndex >= questData.steps.Length)
        {
            return null;
        }

        return questData.steps[currentStepIndex];
    }

    public bool IsOnFinalStep()
    {
        if (questData == null || questData.steps == null)
        {
            return false;
        }

        return currentStepIndex >= questData.steps.Length - 1;
    }
}