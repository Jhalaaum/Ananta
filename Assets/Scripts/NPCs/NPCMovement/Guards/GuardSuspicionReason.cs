public enum GuardSuspicionReason
{
    None,

    SawPlayerNormally,
    SuspiciousMovementInPublic,
    RunningNearGuard,
    CrouchingInPublic,
    CrawlingInPublic,

    UnauthorizedZone,
    WrongDisguise,
    PartialDisguise,
    NoDisguiseInRestrictedZone,

    HeardNoise,
    InvestigatedSound,

    IgnoredWarning,
    FailedQuestioning,
    ContrabandSeen,
    LockpickingSeen,
    PickpocketingSeen,
    AttackedGuard
}