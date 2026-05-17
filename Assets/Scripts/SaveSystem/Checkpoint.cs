using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    public string checkpointID = "checkpoint_01";
    public string checkpointDisplayName = "Checkpoint";

    [Header("Settings")]
    public bool saveOnlyOnce = false;
    public string playerTag = "Player";
    public float saveCooldown = 2f;

    private bool hasSaved;
    private float lastSaveTime = -999f;

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        if (saveOnlyOnce && hasSaved)
        {
            return;
        }

        if (Time.time < lastSaveTime + saveCooldown)
        {
            return;
        }

        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            Debug.Log("[Checkpoint] Skipped checkpoint save because dialogue is active.");
            return;
        }

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[Checkpoint] No SaveManager found.");
            return;
        }

        hasSaved = true;
        lastSaveTime = Time.time;

        SaveManager.Instance.SaveCheckpoint(checkpointDisplayName);

        Debug.Log($"[Checkpoint] Saved checkpoint: {checkpointID}");
    }
}