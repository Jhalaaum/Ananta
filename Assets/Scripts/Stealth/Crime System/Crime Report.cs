using UnityEngine;

[System.Serializable]
public class CrimeReport
{
    public CrimeType crimeType;
    public Transform suspect;
    public Transform reporter;
    public Vector3 crimePosition;

    public string crimeDescription;

    public float suspicionAmount;
    public float zoneAlertAmount;

    public bool immediateChaseIfSeen;
    public bool canBeReportedByCivilians;
    public bool createsContraband;

    public GameObject crimeObject;
}