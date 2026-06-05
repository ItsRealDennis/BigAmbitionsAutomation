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
        // Master switch is the single source of truth: off = the engine does nothing, no matter how it
        // was triggered (daily tick or the panel's "run now" button). Each manager additionally gates
        // on its own feature flag, so master ON still does nothing until a feature is enabled too.
        if (!config.MasterEnabled || !state.IsWorldReady())
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
                // Defense in depth: the adapter contract says commands never throw, but a buggy or
                // future command must not abort the rest of the tick (or escape into the game).
                CommandResult result;
                try { result = action.Apply(_commands); }
                catch (Exception ex) { result = CommandResult.Failed(ex.Message); }

                if (result.Outcome == CommandOutcome.Failed)
                    _logger.Error($"Action failed [{action.Description}]: {result.Reason}");
            }
        }
    }
}
