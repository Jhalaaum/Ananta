using UnityEngine;

public class QuestGiver : MonoBehaviour
{
    [Header("Quest")]
    public QuestData questToGive;

    [Header("NPC Target")]
    public QuestTarget questTarget;

    [Header("NPC Info")]
    public string speakerName = "NPC";

    [Header("Dialogue Lines")]
    [TextArea(2, 4)]
    public string beforeQuestLine = "I need your help.";

    [TextArea(2, 4)]
    public string questStartedLine = "Good. Come back when it is done.";

    [TextArea(2, 4)]
    public string questAlreadyActiveLine = "You already know what needs to be done.";

    [TextArea(2, 4)]
    public string questCompletedLine = "You did well.";

    public void Interact()
    {
        if (questToGive == null)
        {
            Debug.LogWarning($"{name} has no quest assigned.");
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("No QuestManager found in scene.");
            return;
        }

        if (QuestManager.Instance.IsQuestCompleted(questToGive.questID))
        {
            ShowLine(questCompletedLine);
            return;
        }

        if (QuestManager.Instance.IsQuestActive(questToGive.questID))
        {
            ShowLine(questAlreadyActiveLine);
            return;
        }

        ShowLine(beforeQuestLine);

        QuestManager.Instance.StartQuest(questToGive);

        ShowLine(questStartedLine);

        if (questTarget != null)
        {
            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.TalkToNPC,
                questTarget.targetID
            );
        }
    }

    private void ShowLine(string line)
    {
        DialogueUI dialogueUI = FindObjectOfType<DialogueUI>();

        if (dialogueUI != null)
        {
            dialogueUI.ShowLine(speakerName, line);
        }
        else
        {
            Debug.Log($"{speakerName}: {line}");
        }
    }
}