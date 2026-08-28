using System;
using UnityEngine;

[Serializable]
public class StoryFlagRequirement
{
    [Tooltip("Persistent StoryFlagManager flag to check.")]
    public string flagID;

    [Tooltip(
        "The value this flag must have for the condition to pass."
    )]
    public bool requiredValue = true;

    public bool IsMet()
    {
        if (string.IsNullOrWhiteSpace(flagID))
        {
            return true;
        }

        if (StoryFlagManager.Instance == null)
        {
            Debug.LogWarning(
                "[StoryFlagRequirement] No StoryFlagManager exists."
            );

            return false;
        }

        bool currentValue =
            StoryFlagManager.Instance.GetFlag(
                flagID.Trim()
            );

        return currentValue == requiredValue;
    }
}