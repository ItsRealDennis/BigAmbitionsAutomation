using MelonLoader;

namespace BAA.Mod;

/// <summary>
/// MelonLoader entry point. M3 "probe" build: proves the mod loads, links BAA.Core, reads real game
/// state via SaveGameManager.Current, and fires on the daily tick (GameManager.NewDay) — the exact
/// hook auto-restock will use. No game state is modified yet.
/// </summary>
public sealed class ModEntry : MelonMod
{
    /// <summary>Shared logger for Harmony patches (which are static).</summary>
    internal static MelonLogger.Instance Log;

    private float _probeTimer;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        Log.Msg("================ BA BOT loaded (M3 probe) ================");

        // Prove the Core "brain" assembly loaded in-process (default-OFF config).
        var config = new BAA.Core.Config.AutomationConfig();
        Log.Msg($"Core linked OK. RestockEnabled default = {config.RestockEnabled} (expected False).");
        Log.Msg("Load a save; per-day snapshots will print on each in-game NewDay tick.");
    }

    public override void OnUpdate()
    {
        // Throttled live snapshot so we can watch state without spamming every frame.
        _probeTimer += UnityEngine.Time.deltaTime;
        if (_probeTimer < 15f)
            return;
        _probeTimer = 0f;
        GameProbe.LogSnapshot("probe");
    }
}
