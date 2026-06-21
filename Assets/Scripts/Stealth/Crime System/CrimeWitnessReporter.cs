using System.Collections;
using UnityEngine;

public class CrimeWitnessReporter : MonoBehaviour
{
    [Header("Witness Settings")]
    public bool canWitnessCrimes = true;
    public bool isCivilian = false;
    public bool isGuard = false;

    public float witnessDistance = 10f;
    [Range(1f, 180f)] public float witnessAngle = 100f;
    public float eyeHeight = 1.5f;

    [Header("Line Of Sight")]
    public LayerMask lineOfSightMask = ~0;
    public bool requireLineOfSight = true;

    [Header("Civilian Reporting")]
    public float civilianReportDelay = 1.5f;
    public float nearestGuardSearchRadius = 18f;
    public bool civilianReportsToNearestGuard = true;

    [Header("Runtime")]
    public bool hasPendingReport;
    public CrimeReport pendingReport;
    [TextArea(2, 4)] public string witnessDebugStatus;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private AdvancedGuardAIController ownGuardAI;

    private void Awake()
    {
        ownGuardAI = GetComponent<AdvancedGuardAIController>();

        if (ownGuardAI != null)
        {
            isGuard = true;
        }
    }

    private void OnEnable()
    {
        CrimeManager.OnCrimeCommitted += HandleCrimeCommitted;
    }

    private void OnDisable()
    {
        CrimeManager.OnCrimeCommitted -= HandleCrimeCommitted;
    }

    private void HandleCrimeCommitted(CrimeReport report)
    {
        if (!canWitnessCrimes || report == null)
        {
            return;
        }

        if (isCivilian && !report.canBeReportedByCivilians)
        {
            return;
        }

        if (!CanWitnessCrime(report))
        {
            return;
        }

        CrimeReport witnessedReport = CopyReport(report);
        witnessedReport.reporter = transform;

        witnessDebugStatus =
            $"Witnessed {report.crimeType}\n" +
            $"Description: {report.crimeDescription}\n" +
            $"Position: {report.crimePosition}";

        if (showDebugLogs)
        {
            Debug.Log($"[{gameObject.name}] Witnessed crime: {report.crimeType}");
        }

        if (isGuard && ownGuardAI != null)
        {
            ownGuardAI.ReceiveCrimeReport(witnessedReport);
            return;
        }

        if (isCivilian && civilianReportsToNearestGuard)
        {
            StartCoroutine(CivilianReportRoutine(witnessedReport));
        }
    }

    private bool CanWitnessCrime(CrimeReport report)
    {
        Vector3 eyePosition = transform.position + Vector3.up * eyeHeight;
        Vector3 directionToCrime = report.crimePosition - eyePosition;
        float distance = directionToCrime.magnitude;

        if (distance > witnessDistance)
        {
            return false;
        }

        if (distance <= 0.01f)
        {
            return true;
        }

        Vector3 direction = directionToCrime.normalized;
        float angle = Vector3.Angle(transform.forward, direction);

        if (angle > witnessAngle * 0.5f)
        {
            return false;
        }

        if (requireLineOfSight)
        {
            if (Physics.Raycast(
                    eyePosition,
                    direction,
                    out RaycastHit hit,
                    distance,
                    lineOfSightMask,
                    QueryTriggerInteraction.Ignore))
            {
                if (hit.collider != null &&
                    !hit.collider.transform.IsChildOf(report.crimeObject.transform))
                {
                    return false;
                }
            }
        }

        return true;
    }

    private IEnumerator CivilianReportRoutine(CrimeReport report)
    {
        hasPendingReport = true;
        pendingReport = report;

        yield return new WaitForSeconds(civilianReportDelay);

        AdvancedGuardAIController nearestGuard = FindNearestGuard();

        if (nearestGuard != null)
        {
            nearestGuard.ReceiveCrimeReport(report);

            if (showDebugLogs)
            {
                Debug.Log($"[{gameObject.name}] Reported crime to {nearestGuard.gameObject.name}");
            }
        }

        hasPendingReport = false;
        pendingReport = null;
    }

    private AdvancedGuardAIController FindNearestGuard()
    {
        AdvancedGuardAIController[] guards = FindObjectsOfType<AdvancedGuardAIController>();

        AdvancedGuardAIController nearest = null;
        float nearestDistance = Mathf.Infinity;

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, guard.transform.position);

            if (distance > nearestGuardSearchRadius)
            {
                continue;
            }

            if (distance < nearestDistance)
            {
                nearestDistance = distance;
                nearest = guard;
            }
        }

        return nearest;
    }

    private CrimeReport CopyReport(CrimeReport source)
    {
        return new CrimeReport
        {
            crimeType = source.crimeType,
            suspect = source.suspect,
            reporter = source.reporter,
            crimePosition = source.crimePosition,
            crimeDescription = source.crimeDescription,
            suspicionAmount = source.suspicionAmount,
            zoneAlertAmount = source.zoneAlertAmount,
            immediateChaseIfSeen = source.immediateChaseIfSeen,
            canBeReportedByCivilians = source.canBeReportedByCivilians,
            createsContraband = source.createsContraband,
            crimeObject = source.crimeObject
        };
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = isCivilian ? Color.green : Color.red;
        Gizmos.DrawWireSphere(transform.position, witnessDistance);
    }
}