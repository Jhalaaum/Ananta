using UnityEngine;

[RequireComponent(typeof(Collider))]
public class Checkpoint : MonoBehaviour
{
    [Header("Checkpoint")]
    public string checkpointID = "checkpoint_01";
    public string checkpointDisplayName = "Checkpoint Saved";

    [Header("Settings")]
    public bool saveOnlyOnce = false;
    public string playerTag = "Player";

    private bool hasSaved;

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

        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[Checkpoint] No SaveManager found.");
            return;
        }

        hasSaved = true;

        SaveManager.Instance.SaveCheckpoint(checkpointDisplayName);

        Debug.Log($"[Checkpoint] Saved checkpoint: {checkpointID}");
    }
}