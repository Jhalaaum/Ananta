using System;
using System.Collections.Generic;
using UnityEngine;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Debug")]
    public bool showDebugLogs = true;

    [Header("Active Quests")]
    [SerializeField] private List<QuestState> activeQuests = new List<QuestState>();

    [Header("Completed Quest IDs")]
    [SerializeField] private List<string> completedQuestIDs = new List<string>();

    public event Action<QuestState> OnQuestStarted;
    public event Action<QuestState> OnQuestUpdated;
    public event Action<QuestState> OnQuestCompleted;
    public event Action<QuestStepData> OnQuestStepStarted;
    public event Action<QuestStepData> OnQuestStepCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void StartQuest(QuestData questData)
    {
        if (questData == null)
        {
            Debug.LogWarning("Cannot start quest. QuestData is null.");
            return;
        }

        if (IsQuestActive(questData.questID))
        {
            Log($"Quest already active: {questData.questName}");
            return;
        }

        if (IsQuestCompleted(questData.questID))
        {
            Log($"Quest already completed: {questData.questName}");
            return;
        }

        QuestState newState = new QuestState(questData);
        activeQuests.Add(newState);

        Log($"Quest started: {questData.questName}");

        OnQuestStarted?.Invoke(newState);

        QuestStepData firstStep = newState.GetCurrentStep();
        if (firstStep != null)
        {
            OnQuestStepStarted?.Invoke(firstStep);

            if (!string.IsNullOrWhiteSpace(firstStep.stepStartMessage))
            {
                Log(firstStep.stepStartMessage);
            }
        }

        OnQuestUpdated?.Invoke(newState);
    }

    public void ProgressObjective(QuestObjectiveType objectiveType, string targetID, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(targetID))
        {
            Debug.LogWarning("ProgressObjective called with empty targetID.");
            return;
        }

        for (int i = 0; i < activeQuests.Count; i++)
        {
            QuestState questState = activeQuests[i];

            if (questState == null || questState.isCompleted)
            {
                continue;
            }

            QuestStepData currentStep = questState.GetCurrentStep();

            if (currentStep == null)
            {
                continue;
            }

            bool objectiveMatches = currentStep.objectiveType == objectiveType;
            bool targetMatches = currentStep.targetID == targetID;

            if (!objectiveMatches || !targetMatches)
            {
                continue;
            }

            questState.currentAmount += amount;

            Log($"Quest progress: {questState.questData.questName} | {questState.currentAmount}/{currentStep.requiredAmount}");

            OnQuestUpdated?.Invoke(questState);

            if (questState.currentAmount >= currentStep.requiredAmount)
            {
                CompleteCurrentStep(questState);
            }

            return;
        }
    }

    private void CompleteCurrentStep(QuestState questState)
    {
        if (questState == null || questState.isCompleted)
        {
            return;
        }

        QuestStepData completedStep = questState.GetCurrentStep();

        if (completedStep == null)
        {
            return;
        }

        Log($"Step completed: {completedStep.stepName}");

        if (!string.IsNullOrWhiteSpace(completedStep.stepCompleteMessage))
        {
            Log(completedStep.stepCompleteMessage);
        }

        OnQuestStepCompleted?.Invoke(completedStep);

        if (!string.IsNullOrWhiteSpace(completedStep.unlockAreaID))
        {
            QuestAreaUnlocker.UnlockArea(completedStep.unlockAreaID);
        }

        if (questState.IsOnFinalStep())
        {
            CompleteQuest(questState);
            return;
        }

        questState.currentStepIndex++;
        questState.currentAmount = 0;

        QuestStepData nextStep = questState.GetCurrentStep();

        if (nextStep != null)
        {
            Log($"New objective: {nextStep.objectiveDescription}");

            if (!string.IsNullOrWhiteSpace(nextStep.stepStartMessage))
            {
                Log(nextStep.stepStartMessage);
            }

            OnQuestStepStarted?.Invoke(nextStep);
        }

        OnQuestUpdated?.Invoke(questState);
    }

    private void CompleteQuest(QuestState questState)
    {
        if (questState == null || questState.questData == null)
        {
            return;
        }

        questState.isCompleted = true;

        if (!completedQuestIDs.Contains(questState.questData.questID))
        {
            completedQuestIDs.Add(questState.questData.questID);
        }

        Log($"Quest completed: {questState.questData.questName}");

        if (!string.IsNullOrWhiteSpace(questState.questData.completionMessage))
        {
            Log(questState.questData.completionMessage);
        }

        OnQuestCompleted?.Invoke(questState);
        OnQuestUpdated?.Invoke(questState);

        if (questState.questData.nextQuestToUnlock != null)
        {
            StartQuest(questState.questData.nextQuestToUnlock);
        }
    }

    public QuestState GetActiveQuest(string questID)
    {
        foreach (QuestState state in activeQuests)
        {
            if (state.questData != null && state.questData.questID == questID && !state.isCompleted)
            {
                return state;
            }
        }

        return null;
    }

    public QuestState GetFirstActiveQuest()
    {
        foreach (QuestState state in activeQuests)
        {
            if (state != null && !state.isCompleted)
            {
                return state;
            }
        }

        return null;
    }

    public bool IsQuestActive(string questID)
    {
        return GetActiveQuest(questID) != null;
    }

    public bool IsQuestCompleted(string questID)
    {
        return completedQuestIDs.Contains(questID);
    }

    public List<QuestState> GetActiveQuests()
    {
        return activeQuests;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[QuestManager] {message}");
        }
    }
}