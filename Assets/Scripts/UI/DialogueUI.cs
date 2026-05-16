using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject dialoguePanel;
    public TMP_Text interactPromptText;

    [Header("Dialogue Text")]
    public TMP_Text speakerNameText;
    public TMP_Text dialogueText;
    public TMP_Text continuePromptText;

    [Header("Choices")]
    public Transform choiceContainer;
    public Button choiceButtonPrefab;

    [Header("Typewriter")]
    public float typewriterSpeed = 0.025f;

    private Coroutine typingCoroutine;
    private string currentFullLine;
    private bool isTyping;

    public bool IsTyping => isTyping;

    private void Start()
    {
        HideDialogue();
        HidePrompt();
    }

    public void ShowPrompt(string prompt = "Press E to talk")
    {
        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(true);
            interactPromptText.text = prompt;
        }
    }
    public void HidePrompt()
    {
        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(false);
        }
    }

    public void ShowDialoguePanel()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }
    }

    public void HideDialogue()
    {
        if (choiceContainer != null)
        {
            choiceContainer.gameObject.SetActive(false);
        }
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        ClearChoices();
    }

    public void ShowLine(string speakerName, string line)
    {
        ShowDialoguePanel();

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }

        currentFullLine = line;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    private IEnumerator TypeLine(string line)
    {
        isTyping = true;

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        ClearChoices();

        foreach (char letter in line)
        {
            if (dialogueText != null)
            {
                dialogueText.text += letter;
            }

            yield return new WaitForSeconds(typewriterSpeed);
        }

        isTyping = false;
    }

    public void CompleteTypingImmediately()
    {
        if (!isTyping)
        {
            return;
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        if (dialogueText != null)
        {
            dialogueText.text = currentFullLine;
        }

        isTyping = false;
    }

    public void ShowContinuePrompt()
    {
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
            continuePromptText.text = "Press E to continue";
        }
    }

    public void ShowChoices(DialogueChoice[] choices, System.Action<int> onChoiceSelected)
    {
        ClearChoices();

        if (choiceContainer != null)
        {
            choiceContainer.gameObject.SetActive(false);
        }

        if (choiceButtonPrefab != null)
        {
            choiceButtonPrefab.gameObject.SetActive(false);
        }

        if (choices == null || choices.Length == 0)
        {
            ShowContinuePrompt();
            return;
        }

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        if (choiceContainer != null)
        {
            choiceContainer.gameObject.SetActive(true);
        }

        for (int i = 0; i < choices.Length; i++)
        {
            int choiceIndex = i;

            Button button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.gameObject.SetActive(true);

            TMP_Text buttonText = button.GetComponentInChildren<TMP_Text>();

            if (buttonText != null)
            {
                buttonText.text = choices[i].responseText;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                ClearChoices();
                onChoiceSelected?.Invoke(choiceIndex);
            });
        }
    }
    public void ClearChoices()
    {
        if (choiceContainer == null)
        {
            return;
        }

        for (int i = choiceContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = choiceContainer.GetChild(i);

            if (choiceButtonPrefab != null && child.gameObject == choiceButtonPrefab.gameObject)
            {
                child.gameObject.SetActive(false);
                continue;
            }

            Destroy(child.gameObject);
        }
    }
}