using UnityEngine;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject dialoguePanel;
    public TMP_Text dialogueText;
    public TMP_Text interactPromptText;

    private void Start()
    {
        HideDialogue();
        HidePrompt();
    }

    public void ShowPrompt()
    {
        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(true);
        }
    }

    public void HidePrompt()
    {
        if (interactPromptText != null)
        {
            interactPromptText.gameObject.SetActive(false);
        }
    }

    public void ShowDialogue(string line)
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        if (dialogueText != null)
        {
            dialogueText.text = line;
        }
    }

    public void HideDialogue()
    {
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        if (dialogueText != null)
        {
            dialogueText.text = "";
        }
    }
}