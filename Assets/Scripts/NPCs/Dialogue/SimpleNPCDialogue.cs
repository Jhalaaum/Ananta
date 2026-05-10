using UnityEngine;

public class SimpleNPCDialogue : MonoBehaviour
{
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
}