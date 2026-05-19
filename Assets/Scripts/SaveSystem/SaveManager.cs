using System;
using System.Collections;
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

    [Header("Fallback Respawn")]
    public Transform defaultRespawnPoint;
    public bool reloadSceneIfNoCheckpoint = false;

    [Header("Start Checkpoint")]
    public bool createStartCheckpoint = true;
    public bool overwriteSaveOnSceneStart = true;
    public float startCheckpointDelay = 0.2f;

    [Header("Save File")]
    public string checkpointFileName = "checkpoint_save.json";

    [Header("UI Reset")]
    public DialogueUI dialogueUI;
    public DialogueManager dialogueManager;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string CheckpointPath => Path.Combine(Application.persistentDataPath, checkpointFileName);

    private GameSaveData currentCheckpointData;
    private bool isApplyingCheckpoint;

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

        if (createStartCheckpoint)
        {
            StartCoroutine(CreateStartCheckpointAfterSceneSettles());
        }
    }

    private IEnumerator CreateStartCheckpointAfterSceneSettles()
    {
        yield return null;

        if (startCheckpointDelay > 0f)
        {
            yield return new WaitForSeconds(startCheckpointDelay);
        }

        AutoFindReferences();

        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot create start checkpoint. Player missing.");
            yield break;
        }

        if (playerCheckpointState == null)
        {
            Debug.LogWarning("[SaveManager] Cannot create start checkpoint. PlayerCheckpointState missing.");
            yield break;
        }

        if (overwriteSaveOnSceneStart)
        {
            ClearCheckpoint();
        }

        SaveCheckpoint("Start", false);

        Log("Start checkpoint created after scene settled.");
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

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    public void SaveCheckpoint(string saveName = "Checkpoint")
    {
        SaveCheckpoint(saveName, true);
    }

    public void SaveCheckpoint(string saveName, bool resetRuntimeUI)
    {
        AutoFindReferences();

        if (resetRuntimeUI)
        {
            ResetRuntimeUI();
        }

        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot save checkpoint. Player reference missing.");
            return;
        }

        if (playerCheckpointState == null)
        {
            Debug.LogWarning("[SaveManager] Cannot save checkpoint. PlayerCheckpointState missing.");
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

        // This explicitly overrides the previous checkpoint.
        File.WriteAllText(CheckpointPath, json);

        Log($"Checkpoint saved and previous checkpoint overwritten: {CheckpointPath}");
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
            Debug.LogWarning("[SaveManager] No checkpoint found. Using fallback respawn.");
            RespawnWithoutCheckpoint();
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
        if (isApplyingCheckpoint)
        {
            return;
        }

        isApplyingCheckpoint = true;

        AutoFindReferences();
        ResetRuntimeUI();

        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] Cannot apply null save data.");
            isApplyingCheckpoint = false;
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot restore player. Player missing.");
            isApplyingCheckpoint = false;
            return;
        }

        if (playerCheckpointState == null)
        {
            Debug.LogWarning("[SaveManager] Cannot restore player. PlayerCheckpointState missing.");
            isApplyingCheckpoint = false;
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

        ResetAllGuardsForCheckpoint();
        ResetRuntimeUI();

        currentCheckpointData = saveData;

        isApplyingCheckpoint = false;

        Log("Checkpoint loaded.");
    }

    private void RespawnWithoutCheckpoint()
    {
        AutoFindReferences();
        ResetRuntimeUI();

        if (reloadSceneIfNoCheckpoint)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            return;
        }

        if (player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot fallback respawn. Player missing.");
            return;
        }

        if (defaultRespawnPoint == null)
        {
            Debug.LogWarning("[SaveManager] No defaultRespawnPoint assigned. Cannot respawn safely.");
            return;
        }

        if (playerCheckpointState != null)
        {
            playerCheckpointState.RestoreTransform(
                defaultRespawnPoint.position,
                defaultRespawnPoint.rotation
            );
        }
        else
        {
            player.position = defaultRespawnPoint.position;
            player.rotation = defaultRespawnPoint.rotation;
        }

        ResetAllGuardsForCheckpoint();
        ResetRuntimeUI();

        Log("Player respawned using fallback respawn point.");
    }

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

        Log("Runtime UI reset for checkpoint.");
    }

    private void ResetAllGuardsForCheckpoint()
    {
        AdvancedGuardAIController[] guards = FindObjectsOfType<AdvancedGuardAIController>();

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard != null)
            {
                guard.ResetGuardForCheckpoint();
            }
        }

        Log("All guards reset for checkpoint.");
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

    public void ClearCheckpoint()
    {
        currentCheckpointData = null;

        if (File.Exists(CheckpointPath))
        {
            File.Delete(CheckpointPath);
        }

        Log("Previous checkpoint cleared.");
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[SaveManager] {message}");
        }
    }
}