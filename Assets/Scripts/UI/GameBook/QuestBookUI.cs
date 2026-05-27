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

    [Header("Button Colors")]
    public Color inProgressButtonColor;
    public Color completedButtonColor;
    public Color selectedButtonColor;
    public Color inProgressTextColor;
    public Color completedTextColor;
    public Color selectedTextColor = Color.black;

    [Header("Button Layout")]
    public float buttonHeight = 44f;

    [Header("Empty State")]
    public string noQuestSelectedText = "Select a quest to view details.";

    [Header("Debug")]
    public bool showDebugLogs = true;

    private QuestState selectedQuest;
    private readonly Dictionary<Button, QuestState> buttonQuestLookup = new Dictionary<Button, QuestState>();

    private void Awake()
    {
        ColorUtility.TryParseHtmlString("#2E2417", out inProgressButtonColor);
        ColorUtility.TryParseHtmlString("#1F2E1F", out completedButtonColor);
        ColorUtility.TryParseHtmlString("#C9A45D", out selectedButtonColor);
        ColorUtility.TryParseHtmlString("#F1E6C8", out inProgressTextColor);
        ColorUtility.TryParseHtmlString("#BDE5B8", out completedTextColor);
    }

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
        buttonQuestLookup.Clear();

        if (QuestManager.Instance == null)
        {
            ClearDetails();
            return;
        }

        List<QuestState> quests = QuestManager.Instance.GetActiveQuests();

        if (selectedQuest != null)
        {
            selectedQuest = FindSameQuestAgain(quests, selectedQuest);
        }

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
            selectedQuest = FindFirstQuest(quests);
        }

        UpdateAllButtonVisuals();
        ShowQuestDetails(selectedQuest);
    }

    private QuestState FindSameQuestAgain(List<QuestState> quests, QuestState previousSelected)
    {
        if (previousSelected == null || previousSelected.questData == null)
        {
            return null;
        }

        string previousID = previousSelected.questData.questID;

        foreach (QuestState questState in quests)
        {
            if (questState == null || questState.questData == null)
            {
                continue;
            }

            if (questState.questData.questID == previousID)
            {
                return questState;
            }
        }

        return null;
    }

    private QuestState FindFirstQuest(List<QuestState> quests)
    {
        foreach (QuestState questState in quests)
        {
            if (questState != null && questState.questData != null)
            {
                return questState;
            }
        }

        return null;
    }

    private void CreateQuestButton(Transform container, QuestState questState)
    {
        if (container == null)
        {
            Debug.LogWarning("[QuestBookUI] Missing quest container.");
            return;
        }

        if (questButtonPrefab == null)
        {
            Debug.LogWarning("[QuestBookUI] Missing QuestButtonPrefab.");
            return;
        }

        Button button = Instantiate(questButtonPrefab, container);
        button.gameObject.SetActive(true);
        button.name = questState.questData.questName + "_Button";

        ForceButtonObjectVisible(button);
        SetButtonText(button, questState);

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() =>
        {
            selectedQuest = questState;
            UpdateAllButtonVisuals();
            ShowQuestDetails(selectedQuest);
        });

        buttonQuestLookup[button] = questState;

        if (showDebugLogs)
        {
            Debug.Log($"[QuestBookUI] Created quest button: {questState.questData.questName}, Completed: {questState.isCompleted}");
        }
    }

    private void ForceButtonObjectVisible(Button button)
    {
        RectTransform rect = button.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
            rect.anchoredPosition3D = Vector3.zero;
            rect.sizeDelta = new Vector2(rect.sizeDelta.x, buttonHeight);
        }

        LayoutElement layoutElement = button.GetComponent<LayoutElement>();

        if (layoutElement == null)
        {
            layoutElement = button.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minHeight = buttonHeight;
        layoutElement.preferredHeight = buttonHeight;
        layoutElement.flexibleHeight = 0;

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.enabled = true;
            image.raycastTarget = true;
            image.color = inProgressButtonColor;
        }

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        button.interactable = true;
    }

    private void SetButtonText(Button button, QuestState questState)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            Debug.LogWarning($"[QuestBookUI] Quest button prefab has no TMP_Text child: {button.name}");
            return;
        }

        text.gameObject.SetActive(true);
        text.enabled = true;
        text.alpha = 1f;
        text.raycastTarget = false;
        text.fontSize = 18;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.enableWordWrapping = false;

        string prefix = questState.isCompleted ? "✓ " : "";
        text.text = prefix + questState.questData.questName;
    }

    private void UpdateAllButtonVisuals()
    {
        foreach (KeyValuePair<Button, QuestState> pair in buttonQuestLookup)
        {
            Button button = pair.Key;
            QuestState questState = pair.Value;

            if (button == null || questState == null)
            {
                continue;
            }

            bool isSelected =
                selectedQuest != null &&
                selectedQuest.questData != null &&
                questState.questData != null &&
                selectedQuest.questData.questID == questState.questData.questID;

            ApplyButtonVisual(button, questState, isSelected);
        }
    }

    private void ApplyButtonVisual(Button button, QuestState questState, bool selected)
    {
        Image image = button.GetComponent<Image>();
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

        if (image != null)
        {
            image.enabled = true;

            if (selected)
            {
                image.color = selectedButtonColor;
            }
            else
            {
                image.color = questState.isCompleted ? completedButtonColor : inProgressButtonColor;
            }
        }

        if (text != null)
        {
            text.gameObject.SetActive(true);
            text.enabled = true;
            text.alpha = 1f;

            if (selected)
            {
                text.color = selectedTextColor;
            }
            else
            {
                text.color = questState.isCompleted ? completedTextColor : inProgressTextColor;
            }
        }
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