using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance;

    [Header("References")]
    public Transform player;
    public PlayerCheckpointState playerCheckpointState;
    public ZoneTracker zoneTracker;

    [Header("Save File")]
    public string checkpointFileName = "checkpoint_save.json";
    [Header("UI Reset")]
    public DialogueUI dialogueUI;
    public DialogueManager dialogueManager;
    [Header("Debug")]
    public bool showDebugLogs = true;

    private string CheckpointPath => Path.Combine(Application.persistentDataPath, checkpointFileName);

    private GameSaveData currentCheckpointData;
    private void ResetRuntimeUI()
    {
        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        if (dialogueUI != null)
        {
            dialogueUI.ResetDialogueUIForCheckpoint();
        }

        Debug.Log("[SaveManager] Runtime UI reset for checkpoint.");
    }
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        AutoFindReferences();
    }

    private void AutoFindReferences()
    {
        if (player == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");

            if (playerObject != null)
            {
                player = playerObject.transform;
            }
        }

        if (player != null)
        {
            if (playerCheckpointState == null)
            {
                playerCheckpointState = player.GetComponent<PlayerCheckpointState>();
            }

            if (zoneTracker == null)
            {
                zoneTracker = player.GetComponent<ZoneTracker>();
            }
        }
    }

    public void SaveCheckpoint(string saveName = "Checkpoint")
    {
        AutoFindReferences();

        ResetRuntimeUI();

        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot save checkpoint. Player reference missing.");
            return;
        }

        GameSaveData saveData = new GameSaveData();
        saveData.saveName = saveName;
        saveData.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string sceneName = SceneManager.GetActiveScene().name;
        string zoneID = zoneTracker != null ? zoneTracker.GetCurrentZoneID() : "unknown_zone";

        saveData.player.Capture(player, sceneName, zoneID);

        if (QuestManager.Instance != null)
        {
            saveData.questProgress = QuestManager.Instance.CaptureQuestProgress();
        }

        currentCheckpointData = saveData;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(CheckpointPath, json);

        Log($"Checkpoint saved: {CheckpointPath}");
    }

    public void LoadCheckpoint()
    {
        AutoFindReferences();

        GameSaveData saveData = currentCheckpointData;

        if (saveData == null)
        {
            saveData = LoadCheckpointFromDisk();
        }

        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] No checkpoint found to load.");
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(saveData.player.sceneName) &&
            saveData.player.sceneName != currentScene)
        {
            PlayerPrefs.SetString("PendingCheckpointLoad", JsonUtility.ToJson(saveData));
            SceneManager.LoadScene(saveData.player.sceneName);
            return;
        }

        ApplyCheckpointData(saveData);
    }

    public void ApplyCheckpointData(GameSaveData saveData)
    {
        AutoFindReferences();

        ResetRuntimeUI();

        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] Cannot apply null save data.");
            return;
        }

        if (playerCheckpointState == null)
        {
            Debug.LogWarning("[SaveManager] Cannot restore player. PlayerCheckpointState missing.");
            return;
        }

        playerCheckpointState.RestoreTransform(
            saveData.player.GetPosition(),
            saveData.player.GetRotation()
        );

        if (zoneTracker != null)
        {
            zoneTracker.SetCurrentZone(saveData.player.zoneID);
        }

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.RestoreQuestProgress(saveData.questProgress);
        }

        ResetRuntimeUI();

        currentCheckpointData = saveData;

        Log("Checkpoint loaded.");
    }
    public GameSaveData LoadCheckpointFromDisk()
    {
        if (!File.Exists(CheckpointPath))
        {
            return null;
        }

        string json = File.ReadAllText(CheckpointPath);

        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);
        return saveData;
    }

    public bool HasCheckpoint()
    {
        return currentCheckpointData != null || File.Exists(CheckpointPath);
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }
}