using UnityEngine;

public class SimpleNPCDialogue : MonoBehaviour
{
    [Header("Civilian Settings")]
    public string speakerName = "Civilian";

    [Header("Random Dialogue Lines")]
    [TextArea(2, 4)]
    public string[] lines;

    public string GetRandomLine()
    {
        if (lines == null || lines.Length == 0)
        {
            return "";
        }

        int randomIndex = Random.Range(0, lines.Length);
        return lines[randomIndex];
    }

    public DialogueConversation CreateRandomConversation()
    {
        string randomLine = GetRandomLine();

        if (string.IsNullOrWhiteSpace(randomLine))
        {
            return null;
        }

        DialogueConversation tempConversation = ScriptableObject.CreateInstance<DialogueConversation>();
        tempConversation.conversationID = "temporary_civilian_dialogue";

        DialogueLine dialogueLine = new DialogueLine
        {
            speakerName = speakerName,
            lineText = randomLine,
            playerChoices = null
        };

        tempConversation.lines = new DialogueLine[]
        {
            dialogueLine
        };

        return tempConversation;
    }
}