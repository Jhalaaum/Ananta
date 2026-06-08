using UnityEngine;

public class QuestReturnNPC : MonoBehaviour, IInteractable
{
    [Header("Return Objective")]
    public string targetID = "devika";
    public QuestObjectiveType objectiveType = QuestObjectiveType.ReturnToNPC;
    public int amount = 1;

    [Header("Prompt")]
    public string returnPrompt = "Press E to report back";
    public string normalPrompt = "Press E to talk";

    [Header("Optional Dialogue")]
    public DialogueConversation normalConversation;
    public DialogueConversation returnConversation;

    [Header("References")]
    public DialogueManager dialogueManager;

    [Header("Settings")]
    public bool canInteract = true;

    private void Awake()
    {
        if (dialogueManager == null)
        {
            dialogueManager = FindObjectOfType<DialogueManager>();
        }
    }

    public string GetPromptText()
    {
        if (HasMatchingReturnObjective())
        {
            return returnPrompt;
        }

        return normalPrompt;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return false;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            return false;
        }

        return true;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        if (HasMatchingReturnObjective())
        {
            CompleteReturnObjective();

            if (returnConversation != null && dialogueManager != null)
            {
                dialogueManager.StartDialogue(returnConversation, this, interactor);
            }

            return;
        }

        if (normalConversation != null && dialogueManager != null)
        {
            dialogueManager.StartDialogue(normalConversation, this, interactor);
        }
    }

    private bool HasMatchingReturnObjective()
    {
        if (QuestManager.Instance == null)
        {
            return false;
        }

        return QuestManager.Instance.HasActiveObjective(
            objectiveType,
            targetID
        );
    }

    private void CompleteReturnObjective()
    {
        if (QuestManager.Instance == null)
        {
            return;
        }

        QuestManager.Instance.ProgressObjective(
            objectiveType,
            targetID,
            amount
        );

        Debug.Log($"[QuestReturnNPC] Returned to NPC: {targetID}");
    }
}