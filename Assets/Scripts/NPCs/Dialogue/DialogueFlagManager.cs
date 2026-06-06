using System.Collections.Generic;
using UnityEngine;

public class DialogueFlagManager : MonoBehaviour
{
    public static DialogueFlagManager Instance;

    [Header("Runtime Flags")]
    [SerializeField] private List<string> flags = new List<string>();

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void SetFlag(string flagID)
    {
        if (string.IsNullOrWhiteSpace(flagID))
        {
            return;
        }

        string cleanID = flagID.Trim();

        if (!flags.Contains(cleanID))
        {
            flags.Add(cleanID);
            Log("Flag set: " + cleanID);
        }
    }

    public void ClearFlag(string flagID)
    {
        if (string.IsNullOrWhiteSpace(flagID))
        {
            return;
        }

        string cleanID = flagID.Trim();

        if (flags.Contains(cleanID))
        {
            flags.Remove(cleanID);
            Log("Flag cleared: " + cleanID);
        }
    }

    public bool HasFlag(string flagID)
    {
        if (string.IsNullOrWhiteSpace(flagID))
        {
            return false;
        }

        return flags.Contains(flagID.Trim());
    }

    public List<string> GetAllFlags()
    {
        return flags;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[DialogueFlagManager] " + message);
        }
    }
}