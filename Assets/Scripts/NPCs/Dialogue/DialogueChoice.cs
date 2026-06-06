using System;
using UnityEngine;

[Serializable]
public class DialogueChoice
{
    [Header("Choice Text")]
    public string responseText;

    [Header("Next Node")]
    public string nextNodeID;

    [Header("Requirements")]
    public DialogueCondition[] conditions;

    [Header("Consequences")]
    public DialogueEffect[] effects;

    [Header("UI")]
    public bool hideIfUnavailable = true;
    public string unavailableReason = "Unavailable";

    public bool IsAvailable()
    {
        if (conditions == null || conditions.Length == 0)
        {
            return true;
        }

        foreach (DialogueCondition condition in conditions)
        {
            if (condition == null)
            {
                continue;
            }

            if (!condition.IsMet())
            {
                return false;
            }
        }

        return true;
    }

    public void ApplyEffects()
    {
        if (effects == null)
        {
            return;
        }

        foreach (DialogueEffect effect in effects)
        {
            effect?.Apply();
        }
    }
}