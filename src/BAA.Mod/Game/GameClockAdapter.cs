using BAA.Core.Abstractions;
using Il2Cpp;

namespace BAA.Mod.Game;

/// <summary>IGameClock over the live GameInstance / GameManager.</summary>
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

    public float SpeedMultiplier
    {
        get { try { return GameManager.MinutesMultiplier; } catch { return 1f; } }
    }

    public bool IsPaused => false;
}
