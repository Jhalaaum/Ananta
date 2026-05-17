using UnityEngine;

public interface IInteractable
{
    string GetPromptText();

    bool CanInteract(Transform interactor);

    void Interact(Transform interactor);
}