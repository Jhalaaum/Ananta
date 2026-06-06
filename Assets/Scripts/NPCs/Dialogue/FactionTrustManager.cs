using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class FactionTrustEntry
{
    public string factionID;
    public int trust;
}

public class FactionTrustManager : MonoBehaviour
{
    public static FactionTrustManager Instance;

    [Header("Faction Trust")]
    public List<FactionTrustEntry> factionTrust = new List<FactionTrustEntry>();

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

    public int GetTrust(string factionID)
    {
        if (string.IsNullOrWhiteSpace(factionID))
        {
            return 0;
        }

        FactionTrustEntry entry = GetOrCreateEntry(factionID);
        return entry.trust;
    }

    public void SetTrust(string factionID, int value)
    {
        FactionTrustEntry entry = GetOrCreateEntry(factionID);
        entry.trust = value;

        Log($"Trust set: {factionID} = {entry.trust}");
    }

    public void AddTrust(string factionID, int amount)
    {
        FactionTrustEntry entry = GetOrCreateEntry(factionID);
        entry.trust += amount;

        Log($"Trust changed: {factionID} = {entry.trust}");
    }

    private FactionTrustEntry GetOrCreateEntry(string factionID)
    {
        string cleanID = factionID.Trim();

        foreach (FactionTrustEntry entry in factionTrust)
        {
            if (entry.factionID == cleanID)
            {
                return entry;
            }
        }

        FactionTrustEntry newEntry = new FactionTrustEntry
        {
            factionID = cleanID,
            trust = 0
        };

        factionTrust.Add(newEntry);
        return newEntry;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log("[FactionTrustManager] " + message);
        }
    }
}