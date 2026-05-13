using UnityEngine;

public class QuestTarget : MonoBehaviour
{
    [Header("Quest Target")]
    public string targetID;

    public string GetTargetID()
    {
        return targetID;
    }
}