using System.Collections.Generic;
using UnityEngine;

public class PlayerZoneTracker : MonoBehaviour
{
    public static PlayerZoneTracker Instance;

    [Header("Current Zone")]
    public RestrictedZone currentZone;

    [Header("Default/Public Zone")]
    public string publicZoneID = "public_city";

    [Header("Refresh Settings")]
    public LayerMask zoneLayerMask = ~0;
    public float refreshRadius = 0.75f;
    public bool refreshOnStart = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private readonly List<RestrictedZone> zonesCurrentlyInside =
        new List<RestrictedZone>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        if (refreshOnStart)
        {
            ForceRefreshCurrentZone();
        }
    }

    private void Update()
    {
        CleanupMissingZones();
        ResolveCurrentZone();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        RestrictedZone zone =
            other.GetComponent<RestrictedZone>();

        if (zone == null)
        {
            zone =
                other.GetComponentInParent<RestrictedZone>();
        }

        if (zone == null)
        {
            return;
        }

        AddZone(zone);

        ResolveCurrentZone();
    }

    private void OnTriggerExit(Collider other)
    {
        RestrictedZone zone =
            other.GetComponent<RestrictedZone>();

        if (zone == null)
        {
            zone =
                other.GetComponentInParent<RestrictedZone>();
        }

        if (zone == null)
        {
            return;
        }

        RemoveZone(zone);

        ResolveCurrentZone();
    }

    private void AddZone(RestrictedZone zone)
    {
        if (zone == null)
        {
            return;
        }

        if (zonesCurrentlyInside.Contains(zone))
        {
            return;
        }

        zonesCurrentlyInside.Add(zone);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PlayerZoneTracker] Entered zone: " +
                $"{zone.GetZoneDebugName()}"
            );
        }
    }

    private void RemoveZone(RestrictedZone zone)
    {
        if (zone == null)
        {
            return;
        }

        if (!zonesCurrentlyInside.Contains(zone))
        {
            return;
        }

        zonesCurrentlyInside.Remove(zone);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PlayerZoneTracker] Exited zone: " +
                $"{zone.GetZoneDebugName()}"
            );
        }
    }

    private void CleanupMissingZones()
    {
        for (int i = zonesCurrentlyInside.Count - 1;
             i >= 0;
             i--)
        {
            if (zonesCurrentlyInside[i] == null)
            {
                zonesCurrentlyInside.RemoveAt(i);
            }
        }
    }

    private void ResolveCurrentZone()
    {
        CleanupMissingZones();

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

        if (currentZone == bestZone)
        {
            return;
        }

        currentZone = bestZone;

        if (showDebugLogs)
        {
            if (currentZone != null)
            {
                Debug.Log(
                    $"[PlayerZoneTracker] Current zone is now: " +
                    $"{currentZone.GetZoneDebugName()}"
                );
            }
            else
            {
                Debug.Log(
                    $"[PlayerZoneTracker] Current zone is now: " +
                    $"{publicZoneID}"
                );
            }
        }
    }

    public RestrictedZone GetCurrentZone()
    {
        CleanupMissingZones();
        ResolveCurrentZone();

        return currentZone;
    }

    public string GetCurrentZoneID()
    {
        RestrictedZone zone = GetCurrentZone();

        if (zone == null)
        {
            return publicZoneID;
        }

        if (!string.IsNullOrWhiteSpace(zone.zoneID))
        {
            return zone.zoneID.Trim();
        }

        return zone.gameObject.name;
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

    public bool IsInPublicZone()
    {
        RestrictedZone zone = GetCurrentZone();

        if (zone == null)
        {
            return true;
        }

        return zone.IsPublicZone();
    }

    public bool IsCurrentlyInsideZone(
        RestrictedZone zone)
    {
        if (zone == null)
        {
            return false;
        }

        CleanupMissingZones();

        return zonesCurrentlyInside.Contains(zone);
    }

    public bool IsCurrentlyInsideZoneID(
        string zoneID)
    {
        if (string.IsNullOrWhiteSpace(zoneID))
        {
            return false;
        }

        CleanupMissingZones();

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
            Debug.Log(
                "[PlayerZoneTracker] Force cleared all zones."
            );
        }
    }

    public void ForceRefreshCurrentZone()
    {
        zonesCurrentlyInside.Clear();

        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            refreshRadius,
            zoneLayerMask,
            QueryTriggerInteraction.Collide
        );

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            RestrictedZone zone =
                hit.GetComponent<RestrictedZone>();

            if (zone == null)
            {
                zone =
                    hit.GetComponentInParent<RestrictedZone>();
            }

            if (zone == null)
            {
                continue;
            }

            if (!zonesCurrentlyInside.Contains(zone))
            {
                zonesCurrentlyInside.Add(zone);
            }
        }

        ResolveCurrentZone();

        if (showDebugLogs)
        {
            Debug.Log(
                $"[PlayerZoneTracker] Force refresh complete. " +
                $"Detected {zonesCurrentlyInside.Count} zone(s). " +
                $"Current: {GetCurrentZoneID()}"
            );
        }
    }

    public void ForceRefreshCurrentZoneAfterTeleport()
    {
        ForceRefreshCurrentZone();
    }

    public int GetOverlappingZoneCount()
    {
        CleanupMissingZones();

        return zonesCurrentlyInside.Count;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(
            transform.position,
            refreshRadius
        );
    }
}