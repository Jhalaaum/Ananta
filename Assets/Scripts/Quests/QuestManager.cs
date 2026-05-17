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

        if (string.IsNullOrWhiteSpace(questData.questID))
        {
            Debug.LogWarning($"Cannot start quest '{questData.name}'. Quest ID is empty.");
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

        if (questData.steps == null || questData.steps.Length == 0)
        {
            Debug.LogWarning($"Cannot start quest '{questData.questName}'. It has no steps.");
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
    public QuestSaveCollection CaptureQuestProgress()
    {
        QuestSaveCollection collection = new QuestSaveCollection();

        foreach (QuestState state in activeQuests)
        {
            if (state == null || state.questData == null)
            {
                continue;
            }

            QuestSaveData saveData = new QuestSaveData
            {
                questID = state.questData.questID,
                currentStepIndex = state.currentStepIndex,
                currentAmount = state.currentAmount,
                isStarted = state.isStarted,
                isCompleted = state.isCompleted
            };

            collection.quests.Add(saveData);
        }

        foreach (string completedQuestID in completedQuestIDs)
        {
            bool alreadyIncluded = false;

            foreach (QuestSaveData quest in collection.quests)
            {
                if (quest.questID == completedQuestID)
                {
                    alreadyIncluded = true;
                    quest.isCompleted = true;
                    break;
                }
            }

            if (!alreadyIncluded)
            {
                collection.quests.Add(new QuestSaveData
                {
                    questID = completedQuestID,
                    currentStepIndex = 0,
                    currentAmount = 0,
                    isStarted = true,
                    isCompleted = true
                });
            }
        }

        return collection;
    }
    public void RestoreQuestProgress(QuestSaveCollection saveCollection)
    {
        activeQuests.Clear();
        completedQuestIDs.Clear();

        if (saveCollection == null || saveCollection.quests == null)
        {
            return;
        }

        foreach (QuestSaveData saveData in saveCollection.quests)
        {
            if (saveData == null || string.IsNullOrWhiteSpace(saveData.questID))
            {
                continue;
            }

            QuestData questData = null;

            if (QuestDatabase.Instance != null)
            {
                questData = QuestDatabase.Instance.GetQuestByID(saveData.questID);
            }

            if (questData == null)
            {
                Debug.LogWarning($"[QuestManager] Could not restore quest. Missing quest data for ID: {saveData.questID}");
                continue;
            }

            QuestState restoredState = new QuestState(questData)
            {
                currentStepIndex = saveData.currentStepIndex,
                currentAmount = saveData.currentAmount,
                isStarted = saveData.isStarted,
                isCompleted = saveData.isCompleted
            };

            activeQuests.Add(restoredState);

            if (saveData.isCompleted && !completedQuestIDs.Contains(saveData.questID))
            {
                completedQuestIDs.Add(saveData.questID);
            }

            if (!saveData.isCompleted)
            {
                OnQuestStarted?.Invoke(restoredState);

                QuestStepData currentStep = restoredState.GetCurrentStep();

                if (currentStep != null)
                {
                    OnQuestStepStarted?.Invoke(currentStep);
                }

                OnQuestUpdated?.Invoke(restoredState);
            }
        }
    }
    public void ProgressObjective(QuestObjectiveType objectiveType, string targetID, int amount = 1)
    {
        if (string.IsNullOrWhiteSpace(targetID))
        {
            Debug.LogWarning("ProgressObjective called with empty targetID.");
            return;
        }

        if (amount <= 0)
        {
            Debug.LogWarning("ProgressObjective called with amount less than or equal to zero.");
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

            Log(
                $"Quest progress: {questState.questData.questName} | " +
                $"{questState.currentAmount}/{currentStep.requiredAmount}"
            );

            OnQuestUpdated?.Invoke(questState);

            if (questState.currentAmount >= currentStep.requiredAmount)
            {
                CompleteCurrentStep(questState);
            }

            return;
        }

        Log($"No matching active objective found. Type: {objectiveType}, Target: {targetID}");
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

        GiveStepRewards(completedStep);

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

        GiveQuestCompletionRewards(questState.questData);

        OnQuestCompleted?.Invoke(questState);
        OnQuestUpdated?.Invoke(questState);

        if (questState.questData.nextQuestToUnlock != null)
        {
            StartQuest(questState.questData.nextQuestToUnlock);
        }
    }

    private void GiveStepRewards(QuestStepData completedStep)
    {
        if (completedStep == null)
        {
            return;
        }

        if (completedStep.rewards == null || completedStep.rewards.Length == 0)
        {
            return;
        }

        if (QuestRewardManager.Instance == null)
        {
            Debug.LogWarning(
                $"Step '{completedStep.stepName}' has rewards, but no QuestRewardManager exists in the scene."
            );
            return;
        }

        QuestRewardManager.Instance.GiveRewards(completedStep.rewards);
    }

    private void GiveQuestCompletionRewards(QuestData questData)
    {
        if (questData == null)
        {
            return;
        }

        if (questData.completionRewards == null || questData.completionRewards.Length == 0)
        {
            return;
        }

        if (QuestRewardManager.Instance == null)
        {
            Debug.LogWarning(
                $"Quest '{questData.questName}' has completion rewards, but no QuestRewardManager exists in the scene."
            );
            return;
        }

        QuestRewardManager.Instance.GiveRewards(questData.completionRewards);
    }

    public QuestState GetActiveQuest(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID))
        {
            return null;
        }

        foreach (QuestState state in activeQuests)
        {
            if (state == null || state.questData == null)
            {
                continue;
            }

            if (state.questData.questID == questID && !state.isCompleted)
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

    public List<QuestState> GetActiveQuests()
    {
        return activeQuests;
    }

    public bool IsQuestActive(string questID)
    {
        return GetActiveQuest(questID) != null;
    }

    public bool IsQuestCompleted(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID))
        {
            return false;
        }

        return completedQuestIDs.Contains(questID);
    }

    public void ForceCompleteQuest(string questID)
    {
        QuestState questState = GetActiveQuest(questID);

        if (questState == null)
        {
            Debug.LogWarning($"Cannot force complete quest. No active quest found with ID: {questID}");
            return;
        }

        CompleteQuest(questState);
    }

    public void ResetQuestProgress(string questID)
    {
        QuestState questState = GetActiveQuest(questID);

        if (questState == null)
        {
            Debug.LogWarning($"Cannot reset quest. No active quest found with ID: {questID}");
            return;
        }

        questState.currentStepIndex = 0;
        questState.currentAmount = 0;
        questState.isCompleted = false;

        QuestStepData firstStep = questState.GetCurrentStep();

        if (firstStep != null)
        {
            OnQuestStepStarted?.Invoke(firstStep);
        }

        OnQuestUpdated?.Invoke(questState);

        Log($"Quest reset: {questState.questData.questName}");
    }

    public void RemoveCompletedQuestFromActiveList(string questID)
    {
        for (int i = activeQuests.Count - 1; i >= 0; i--)
        {
            QuestState state = activeQuests[i];

            if (state == null || state.questData == null)
            {
                continue;
            }

            if (state.questData.questID == questID && state.isCompleted)
            {
                activeQuests.RemoveAt(i);
                return;
            }
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[QuestManager] {message}");
        }
    }
}