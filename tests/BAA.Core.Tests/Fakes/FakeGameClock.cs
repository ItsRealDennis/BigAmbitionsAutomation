namespace BAA.Core.Tests.Fakes;

/// <summary>An advanceable in-memory clock for tests.</summary>
public sealed class FakeGameClock : IGameClock
{
    public GameTimeInfo Now { get; set; } = new(2023, 3, 10, 8, 0, DayOfWeek.Friday, 0);
    public float SpeedMultiplier { get; set; } = 1f;
    public bool IsPaused { get; set; }
}
