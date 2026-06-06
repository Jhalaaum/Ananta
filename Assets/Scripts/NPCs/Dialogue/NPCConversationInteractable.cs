using UnityEngine;
using UnityEngine.InputSystem;

public class NPCConversationInteractable : MonoBehaviour
{
    [Header("Conversation")]
    public DialogueConversation conversation;

    [Header("References")]
    public DialogueManager dialogueManager;
    public DialogueUI dialogueUI;

    [Header("Prompt")]
    public string promptText = "Press E to listen";

    [Header("Settings")]
    public bool canOnlyListenOnce = true;

    private bool playerInRange;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }
    }

    private void Update()
    {
        if (!playerInRange)
        {
            dialogueUI?.HidePrompt(this);
            return;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            dialogueUI?.HidePrompt(this);
            return;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            dialogueUI?.HidePrompt(this);
            return;
        }

        if (conversation == null)
        {
            dialogueUI?.HidePrompt(this);
            return;
        }

        if (canOnlyListenOnce &&
            DialogueFlagManager.Instance != null &&
            DialogueFlagManager.Instance.HasFlag("dialogue_completed_" + conversation.conversationID))
        {
            dialogueUI?.HidePrompt(this);
            return;
        }

        dialogueUI?.ShowPrompt(promptText, this);

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            dialogueUI?.HidePrompt(this);
            dialogueManager.StartDialogue(conversation, this, null);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
        {
            return;
        }

        playerInRange = false;
        dialogueUI?.HidePrompt(this);
    }

    private void OnDisable()
    {
        playerInRange = false;
        dialogueUI?.HidePrompt(this);
    }
}