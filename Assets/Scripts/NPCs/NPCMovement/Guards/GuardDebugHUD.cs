using UnityEngine;

public class GuardDebugHUD : MonoBehaviour
{
    [Header("Debug HUD")]
    public bool showHUD = true;
    public bool printToConsole;
    public float consolePrintInterval = 1f;

    [Header("Target")]
    public AdvancedGuardAIController[] guards;

    [Header("Player Systems")]
    public PlayerCoverIdentity playerCoverIdentity;
    public PlayerZoneTracker playerZoneTracker;

    [Header("HUD Position")]
    public int x = 20;
    public int y = 20;
    public int width = 520;
    public int lineHeight = 22;

    private float consoleTimer;

    private void Start()
    {
        AutoFindReferences();
    }

    private void Update()
    {
        if (guards == null || guards.Length == 0)
        {
            AutoFindReferences();
        }

        if (printToConsole)
        {
            consoleTimer -= Time.deltaTime;

            if (consoleTimer <= 0f)
            {
                consoleTimer = consolePrintInterval;
                PrintDebugToConsole();
            }
        }
    }

    private void AutoFindReferences()
    {
        if (guards == null || guards.Length == 0)
        {
            guards = FindObjectsOfType<AdvancedGuardAIController>();
        }

        if (playerCoverIdentity == null)
        {
            playerCoverIdentity = FindObjectOfType<PlayerCoverIdentity>();
        }

        if (playerZoneTracker == null)
        {
            playerZoneTracker = FindObjectOfType<PlayerZoneTracker>();
        }
    }

    private void OnGUI()
    {
        if (!showHUD)
        {
            return;
        }

        int currentY = y;

        GUI.Box(new Rect(x - 10, y - 10, width, GetBoxHeight()), "Guard / Disguise Debug");

        DrawLine(ref currentY, BuildPlayerCoverLine());
        DrawLine(ref currentY, BuildZoneLine());
        DrawLine(ref currentY, "");

        if (guards == null || guards.Length == 0)
        {
            DrawLine(ref currentY, "No guards found.");
            return;
        }

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard == null)
            {
                continue;
            }

            DrawLine(ref currentY, BuildGuardLine(guard));
        }
    }

    private int GetBoxHeight()
    {
        int guardCount = guards != null ? guards.Length : 0;
        return 100 + guardCount * lineHeight;
    }

    private void DrawLine(ref int currentY, string text)
    {
        GUI.Label(new Rect(x, currentY, width - 20, lineHeight), text);
        currentY += lineHeight;
    }

    private string BuildPlayerCoverLine()
    {
        if (playerCoverIdentity == null)
        {
            return "Cover: No PlayerCoverIdentity found";
        }

        string setID = playerCoverIdentity.GetBestDisguiseSetID();
        float completeness = playerCoverIdentity.GetBestDisguiseCompleteness();

        if (string.IsNullOrWhiteSpace(setID))
        {
            setID = "none";
        }

        return $"Cover: {setID} | Completeness: {Mathf.RoundToInt(completeness * 100f)}%";
    }

    private string BuildZoneLine()
    {
        if (playerZoneTracker == null)
        {
            return "Zone: No PlayerZoneTracker found";
        }

        RestrictedZone zone = playerZoneTracker.GetCurrentZone();

        if (zone == null)
        {
            return "Zone: none / public";
        }

        bool allowed = playerCoverIdentity == null || playerCoverIdentity.IsAllowedInZone(zone);

        return $"Zone: {zone.zoneDisplayName} ({zone.accessType}) | Allowed: {allowed}";
    }

    private string BuildGuardLine(AdvancedGuardAIController guard)
    {
        string guardName = guard.gameObject.name;
        string state = guard.currentState.ToString();
        float suspicion = guard.suspicion;

        string coverSet = "none";
        float completeness = 0f;
        string zoneName = "none";
        bool allowed = true;

        if (playerCoverIdentity != null)
        {
            coverSet = playerCoverIdentity.GetBestDisguiseSetID();

            if (string.IsNullOrWhiteSpace(coverSet))
            {
                coverSet = "none";
            }

            completeness = playerCoverIdentity.GetBestDisguiseCompleteness();
        }

        if (playerZoneTracker != null)
        {
            RestrictedZone zone = playerZoneTracker.GetCurrentZone();

            if (zone != null)
            {
                zoneName = zone.zoneDisplayName;
                allowed = playerCoverIdentity == null || playerCoverIdentity.IsAllowedInZone(zone);
            }
        }

        return
            $"{guardName} | State: {state} | Suspicion: {suspicion:0.0}/100 | " +
            $"Cover: {coverSet} {Mathf.RoundToInt(completeness * 100f)}% | " +
            $"Zone: {zoneName} | Allowed: {allowed}";
    }

    private void PrintDebugToConsole()
    {
        Debug.Log("[GuardDebugHUD] " + BuildPlayerCoverLine());
        Debug.Log("[GuardDebugHUD] " + BuildZoneLine());

        if (guards == null)
        {
            return;
        }

        foreach (AdvancedGuardAIController guard in guards)
        {
            if (guard != null)
            {
                Debug.Log("[GuardDebugHUD] " + BuildGuardLine(guard));
            }
        }
    }
}