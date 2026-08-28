using System.Collections.Generic;
using UnityEngine;

public class EvidencePageUI : MonoBehaviour
{
    [Header("References")]
    public Transform contentParent;

    public EvidenceEntryUI evidenceEntryPrefab;

    [Header("Settings")]
    public bool refreshOnEnable = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<EvidenceEntryUI>
        spawnedEntries =
            new List<EvidenceEntryUI>();

    private bool subscribed;

    private void Start()
    {
        Subscribe();

        Refresh();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        Subscribe();

        if (refreshOnEnable)
        {
            Refresh();
        }
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed)
        {
            return;
        }

        if (EvidenceManager.Instance == null)
        {
            return;
        }

        EvidenceManager.Instance.OnEvidenceDiscovered +=
            HandleEvidenceDiscovered;

        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed)
        {
            return;
        }

        if (EvidenceManager.Instance != null)
        {
            EvidenceManager.Instance.OnEvidenceDiscovered -=
                HandleEvidenceDiscovered;
        }

        subscribed = false;
    }

    private void HandleEvidenceDiscovered(
        EvidenceData evidence)
    {
        AddEntry(evidence);
    }

    public void Refresh()
    {
        ClearEntries();

        if (EvidenceManager.Instance == null)
        {
            if (showDebugLogs)
            {
                Debug.LogWarning(
                    "[EvidencePageUI] EvidenceManager missing."
                );
            }

            return;
        }

        List<EvidenceData> evidence =
            EvidenceManager.Instance
                .GetDiscoveredEvidence();

        foreach (EvidenceData item in evidence)
        {
            AddEntry(item);
        }

        if (showDebugLogs)
        {
            Debug.Log(
                "[EvidencePageUI] Refreshed. Evidence count: " +
                evidence.Count
            );
        }
    }

    private void AddEntry(EvidenceData evidence)
    {
        if (evidence == null ||
            evidenceEntryPrefab == null ||
            contentParent == null)
        {
            return;
        }

        foreach (EvidenceEntryUI existing
                 in spawnedEntries)
        {
            if (existing != null &&
                existing.CurrentEvidence == evidence)
            {
                return;
            }
        }

        EvidenceEntryUI entry =
            Instantiate(
                evidenceEntryPrefab,
                contentParent
            );

        entry.Setup(evidence);

        spawnedEntries.Add(entry);
    }

    private void ClearEntries()
    {
        foreach (EvidenceEntryUI entry
                 in spawnedEntries)
        {
            if (entry != null)
            {
                Destroy(entry.gameObject);
            }
        }

        spawnedEntries.Clear();
    }
}