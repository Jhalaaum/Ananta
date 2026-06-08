using UnityEngine;

public class SimpleNPCDialogue : MonoBehaviour
{
    [Header("Actor")]
    public DialogueActor npcActor;

    [Header("Simple Lines")]
    [TextArea(2, 5)]
    public string[] randomLines;

    [Header("Conversation Settings")]
    public string conversationIDPrefix = "simple_npc_";
    public bool canRepeat = true;

    public DialogueConversation CreateRandomConversation()
    {
        if (randomLines == null || randomLines.Length == 0)
        {
            Debug.LogWarning("[SimpleNPCDialogue] No random lines assigned.");
            return null;
        }

        string selectedLine = randomLines[Random.Range(0, randomLines.Length)];

        DialogueConversation conversation = ScriptableObject.CreateInstance<DialogueConversation>();

        conversation.conversationID = conversationIDPrefix + gameObject.name;
        conversation.conversationName = gameObject.name + " Simple Dialogue";
        conversation.description = "Runtime-generated simple NPC dialogue.";
        conversation.startNodeID = "start";
        conversation.canRepeat = canRepeat;
        conversation.markCompletedOnEnd = false;

        DialogueNode node = new DialogueNode
        {
            nodeID = "start",
            speaker = npcActor,
            line = selectedLine,
            lineType = DialogueLineType.Spoken,
            nextNodeID = "",
            choices = null,
            autoContinue = false
        };

        conversation.nodes = new DialogueNode[] { node };

        // Legacy support, harmless.
        conversation.lines = new string[] { selectedLine };

        return conversation;
    }
}