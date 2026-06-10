using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ZoneAlertManager : MonoBehaviour
{
    public static ZoneAlertManager Instance;

    [Header("Alert Values")]
    public float suspiciousThreshold = 25f;
    public float alertedThreshold = 55f;
    public float lockdownThreshold = 85f;

    [Header("Decay")]
    public bool decayOverTime = true;
    public float calmDecayPerSecond = 0.6f;
    public float suspiciousDecayPerSecond = 0.35f;
    public float alertedDecayPerSecond = 0.18f;
    public float lockdownDecayPerSecond = 0.08f;

    [Header("Suspicion Effects")]
    public float calmSuspicionMultiplier = 1f;
    public float suspiciousSuspicionMultiplier = 1.2f;
    public float alertedSuspicionMultiplier = 1.45f;
    public float lockdownSuspicionMultiplier = 1.85f;

    [Header("Questioning Effects")]
    public float calmQuestioningThresholdModifier = 0f;
    public float suspiciousQuestioningThresholdModifier = -5f;
    public float alertedQuestioningThresholdModifier = -12f;
    public float lockdownQuestioningThresholdModifier = -22f;

    [Header("Sound Investigation Effects")]
    public float calmSoundSensitivityMultiplier = 1f;
    public float suspiciousSoundSensitivityMultiplier = 1.15f;
    public float alertedSoundSensitivityMultiplier = 1.35f;
    public float lockdownSoundSensitivityMultiplier = 1.6f;

    public float calmInvestigationSpeedMultiplier = 1f;
    public float suspiciousInvestigationSpeedMultiplier = 1.08f;
    public float alertedInvestigationSpeedMultiplier = 1.18f;
    public float lockdownInvestigationSpeedMultiplier = 1.32f;

    [Header("Search Effects")]
    public float calmSearchRadiusMultiplier = 1f;
    public float suspiciousSearchRadiusMultiplier = 1.15f;
    public float alertedSearchRadiusMultiplier = 1.35f;
    public float lockdownSearchRadiusMultiplier = 1.65f;

    public int calmExtraSearchPoints = 0;
    public int suspiciousExtraSearchPoints = 1;
    public int alertedExtraSearchPoints = 2;
    public int lockdownExtraSearchPoints = 3;

    public float calmSearchWaitMultiplier = 1f;
    public float suspiciousSearchWaitMultiplier = 1.1f;
    public float alertedSearchWaitMultiplier = 1.25f;
    public float lockdownSearchWaitMultiplier = 1.45f;

    [Header("Records")]
    public List<ZoneAlertRecord> records = new List<ZoneAlertRecord>();

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool enablePrintKey = true;
    public Key printAlertKey = Key.N;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        if (decayOverTime)
        {
            DecayAlerts();
        }

        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        if (!enablePrintKey || Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[printAlertKey].wasPressedThisFrame)
        {
            PrintAlerts();
        }
    }

    public void AddAlert(string zoneID, float amount, string reason)
    {
        ZoneAlertRecord record = GetOrCreateRecord(zoneID);
        ZoneAlertLevel oldLevel = record.alertLevel;

        record.alertValue += amount;
        record.alertValue = Mathf.Clamp(record.alertValue, 0f, 100f);
        record.timeSinceLastIncrease = 0f;
        record.alertLevel = CalculateLevel(record.alertValue);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[ZoneAlert] {record.zoneID}: +{amount:0.0} because {reason}. " +
                $"Value: {record.alertValue:0.0}, Level: {record.alertLevel}"
            );
        }

        if (oldLevel != record.alertLevel && showDebugLogs)
        {
            Debug.Log($"[ZoneAlert] {record.zoneID} changed from {oldLevel} to {record.alertLevel}");
        }
    }

    public void ReduceAlert(string zoneID, float amount, string reason)
    {
        ZoneAlertRecord record = GetOrCreateRecord(zoneID);
        ZoneAlertLevel oldLevel = record.alertLevel;

        record.alertValue -= amount;
        record.alertValue = Mathf.Clamp(record.alertValue, 0f, 100f);
        record.alertLevel = CalculateLevel(record.alertValue);

        if (showDebugLogs)
        {
            Debug.Log(
                $"[ZoneAlert] {record.zoneID}: -{amount:0.0} because {reason}. " +
                $"Value: {record.alertValue:0.0}, Level: {record.alertLevel}"
            );
        }

        if (oldLevel != record.alertLevel && showDebugLogs)
        {
            Debug.Log($"[ZoneAlert] {record.zoneID} changed from {oldLevel} to {record.alertLevel}");
        }
    }

    public ZoneAlertLevel GetAlertLevel(string zoneID)
    {
        return GetOrCreateRecord(zoneID).alertLevel;
    }

    public float GetAlertValue(string zoneID)
    {
        return GetOrCreateRecord(zoneID).alertValue;
    }

    public float GetSuspicionMultiplier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousSuspicionMultiplier;

            case ZoneAlertLevel.Alerted:
                return alertedSuspicionMultiplier;

            case ZoneAlertLevel.Lockdown:
                return lockdownSuspicionMultiplier;

            default:
                return calmSuspicionMultiplier;
        }
    }

    public float GetQuestioningThresholdModifier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousQuestioningThresholdModifier;

            case ZoneAlertLevel.Alerted:
                return alertedQuestioningThresholdModifier;

            case ZoneAlertLevel.Lockdown:
                return lockdownQuestioningThresholdModifier;

            default:
                return calmQuestioningThresholdModifier;
        }
    }

    public float GetSoundSensitivityMultiplier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousSoundSensitivityMultiplier;

            case ZoneAlertLevel.Alerted:
                return alertedSoundSensitivityMultiplier;

            case ZoneAlertLevel.Lockdown:
                return lockdownSoundSensitivityMultiplier;

            default:
                return calmSoundSensitivityMultiplier;
        }
    }

    public float GetInvestigationSpeedMultiplier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousInvestigationSpeedMultiplier;

            case ZoneAlertLevel.Alerted:
                return alertedInvestigationSpeedMultiplier;

            case ZoneAlertLevel.Lockdown:
                return lockdownInvestigationSpeedMultiplier;

            default:
                return calmInvestigationSpeedMultiplier;
        }
    }

    public float GetSearchRadiusMultiplier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousSearchRadiusMultiplier;

            case ZoneAlertLevel.Alerted:
                return alertedSearchRadiusMultiplier;

            case ZoneAlertLevel.Lockdown:
                return lockdownSearchRadiusMultiplier;

            default:
                return calmSearchRadiusMultiplier;
        }
    }

    public int GetExtraSearchPoints(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousExtraSearchPoints;

            case ZoneAlertLevel.Alerted:
                return alertedExtraSearchPoints;

            case ZoneAlertLevel.Lockdown:
                return lockdownExtraSearchPoints;

            default:
                return calmExtraSearchPoints;
        }
    }

    public float GetSearchWaitMultiplier(string zoneID)
    {
        switch (GetAlertLevel(zoneID))
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousSearchWaitMultiplier;

            case ZoneAlertLevel.Alerted:
                return alertedSearchWaitMultiplier;

            case ZoneAlertLevel.Lockdown:
                return lockdownSearchWaitMultiplier;

            default:
                return calmSearchWaitMultiplier;
        }
    }

    public bool IsSuspiciousOrHigher(string zoneID)
    {
        ZoneAlertLevel level = GetAlertLevel(zoneID);

        return level == ZoneAlertLevel.Suspicious ||
               level == ZoneAlertLevel.Alerted ||
               level == ZoneAlertLevel.Lockdown;
    }

    public bool IsAlertedOrHigher(string zoneID)
    {
        ZoneAlertLevel level = GetAlertLevel(zoneID);

        return level == ZoneAlertLevel.Alerted ||
               level == ZoneAlertLevel.Lockdown;
    }

    public bool IsLockdown(string zoneID)
    {
        return GetAlertLevel(zoneID) == ZoneAlertLevel.Lockdown;
    }

    private void DecayAlerts()
    {
        foreach (ZoneAlertRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            record.timeSinceLastIncrease += Time.deltaTime;

            if (record.alertValue <= 0f)
            {
                record.alertValue = 0f;
                record.alertLevel = ZoneAlertLevel.Calm;
                continue;
            }

            float decay = GetDecayRate(record.alertLevel);

            record.alertValue -= decay * Time.deltaTime;
            record.alertValue = Mathf.Clamp(record.alertValue, 0f, 100f);
            record.alertLevel = CalculateLevel(record.alertValue);
        }
    }

    private float GetDecayRate(ZoneAlertLevel level)
    {
        switch (level)
        {
            case ZoneAlertLevel.Suspicious:
                return suspiciousDecayPerSecond;

            case ZoneAlertLevel.Alerted:
                return alertedDecayPerSecond;

            case ZoneAlertLevel.Lockdown:
                return lockdownDecayPerSecond;

            default:
                return calmDecayPerSecond;
        }
    }

    private ZoneAlertLevel CalculateLevel(float value)
    {
        if (value >= lockdownThreshold)
        {
            return ZoneAlertLevel.Lockdown;
        }

        if (value >= alertedThreshold)
        {
            return ZoneAlertLevel.Alerted;
        }

        if (value >= suspiciousThreshold)
        {
            return ZoneAlertLevel.Suspicious;
        }

        return ZoneAlertLevel.Calm;
    }

    public ZoneAlertRecord GetOrCreateRecord(string zoneID)
    {
        string cleanZoneID = string.IsNullOrWhiteSpace(zoneID)
            ? "public_city"
            : zoneID.Trim();

        foreach (ZoneAlertRecord record in records)
        {
            if (record != null && record.zoneID == cleanZoneID)
            {
                return record;
            }
        }

        ZoneAlertRecord newRecord = new ZoneAlertRecord
        {
            zoneID = cleanZoneID,
            alertLevel = ZoneAlertLevel.Calm,
            alertValue = 0f,
            timeSinceLastIncrease = 0f
        };

        records.Add(newRecord);
        return newRecord;
    }

    public void PrintAlerts()
    {
        Debug.Log(GetDebugText());
    }

    public string GetDebugText()
    {
        if (records == null || records.Count == 0)
        {
            return "[ZoneAlert] No zone alert records.";
        }

        string text = "ZONE ALERTS\n";

        foreach (ZoneAlertRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            text +=
                $"Zone: {record.zoneID} | " +
                $"Value: {record.alertValue:0.0} | " +
                $"Level: {record.alertLevel} | " +
                $"Time Since Increase: {record.timeSinceLastIncrease:0.0}s\n";
        }

        return text;
    }

    public void ClearAllAlerts()
    {
        records.Clear();

        if (showDebugLogs)
        {
            Debug.Log("[ZoneAlert] Cleared all alerts.");
        }
    }
}