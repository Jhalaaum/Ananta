using UnityEngine;

public class QuestReturnNPC : MonoBehaviour
{
    [Header("Quest")]
    public QuestData relatedQuest;

    [Header("NPC Target")]
    public QuestTarget questTarget;

    [Header("NPC Info")]
    public string speakerName = "NPC";

    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string notStartedLine = "I do not have anything for you right now.";

    [TextArea(2, 4)]
    public string notReadyLine = "You have not finished what I asked.";

    [TextArea(2, 4)]
    public string returnLine = "You found what we needed.";

    [TextArea(2, 4)]
    public string completedLine = "There is nothing more for now.";

    public void Interact()
    {
        if (relatedQuest == null)
        {
            Debug.LogWarning($"{name} is missing relatedQuest.");
            return;
        }

        if (questTarget == null)
        {
            Debug.LogWarning($"{name} is missing QuestTarget.");
            return;
        }

        if (QuestManager.Instance == null)
        {
            Debug.LogWarning("No QuestManager found in scene.");
            return;
        }

        if (QuestManager.Instance.IsQuestCompleted(relatedQuest.questID))
        {
            ShowLine(completedLine);
            return;
        }

        QuestState state = QuestManager.Instance.GetActiveQuest(relatedQuest.questID);

        if (state == null)
        {
            ShowLine(notStartedLine);
            return;
        }

        QuestStepData step = state.GetCurrentStep();

        if (step == null)
        {
            ShowLine(notReadyLine);
            return;
        }

        bool isCorrectObjective =
            step.objectiveType == QuestObjectiveType.ReturnToNPC &&
            step.targetID == questTarget.targetID;

        if (isCorrectObjective)
        {
            ShowLine(returnLine);

            QuestManager.Instance.ProgressObjective(
                QuestObjectiveType.ReturnToNPC,
                questTarget.targetID
            );
        }
        else
        {
            ShowLine(notReadyLine);
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