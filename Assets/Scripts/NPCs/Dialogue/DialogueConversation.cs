using UnityEngine;

[CreateAssetMenu(fileName = "NewDialogueConversation", menuName = "Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    public string conversationID;
    public DialogueLine[] lines;
}