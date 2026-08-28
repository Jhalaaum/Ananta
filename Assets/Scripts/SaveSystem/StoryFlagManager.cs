using System.Collections.Generic;
using UnityEngine;

public class StoryFlagManager : MonoBehaviour
{
    public static StoryFlagManager Instance;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly Dictionary<string, bool> flags =
        new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetFlag(string flagID, bool value = true)
    {
        string cleanID = CleanID(flagID);

        if (string.IsNullOrWhiteSpace(cleanID))
        {
            Debug.LogWarning(
                "[StoryFlagManager] Cannot set empty flag ID."
            );
            return;
        }

        flags[cleanID] = value;

        if (showDebugLogs)
        {
            Debug.Log(
                "[StoryFlagManager] " +
                cleanID +
                " = " +
                value
            );
        }
    }

    public bool GetFlag(string flagID)
    {
        string cleanID = CleanID(flagID);

        if (string.IsNullOrWhiteSpace(cleanID))
            return false;

        return flags.TryGetValue(
                   cleanID,
                   out bool value
               ) &&
               value;
    }

    public bool HasFlag(string flagID)
    {
        string cleanID = CleanID(flagID);

        if (string.IsNullOrWhiteSpace(cleanID))
            return false;

        return flags.ContainsKey(cleanID);
    }

    public void ClearFlag(string flagID)
    {
        string cleanID = CleanID(flagID);

        if (string.IsNullOrWhiteSpace(cleanID))
            return;

        flags.Remove(cleanID);
    }

    public void ClearAllFlags()
    {
        flags.Clear();

        if (showDebugLogs)
        {
            Debug.Log(
                "[StoryFlagManager] All flags cleared."
            );
        }
    }

    public StoryFlagSaveData CaptureFlags()
    {
        StoryFlagSaveData saveData =
            new StoryFlagSaveData();

        foreach (
            KeyValuePair<string, bool> pair
            in flags)
        {
            StoryFlagEntrySaveData entry =
                new StoryFlagEntrySaveData();

            entry.flagID = pair.Key;
            entry.value = pair.Value;

            saveData.flags.Add(entry);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[StoryFlagManager] Captured " +
                saveData.flags.Count +
                " flags."
            );
        }

        return saveData;
    }

    public void RestoreFlags(
        StoryFlagSaveData saveData)
    {
        flags.Clear();

        if (saveData == null ||
            saveData.flags == null)
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[StoryFlagManager] " +
                    "No saved flags. Restored empty state."
                );
            }

            return;
        }

        foreach (
            StoryFlagEntrySaveData entry
            in saveData.flags)
        {
            if (entry == null)
                continue;

            string cleanID =
                CleanID(entry.flagID);

            if (string.IsNullOrWhiteSpace(cleanID))
                continue;

            flags[cleanID] =
                entry.value;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[StoryFlagManager] Restored " +
                flags.Count +
                " flags."
            );
        }
    }

    private string CleanID(string value)
    {
        return value == null
            ? ""
            : value.Trim();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}