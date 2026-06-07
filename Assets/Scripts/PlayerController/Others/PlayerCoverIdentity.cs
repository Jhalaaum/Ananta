using UnityEngine;

public class PlayerCoverIdentity : MonoBehaviour
{
    public static PlayerCoverIdentity Instance;

    private void Awake()
    {
        Instance = this;
    }

    public string GetBestDisguiseSetID()
    {
        if (EquipmentManager.Instance == null)
        {
            return "";
        }

        return EquipmentManager.Instance.GetBestDisguiseSetID();
    }

    public float GetBestDisguiseCompleteness()
    {
        if (EquipmentManager.Instance == null)
        {
            return 0f;
        }

        return EquipmentManager.Instance.GetBestDisguiseCompleteness();
    }

    public bool HasCoverStory(string coverStoryID)
    {
        if (string.IsNullOrWhiteSpace(coverStoryID))
        {
            return false;
        }

        return DialogueFlagManager.Instance != null &&
               DialogueFlagManager.Instance.HasFlag("cover_" + coverStoryID);
    }

    public bool IsAllowedInZone(RestrictedZone zone)
    {
        if (zone == null || zone.IsPublicZone())
        {
            return true;
        }

        string disguiseSet = GetBestDisguiseSetID();
        float completeness = GetBestDisguiseCompleteness();

        return zone.IsDisguiseAllowed(disguiseSet, completeness);
    }

    public float GetZoneSuspicionMultiplier(RestrictedZone zone)
    {
        if (zone == null || zone.IsPublicZone())
        {
            return 1f;
        }

        bool allowed = IsAllowedInZone(zone);
        float completeness = GetBestDisguiseCompleteness();

        if (allowed)
        {
            if (completeness >= 1f)
            {
                return 0.2f;
            }

            if (completeness >= 0.75f)
            {
                return 0.45f;
            }

            return 0.7f;
        }

        return 1.5f;
    }
}