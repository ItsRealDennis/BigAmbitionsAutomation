namespace BAA.Core.Abstractions;

public enum DeliveryFrequency
{
    OneTime,
    Daily,
    Weekly,
}

public enum CommandOutcome
{
    Applied,
    Skipped,
    Failed,
}

/// <summary>
/// Deterministic per-tick run order. Lower number runs first. Finance collects income
/// before Restock spends it; TimeSkip runs last and only if everything above is healthy.
/// </summary>
public enum ManagerPriority
{
    Finance = 10,
    Logistics = 20,
    Pricing = 25,
    Restock = 30,
    Employee = 40,
    TimeSkip = 90,
}

public enum BreakerVerdict
{
    Pass,
    Warn,
    Trip,
}

/// <summary>How far a tripped breaker reaches.</summary>
public enum BreakerSeverity
{
    /// <summary>Drop just the offending action; keep going.</summary>
    SkipAction,

    /// <summary>Halt the feature that produced the action for this tick.</summary>
    HaltFeature,

    /// <summary>Halt ALL automation this tick and abort any running time-skip.</summary>
    HaltAll,
}
