using UnityEngine;
using UnityEngine.InputSystem;

public class NPCConversationInteractable : MonoBehaviour
{
    [Header("Conversation")]
    public DialogueConversation conversation;
    public DialogueActor[] actors;

    [Header("References")]
    public DialogueManager dialogueManager;
    public DialogueUI dialogueUI;

    [Header("Prompt")]
    public string promptText = "Press E to listen";

    [Header("Settings")]
    public bool canOnlyListenOnce = false;

    private bool playerInRange;
    private bool hasListened;

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

        dialogueUI?.ShowPrompt(promptText, this);

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            StartListening();
        }
    }
    private void StartListening()
    {
        dialogueUI?.HidePrompt(this);

        if (conversation == null)
        {
            Debug.LogWarning($"{name} has no conversation assigned.");
            return;
        }

        if (dialogueManager == null)
        {
            Debug.LogWarning($"{name} has no DialogueManager assigned.");
            return;
        }

        hasListened = true;

        dialogueManager.StartNPCConversation(conversation, actors);
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