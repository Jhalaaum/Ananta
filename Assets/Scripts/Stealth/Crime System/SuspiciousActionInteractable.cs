using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class SuspiciousActionInteractable : MonoBehaviour
{
    [Header("Action")]
    public SuspiciousActionDefinition actionDefinition;

    [Header("Interaction")]
    public string playerTag = "Player";
    public float interactionDistance = 2.2f;
    public Key interactKey = Key.E;
    public bool requirePlayerInRange = true;

    [Header("Runtime")]
    public bool playerInRange;
    public bool actionInProgress;
    public bool actionCompleted;

    [Header("Optional Debug Visual")]
    public Renderer targetRenderer;
    public Color availableColor = Color.yellow;
    public Color completedColor = Color.gray;

    private Transform player;

    private void Awake()
    {
        if (targetRenderer == null)
        {
            targetRenderer = GetComponentInChildren<Renderer>();
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);

        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private void Update()
    {
        if (actionDefinition == null || actionCompleted || actionInProgress)
        {
            return;
        }

        UpdatePlayerRange();

        if (requirePlayerInRange && !playerInRange)
        {
            return;
        }

        if (Keyboard.current != null &&
            Keyboard.current[interactKey].wasPressedThisFrame)
        {
            StartCoroutine(PerformActionRoutine());
        }
    }

    private void UpdatePlayerRange()
    {
        if (player == null)
        {
            playerInRange = false;
            return;
        }

        playerInRange = Vector3.Distance(transform.position, player.position) <= interactionDistance;
    }

    private IEnumerator PerformActionRoutine()
    {
        actionInProgress = true;

        if (actionDefinition.showDebugLogs)
        {
            Debug.Log($"[SuspiciousAction] Started: {actionDefinition.displayName}");
        }

        float timer = Mathf.Max(0f, actionDefinition.actionDuration);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        CompleteAction();
    }

    private void CompleteAction()
    {
        actionInProgress = false;
        actionCompleted = true;

        CrimeReport report = new CrimeReport
        {
            crimeType = actionDefinition.crimeType,
            suspect = player,
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

        if (targetRenderer != null)
        {
            targetRenderer.material.color = completedColor;
        }

        if (actionDefinition.destroyObjectAfterAction)
        {
            Destroy(gameObject);
        }
        else if (actionDefinition.disableObjectAfterAction)
        {
            gameObject.SetActive(false);
        }

        if (actionDefinition.showDebugLogs)
        {
            Debug.Log($"[SuspiciousAction] Completed: {actionDefinition.displayName}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactionDistance);
    }
}