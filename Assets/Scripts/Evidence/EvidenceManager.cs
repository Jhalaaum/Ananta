using System;
using System.Collections.Generic;
using UnityEngine;

public class EvidenceManager : MonoBehaviour
{
    public static EvidenceManager Instance;

    [Header("Evidence Database")]
    [Tooltip(
        "Add every EvidenceData asset used in this scene/game here."
    )]
    public List<EvidenceData> evidenceDatabase =
        new List<EvidenceData>();

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly HashSet<string> discoveredIDs =
        new HashSet<string>();

    public event Action<EvidenceData> OnEvidenceDiscovered;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public bool DiscoverEvidence(EvidenceData evidence)
    {
        if (evidence == null)
        {
            Debug.LogWarning(
                "[EvidenceManager] Tried to discover null evidence."
            );

            return false;
        }

        if (string.IsNullOrWhiteSpace(evidence.evidenceID))
        {
            Debug.LogWarning(
                "[EvidenceManager] Evidence has an empty evidenceID: " +
                evidence.name
            );

            return false;
        }

        string id = evidence.evidenceID.Trim();

        if (discoveredIDs.Contains(id))
        {
            if (showDebugLogs)
            {
                Debug.Log(
                    "[EvidenceManager] Already discovered: " +
                    evidence.displayName
                );
            }

            return false;
        }

        discoveredIDs.Add(id);

        if (showDebugLogs)
        {
            Debug.Log(
                "[EvidenceManager] Discovered evidence: " +
                evidence.displayName
            );
        }

        OnEvidenceDiscovered?.Invoke(evidence);

        return true;
    }

    public bool HasEvidence(string evidenceID)
    {
        if (string.IsNullOrWhiteSpace(evidenceID))
        {
            return false;
        }

        return discoveredIDs.Contains(
            evidenceID.Trim()
        );
    }

    public EvidenceData GetEvidenceByID(string evidenceID)
    {
        if (string.IsNullOrWhiteSpace(evidenceID))
        {
            return null;
        }

        string targetID = evidenceID.Trim();

        foreach (EvidenceData evidence in evidenceDatabase)
        {
            if (evidence == null)
            {
                continue;
            }

            if (evidence.evidenceID == targetID)
            {
                return evidence;
            }
        }

        return null;
    }

    public List<EvidenceData> GetDiscoveredEvidence()
    {
        List<EvidenceData> result =
            new List<EvidenceData>();

        foreach (EvidenceData evidence in evidenceDatabase)
        {
            if (evidence == null)
            {
                continue;
            }

            if (HasEvidence(evidence.evidenceID))
            {
                result.Add(evidence);
            }
        }

        return result;
    }

    public void ClearAllEvidence()
    {
        discoveredIDs.Clear();

        if (showDebugLogs)
        {
            Debug.Log(
                "[EvidenceManager] All discovered evidence cleared."
            );
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}