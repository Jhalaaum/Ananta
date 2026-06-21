using UnityEngine;

[CreateAssetMenu(
    fileName = "New Suspicious Action",
    menuName = "Stealth/Suspicious Action"
)]
public class SuspiciousActionDefinition : ScriptableObject
{
    [Header("Basic")]
    public string actionID = "steal_item";
    public string displayName = "Steal Item";
    public CrimeType crimeType = CrimeType.Theft;

    [TextArea(2, 4)]
    public string crimeDescription = "stealing a restricted item";

    [Header("Consequences")]
    public float suspicionAmount = 45f;
    public float zoneAlertAmount = 8f;
    public bool immediateChaseIfSeen = false;
    public bool canBeReportedByCivilians = true;
    public bool createsContraband = true;

    [Header("Action Timing")]
    public float actionDuration = 0.75f;
    public bool destroyObjectAfterAction = false;
    public bool disableObjectAfterAction = true;

    [Header("Debug")]
    public bool showDebugLogs = true;
}