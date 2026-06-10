using System.Collections.Generic;
using UnityEngine;

public class PlayerZoneTracker : MonoBehaviour
{
    public static PlayerZoneTracker Instance;

    [Header("Current Zone")]
    public RestrictedZone currentZone;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<RestrictedZone> zonesCurrentlyInside = new List<RestrictedZone>();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        CleanupMissingZones();
        ResolveCurrentZone();
    }

    private void OnTriggerEnter(Collider other)
    {
        RestrictedZone zone = other.GetComponent<RestrictedZone>();

        if (zone == null)
        {
            zone = other.GetComponentInParent<RestrictedZone>();
        }

        if (zone == null)
        {
            return;
        }

        if (!zonesCurrentlyInside.Contains(zone))
        {
            zonesCurrentlyInside.Add(zone);

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerZoneTracker] Entered zone: {zone.GetZoneDebugName()}");
            }
        }

        ResolveCurrentZone();
    }

    private void OnTriggerExit(Collider other)
    {
        RestrictedZone zone = other.GetComponent<RestrictedZone>();

        if (zone == null)
        {
            zone = other.GetComponentInParent<RestrictedZone>();
        }

        if (zone == null)
        {
            return;
        }

        if (zonesCurrentlyInside.Contains(zone))
        {
            zonesCurrentlyInside.Remove(zone);

            if (showDebugLogs)
            {
                Debug.Log($"[PlayerZoneTracker] Exited zone: {zone.GetZoneDebugName()}");
            }
        }

        ResolveCurrentZone();
    }

    private void CleanupMissingZones()
    {
        for (int i = zonesCurrentlyInside.Count - 1; i >= 0; i--)
        {
            if (zonesCurrentlyInside[i] == null)
            {
                zonesCurrentlyInside.RemoveAt(i);
            }
        }
    }

    private void ResolveCurrentZone()
    {
        RestrictedZone bestZone = null;
        int bestPriority = int.MinValue;

        foreach (RestrictedZone zone in zonesCurrentlyInside)
        {
            if (zone == null)
            {
                continue;
            }

            if (zone.zonePriority > bestPriority)
            {
                bestPriority = zone.zonePriority;
                bestZone = zone;
            }
        }

        if (currentZone != bestZone)
        {
            currentZone = bestZone;

            if (showDebugLogs)
            {
                if (currentZone != null)
                {
                    Debug.Log($"[PlayerZoneTracker] Current zone is now: {currentZone.GetZoneDebugName()}");
                }
                else
                {
                    Debug.Log("[PlayerZoneTracker] Current zone is now: PUBLIC / NONE");
                }
            }
        }
    }

    public RestrictedZone GetCurrentZone()
    {
        CleanupMissingZones();
        ResolveCurrentZone();
        return currentZone;
    }

    public bool IsInsideRestrictedZone()
    {
        RestrictedZone zone = GetCurrentZone();

        if (zone == null)
        {
            return false;
        }

        return !zone.IsPublicZone();
    }

    public bool IsCurrentlyInsideZone(RestrictedZone zone)
    {
        if (zone == null)
        {
            return false;
        }

        return zonesCurrentlyInside.Contains(zone);
    }

    public bool IsCurrentlyInsideZoneID(string zoneID)
    {
        if (string.IsNullOrWhiteSpace(zoneID))
        {
            return false;
        }

        string cleanID = zoneID.Trim();

        foreach (RestrictedZone zone in zonesCurrentlyInside)
        {
            if (zone == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(zone.zoneID) &&
                zone.zoneID.Trim() == cleanID)
            {
                return true;
            }
        }

        return false;
    }

    public void ForceClearZones()
    {
        zonesCurrentlyInside.Clear();
        currentZone = null;

        if (showDebugLogs)
        {
            Debug.Log("[PlayerZoneTracker] Force cleared all zones.");
        }
    }
}