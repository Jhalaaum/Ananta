using UnityEngine;
using UnityEngine.Events;

public class UnityEventInteractable : MonoBehaviour, IInteractable
{
    [Header("Prompt")]
    public string promptText = "Press E to interact";

    [Header("Settings")]
    public bool canInteract = true;
    public bool canOnlyUseOnce;

    [Header("Events")]
    public UnityEvent onInteract;

    private bool hasInteracted;

    public string GetPromptText()
    {
        return promptText;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (canOnlyUseOnce && hasInteracted)
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

        hasInteracted = true;
        onInteract?.Invoke();
    }

    public void SetCanInteract(bool value)
    {
        canInteract = value;
    }

    public void ResetInteraction()
    {
        hasInteracted = false;
    }
}