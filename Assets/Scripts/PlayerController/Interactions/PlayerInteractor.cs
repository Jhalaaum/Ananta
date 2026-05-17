using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInteractor : MonoBehaviour
{
    [Header("Raycast")]
    public Camera playerCamera;
    public float interactDistance = 3f;
    public LayerMask interactLayerMask = ~0;
    public QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Collide;

    [Header("References")]
    public DialogueUI dialogueUI;
    public DialogueManager dialogueManager;

    [Header("Prompt")]
    public string fallbackPrompt = "Press E to interact";

    [Header("Debug")]
    public bool showDebugRay = true;
    public bool showDebugLogs;

    private IInteractable currentInteractable;
    private Transform currentInteractableTransform;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        if (ShouldBlockInteraction())
        {
            ClearCurrentInteractable();
            return;
        }

        FindInteractable();

        if (currentInteractable != null)
        {
            ShowPrompt();

            if (Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame)
            {
                InteractWithCurrent();
            }
        }
        else
        {
            HidePrompt();
        }
    }

    private bool ShouldBlockInteraction()
    {
        if (dialogueManager != null && dialogueManager.IsDialogueActive)
        {
            return true;
        }

        return false;
    }

    private void FindInteractable()
    {
        currentInteractable = null;
        currentInteractableTransform = null;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;

            if (playerCamera == null)
            {
                return;
            }
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (showDebugRay)
        {
            Debug.DrawRay(ray.origin, ray.direction * interactDistance, Color.green);
        }

        if (!Physics.Raycast(
                ray,
                out RaycastHit hit,
                interactDistance,
                interactLayerMask,
                triggerInteraction))
        {
            return;
        }

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();

        if (interactable == null)
        {
            return;
        }

        if (!interactable.CanInteract(transform))
        {
            return;
        }

        currentInteractable = interactable;
        currentInteractableTransform = hit.collider.transform;

        if (showDebugLogs)
        {
            Debug.Log($"[PlayerInteractor] Looking at interactable: {hit.collider.name}");
        }
    }

    private void ShowPrompt()
    {
        if (dialogueUI == null)
        {
            return;
        }

        string prompt = fallbackPrompt;

        if (currentInteractable != null)
        {
            string interactablePrompt = currentInteractable.GetPromptText();

            if (!string.IsNullOrWhiteSpace(interactablePrompt))
            {
                prompt = interactablePrompt;
            }
        }

        dialogueUI.ShowPrompt(prompt, this);
    }

    private void HidePrompt()
    {
        if (dialogueUI != null)
        {
            dialogueUI.HidePrompt(this);
        }
    }

    private void InteractWithCurrent()
    {
        if (currentInteractable == null)
        {
            return;
        }

        HidePrompt();

        currentInteractable.Interact(transform);
    }

    private void ClearCurrentInteractable()
    {
        currentInteractable = null;
        currentInteractableTransform = null;
        HidePrompt();
    }
}