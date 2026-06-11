using UnityEngine;

public class ZoneTracker : MonoBehaviour
{
    [Header("Current Zone")]
    public string currentZoneID = "default_zone";
    public RestrictedZone currentRestrictedZone;

    [Header("Refresh Settings")]
    public LayerMask zoneLayerMask = ~0;
    public float refreshRadius = 0.75f;
    public bool refreshOnStart = true;
    public bool useTriggersOnly = true;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private void Start()
    {
        if (refreshOnStart)
        {
            ForceRefreshCurrentZone();
        }
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

        SetCurrentZone(zone);
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

        if (currentRestrictedZone == zone)
        {
            currentRestrictedZone = null;
            currentZoneID = "public_city";

            Log("Exited restricted zone. Current zone set to public_city.");
        }
    }

    public void SetCurrentZone(string zoneID)
    {
        if (string.IsNullOrWhiteSpace(zoneID))
        {
            return;
        }

        currentZoneID = zoneID.Trim();

        RestrictedZone foundZone = FindRestrictedZoneByID(currentZoneID);

        if (foundZone != null)
        {
            currentRestrictedZone = foundZone;
        }
        else if (currentZoneID == "public_city" || currentZoneID == "default_zone")
        {
            currentRestrictedZone = null;
        }

        Log($"Entered zone: {currentZoneID}");
    }

    public void SetCurrentZone(RestrictedZone zone)
    {
        currentRestrictedZone = zone;

        if (zone == null)
        {
            currentZoneID = "public_city";
            Log("Current zone set to public_city.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(zone.zoneID))
        {
            currentZoneID = zone.zoneID.Trim();
        }
        else
        {
            currentZoneID = zone.gameObject.name;
        }

        Log($"Entered zone: {currentZoneID}");
    }

    public string GetCurrentZoneID()
    {
        if (currentRestrictedZone != null && !string.IsNullOrWhiteSpace(currentRestrictedZone.zoneID))
        {
            return currentRestrictedZone.zoneID.Trim();
        }

        if (string.IsNullOrWhiteSpace(currentZoneID))
        {
            return "public_city";
        }

        return currentZoneID.Trim();
    }

    public RestrictedZone GetCurrentRestrictedZone()
    {
        return currentRestrictedZone;
    }

    public bool IsInPublicZone()
    {
        if (currentRestrictedZone == null)
        {
            return true;
        }

        return currentRestrictedZone.IsPublicZone();
    }

    public void ForceRefreshCurrentZone()
    {
        RestrictedZone bestZone = FindZoneAtCurrentPosition();

        if (bestZone != null)
        {
            SetCurrentZone(bestZone);
            return;
        }

        currentRestrictedZone = null;
        currentZoneID = "public_city";

        Log("Force refresh found no restricted zone. Current zone set to public_city.");
    }

    public void ForceRefreshCurrentZoneAfterTeleport()
    {
        ForceRefreshCurrentZone();
    }

    private RestrictedZone FindZoneAtCurrentPosition()
    {
        Collider[] hits = Physics.OverlapSphere(
            transform.position,
            refreshRadius,
            zoneLayerMask,
            QueryTriggerInteraction.Collide
        );

        RestrictedZone bestZone = null;
        float bestScore = -999f;

        foreach (Collider hit in hits)
        {
            if (hit == null)
            {
                continue;
            }

            RestrictedZone zone = hit.GetComponent<RestrictedZone>();

            if (zone == null)
            {
                zone = hit.GetComponentInParent<RestrictedZone>();
            }

            if (zone == null)
            {
                continue;
            }

            float distance = Vector3.Distance(transform.position, hit.ClosestPoint(transform.position));
            float score = -distance;

            if (!zone.IsPublicZone())
            {
                score += 10f;
            }

            if (score > bestScore)
            {
                bestScore = score;
                bestZone = zone;
            }
        }

        return bestZone;
    }

    private RestrictedZone FindRestrictedZoneByID(string zoneID)
    {
        if (string.IsNullOrWhiteSpace(zoneID))
        {
            return null;
        }

        RestrictedZone[] zones = FindObjectsOfType<RestrictedZone>();

        foreach (RestrictedZone zone in zones)
        {
            if (zone == null)
            {
                continue;
            }

            if (!string.IsNullOrWhiteSpace(zone.zoneID) &&
                zone.zoneID.Trim() == zoneID.Trim())
            {
                return zone;
            }
        }

        return null;
    }

    private void Log(string message)
    {
        if (showDebugLogs)
        {
            Debug.Log($"[ZoneTracker] {message}");
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, refreshRadius);
    }
}