using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    public bool endAfterThisLine;
    public int nextLineIndex = -1;
    public string lineID;
    public string speakerName;

    [TextArea(2, 5)]
    public string lineText;

    public DialogueChoice[] playerChoices;
}