using UnityEngine;

[CreateAssetMenu(fileName = "NewNPCIdentity", menuName = "NPC/NPC Identity")]
public class NPCIdentityData : ScriptableObject
{
    public string npcID;
    public string displayName;
    public NPCType npcType;

    [TextArea]
    public string description;
}