using UnityEngine;

public class QuestDatabase : MonoBehaviour
{
    public static QuestDatabase Instance;

    [Header("All Quest Assets")]
    public QuestData[] allQuests;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public QuestData GetQuestByID(string questID)
    {
        if (string.IsNullOrWhiteSpace(questID))
        {
            return null;
        }

        foreach (QuestData quest in allQuests)
        {
            if (quest == null)
            {
                continue;
            }

            if (quest.questID.Trim() == questID.Trim())
            {
                return quest;
            }
        }

        Debug.LogWarning($"[QuestDatabase] Quest not found: {questID}");
        return null;
    }
}