using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Conversation", menuName = "Dialogue/Conversation")]
public class DialogueConversation : ScriptableObject
{
    [Header("Identity")]
    public string conversationID;
    public string conversationName;

    [TextArea(2, 5)]
    public string description;

    [Header("Legacy Simple Dialogue Support")]
    public string[] lines;

    [Header("Start")]
    public string startNodeID = "start";

    [Header("Nodes")]
    public DialogueNode[] nodes;

    [Header("Conversation Settings")]
    public bool canRepeat = true;
    public bool markCompletedOnEnd = true;

    [Header("GDD Purpose")]
    public bool isEavesdroppingConversation;
    public bool canRevealInformation;
    public bool canStartQuest;
    public bool canChangeTrust;

    public DialogueNode GetNode(string nodeID)
    {
        if (nodes == null || nodes.Length == 0)
        {
            return null;
        }

        foreach (DialogueNode node in nodes)
        {
            if (node == null)
            {
                continue;
            }

            if (node.nodeID == nodeID)
            {
                return node;
            }
        }

        return null;
    }

    public DialogueNode GetStartNode()
    {
        DialogueNode start = GetNode(startNodeID);

        if (start != null)
        {
            return start;
        }

        if (nodes != null && nodes.Length > 0)
        {
            return nodes[0];
        }

        return null;
    }

    public bool HasNodeDialogue()
    {
        return nodes != null && nodes.Length > 0;
    }

    public bool HasLegacyLines()
    {
        return lines != null && lines.Length > 0;
    }
}