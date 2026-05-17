using UnityEngine;

public class QuestReturnNPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to talk";

    [Header("Quest Return NPC")]
    public QuestReturnNPC questReturnNPC;

    [Header("Settings")]
    public bool canInteract = true;

    private void Awake()
    {
        if (questReturnNPC == null)
        {
            questReturnNPC = GetComponentInParent<QuestReturnNPC>();
        }
    }

    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        return canInteract && questReturnNPC != null;
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        questReturnNPC.Interact();
    }
}