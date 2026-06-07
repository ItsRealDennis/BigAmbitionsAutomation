using BAA.Core.Abstractions;

namespace BaBot.Game;

/// <summary>IGameClock over the live GameInstance. Speed multiplier is reported as 1 (time-skip is
/// deferred in the official-mod port); the engine only uses Now for boundary info.</summary>
internal sealed class GameClockAdapter : IGameClock
{
    public GameTimeInfo Now
    {
        get
        {
            var gi = SaveGameManager.Current;
            if (gi == null)
                return new GameTimeInfo(0, 0, 0, 0, 0, System.DayOfWeek.Monday, 0);
            int d = gi.Day, h = gi.Hour, m = (int)gi.Minute;
            return new GameTimeInfo(0, 0, d, h, m, System.DayOfWeek.Monday, (long)d * 1440 + h * 60 + m);
        }
    }

    public float SpeedMultiplier => 1f;
    public bool IsPaused => false;
}
