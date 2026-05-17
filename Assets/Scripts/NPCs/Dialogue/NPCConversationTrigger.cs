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

    private bool playerInRange;

    private void Update()
    {
        if (!playerInRange)
        {
            return;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return;
        }

        dialogueUI.ShowPrompt(promptText, this);

        if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
        {
            dialogueUI?.HidePrompt(this);

            if (dialogueManager != null)
            {
                dialogueManager.StartNPCConversation(conversation, actors);
            }
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
        dialogueUI?.HidePrompt();
    }
}