using System;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    public string playerSpeakerName = "You";
    [TextArea(1, 3)]
    public string responseText;

    public string nextLineID;
}