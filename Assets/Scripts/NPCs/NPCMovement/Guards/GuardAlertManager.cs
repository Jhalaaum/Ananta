using UnityEngine;

public class GuardAlertManager : MonoBehaviour
{
    public static GuardAlertManager Instance;

    [Header("Default Alert Settings")]
    public float defaultAlertRadius = 18f;
    public float defaultUrgency = 1f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void BroadcastGuardAlert(
        AdvancedGuardAIController caller,
        Vector3 alertPosition,
        float alertRadius,
        float urgency,
        string reason
    )
    {
        if (caller == null)
        {
            return;
        }

        AdvancedGuardAIController[] guards = FindObjectsOfType<AdvancedGuardAIController>();

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            if (guard == caller)
            {
                continue;
            }

            float distance = Vector3.Distance(
                caller.transform.position,
                guard.transform.position
            );

            if (distance > alertRadius)
            {
                continue;
            }

            guard.ReceiveExternalGuardAlert(
                caller,
                alertPosition,
                urgency,
                reason
            );
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardAlertManager] {caller.name} called nearby guards. " +
                $"Reason: {reason}, Radius: {alertRadius}, Position: {alertPosition}"
            );
        }
    }
}