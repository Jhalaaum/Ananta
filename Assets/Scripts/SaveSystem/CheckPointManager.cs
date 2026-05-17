using UnityEngine;

public class CheckpointManager : MonoBehaviour
{
    public static CheckpointManager Instance;

    [Header("Caught Settings")]
    public bool reloadCheckpointWhenCaught = true;

    [Header("Optional Penalty")]
    public bool saveAfterApplyingCaughtPenalty = false;

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

    public void SaveCurrentCheckpoint(string checkpointName)
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[CheckpointManager] No SaveManager found.");
            return;
        }

        SaveManager.Instance.SaveCheckpoint(checkpointName);
    }

    public void RestoreLastCheckpoint()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[CheckpointManager] No SaveManager found.");
            return;
        }

        if (!SaveManager.Instance.HasCheckpoint())
        {
            Debug.LogWarning("[CheckpointManager] No checkpoint available.");
            return;
        }

        SaveManager.Instance.LoadCheckpoint();
    }

    public void HandlePlayerCaught()
    {
        Log("Player caught.");

        // Future:
        // - lose money
        // - increase suspicion
        // - remove stolen items
        // - reset guard alert state
        // - play caught UI / fade screen

        if (reloadCheckpointWhenCaught)
        {
            RestoreLastCheckpoint();
        }

        if (saveAfterApplyingCaughtPenalty)
        {
            SaveCurrentCheckpoint("Caught Penalty Checkpoint");
        }
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[CheckpointManager] {message}");
        }
    }
}