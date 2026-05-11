using UnityEngine;

[CreateAssetMenu(menuName = "Guards/Guard Profile")]
public class GuardProfile : ScriptableObject
{
    public enum GuardType
    {
        CityGuard,
        ScoutGuard,
        PalaceElite,
        ForestWatcher,
        TempleGuardian
    }

    [Header("Identity")]
    public GuardType guardType;

    [Header("Movement")]
    public float patrolSpeed = 2.5f;
    public float investigateSpeed = 3.2f;
    public float chaseSpeed = 5.2f;
    public float returnSpeed = 2.8f;

    [Header("Suspicion")]
    public float suspiciousThreshold = 35f;
    public float chaseThreshold = 100f;
    public float suspicionIncreasePerSecond = 35f;
    public float suspicionDecreasePerSecond = 18f;
    public float soundSuspicionBoost = 25f;
    public float minAudibilityToInvestigate = 0.12f;

    [Header("Vision")]
    public float viewDistance = 10f;
    public float viewAngle = 90f;
    public bool canDetectWeakDisguise = false;

    [Header("Search")]
    public int searchPointCount = 3;
    public float searchRadius = 6f;

    [Header("Difficulty Feel")]
    public bool easyToDistract = true;
    public bool checksRooftopsOrForestPaths = false;
    public bool highlySensitiveToSound = false;
}