using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

[Serializable]
public class GuardMemoryRecord
{
    public string zoneID;
    public float suspicionMemory;
    public int warningsGiven;
    public int failedQuestionings;
    public int timesCaughtTrespassing;
    public GuardSuspicionReason lastReason;
    public string lastReasonText;
}

public class GuardMemoryManager : MonoBehaviour
{
    public static GuardMemoryManager Instance;

    [Header("Memory")]
    public float memoryDecayPerSecond = 0.35f;
    public float maxMemory = 100f;

    [Header("Thresholds")]
    public float rememberedThreshold = 25f;
    public float hostileThreshold = 70f;

    [Header("Runtime Records")]
    public List<GuardMemoryRecord> records = new List<GuardMemoryRecord>();

    [Header("Debug")]
    public bool showDebugLogs = true;
    public bool allowKeyboardDebugPrint = true;
    public Key printMemoryKey = Key.M;

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
        DecayMemory();
        HandleDebugInput();
    }

    private void HandleDebugInput()
    {
        if (!allowKeyboardDebugPrint)
        {
            return;
        }

        if (Keyboard.current == null)
        {
            return;
        }

        if (Keyboard.current[printMemoryKey].wasPressedThisFrame)
        {
            PrintMemoryToConsole();
        }
    }

    public void ReportSuspicion(
        string zoneID,
        GuardSuspicionReason reason,
        string reasonText,
        float amount,
        bool serious
    )
    {
        GuardMemoryRecord record = GetOrCreateRecord(zoneID);

        float finalAmount = serious ? amount * 1.5f : amount;

        record.suspicionMemory += finalAmount;
        record.suspicionMemory = Mathf.Clamp(record.suspicionMemory, 0f, maxMemory);

        record.lastReason = reason;
        record.lastReasonText = reasonText;

        switch (reason)
        {
            case GuardSuspicionReason.IgnoredWarning:
                record.warningsGiven++;
                break;

            case GuardSuspicionReason.FailedQuestioning:
                record.failedQuestionings++;
                break;

            case GuardSuspicionReason.UnauthorizedZone:
            case GuardSuspicionReason.NoDisguiseInRestrictedZone:
            case GuardSuspicionReason.WrongDisguise:
                record.timesCaughtTrespassing++;
                break;
        }

        if (showDebugLogs)
        {
            Debug.Log(
                $"[GuardMemory] Zone: {record.zoneID} | +" +
                $"{finalAmount:0.0} memory | Reason: {reasonText} | " +
                $"Total: {record.suspicionMemory:0.0}"
            );
        }
    }

    public float GetMemory(string zoneID)
    {
        return GetOrCreateRecord(zoneID).suspicionMemory;
    }

    public bool IsRemembered(string zoneID)
    {
        return GetMemory(zoneID) >= rememberedThreshold;
    }

    public bool IsHostile(string zoneID)
    {
        return GetMemory(zoneID) >= hostileThreshold;
    }

    public float GetMemorySuspicionBonus(string zoneID)
    {
        float memory = GetMemory(zoneID);

        if (memory < rememberedThreshold)
        {
            return 0f;
        }

        if (memory >= hostileThreshold)
        {
            return 25f;
        }

        return 10f;
    }

    public float GetMemoryMultiplier(string zoneID)
    {
        float memory = GetMemory(zoneID);

        if (memory < rememberedThreshold)
        {
            return 1f;
        }

        if (memory >= hostileThreshold)
        {
            return 1.65f;
        }

        return 1.25f;
    }

    public GuardMemoryRecord GetOrCreateRecord(string zoneID)
    {
        string cleanZoneID = string.IsNullOrWhiteSpace(zoneID)
            ? "public_city"
            : zoneID.Trim();

        foreach (GuardMemoryRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            if (record.zoneID == cleanZoneID)
            {
                return record;
            }
        }

        GuardMemoryRecord newRecord = new GuardMemoryRecord
        {
            zoneID = cleanZoneID,
            suspicionMemory = 0f,
            warningsGiven = 0,
            failedQuestionings = 0,
            timesCaughtTrespassing = 0,
            lastReason = GuardSuspicionReason.None,
            lastReasonText = ""
        };

        records.Add(newRecord);
        return newRecord;
    }

    private void DecayMemory()
    {
        if (records == null)
        {
            return;
        }

        foreach (GuardMemoryRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            if (record.suspicionMemory <= 0f)
            {
                continue;
            }

            record.suspicionMemory -= memoryDecayPerSecond * Time.deltaTime;
            record.suspicionMemory = Mathf.Clamp(record.suspicionMemory, 0f, maxMemory);
        }
    }

    public string GetMemoryDebugText()
    {
        StringBuilder builder = new StringBuilder();

        builder.AppendLine("GUARD MEMORY");

        if (records == null || records.Count == 0)
        {
            builder.AppendLine("No memory records.");
            return builder.ToString();
        }

        foreach (GuardMemoryRecord record in records)
        {
            if (record == null)
            {
                continue;
            }

            builder.AppendLine("--------------------");
            builder.AppendLine($"Zone: {record.zoneID}");
            builder.AppendLine($"Memory: {record.suspicionMemory:0.0}");
            builder.AppendLine($"Warnings: {record.warningsGiven}");
            builder.AppendLine($"Failed Questions: {record.failedQuestionings}");
            builder.AppendLine($"Trespassing: {record.timesCaughtTrespassing}");
            builder.AppendLine($"Last Reason: {record.lastReason}");
            builder.AppendLine($"Last Text: {record.lastReasonText}");
            builder.AppendLine($"Remembered: {IsRemembered(record.zoneID)}");
            builder.AppendLine($"Hostile: {IsHostile(record.zoneID)}");
        }

        return builder.ToString();
    }

    public void PrintMemoryToConsole()
    {
        Debug.Log(GetMemoryDebugText());
    }

    public void ClearAllMemory()
    {
        if (records != null)
        {
            records.Clear();
        }

        if (showDebugLogs)
        {
            Debug.Log("[GuardMemory] Cleared all memory.");
        }
    }

    public void ClearZoneMemory(string zoneID)
    {
        if (records == null)
        {
            return;
        }

        string cleanZoneID = string.IsNullOrWhiteSpace(zoneID)
            ? "public_city"
            : zoneID.Trim();

        for (int i = records.Count - 1; i >= 0; i--)
        {
            if (records[i] != null && records[i].zoneID == cleanZoneID)
            {
                records.RemoveAt(i);
            }
        }

        if (showDebugLogs)
        {
            Debug.Log($"[GuardMemory] Cleared memory for zone: {cleanZoneID}");
        }
    }
}