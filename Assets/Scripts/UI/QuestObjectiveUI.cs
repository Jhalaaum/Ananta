using TMPro;
using UnityEngine;

public class QuestObjectiveUI : MonoBehaviour
{
    [Header("Objective Panel")]
    public GameObject questObjectivePanel;
    public TMP_Text questTitleText;
    public TMP_Text questDescriptionText;
    public TMP_Text objectiveHeaderText;
    public TMP_Text objectiveText;
    public TMP_Text progressText;

    [Header("Settings")]
    public bool hideWhenNoActiveQuest = true;

    private QuestState currentQuest;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            QuestManager.Instance.OnQuestStepStarted += HandleQuestStepStarted;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            QuestManager.Instance.OnQuestStepStarted -= HandleQuestStepStarted;
        }
    }

    private void HandleQuestStarted(QuestState questState)
    {
        currentQuest = questState;
        Refresh();
    }

    private void HandleQuestUpdated(QuestState questState)
    {
        currentQuest = questState;
        Refresh();
    }

    private void HandleQuestCompleted(QuestState questState)
    {
        currentQuest = QuestManager.Instance.GetFirstActiveQuest();
        Refresh();
    }

    private void HandleQuestStepStarted(QuestStepData stepData)
    {
        Refresh();
    }

    private void Refresh()
    {
        if (QuestManager.Instance == null)
        {
            HidePanel();
            return;
        }

        if (currentQuest == null || currentQuest.isCompleted)
        {
            currentQuest = QuestManager.Instance.GetFirstActiveQuest();
        }

        if (currentQuest == null)
        {
            if (hideWhenNoActiveQuest)
            {
                HidePanel();
            }

            return;
        }

        QuestData questData = currentQuest.questData;
        QuestStepData currentStep = currentQuest.GetCurrentStep();

        ShowPanel();

        if (questTitleText != null)
        {
            questTitleText.text = questData.questName;
        }

        if (questDescriptionText != null)
        {
            questDescriptionText.text = questData.questDescription;
        }

        if (objectiveHeaderText != null)
        {
            objectiveHeaderText.text = "Objective";
        }

        if (objectiveText != null)
        {
            objectiveText.text = currentStep != null
                ? currentStep.objectiveDescription
                : "";
        }

        if (progressText != null)
        {
            if (currentStep != null && currentStep.requiredAmount > 1)
            {
                progressText.text = $"{currentQuest.currentAmount} / {currentStep.requiredAmount}";
            }
            else
            {
                progressText.text = "";
            }
        }
    }

    private void ShowPanel()
    {
        if (questObjectivePanel != null)
        {
            questObjectivePanel.SetActive(true);
        }
    }

    private void HidePanel()
    {
        if (questObjectivePanel != null)
        {
            questObjectivePanel.SetActive(false);
        }
    }
}