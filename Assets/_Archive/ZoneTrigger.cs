using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ZoneTrigger : MonoBehaviour
{
    [Header("Zone")]
    public string zoneID;

    [Header("Settings")]
    public string playerTag = "Player";

    private void Reset()
    {
        Collider col = GetComponent<Collider>();
        col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
        {
            return;
        }

        ZoneTracker tracker = other.GetComponent<ZoneTracker>();

        if (tracker != null)
        {
            tracker.SetCurrentZone(zoneID);
        }
    }
}