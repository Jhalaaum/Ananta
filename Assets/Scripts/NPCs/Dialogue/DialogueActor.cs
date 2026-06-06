using UnityEngine;

[CreateAssetMenu(fileName = "New Dialogue Actor", menuName = "Dialogue/Actor")]
public class DialogueActor : ScriptableObject
{
    [Header("Identity")]
    public string actorID;
    public string displayName;

    [Header("Role")]
    public DialogueSpeakerRole speakerRole = DialogueSpeakerRole.NPC;

    [Header("Optional Portrait")]
    public Sprite portrait;

    [Header("Style")]
    [TextArea(2, 5)]
    public string dialogueStyleNote;
}