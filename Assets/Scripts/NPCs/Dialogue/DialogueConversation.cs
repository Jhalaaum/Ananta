using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    public string conversationID;

    [Header("Repeat Settings")]
    public bool canRepeat = true;

    [Tooltip("Optional: dialogue to use after this conversation has been completed once.")]
    public DialogueConversation repeatConversation;

    public DialogueLine[] lines;
}