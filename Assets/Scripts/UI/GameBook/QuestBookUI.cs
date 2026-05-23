using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class QuestBookUI : MonoBehaviour
{
    [Header("Quest List")]
    public Transform inProgressQuestContainer;
    public Transform completedQuestContainer;
    public Button questButtonPrefab;

    [Header("Quest Details")]
    public TMP_Text selectedQuestTitleText;
    public TMP_Text selectedQuestDescriptionText;
    public TMP_Text selectedQuestStepText;
    public TMP_Text selectedQuestObjectiveTypeText;
    public TMP_Text selectedQuestTargetText;
    public TMP_Text selectedQuestStatusText;

    [Header("Empty State")]
    public string noQuestSelectedText = "Select a quest to view details.";

    private QuestState selectedQuest;

    private void OnEnable()
    {
        Refresh();
    }

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestChanged;
            QuestManager.Instance.OnQuestUpdated += HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted += HandleQuestChanged;
        }

        Refresh();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestChanged;
            QuestManager.Instance.OnQuestUpdated -= HandleQuestChanged;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestChanged;
        }
    }

    private void HandleQuestChanged(QuestState questState)
    {
        Refresh();
    }

    public void Refresh()
    {
        ClearContainer(inProgressQuestContainer);
        ClearContainer(completedQuestContainer);

        if (QuestManager.Instance == null)
        {
            ClearDetails();
            return;
        }

        List<QuestState> quests = QuestManager.Instance.GetActiveQuests();

        selectedQuest = null;

        foreach (QuestState questState in quests)
        {
            if (questState == null || questState.questData == null)
            {
                continue;
            }

            if (questState.isCompleted)
            {
                CreateQuestButton(completedQuestContainer, questState);
            }
            else
            {
                CreateQuestButton(inProgressQuestContainer, questState);

                if (selectedQuest == null)
                {
                    selectedQuest = questState;
                }
            }
        }

        if (selectedQuest == null)
        {
            foreach (QuestState questState in quests)
            {
                if (questState != null && questState.questData != null)
                {
                    selectedQuest = questState;
                    break;
                }
            }
        }

        ShowQuestDetails(selectedQuest);
    }

    private void CreateQuestButton(Transform container, QuestState questState)
    {
        if (container == null || questButtonPrefab == null)
        {
            return;
        }

        Button button = Instantiate(questButtonPrefab, container);
        button.gameObject.SetActive(true);

        TMP_Text text = button.GetComponentInChildren<TMP_Text>();

        if (text != null)
        {
            string prefix = questState.isCompleted ? "✓ " : "";
            text.text = prefix + questState.questData.questName;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            selectedQuest = questState;
            ShowQuestDetails(selectedQuest);
        });
    }

    private void ShowQuestDetails(QuestState questState)
    {
        if (questState == null || questState.questData == null)
        {
            ClearDetails();
            return;
        }

        QuestData questData = questState.questData;
        QuestStepData step = questState.GetCurrentStep();

        if (selectedQuestTitleText != null)
        {
            selectedQuestTitleText.text = questData.questName;
        }

        if (selectedQuestDescriptionText != null)
        {
            selectedQuestDescriptionText.text = questData.questDescription;
        }

        if (selectedQuestStepText != null)
        {
            if (questState.isCompleted)
            {
                selectedQuestStepText.text = "Quest completed.";
            }
            else if (step != null)
            {
                selectedQuestStepText.text = step.objectiveDescription;
            }
            else
            {
                selectedQuestStepText.text = "No current objective.";
            }
        }

        if (selectedQuestObjectiveTypeText != null)
        {
            selectedQuestObjectiveTypeText.text =
                step != null && !questState.isCompleted
                    ? $"Objective Type: {step.objectiveType}"
                    : "Objective Type: —";
        }

        if (selectedQuestTargetText != null)
        {
            selectedQuestTargetText.text =
                step != null && !questState.isCompleted
                    ? $"Target: {step.targetID}"
                    : "Target: —";
        }

        if (selectedQuestStatusText != null)
        {
            if (questState.isCompleted)
            {
                selectedQuestStatusText.text = "Status: Completed";
            }
            else if (step != null)
            {
                selectedQuestStatusText.text =
                    $"Status: In Progress ({questState.currentAmount}/{step.requiredAmount})";
            }
            else
            {
                selectedQuestStatusText.text = "Status: In Progress";
            }
        }
    }

    private void ClearDetails()
    {
        if (selectedQuestTitleText != null)
        {
            selectedQuestTitleText.text = noQuestSelectedText;
        }

        if (selectedQuestDescriptionText != null)
        {
            selectedQuestDescriptionText.text = "";
        }

        if (selectedQuestStepText != null)
        {
            selectedQuestStepText.text = "";
        }

        if (selectedQuestObjectiveTypeText != null)
        {
            selectedQuestObjectiveTypeText.text = "";
        }

        if (selectedQuestTargetText != null)
        {
            selectedQuestTargetText.text = "";
        }

        if (selectedQuestStatusText != null)
        {
            selectedQuestStatusText.text = "";
        }
    }

    private void ClearContainer(Transform container)
    {
        if (container == null)
        {
            return;
        }

        for (int i = container.childCount - 1; i >= 0; i--)
        {
            Destroy(container.GetChild(i).gameObject);
        }
    }
}