using UnityEngine;

public class QuestReturnNPC : MonoBehaviour
{
    [Header("Quest")]
    public QuestData relatedQuest;

    [Header("NPC Target")]
    public QuestTarget questTarget;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    public string notReadyLine = "You have not finished what I asked.";

    [TextArea(2, 4)]
    public string returnLine = "You found what we needed.";

    [TextArea(2, 4)]
    public string completedLine = "There is nothing more for now.";

    public void Interact()
    {
        if (relatedQuest == null || questTarget == null)
        {
            Debug.LogWarning($"{name} is missing quest or target data.");
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
            ShowLine(notReadyLine);
            return;
        }

        QuestStepData step = state.GetCurrentStep();

        if (step == null)
        {
            ShowLine(notReadyLine);
            return;
        }

        if (step.objectiveType == QuestObjectiveType.ReturnToNPC &&
            step.targetID == questTarget.targetID)
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
            dialogueUI.ShowDialogue(line);
        }
        else
        {
            Debug.Log($"NPC says: {line}");
        }
    }
}