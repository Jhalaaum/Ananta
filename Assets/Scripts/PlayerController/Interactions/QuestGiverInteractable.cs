using UnityEngine;

public class QuestGiverInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to talk";

    [Header("Quest Giver")]
    public QuestGiver questGiver;

    [Header("Settings")]
    public bool canInteract = true;

    private void Awake()
    {
        if (questGiver == null)
        {
            questGiver = GetComponentInParent<QuestGiver>();
        }
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        return canInteract && questGiver != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        questGiver.Interact();
    }
}