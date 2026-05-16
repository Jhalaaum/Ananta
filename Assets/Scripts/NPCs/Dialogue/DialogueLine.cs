using System;
using UnityEngine;

[Serializable]
public class DialogueLine
{
    [Header("Speaker")]
    public string speakerName;

    [Tooltip("Optional ID of the NPC speaking this line. Example: merchant, guard_01, priest")]
    public string speakerID;

    [Tooltip("Optional ID of the NPC this speaker should face.")]
    public string lookAtTargetID;

    [Header("Dialogue")]
    [TextArea(2, 5)]
    public string lineText;

    [Header("Flow")]
    public int nextLineIndex = -1;
    public bool endAfterThisLine;

    [Header("NPC Conversation Settings")]
    public bool autoContinue = false;
    public float autoContinueDelay = 1.2f;

    [Header("Player Choices")]
    public DialogueChoice[] playerChoices;
}