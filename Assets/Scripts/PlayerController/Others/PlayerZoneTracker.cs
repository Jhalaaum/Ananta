using UnityEngine;

public class PlayerZoneTracker : MonoBehaviour
{
    public static PlayerZoneTracker Instance;

    [Header("Current Zone")]
    public RestrictedZone currentZone;

    private void Awake()
    {
        Instance = this;
    }

    private void OnTriggerEnter(Collider other)
    {
        RestrictedZone zone = other.GetComponent<RestrictedZone>();

        if (zone != null)
        {
            currentZone = zone;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        RestrictedZone zone = other.GetComponent<RestrictedZone>();

        if (zone != null && currentZone == zone)
        {
            currentZone = null;
        }
    }

    public RestrictedZone GetCurrentZone()
    {
        return currentZone;
    }
}