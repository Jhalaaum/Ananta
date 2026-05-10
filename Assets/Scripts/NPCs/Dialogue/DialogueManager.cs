using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("References")]
    public DialogueUI dialogueUI;
    public PlayerMovement playerMovementScript;
    private DialogueConversation activeConversation;
    private StoryNPCDialogue activeNPC;
    private Transform player;

    private int currentLineIndex;
    private bool isDialogueActive;
    private bool waitingForChoice;

    public bool IsDialogueActive => isDialogueActive;

    public void StartDialogue(DialogueConversation conversation, StoryNPCDialogue npc, Transform playerTransform)
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (conversation == null || conversation.lines == null || conversation.lines.Length == 0)
        {
            Debug.LogWarning("No dialogue conversation assigned.");
            return;
        }

        activeConversation = conversation;
        activeNPC = npc;
        player = playerTransform;
        currentLineIndex = 0;
        isDialogueActive = true;
        waitingForChoice = false;

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = false;
        }

        if (activeNPC != null)
        {
            activeNPC.StartFacingPlayer(playerTransform);
        }

        if (dialogueUI != null)
        {
            dialogueUI.HidePrompt();
        }

        ShowCurrentLine();
    }

    private void Update()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            EndDialogue();
            return;
        }

        if (waitingForChoice)
        {
            return;
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            if (dialogueUI != null && dialogueUI.IsTyping)
            {
                dialogueUI.CompleteTypingImmediately();
                ShowChoicesOrContinue();
                return;
            }

            GoToNextLine();
        }
    }

    private void ShowCurrentLine()
    {
        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(line.speakerName, line.lineText);
        }

        Invoke(nameof(ShowChoicesOrContinue), GetLineRevealDelay(line.lineText));
    }

    private float GetLineRevealDelay(string line)
    {
        if (dialogueUI == null)
        {
            return 0f;
        }

        if (string.IsNullOrEmpty(line))
        {
            return 0f;
        }

        return Mathf.Max(0.1f, line.Length * dialogueUI.typewriterSpeed + 0.05f);
    }

    private void ShowChoicesOrContinue()
    {
        if (!isDialogueActive)
        {
            return;
        }

        if (!IsValidLineIndex(currentLineIndex))
        {
            return;
        }

        if (dialogueUI != null && dialogueUI.IsTyping)
        {
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (line.playerChoices != null && line.playerChoices.Length > 0)
        {
            waitingForChoice = true;

            dialogueUI.ShowChoices(line.playerChoices, HandleChoiceSelected);
        }
        else
        {
            waitingForChoice = false;

            if (dialogueUI != null)
            {
                dialogueUI.ShowContinuePrompt();
            }
        }
    }

    private void HandleChoiceSelected(int choiceIndex)
    {
        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (line.playerChoices == null || choiceIndex < 0 || choiceIndex >= line.playerChoices.Length)
        {
            EndDialogue();
            return;
        }

        DialogueChoice choice = line.playerChoices[choiceIndex];

        waitingForChoice = false;

        if (choice.nextLineIndex < 0)
        {
            EndDialogue();
            return;
        }

        currentLineIndex = choice.nextLineIndex;
        ShowCurrentLine();
    }

    private void GoToNextLine()
    {
        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        DialogueLine line = activeConversation.lines[currentLineIndex];

        if (line.playerChoices != null && line.playerChoices.Length > 0)
        {
            ShowChoicesOrContinue();
            return;
        }

        currentLineIndex++;

        if (!IsValidLineIndex(currentLineIndex))
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private bool IsValidLineIndex(int index)
    {
        return activeConversation != null &&
               activeConversation.lines != null &&
               index >= 0 &&
               index < activeConversation.lines.Length;
    }

    public void EndDialogue()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isDialogueActive = false;
        waitingForChoice = false;

        if (dialogueUI != null)
        {
            dialogueUI.HideDialogue();
        }

        if (playerMovementScript != null)
        {
            playerMovementScript.enabled = true;
        }

        if (activeNPC != null)
        {
            activeNPC.StopFacingPlayer();
        }

        activeConversation = null;
        activeNPC = null;
        player = null;
    }
}