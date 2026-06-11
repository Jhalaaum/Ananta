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

    [Tooltip("If true, the start checkpoint will ONLY be created when there is no checkpoint already saved.")]
    public bool onlyCreateStartCheckpointIfNoneExists = true;

    public float startCheckpointDelay = 0.2f;

    [Header("Save File")]
    public string checkpointFileName = "checkpoint_save.json";

    [Header("UI Reset")]
    public DialogueUI dialogueUI;
    public DialogueManager dialogueManager;

    [Header("Checkpoint Restore")]
    public bool forceRefreshZoneAfterCheckpoint = true;
    public bool resetGuardsAfterCheckpoint = true;
    public bool resetDialogueAfterCheckpoint = true;
    public bool resetRuntimeDroppedItems = true;
    public bool restoreWorldItems = true;
    public bool restoreInventory = true;
    public bool restoreQuests = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private string CheckpointPath => Path.Combine(Application.persistentDataPath, checkpointFileName);

    private GameSaveData currentCheckpointData;
    private bool isApplyingCheckpoint;
    private bool hasCreatedStartCheckpointThisScene;

    private const string PendingCheckpointLoadKey = "PendingCheckpointLoad";

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

        if (PlayerPrefs.HasKey(PendingCheckpointLoadKey))
        {
            string pendingJson = PlayerPrefs.GetString(PendingCheckpointLoadKey);
            PlayerPrefs.DeleteKey(PendingCheckpointLoadKey);

            if (!string.IsNullOrWhiteSpace(pendingJson))
            {
                GameSaveData pendingSave = JsonUtility.FromJson<GameSaveData>(pendingJson);

                if (pendingSave != null)
                {
                    StartCoroutine(ApplyPendingCheckpointAfterSceneSettles(pendingSave));
                    return;
                }
            }
        }

        if (createStartCheckpoint)
        {
            StartCoroutine(CreateStartCheckpointAfterSceneSettles());
        }
    }

    private IEnumerator ApplyPendingCheckpointAfterSceneSettles(GameSaveData pendingSave)
    {
        yield return null;

        if (startCheckpointDelay > 0f)
        {
            yield return new WaitForSeconds(startCheckpointDelay);
        }

        AutoFindReferences();
        ApplyCheckpointData(pendingSave);
    }

    private IEnumerator CreateStartCheckpointAfterSceneSettles()
    {
        if (hasCreatedStartCheckpointThisScene)
        {
            yield break;
        }

        hasCreatedStartCheckpointThisScene = true;

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

        if (onlyCreateStartCheckpointIfNoneExists && HasCheckpoint())
        {
            Log("Start checkpoint skipped because a checkpoint already exists.");
            yield break;
        }

        SaveCheckpoint("Start");
        Log("Start checkpoint created.");
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
        AutoFindReferences();

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

        if (forceRefreshZoneAfterCheckpoint && zoneTracker != null)
        {
            zoneTracker.ForceRefreshCurrentZone();
        }

        GameSaveData saveData = new GameSaveData();

        saveData.saveName = saveName;
        saveData.savedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        string sceneName = SceneManager.GetActiveScene().name;
        string zoneID = zoneTracker != null ? zoneTracker.GetCurrentZoneID() : "unknown_zone";

        if (saveData.player == null)
        {
            saveData.player = new PlayerSaveData();
        }

        saveData.player.Capture(player, sceneName, zoneID);

        if (QuestManager.Instance != null)
        {
            saveData.questProgress = QuestManager.Instance.CaptureQuestProgress();
        }

        if (InventoryManager.Instance != null)
        {
            saveData.inventorySaveData = InventoryManager.Instance.CaptureInventory();
        }

        if (WorldItemStateManager.Instance != null)
        {
            saveData.worldItemStateSaveData = WorldItemStateManager.Instance.CaptureWorldItemStates();
        }

        currentCheckpointData = saveData;

        string json = JsonUtility.ToJson(saveData, true);
        File.WriteAllText(CheckpointPath, json);

        Log($"Checkpoint saved and previous checkpoint overwritten: {saveName}");
        Log($"Saved position: {player.position}");
        Log($"Saved zone: {zoneID}");
    }

    public void LoadCheckpoint()
    {
        if (isApplyingCheckpoint)
        {
            return;
        }

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

        if (saveData.player == null)
        {
            Debug.LogWarning("[SaveManager] Checkpoint data has no player data. Using fallback respawn.");
            RespawnWithoutCheckpoint();
            return;
        }

        string currentScene = SceneManager.GetActiveScene().name;

        if (!string.IsNullOrWhiteSpace(saveData.player.sceneName) &&
            saveData.player.sceneName != currentScene)
        {
            PlayerPrefs.SetString(PendingCheckpointLoadKey, JsonUtility.ToJson(saveData));
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

        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] Cannot apply null save data.");
            isApplyingCheckpoint = false;
            return;
        }

        if (saveData.player == null)
        {
            Debug.LogWarning("[SaveManager] Cannot apply checkpoint. Player save data is null.");
            isApplyingCheckpoint = false;
            RespawnWithoutCheckpoint();
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

        ResetRuntimeUI();

        if (resetRuntimeDroppedItems)
        {
            RuntimeDroppedItemCleaner.Instance?.ClearRuntimeDroppedItems();
        }

        if (restoreInventory && InventoryManager.Instance != null)
        {
            InventoryManager.Instance.RestoreInventory(saveData.inventorySaveData);
        }

        if (restoreWorldItems && WorldItemStateManager.Instance != null)
        {
            WorldItemStateManager.Instance.RestoreWorldItemStates(saveData.worldItemStateSaveData);
        }

        RestorePlayerTransformSafely(
            saveData.player.GetPosition(),
            saveData.player.GetRotation()
        );

        RefreshZoneAfterCheckpoint(saveData.player.zoneID);

        if (restoreQuests && QuestManager.Instance != null)
        {
            QuestManager.Instance.RestoreQuestProgress(saveData.questProgress);
        }

        if (resetGuardsAfterCheckpoint)
        {
            ResetAllGuardsForCheckpoint();
        }

        ResetRuntimeUI();

        currentCheckpointData = saveData;

        isApplyingCheckpoint = false;

        Log($"Checkpoint loaded: {saveData.saveName}");
        Log($"Loaded position: {saveData.player.GetPosition()}");
        Log($"Loaded zone: {(zoneTracker != null ? zoneTracker.GetCurrentZoneID() : "No ZoneTracker")}");
    }

    private void RestorePlayerTransformSafely(Vector3 position, Quaternion rotation)
    {
        CharacterController controller = player.GetComponent<CharacterController>();

        bool controllerWasEnabled = false;

        if (controller != null)
        {
            controllerWasEnabled = controller.enabled;
            controller.enabled = false;
        }

        playerCheckpointState.RestoreTransform(position, rotation);

        if (controller != null)
        {
            controller.enabled = controllerWasEnabled;
        }
    }

    private void RefreshZoneAfterCheckpoint(string savedZoneID)
    {
        AutoFindReferences();

        if (zoneTracker == null)
        {
            Log("No ZoneTracker found to refresh.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(savedZoneID))
        {
            zoneTracker.SetCurrentZone(savedZoneID);
        }

        if (forceRefreshZoneAfterCheckpoint)
        {
            zoneTracker.ForceRefreshCurrentZoneAfterTeleport();
        }

        PlayerZoneTracker playerZoneTracker = player != null
            ? player.GetComponent<PlayerZoneTracker>()
            : null;

        if (playerZoneTracker != null)
        {
            playerZoneTracker.SendMessage(
                "ForceRefreshCurrentZoneAfterTeleport",
                SendMessageOptions.DontRequireReceiver
            );

            playerZoneTracker.SendMessage(
                "ForceRefreshCurrentZone",
                SendMessageOptions.DontRequireReceiver
            );
        }
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

        RestorePlayerTransformSafely(
            defaultRespawnPoint.position,
            defaultRespawnPoint.rotation
        );

        if (zoneTracker != null)
        {
            zoneTracker.ForceRefreshCurrentZoneAfterTeleport();
        }

        if (resetGuardsAfterCheckpoint)
        {
            ResetAllGuardsForCheckpoint();
        }

        ResetRuntimeUI();

        Log("Player respawned using fallback respawn point.");
    }

    private void ResetRuntimeUI()
    {
        if (!resetDialogueAfterCheckpoint)
        {
            return;
        }

        if (dialogueUI == null)
        {
            dialogueUI = FindObjectOfType<DialogueUI>();
        }

        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }

        if (dialogueManager != null)
        {
            dialogueManager.ResetDialogueManagerForCheckpoint();
        }
        else if (dialogueUI != null)
        {
            dialogueUI.ClearChoices();
            dialogueUI.HidePrompt();
            dialogueUI.HideDialogue();
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

        if (saveData == null)
        {
            Debug.LogWarning("[SaveManager] Failed to read checkpoint file.");
            return null;
        }

        if (saveData.player == null)
        {
            Debug.LogWarning("[SaveManager] Checkpoint file has no player data.");
            return null;
        }

        currentCheckpointData = saveData;
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