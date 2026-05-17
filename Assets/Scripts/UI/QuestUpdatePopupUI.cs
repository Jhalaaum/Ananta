using System.Collections;
using TMPro;
using UnityEngine;

public class QuestUpdatePopupUI : MonoBehaviour
{
    [Header("Popup Panel")]
    public GameObject popupPanel;
    public TMP_Text titleText;
    public TMP_Text bodyText;

    [Header("Timing")]
    public float displayTime = 2f;

    private Coroutine currentRoutine;

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += HandleQuestStarted;
            QuestManager.Instance.OnQuestCompleted += HandleQuestCompleted;
            QuestManager.Instance.OnQuestStepStarted += HandleQuestStepStarted;
        }

        HideInstant();
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= HandleQuestStarted;
            QuestManager.Instance.OnQuestCompleted -= HandleQuestCompleted;
            QuestManager.Instance.OnQuestStepStarted -= HandleQuestStepStarted;
        }
    }

    private void HandleQuestStarted(QuestState questState)
    {
        if (questState == null || questState.questData == null)
        {
            return;
        }

        ShowPopup("New Quest", questState.questData.questName);
    }

    private void HandleQuestCompleted(QuestState questState)
    {
        if (questState == null || questState.questData == null)
        {
            return;
        }

        ShowPopup("Quest Complete", questState.questData.questName);
    }

    private void HandleQuestStepStarted(QuestStepData stepData)
    {
        if (stepData == null)
        {
            return;
        }

        ShowPopup("Objective Updated", stepData.objectiveDescription);
    }

    public void ShowPopup(string title, string body)
    {
        if (popupPanel == null)
        {
            return;
        }

        if (titleText != null)
        {
            titleText.text = title;
        }

        if (bodyText != null)
        {
            bodyText.text = body;
        }

        if (currentRoutine != null)
        {
            StopCoroutine(currentRoutine);
        }

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        popupPanel.SetActive(true);

        yield return new WaitForSeconds(displayTime);

        popupPanel.SetActive(false);
        currentRoutine = null;
    }

    private void HideInstant()
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
        }
    }
}