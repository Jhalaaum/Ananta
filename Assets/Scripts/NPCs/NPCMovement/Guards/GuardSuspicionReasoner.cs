using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class GuardReasonSuspicionRule
{
    [Header("Reason")]
    public GuardSuspicionReason reason;

    [Header("Display")]
    public string reasonText;

    [Header("Suspicion")]
    public float suspicionPerSecond = 10f;
    public float minimumSuspicion = 0f;

    [Header("Escalation")]
    public bool canTriggerQuestioning = true;
    public bool canTriggerChase = false;
    public bool isSerious = false;

    [Header("Memory")]
    public float memoryAmount = 5f;
}

public class GuardSuspicionReasoner : MonoBehaviour
{
    [Header("Public Area Rules")]
    public bool ignoreNormalWalkingInPublic = true;

    [Header("Restricted Area Rules")]
    public float validDisguiseSuspicionMultiplier = 0.15f;
    public float partialDisguiseSuspicionMultiplier = 0.65f;
    public float noDisguiseSuspicionMultiplier = 1.25f;
    public float wrongDisguiseSuspicionMultiplier = 1.6f;

    [Header("Restricted Movement Multipliers")]
    public float restrictedIdleMultiplier = 0.8f;
    public float restrictedWalkingMultiplier = 1f;
    public float restrictedRunningMultiplier = 1.4f;
    public float restrictedCrouchingMultiplier = 1.2f;
    public float restrictedCrawlingMultiplier = 1.6f;

    [Header("Reason Rules")]
    public GuardReasonSuspicionRule[] reasonRules =
    {
        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.SawPlayerNormally,
            reasonText = "standing here",
            suspicionPerSecond = 0f,
            minimumSuspicion = 0f,
            canTriggerQuestioning = false,
            canTriggerChase = false,
            isSerious = false,
            memoryAmount = 0f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.RunningNearGuard,
            reasonText = "running near the guards",
            suspicionPerSecond = 8f,
            minimumSuspicion = 10f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            isSerious = false,
            memoryAmount = 3f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.CrouchingInPublic,
            reasonText = "crouching in public",
            suspicionPerSecond = 18f,
            minimumSuspicion = 20f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            isSerious = false,
            memoryAmount = 6f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.CrawlingInPublic,
            reasonText = "crawling through the street",
            suspicionPerSecond = 28f,
            minimumSuspicion = 35f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            isSerious = true,
            memoryAmount = 10f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.SuspiciousMovementInPublic,
            reasonText = "moving strangely in public",
            suspicionPerSecond = 12f,
            minimumSuspicion = 15f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            isSerious = false,
            memoryAmount = 5f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.NoDisguiseInRestrictedZone,
            reasonText = "entering a restricted place without proper clothing",
            suspicionPerSecond = 45f,
            minimumSuspicion = 45f,
            canTriggerQuestioning = true,
            canTriggerChase = true,
            isSerious = true,
            memoryAmount = 18f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.WrongDisguise,
            reasonText = "wearing the wrong disguise here",
            suspicionPerSecond = 55f,
            minimumSuspicion = 55f,
            canTriggerQuestioning = true,
            canTriggerChase = true,
            isSerious = true,
            memoryAmount = 22f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.PartialDisguise,
            reasonText = "wearing an incomplete disguise",
            suspicionPerSecond = 24f,
            minimumSuspicion = 25f,
            canTriggerQuestioning = true,
            canTriggerChase = false,
            isSerious = false,
            memoryAmount = 8f
        },

