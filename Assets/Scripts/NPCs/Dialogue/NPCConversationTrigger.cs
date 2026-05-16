using UnityEngine;

public class NPCConversationTrigger : MonoBehaviour
{
    [Header("Conversation")]
    public DialogueConversation conversation;

    [Header("Actors")]
    public DialogueActor[] actors;

    [Header("Trigger Settings")]
    public bool playOnlyOnce = true;
    public bool startWhenPlayerEnters = true;

    private bool hasPlayed;

    private void OnTriggerEnter(Collider other)
    {
        if (!startWhenPlayerEnters)
        {
            return;
        }

        if (hasPlayed && playOnlyOnce)
        {
            return;
        }

        if (!other.CompareTag("Player"))
        {
            return;
        }

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();

        if (dialogueManager == null)
        {
            Debug.LogWarning("No DialogueManager found in scene.");
            return;
        }

        hasPlayed = true;
        dialogueManager.StartNPCConversation(conversation, actors);
    }
}