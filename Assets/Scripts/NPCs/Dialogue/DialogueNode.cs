using System;
using UnityEngine;

[Serializable]
public class DialogueNode
{
    [Header("Identity")]
    public string nodeID = "node_01";

    [Header("Speaker")]
    public DialogueActor speaker;

    [Header("Line")]
    [TextArea(3, 8)]
    public string line;

    public DialogueLineType lineType = DialogueLineType.Spoken;

    [Header("Flow")]
    public string nextNodeID;
    public DialogueChoice[] choices;

    [Header("Node Requirements")]
    public DialogueCondition[] conditions;

    [Header("Effects On Enter")]
    public DialogueEffect[] onEnterEffects;

    [Header("Effects On Exit")]
    public DialogueEffect[] onExitEffects;

    [Header("Settings")]
    public bool autoContinue;
    public float autoContinueDelay = 1.5f;

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

    public void ApplyEnterEffects()
    {
        ApplyEffects(onEnterEffects);
    }

    public void ApplyExitEffects()
    {
        ApplyEffects(onExitEffects);
    }

    private void ApplyEffects(DialogueEffect[] effects)
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