        new GuardReasonSuspicionRule
        {
            reason = GuardSuspicionReason.UnauthorizedZone,
            reasonText = "being somewhere you are not allowed",
            suspicionPerSecond = 50f,
            minimumSuspicion = 50f,
            canTriggerQuestioning = true,
            canTriggerChase = true,
            isSerious = true,
            memoryAmount = 20f
        }
    };

    [Header("Memory")]
    public bool useGuardMemory = true;
    public float memoryReportInterval = 2f;

    [Header("Debug")]
    public bool showDebugLogs = true;

    private float memoryReportTimer;

    public GuardSuspicionEvaluation EvaluateVision(
        RestrictedZone zone,
        PlayerCoverIdentity coverIdentity,
        PlayerMovementStateTracker movementTracker
    )
    {
        bool inPublicZone = zone == null || zone.IsPublicZone();

        if (inPublicZone)
        {
            return EvaluatePublicVision(movementTracker);
        }

        return EvaluateRestrictedZoneVision(zone, coverIdentity, movementTracker);
    }

    private GuardSuspicionEvaluation EvaluatePublicVision(PlayerMovementStateTracker movementTracker)
    {
        if (movementTracker == null)
        {
            if (ignoreNormalWalkingInPublic)
            {
                return GuardSuspicionEvaluation.Ignore();
            }

            return BuildEvaluation(GuardSuspicionReason.SawPlayerNormally, 1f);
        }

        PlayerMovementVisualState state = movementTracker.GetCurrentState();

        switch (state)
        {
            case PlayerMovementVisualState.Idle:
            case PlayerMovementVisualState.Walking:
                if (ignoreNormalWalkingInPublic)
                {
                    return GuardSuspicionEvaluation.Ignore();
                }

                return BuildEvaluation(GuardSuspicionReason.SawPlayerNormally, 1f);

            case PlayerMovementVisualState.Running:
                return BuildEvaluation(GuardSuspicionReason.RunningNearGuard, 1f);

            case PlayerMovementVisualState.Crouching:
                return BuildEvaluation(GuardSuspicionReason.CrouchingInPublic, 1f);

            case PlayerMovementVisualState.Crawling:
                return BuildEvaluation(GuardSuspicionReason.CrawlingInPublic, 1f);

            case PlayerMovementVisualState.Jumping:
            case PlayerMovementVisualState.Falling:
                return BuildEvaluation(GuardSuspicionReason.SuspiciousMovementInPublic, 1f);

            default:
                return GuardSuspicionEvaluation.Ignore();
        }
    }

    private GuardSuspicionEvaluation EvaluateRestrictedZoneVision(
        RestrictedZone zone,
        PlayerCoverIdentity coverIdentity,
        PlayerMovementStateTracker movementTracker
    )
    {
        float movementMultiplier = GetRestrictedMovementMultiplier(movementTracker);

        if (coverIdentity == null)
        {
            return BuildEvaluation(
                GuardSuspicionReason.NoDisguiseInRestrictedZone,
                noDisguiseSuspicionMultiplier * movementMultiplier,
                zone
            );
        }

        string disguiseSet = coverIdentity.GetBestDisguiseSetID();
        float completeness = coverIdentity.GetBestDisguiseCompleteness();
        bool allowed = coverIdentity.IsAllowedInZone(zone);

        if (allowed)
        {
            if (completeness >= 1f)
            {
                return BuildEvaluation(
                    GuardSuspicionReason.None,
                    validDisguiseSuspicionMultiplier * movementMultiplier,
                    zone
                );
            }

            return BuildEvaluation(
                GuardSuspicionReason.PartialDisguise,
                partialDisguiseSuspicionMultiplier * movementMultiplier,
                zone
            );
        }

        if (string.IsNullOrWhiteSpace(disguiseSet))
        {
            return BuildEvaluation(
                GuardSuspicionReason.NoDisguiseInRestrictedZone,
                noDisguiseSuspicionMultiplier * movementMultiplier,
                zone
            );
        }

        return BuildEvaluation(
            GuardSuspicionReason.WrongDisguise,
            wrongDisguiseSuspicionMultiplier * movementMultiplier,
            zone,
            $"wearing the wrong disguise '{disguiseSet}' in {GetZoneName(zone)}"
        );
    }

    private float GetRestrictedMovementMultiplier(PlayerMovementStateTracker movementTracker)
    {
        if (movementTracker == null)
        {
            return restrictedWalkingMultiplier;
        }

        switch (movementTracker.GetCurrentState())
        {
            case PlayerMovementVisualState.Idle:
                return restrictedIdleMultiplier;

            case PlayerMovementVisualState.Walking:
                return restrictedWalkingMultiplier;

            case PlayerMovementVisualState.Running:
                return restrictedRunningMultiplier;

            case PlayerMovementVisualState.Crouching:
                return restrictedCrouchingMultiplier;

            case PlayerMovementVisualState.Crawling:
                return restrictedCrawlingMultiplier;

            case PlayerMovementVisualState.Jumping:
            case PlayerMovementVisualState.Falling:
                return restrictedRunningMultiplier;

            default:
                return restrictedWalkingMultiplier;
        }
    }

    private GuardSuspicionEvaluation BuildEvaluation(
        GuardSuspicionReason reason,
        float multiplier,
        RestrictedZone zone = null,
        string overrideText = ""
    )
    {
        GuardReasonSuspicionRule rule = GetRule(reason);

        if (reason == GuardSuspicionReason.None)
        {
            return GuardSuspicionEvaluation.Suspicious(
                GuardSuspicionReason.None,
                "valid disguise",
                0f,
                false
            );
        }

        if (rule == null)
        {
            return GuardSuspicionEvaluation.Suspicious(
                reason,
                string.IsNullOrWhiteSpace(overrideText) ? reason.ToString() : overrideText,
                multiplier,
                false
            );
        }

        string finalText = !string.IsNullOrWhiteSpace(overrideText)
            ? overrideText
            : rule.reasonText;

        if (zone != null && reason != GuardSuspicionReason.WrongDisguise)
        {
            finalText = $"{finalText} in {GetZoneName(zone)}";
        }

        return new GuardSuspicionEvaluation
        {
            shouldIncreaseSuspicion = rule.suspicionPerSecond > 0f,
            reason = reason,
            reasonText = finalText,
            multiplier = multiplier,
            isSerious = rule.isSerious,
            suspicionPerSecond = rule.suspicionPerSecond,
            minimumSuspicion = rule.minimumSuspicion,
            canTriggerQuestioning = rule.canTriggerQuestioning,
            canTriggerChase = rule.canTriggerChase,
            memoryAmount = rule.memoryAmount
        };
    }

    public GuardReasonSuspicionRule GetRule(GuardSuspicionReason reason)
    {
        if (reasonRules == null)
        {
            return null;
        }

        foreach (GuardReasonSuspicionRule rule in reasonRules)
        {
            if (rule != null && rule.reason == reason)
            {
                return rule;
            }
        }

        return null;
    }

    public void ReportMemoryIfNeeded(string zoneID, GuardSuspicionEvaluation evaluation)
    {
        if (!useGuardMemory)
        {
            return;
        }

        if (!evaluation.shouldIncreaseSuspicion)
        {
            return;
        }

        if (evaluation.reason == GuardSuspicionReason.None)
        {
            return;
        }

        if (GuardMemoryManager.Instance == null)
        {
            return;
        }

        memoryReportTimer -= Time.deltaTime;

        if (memoryReportTimer > 0f)
        {
            return;
        }

        memoryReportTimer = memoryReportInterval;

        GuardMemoryManager.Instance.ReportSuspicion(
            zoneID,
            evaluation.reason,
            evaluation.reasonText,
            evaluation.memoryAmount,
            evaluation.isSerious
        );

        if (showDebugLogs)
        {
            Debug.Log($"[GuardSuspicionReasoner] Memory reported: {evaluation.reasonText}");
        }
    }

    private string GetZoneName(RestrictedZone zone)
    {
        if (zone == null)
        {
            return "this area";
        }

        if (!string.IsNullOrWhiteSpace(zone.zoneDisplayName))
        {
            return zone.zoneDisplayName;
        }

        if (!string.IsNullOrWhiteSpace(zone.zoneID))
        {
            return zone.zoneID;
        }

        return "this area";
    }
}