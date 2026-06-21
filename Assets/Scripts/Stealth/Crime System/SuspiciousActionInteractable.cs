using System.Collections;
using UnityEngine;

public class SuspiciousActionInteractable : MonoBehaviour, IInteractable
{
    [Header("Action")]
    public SuspiciousActionDefinition actionDefinition;

    [Header("Interaction")]
    public bool canInteract = true;
    public bool requirePlayerInRange = true;
    public float interactionDistance = 2.2f;

    [Header("Facing Requirement")]
    public bool requirePlayerFacingObject = true;

    [Tooltip("Use the camera if assigned. If empty, the script uses Camera.main, then falls back to the interactor forward.")]
    public Transform facingReference;

    [Tooltip("Smaller = stricter. 35 to 55 is good.")]
    [Range(5f, 90f)] public float maxFacingAngle = 45f;

    public Transform interactionFocusPoint;

    [Header("Prompt")]
    public string fallbackPromptText = "Press E to interact";
    public string inProgressPromptText = "Working...";
    public string completedPromptText = "";

    [Header("Runtime")]
    public bool playerInRange;
    public bool playerFacingObject;
    public bool actionInProgress;
    public bool actionCompleted;
    public Transform currentInteractor;

    [Header("Optional Debug Visual")]
    public Renderer targetRenderer;
    public Color availableColor = Color.yellow;
    public Color completedColor = Color.gray;
    public Color inProgressColor = Color.cyan;

    [Header("Action Cancellation")]
    public bool cancelIfPlayerMovesAway = true;
    public bool cancelIfPlayerLooksAway = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Coroutine actionRoutine;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        if (facingReference == null && Camera.main != null)
        {
            facingReference = Camera.main.transform;
        }

        if (targetRenderer != null)
        {
            targetRenderer.material.color = availableColor;
        }
    }

    public string GetPromptText()
    {
        if (actionCompleted)
        {
            return completedPromptText;
        }

        if (actionInProgress)
        {
            return inProgressPromptText;
        }

        if (actionDefinition != null && !string.IsNullOrWhiteSpace(actionDefinition.displayName))
        {
            return $"Press E to {actionDefinition.displayName}";
        }

        return fallbackPromptText;
    }

    public bool CanInteract(Transform interactor)
    {
        if (!canInteract)
        {
            return false;
        }

        if (actionDefinition == null)
        {
            return false;
        }

        if (actionCompleted)
        {
            return false;
        }

        if (actionInProgress)
        {
            return false;
        }

        if (interactor == null)
        {
            return false;
        }

        if (GameBookUI.Instance != null && GameBookUI.Instance.isOpen)
        {
            return false;
        }

        if (DialogueManagerIsActive())
        {
            return false;
        }

        playerInRange = IsInteractorInRange(interactor);
        playerFacingObject = IsInteractorFacingObject(interactor);

        if (requirePlayerInRange && !playerInRange)
        {
            return false;
        }

        if (requirePlayerFacingObject && !playerFacingObject)
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

        currentInteractor = interactor;

        if (actionRoutine != null)
        {
            StopCoroutine(actionRoutine);
        }

        actionRoutine = StartCoroutine(PerformActionRoutine(interactor));
    }

    private IEnumerator PerformActionRoutine(Transform interactor)
    {
        actionInProgress = true;

        if (targetRenderer != null)
        {
            targetRenderer.material.color = inProgressColor;
        }

        if (showDebugLogs || actionDefinition.showDebugLogs)
        {
            Debug.Log($"[SuspiciousAction] Started: {actionDefinition.displayName}");
        }

        float timer = Mathf.Max(0f, actionDefinition.actionDuration);

        while (timer > 0f)
        {
            if (interactor == null)
            {
                CancelAction("interactor missing");
                yield break;
            }

            if (cancelIfPlayerMovesAway &&
                requirePlayerInRange &&
                !IsInteractorInRange(interactor))
            {
                CancelAction("player moved away");
                yield break;
            }

            if (cancelIfPlayerLooksAway &&
                requirePlayerFacingObject &&
                !IsInteractorFacingObject(interactor))
            {
                CancelAction("player looked away");
                yield break;
            }

            timer -= Time.deltaTime;
            yield return null;
        }

        CompleteAction(interactor);
    }

    private void CancelAction(string reason)
    {
        actionInProgress = false;
        actionRoutine = null;

        if (targetRenderer != null)
        {
            targetRenderer.material.color = availableColor;
        }

        if (showDebugLogs)
        {
            Debug.Log($"[SuspiciousAction] Cancelled: {actionDefinition.displayName}. Reason: {reason}");
        }
    }

    private void CompleteAction(Transform interactor)
    {
        actionInProgress = false;
        actionCompleted = true;
        actionRoutine = null;

        CrimeReport report = new CrimeReport
        {
            crimeType = actionDefinition.crimeType,
            suspect = interactor,
            reporter = null,
            crimePosition = transform.position,
            crimeDescription = actionDefinition.crimeDescription,
            suspicionAmount = actionDefinition.suspicionAmount,
            zoneAlertAmount = actionDefinition.zoneAlertAmount,
            immediateChaseIfSeen = actionDefinition.immediateChaseIfSeen,
            canBeReportedByCivilians = actionDefinition.canBeReportedByCivilians,
            createsContraband = actionDefinition.createsContraband,
            crimeObject = gameObject
        };

        if (CrimeManager.Instance != null)
        {
            CrimeManager.Instance.ReportCrime(report);
        }
        else
        {
            Debug.LogWarning("[SuspiciousAction] No CrimeManager found in scene.");
        }

        if (targetRenderer != null)
        {
            targetRenderer.material.color = completedColor;
        }

        if (showDebugLogs || actionDefinition.showDebugLogs)
        {
            Debug.Log($"[SuspiciousAction] Completed: {actionDefinition.displayName}");
        }

        if (actionDefinition.destroyObjectAfterAction)
        {
            Destroy(gameObject);
            return;
        }

        if (actionDefinition.disableObjectAfterAction)
        {
            gameObject.SetActive(false);
        }
    }

    private bool IsInteractorInRange(Transform interactor)
    {
        if (interactor == null)
        {
            return false;
        }

        return Vector3.Distance(interactor.position, GetFocusPosition()) <= interactionDistance;
    }

    private bool IsInteractorFacingObject(Transform interactor)
    {
        if (!requirePlayerFacingObject)
        {
            return true;
        }

        if (interactor == null)
        {
            return false;
        }

        Transform source = facingReference != null ? facingReference : interactor;

        Vector3 directionToObject = GetFocusPosition() - source.position;

        if (directionToObject.sqrMagnitude < 0.001f)
        {
            return true;
        }

        directionToObject.Normalize();

        float angle = Vector3.Angle(source.forward, directionToObject);

        return angle <= maxFacingAngle;
    }

    private Vector3 GetFocusPosition()
    {
        if (interactionFocusPoint != null)
        {
            return interactionFocusPoint.position;
        }

        return transform.position;
    }

    private bool DialogueManagerIsActive()
    {
        DialogueManager dialogueManager = FindObjectOfType<DialogueManager>();

        if (dialogueManager == null)
        {
            return false;
        }

        return dialogueManager.IsDialogueActive;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);

        if (interactionFocusPoint != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(interactionFocusPoint.position, 0.15f);
        }
    }
}