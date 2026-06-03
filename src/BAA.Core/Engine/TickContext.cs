namespace BAA.Core.Engine;

/// <summary>Everything a manager needs to plan a tick: a read-only world snapshot, the clock,
/// and the current config. Managers never mutate through this — they return an <see cref="ActionPlan"/>.</summary>
public sealed record TickContext(IGameState State, IGameClock Clock, AutomationConfig Config);
