using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject questPanel;
    public TMP_Text questNameText;
    public TMP_Text questDescriptionText;
    public TMP_Text objectiveText;
    public TMP_Text progressText;

    [Header("Settings")]
    public bool hideWhenNoQuest = true;

    private QuestState currentDisplayedQuest;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated += HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
        }

        RefreshUI();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
        }
    }

    private void HandleQuestStarted(QuestState questState)
    {
        currentDisplayedQuest = questState;
        RefreshUI();
    }

    private void HandleQuestUpdated(QuestState questState)
    {
        currentDisplayedQuest = questState;
        RefreshUI();
    }

    private void HandleQuestCompleted(QuestState questState)
    {
        currentDisplayedQuest = QuestManager.Instance.GetFirstActiveQuest();
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (QuestManager.Instance == null)
        {
            HidePanel();
            return;
        }

        if (currentDisplayedQuest == null || currentDisplayedQuest.isCompleted)
        {
            currentDisplayedQuest = QuestManager.Instance.GetFirstActiveQuest();
        }

        if (currentDisplayedQuest == null)
        {
            if (hideWhenNoQuest)
            {
                HidePanel();
            }

            return;
        }

        ShowPanel();

        QuestData questData = currentDisplayedQuest.questData;
        QuestStepData step = currentDisplayedQuest.GetCurrentStep();

        if (questNameText != null)
        {
            questNameText.text = questData.questName;
        }

        if (questDescriptionText != null)
        {
            questDescriptionText.text = questData.questDescription;
        }

        if (objectiveText != null)
        {
            if (step != null)
            {
                objectiveText.text = step.objectiveDescription;
            }
            else
            {
                objectiveText.text = "";
            }
        }

        if (progressText != null)
        {
            if (step != null && step.requiredAmount > 1)
            {
                progressText.text = $"{currentDisplayedQuest.currentAmount}/{step.requiredAmount}";
            }
            else
            {
                progressText.text = "";
            }
        }
    }

    private void ShowPanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(true);
        }
    }

    private void HidePanel()
    {
        if (questPanel != null)
        {
            questPanel.SetActive(false);
        }
    }
}