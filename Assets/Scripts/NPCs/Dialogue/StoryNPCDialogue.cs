using UnityEngine;

public class StoryNPCDialogue : MonoBehaviour
{
    [Header("Story Dialogue")]
    [TextArea(2, 4)]
    public string[] storyLines;

    private int currentLineIndex = 0;

    public string GetNextLine()
    {
        if (storyLines == null || storyLines.Length == 0)
        {
            return "";
        }

        string line = storyLines[currentLineIndex];

        currentLineIndex++;

        if (currentLineIndex >= storyLines.Length)
        {
            currentLineIndex = 0;
        }

        return line;
    }
}