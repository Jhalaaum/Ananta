using System;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    [TextArea(1, 3)]
    public string responseText;

    public int nextLineIndex = -1;
}