using UnityEngine;

public class StoryNPCDialogue : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueConversation conversation;

    [Header("Repeat Settings")]
    [Tooltip("If true, this NPC can repeat the same conversation every time.")]
    public bool canRepeatConversation = true;

    [Tooltip("If assigned, this conversation will play after the first conversation has been completed.")]
    public DialogueConversation conversationAfterFirstCompletion;

    [Tooltip("If true, the NPC will stop being interactable after the first conversation is completed.")]
    public bool disableInteractionAfterCompletion = false;

    [Header("Interaction Feel")]
    public bool facePlayerDuringDialogue = true;
    public float facePlayerTurnSpeed = 8f;

    private Transform currentPlayer;
    private bool hasCompletedConversation;
    private bool interactionDisabled;

    public DialogueConversation GetConversation()
    {
        if (interactionDisabled)
        {
            return null;
        }

        if (!hasCompletedConversation)
        {
            return conversation;
        }

        if (conversationAfterFirstCompletion != null)
        {
            return conversationAfterFirstCompletion;
        }

        if (canRepeatConversation)
        {
            return conversation;
        }

        return null;
    }

    public bool CanInteract()
    {
        return GetConversation() != null;
    }

    public void MarkConversationCompleted()
    {
        hasCompletedConversation = true;

        if (disableInteractionAfterCompletion && conversationAfterFirstCompletion == null)
        {
            interactionDisabled = true;
        }
    }

    public void ResetDialogueState()
    {
        hasCompletedConversation = false;
        interactionDisabled = false;
    }

    public void StartFacingPlayer(Transform player)
    {
        currentPlayer = player;
    }

    public void StopFacingPlayer()
    {
        currentPlayer = null;
    }

    private void Update()
    {
        if (!facePlayerDuringDialogue)
        {
            return;
        }

        if (currentPlayer == null)
        {
            return;
        }

        Vector3 direction = currentPlayer.position - transform.position;
        direction.y = 0f;

        if (direction.sqrMagnitude < 0.01f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(direction);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            Time.deltaTime * facePlayerTurnSpeed
        );
    }
}