using UnityEngine;

public class NPCDialogueInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to talk";

    [Header("Conversation")]
    public DialogueConversation conversation;

    [Header("Manager")]
    public DialogueManager dialogueManager;

    [Header("Settings")]
    public bool canInteract = true;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (conversation == null)
        {
            return false;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return false;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            return false;
        }

        return true;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        dialogueManager.StartDialogue(conversation, this, interactor);
    }
}