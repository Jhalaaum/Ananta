using UnityEngine;

public class QuestTestStarter : MonoBehaviour
{
    [Header("Test Quest")]
    public QuestData questToStart;

    private void Start()
    {
        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("No QuestManager found in scene.");
            return;
        }

        if (questToStart == null)
        {
            Debug.LogWarning("No quest assigned to QuestTestStarter.");
            return;
        }

        QuestManager.Instance.StartQuest(questToStart);
    }
}