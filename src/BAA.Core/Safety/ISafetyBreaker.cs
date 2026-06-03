namespace BAA.Core.Safety;

/// <summary>What a breaker sees when deciding whether to trip.</summary>
public sealed record BreakerContext(IGameState State, AutomationConfig Config);

/// <summary>
/// A single safety check (low funds, unpaid rent, staff resignation, ...). Breakers are pure
/// functions of the world snapshot, so each is trivially unit-testable in isolation.
/// </summary>
public interface ISafetyBreaker
{
    string Name { get; }

    BreakerResult Check(BreakerContext ctx);
}
