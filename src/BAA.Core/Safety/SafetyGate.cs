namespace BAA.Core.Safety;

/// <summary>
/// The central veto/tripwire. Used in two modes:
/// <list type="bullet">
/// <item>live automation — <see cref="Evaluate"/> filters a plan so spending never breaches the reserve floor;</item>
/// <item>time-skip — <see cref="CheckTripwires"/> is polled between increments to abort on danger.</item>
/// </list>
/// (Stub — logic is filled in test-first.)
/// </summary>
public sealed class SafetyGate
{
    private readonly IReadOnlyList<ISafetyBreaker> _breakers;

    public SafetyGate(IEnumerable<ISafetyBreaker> breakers) => _breakers = breakers.ToList();

    public GateDecision Evaluate(ActionPlan plan, IGameState state, AutomationConfig config)
    {
        // 1. Global tripwires first: any halt-all trip blocks the entire plan.
        var trips = CheckTripwires(state, config);
        if (trips.Count > 0)
            return new GateDecision(Array.Empty<PlannedAction>(), trips.Select(t => t.Reason).ToList(), trips);

        // 2. Budget filter: approve actions while spending never drops cash below the reserve floor.
        //    A shared running balance is threaded through so the COMBINED plan can't overspend,
        //    even if each action looks affordable on its own.
        var approved = new List<PlannedAction>();
        var rejections = new List<string>();
        var runningCash = state.GetFinances().Cash;
        var floor = config.CashReserveFloor;

        foreach (var action in plan.Actions)
        {
            var projected = runningCash + action.CashDelta;
            if (action.CashDelta < 0 && projected < floor)
            {
                rejections.Add($"reserve floor: '{action.Description}' would drop cash to {projected} (floor {floor})");
                continue;
            }

            approved.Add(action);
            runningCash = projected;
        }

        return new GateDecision(approved, rejections, Array.Empty<BreakerResult>());
    }

    public IReadOnlyList<BreakerResult> CheckTripwires(IGameState state, AutomationConfig config)
    {
        var ctx = new BreakerContext(state, config);
        return _breakers
            .Select(b => b.Check(ctx))
            .Where(r => r.Verdict == BreakerVerdict.Trip && r.Severity == BreakerSeverity.HaltAll)
            .ToList();
    }
}
