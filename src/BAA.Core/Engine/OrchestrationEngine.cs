namespace BAA.Core.Engine;

/// <summary>
/// Coordinates the managers each tick. Runs them in priority order, sending each manager's plan
/// through the <see cref="SafetyGate"/> and applying only approved actions — re-reading state between
/// managers so a later one sees an earlier one's effect (e.g. Finance collects cash before Restock spends).
/// (Stub — logic is filled in test-first.)
/// </summary>
public sealed class OrchestrationEngine
{
    private readonly IReadOnlyList<IAutomationManager> _managers;
    private readonly SafetyGate _gate;
    private readonly IGameCommands _commands;
    private readonly IModLogger _logger;

    public OrchestrationEngine(
        IEnumerable<IAutomationManager> managers,
        SafetyGate gate,
        IGameCommands commands,
        IModLogger logger)
    {
        _managers = managers.OrderBy(m => (int)m.Priority).ToList();
        _gate = gate;
        _commands = commands;
        _logger = logger;
    }

    public void Tick(IGameState state, IGameClock clock, AutomationConfig config)
    {
        if (!state.IsWorldReady())
            return;

        foreach (var manager in _managers)
        {
            var plan = manager.Plan(new TickContext(state, clock, config));
            if (plan.IsEmpty)
                continue;

            // Gate re-reads live state here, so it sees the effects of earlier managers this tick.
            var decision = _gate.Evaluate(plan, state, config);
            if (decision.Halted)
            {
                _logger.Warn($"Automation halted: {string.Join("; ", decision.Trips.Select(t => t.Reason))}");
                break;
            }

            foreach (var action in decision.Approved)
            {
                var result = action.Apply(_commands);
                if (result.Outcome == CommandOutcome.Failed)
                    _logger.Error($"Action failed [{action.Description}]: {result.Reason}");
            }
        }
    }
}
