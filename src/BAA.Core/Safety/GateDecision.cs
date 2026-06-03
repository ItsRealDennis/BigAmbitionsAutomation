namespace BAA.Core.Safety;

/// <summary>The gate's verdict on a plan: which actions may run, why others were dropped,
/// and any halt-all breaker trips that blocked the whole plan.</summary>
public sealed record GateDecision(
    IReadOnlyList<PlannedAction> Approved,
    IReadOnlyList<string> Rejections,
    IReadOnlyList<BreakerResult> Trips)
{
    public bool Halted => Trips.Count > 0;
}
