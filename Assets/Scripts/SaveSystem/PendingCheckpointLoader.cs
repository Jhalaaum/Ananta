using UnityEngine;

public class PendingCheckpointLoader : MonoBehaviour
{
    private void Start()
    {
        if (!PlayerPrefs.HasKey("PendingCheckpointLoad"))
        {
            return;
        }

        string json = PlayerPrefs.GetString("PendingCheckpointLoad");
        PlayerPrefs.DeleteKey("PendingCheckpointLoad");

        if (string.IsNullOrWhiteSpace(json))
        {
            return;
        }

        GameSaveData saveData = JsonUtility.FromJson<GameSaveData>(json);

        if (SaveManager.Instance != null)
        {
            SaveManager.Instance.ApplyCheckpointData(saveData);
        }
    }
}