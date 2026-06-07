using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject dialoguePanel;
    public TMP_Text interactPromptText;

    [Header("Dialogue Text")]
    public TMP_Text speakerNameText;
    public TMP_Text lineTypeText;
    public TMP_Text dialogueText;
    public TMP_Text continuePromptText;

    [Header("Choices")]
    public Transform choiceContainer;
    public Button choiceButtonPrefab;

    [Header("Typewriter")]
    public float typewriterSpeed = 0.025f;

    [Header("Choice Colors")]
    public Color availableChoiceTextColor = Color.white;
    public Color unavailableChoiceTextColor = Color.gray;
    public Color choiceButtonColor = new Color(0.18f, 0.14f, 0.09f, 1f);
    public Color choiceButtonHoverColor = new Color(0.30f, 0.24f, 0.15f, 1f);

    private Coroutine typingCoroutine;
    private string currentFullLine;
    private bool isTyping;
    private object promptOwner;

    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    public bool IsTyping => isTyping;
    public bool HasChoicesVisible => spawnedChoiceButtons.Count > 0;

    private void Start()
    {
        HideDialogue();
        ForceHidePrompt();
    }

    public void ShowPrompt(string prompt = "Press E to talk", object owner = null)
    {
        if (promptOwner != null && owner != null && promptOwner != owner)
        {
            return;
        }

        promptOwner = owner;

        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(true);
            interactPromptText.text = prompt;
        }
    }

    public void HidePrompt(object owner = null)
    {
        if (promptOwner != null && owner != null && promptOwner != owner)
        {
            return;
        }

        promptOwner = null;

        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(false);
            interactPromptText.text = "";
        }
    }

    public void ForceHidePrompt()
    {
        promptOwner = null;

        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(false);
            interactPromptText.text = "";
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
        StopTyping();

        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = "";
        }

        if (lineTypeText != null)
        {
            lineTypeText.text = "";
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

    public void ShowLine(DialogueActor speaker, string line, DialogueLineType lineType)
    {
        ShowDialoguePanel();

        if (speakerNameText != null)
        {
            speakerNameText.text = speaker != null ? speaker.displayName : "";
        }

        if (lineTypeText != null)
        {
            lineTypeText.text = GetLineTypeLabel(lineType);
        }

        currentFullLine = line;

        StopTyping();
        ClearChoices();

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    private string GetLineTypeLabel(DialogueLineType lineType)
    {
        switch (lineType)
        {
            case DialogueLineType.Whisper:
                return "WHISPER";

            case DialogueLineType.Thought:
                return "THOUGHT";

            case DialogueLineType.Overheard:
                return "OVERHEARD";

            case DialogueLineType.System:
                return "NOTE";

            default:
                return "";
        }
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

        foreach (char letter in line)
        {
            if (dialogueText != null)
            {
                dialogueText.text += letter;
            }

            yield return new WaitForSecondsRealtime(typewriterSpeed);
        }

        isTyping = false;
    }

    public void CompleteTypingImmediately()
    {
        if (!isTyping)
        {
            return;
        }

        StopTyping();

        if (dialogueText != null)
        {
            dialogueText.text = currentFullLine;
        }

        isTyping = false;
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        isTyping = false;
    }

    public void ShowContinuePrompt()
    {
        ClearChoices();

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
            continuePromptText.text = "Press E to continue";
        }
    }

    public void ShowChoices(List<DialogueChoice> choices, System.Action<int> onChoiceSelected)
    {
        ClearChoices();

        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        if (choiceContainer != null)
        {
            choiceContainer.gameObject.SetActive(true);
        }

        if (choiceButtonPrefab != null)
        {
            choiceButtonPrefab.gameObject.SetActive(false);
        }

        if (choices == null || choices.Count == 0)
        {
            ShowContinuePrompt();
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];

            if (choice == null)
            {
                continue;
            }

            bool available = choice.IsAvailable();

            if (!available && choice.hideIfUnavailable)
            {
                continue;
            }

            int choiceIndex = i;

            Button button = Instantiate(choiceButtonPrefab, choiceContainer);
            button.gameObject.SetActive(true);
            button.interactable = available;

            ForceChoiceButtonVisibleAndClickable(button);

            TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
            {
                text.gameObject.SetActive(true);
                text.enabled = true;
                text.alpha = 1f;
                text.raycastTarget = false;

                if (available)
                {
                    text.text = choice.responseText;
                    text.color = availableChoiceTextColor;
                }
                else
                {
                    text.text = $"{choice.responseText} ({choice.unavailableReason})";
                    text.color = unavailableChoiceTextColor;
                }
            }

            button.onClick.RemoveAllListeners();

            if (available)
            {
                button.onClick.AddListener(() =>
                {
                    ClearChoices();
                    onChoiceSelected?.Invoke(choiceIndex);
                });
            }

            spawnedChoiceButtons.Add(button);
        }

        if (spawnedChoiceButtons.Count == 0)
        {
            ShowContinuePrompt();
        }
    }

    private void ForceChoiceButtonVisibleAndClickable(Button button)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rect = button.GetComponent<RectTransform>();

        if (rect != null)
        {
            rect.localScale = Vector3.one;
            rect.localRotation = Quaternion.identity;
        }

        Image image = button.GetComponent<Image>();

        if (image != null)
        {
            image.enabled = true;
            image.color = choiceButtonColor;
            image.raycastTarget = true;
        }

        CanvasGroup canvasGroup = button.GetComponent<CanvasGroup>();

        if (canvasGroup == null)
        {
            canvasGroup = button.gameObject.AddComponent<CanvasGroup>();
        }

        canvasGroup.alpha = 1f;
        canvasGroup.interactable = true;
        canvasGroup.blocksRaycasts = true;

        ColorBlock colors = button.colors;
        colors.normalColor = choiceButtonColor;
        colors.highlightedColor = choiceButtonHoverColor;
        colors.pressedColor = choiceButtonHoverColor;
        colors.selectedColor = choiceButtonHoverColor;
        colors.disabledColor = Color.gray;
        button.colors = colors;

        button.targetGraphic = image;
    }

    public void ClearChoices()
    {
        foreach (Button button in spawnedChoiceButtons)
        {
            if (button != null)
            {
                Destroy(button.gameObject);
            }
        }

        spawnedChoiceButtons.Clear();

        if (choiceContainer != null)
        {
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

            choiceContainer.gameObject.SetActive(false);
        }
    }

    public void ResetDialogueUIForCheckpoint()
    {
        StopTyping();
        ForceHidePrompt();
        HideDialogue();
        ClearChoices();
    }
    public void ShowLine(DialogueActor speaker, string line)
    {
        ShowLine(speaker, line, DialogueLineType.Spoken);
    }

    public void ShowLine(string speakerName, string line)
    {
        ShowDialoguePanel();

        if (speakerNameText != null)
        {
            speakerNameText.text = speakerName;
        }

        if (lineTypeText != null)
        {
            lineTypeText.text = "";
        }

        currentFullLine = line;

        StopTyping();
        ClearChoices();

        typingCoroutine = StartCoroutine(TypeLine(line));
    }

    public void ShowLine(string line)
    {
        ShowLine("", line);
    }
}