using UnityEngine;

public enum ZoneAccessType
{
    Public,
    StaffOnly,
    GuardOnly,
    NobleOnly,
    TempleOnly,
    Restricted
}

[RequireComponent(typeof(Collider))]
public class RestrictedZone : MonoBehaviour
{
    [Header("Zone Identity")]
    public string zoneID = "ledger_house";
    public string zoneDisplayName = "Ledger House";
    public ZoneAccessType accessType = ZoneAccessType.Restricted;

    [Tooltip("Higher priority wins if the player is inside multiple zones.")]
    public int zonePriority = 0;

    [Header("Allowed Disguises")]
    public string[] allowedDisguiseSetIDs;

    [Header("Disguise Rules")]
    public bool requireCompleteDisguise = true;

    [Range(0f, 1f)]
    public float minimumDisguiseCompleteness = 1f;

    [Header("Suspicion")]
    public bool suspiciousIfUnauthorized = true;
    public float unauthorizedSuspicionPerSecond = 18f;
    public float unauthorizedVisionMultiplier = 1.5f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private Collider zoneCollider;

    private void Awake()
    {
        zoneCollider = GetComponent<Collider>();

        if (zoneCollider != null)
        {
            zoneCollider.isTrigger = true;
        }
    }

    public bool IsPublicZone()
    {
        return accessType == ZoneAccessType.Public;
    }

    public bool IsDisguiseAllowed(string disguiseSetID, float disguiseCompleteness)
    {
        if (IsPublicZone())
        {
            return true;
        }

        if (allowedDisguiseSetIDs == null || allowedDisguiseSetIDs.Length == 0)
        {
            return false;
        }

        if (string.IsNullOrWhiteSpace(disguiseSetID))
        {
            return false;
        }

        string cleanDisguiseID = disguiseSetID.Trim();

        bool disguiseIDAllowed = false;

        foreach (string allowedID in allowedDisguiseSetIDs)
        {
            if (string.IsNullOrWhiteSpace(allowedID))
            {
                continue;
            }

            if (allowedID.Trim() == cleanDisguiseID)
            {
                disguiseIDAllowed = true;
                break;
            }
        }

        if (!disguiseIDAllowed)
        {
            return false;
        }

        if (requireCompleteDisguise)
        {
            return disguiseCompleteness >= minimumDisguiseCompleteness;
        }

        return disguiseCompleteness >= minimumDisguiseCompleteness;
    }

    public string GetZoneDebugName()
    {
        if (!string.IsNullOrWhiteSpace(zoneDisplayName))
        {
            return $"{zoneDisplayName} ({zoneID})";
        }

        return zoneID;
    }

    private void OnValidate()
    {
        Collider col = GetComponent<Collider>();

        if (col != null)
        {
            col.isTrigger = true;
        }
    }
}