namespace BAA.Core.Abstractions;

/// <summary>
/// In-game time. Split from <see cref="IGameState"/> because it is read very frequently and the
/// time-skip feature both reads and writes the speed.
/// </summary>
public interface IGameClock
{
    GameTimeInfo Now { get; }
    float SpeedMultiplier { get; }
    bool IsPaused { get; }
}
