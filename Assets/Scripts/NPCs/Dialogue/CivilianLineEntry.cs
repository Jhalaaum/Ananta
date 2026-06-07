using System;
using UnityEngine;

[Serializable]
public class CivilianLineEntry
{
    [Header("Line")]
    [TextArea(2, 5)]
    public string line;

    public DialogueLineType lineType = DialogueLineType.Spoken;
    public CivilianLineCategory category = CivilianLineCategory.Ambient;

    [Header("Random Weight")]
    [Min(1)]
    public int weight = 1;

    [Header("Optional Requirements")]
    public DialogueCondition[] conditions;

    [Header("Optional Effects")]
    public DialogueEffect[] effects;

    [Header("Settings")]
    public bool canRepeat = true;

    public bool IsAvailable()
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

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