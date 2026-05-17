using UnityEngine;

public class ZoneTracker : MonoBehaviour
{
    [Header("Current Zone")]
    public string currentZoneID = "default_zone";

    public void SetCurrentZone(string zoneID)
    {
        if (string.IsNullOrWhiteSpace(zoneID))
        {
            return;
        }

        currentZoneID = zoneID;
        Debug.Log($"[ZoneTracker] Entered zone: {currentZoneID}");
    }

    public string GetCurrentZoneID()
    {
        return currentZoneID;
    }
}