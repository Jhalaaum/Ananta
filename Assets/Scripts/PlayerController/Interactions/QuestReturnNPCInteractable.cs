using UnityEngine;

public class QuestReturnNPCInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string normalPromptText = "Press E to talk";
    public string returnPromptText = "Press E to report back";

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
        if (questReturnNPC == null)
        {
            return normalPromptText;
        }

        return questReturnNPC.GetPromptText();
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (questReturnNPC == null)
        {
            return false;
        }

        return questReturnNPC.CanInteract(interactor);
    }

    public void Interact(Transform interactor)
    {
        if (!CanInteract(interactor))
        {
            return;
        }

        questReturnNPC.Interact(interactor);
    }
}