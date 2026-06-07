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

public class RestrictedZone : MonoBehaviour
{
    [Header("Zone Identity")]
    public string zoneID;
    public string zoneDisplayName;

    [Header("Access")]
    public ZoneAccessType accessType = ZoneAccessType.Public;

    [Tooltip("Examples: guard, merchant, servant, noble, monk")]
    public string[] allowedDisguiseSetIDs;

    [Header("Suspicion")]
    public bool suspiciousIfUnauthorized = true;
    public float unauthorizedSuspicionPerSecond = 22f;
    public float unauthorizedVisionMultiplier = 1.75f;

    [Header("Disguise Requirements")]
    public bool requireCompleteDisguise;
    [Range(0f, 1f)] public float minimumDisguiseCompleteness = 0.75f;

    public bool IsPublicZone()
    {
        return accessType == ZoneAccessType.Public;
    }

    public bool IsDisguiseAllowed(string disguiseSetID, float completeness)
    {
        if (IsPublicZone())
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(disguiseSetID))
        {
            return false;
        }

        if (requireCompleteDisguise && completeness < minimumDisguiseCompleteness)
        {
            return false;
        }

        if (allowedDisguiseSetIDs == null || allowedDisguiseSetIDs.Length == 0)
        {
            return false;
        }

        string cleanID = disguiseSetID.Trim();

        foreach (string allowedID in allowedDisguiseSetIDs)
        {
            if (string.IsNullOrWhiteSpace(allowedID))
            {
                continue;
            }

            if (allowedID.Trim() == cleanID)
            {
                return true;
            }
        }

        return false;
    }
}