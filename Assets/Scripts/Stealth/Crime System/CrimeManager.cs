using System;
using UnityEngine;

public class CrimeManager : MonoBehaviour
{
    public static CrimeManager Instance;

    public static event Action<CrimeReport> OnCrimeCommitted;

    [Header("Debug")]
    public bool showDebugLogs = true;
    public CrimeReport lastCrimeReport;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void ReportCrime(CrimeReport report)
    {
        if (report == null)
        {
            return;
        }

        lastCrimeReport = report;

        if (showDebugLogs)
        {
            Debug.Log(
                $"[CrimeManager] Crime reported: {report.crimeType} / " +
                $"{report.crimeDescription} at {report.crimePosition}"
            );
        }

        OnCrimeCommitted?.Invoke(report);
    }
}