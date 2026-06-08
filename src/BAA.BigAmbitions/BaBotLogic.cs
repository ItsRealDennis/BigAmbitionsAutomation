using System;
using BAA.Core.Config;
using BAA.Core.Engine;
using BAA.Core.Managers;
using BAA.Core.Safety;
using BAA.Core.Safety.Breakers;
using BAModAPI;
using BAModAPI.Services;
using BaBot.Diagnostics;
using BaBot.Game;
using BaBot.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaBot;

/// <summary>
/// The mod brain + in-game overlay host. Builds the F8 uGUI panel (engine components only) and drives
/// it from the official UnityLifecycleProvider.OnUpdate hook — so there is NO MonoBehaviour of ours
/// (which Unity can't instantiate from a runtime-loaded mod). Automation is SELF-DRIVEN from the
/// per-frame loop (it polls the in-game clock and runs on each new hour/day + once shortly after a
/// save loads) — it does NOT rely on any game event surviving a city load. Everything is gated by the
/// Core safety gate + the LIVE MODE switch; default-OFF and preview-first.
/// </summary>
public sealed class BaBotLogic
{
    /// <summary>Build tag, logged on load so a session's Player.log unambiguously identifies which mod
    /// build was running (the DLL only reloads on a full game restart).</summary>
    internal const string Version = "v0.6.1 (2026-06-08)";

    internal static readonly AutomationConfig Config = new();

    private readonly PanelView _panel = new();
    private GameSnapshot _snapshot;
    private bool _visible;
    private float _refreshTimer;
    private float _sinceWellbeing = 99f;
    private int _lastAutoDay = -1;
    private int _lastAutoHour = -1;
    private float _sinceArm = -1f;          // >=0 once a loaded save is first seen; drives the settle-delayed first pass
    private bool _pendingInitialRun;
    private int _lastHeartbeatDay = -1;     // throttles idle auto heartbeats to once per in-game day (no turbo/skip flood)
    private bool _loggedFrameError;
    private bool _wasVisible;
    private CursorLockMode _savedLock;
    private bool _savedCursorVisible;
    private bool _subscribed;

    private OrchestrationEngine _engine;
    private GameStateAdapter _state;
    private GameClockAdapter _clock;

    public void Initialize(ModContext ctx)
    {
        try { Settings.Load(Config); } catch (Exception ex) { Debug.LogWarning("[BA BOT] settings load: " + ex.Message); }
        Loc.Current = string.Equals(Config.Language, "da", StringComparison.OrdinalIgnoreCase) ? Lang.Da : Lang.En;
        EnsureEngine();

        try
        {
            _panel.Build(Config, () => RunAutomation("manual"), () => { _visible = false; _panel.SetVisible(false); });
            _panel.SetVisible(false);
        }
        catch (Exception ex) { Debug.LogError("[BA BOT] panel build failed: " + ex); }

        UnityLifecycleProvider.OnUpdate += OnFrame;
        _subscribed = true;

        ctx.Logger.Info($"BA BOT {Version} loaded - press F8 in-game for the panel.");
        Activity.Add($"BA BOT {Version} loaded - press F8");
        // One-line config snapshot so the log shows exactly what was armed this session.
        Activity.Add($"Config: MASTER {(Config.MasterEnabled ? "ON" : "OFF")}, mode {(Config.LiveWrites ? "LIVE" : "preview")} | "
            + $"restock {OnOff(Config.RestockEnabled)}, pricing {OnOff(Config.PricingEnabled)}, logistics {OnOff(Config.LogisticsEnabled)}, "
            + $"employees {OnOff(Config.EmployeesEnabled)}, finance {OnOff(Config.FinanceEnabled)}, wellbeing {OnOff(Config.WellbeingEnabled)}");
        if (!Config.MasterEnabled)
            Activity.Add("MASTER is OFF - turn on AUTOMATION (MASTER) in F8 or nothing will run");
    }

    private static string OnOff(bool b) => b ? "on" : "off";

    public void Shutdown()
    {
        if (_subscribed)
        {
            UnityLifecycleProvider.OnUpdate -= OnFrame;
            _subscribed = false;
        }
        try { _panel.Destroy(); } catch { }
        try { GameActions.ResetTurbo(); } catch { } // never leave the game stuck at turbo speed after unload
        try { Settings.SaveIfChanged(Config); } catch { }
    }

    private void EnsureEngine()
    {
        if (_engine != null) return;
        _state = new GameStateAdapter(Config);
        _clock = new GameClockAdapter();
        var commands = new GameCommandsAdapter(_state, Config);
        var breakers = new ISafetyBreaker[] { new LowFundsBreaker(), new UnpaidRentBreaker(), new EmptyInventoryBreaker() };
        var gate = new SafetyGate(breakers);
        var managers = new IAutomationManager[] { new FinanceManager(), new LogisticsManager(), new ContractManager(), new PricingManager(), new RestockManager(), new EmployeeManager(), new SchedulingManager() };
        _engine = new OrchestrationEngine(managers, gate, commands, new ModLog());
    }

    /// <summary>Per-frame, via the official lifecycle hook (no MonoBehaviour). Fully guarded.</summary>
    private void OnFrame()
    {
        try
        {
            float dt = Time.deltaTime;
            _refreshTimer += dt;
            _sinceWellbeing += dt;
            if (_sinceArm >= 0f) _sinceArm += dt;
            if (_refreshTimer >= 1f)
            {
                _refreshTimer = 0f;
                _snapshot = GameProbe.Read();
                MaybeAutoRun();
                MaybeWellbeing();
                Settings.SaveIfChanged(Config);
            }

            // F8 toggle (new Input System), ignored while typing. Own silent guard against per-frame spam.
            try
            {
                var kb = Keyboard.current;
                if (kb != null && kb.f8Key.wasPressedThisFrame && !GameManager.HasInputSelected())
                {
                    _visible = !_visible;
                    _panel.SetVisible(_visible);
                }
            }
            catch { }

            if (_visible)
            {
                if (!_wasVisible) { _savedLock = Cursor.lockState; _savedCursorVisible = Cursor.visible; _wasVisible = true; }
                Cursor.visible = true; Cursor.lockState = CursorLockMode.None;
                _panel.Refresh(Config, _snapshot);
            }
            else if (_wasVisible)
            {
                Cursor.lockState = _savedLock; Cursor.visible = _savedCursorVisible; _wasVisible = false;
            }
        }
        catch (Exception ex)
        {
            if (!_loggedFrameError) { _loggedFrameError = true; Debug.LogWarning("[BA BOT] frame failed (logged once): " + ex.Message); }
        }
    }

    private void MaybeWellbeing()
    {
        if (!(Config.MasterEnabled && Config.WellbeingEnabled && _snapshot.HasSave) || _sinceWellbeing <= 15f) return;
        if (_snapshot.Energy < 30f) { GameActions.RefillEnergy(); _sinceWellbeing = 0f; }
        else if (_snapshot.Happiness < 30f) { GameActions.BoostHappiness(50); Activity.Add("Happiness boosted"); _sinceWellbeing = 0f; }
    }

    /// <summary>Self-driven automation: detect in-game hour/day changes from the polled snapshot and run the
    /// engine. Does NOT depend on any game event subscription surviving a city load, so the bot runs
    /// autonomously every in-game hour/day whenever a save is loaded (no RUN NOW needed).</summary>
    private void MaybeAutoRun()
    {
        if (!_snapshot.HasSave) { _lastAutoDay = -1; _lastAutoHour = -1; _sinceArm = -1f; _pendingInitialRun = false; return; }
        if (_lastAutoDay < 0)
        {
            // First sight of a loaded save: arm the clock trackers and schedule a settle-delayed first pass so
            // the player sees the bot act right after loading (not only on the next in-game hour boundary).
            _lastAutoDay = _snapshot.Day; _lastAutoHour = _snapshot.Hour; _lastHeartbeatDay = -1;
            _sinceArm = 0f; _pendingInitialRun = true;
            return;
        }
        if (_pendingInitialRun)
        {
            if (_sinceArm < 4f) return; // let the world settle ~4s after load before the first pass
            _pendingInitialRun = false;
            RunAutomation("load");
            return;
        }
        if (_snapshot.Day != _lastAutoDay) { _lastAutoDay = _snapshot.Day; _lastAutoHour = _snapshot.Hour; RunAutomation("NewDay"); }
        else if (_snapshot.Hour != _lastAutoHour) { _lastAutoHour = _snapshot.Hour; RunAutomation("hour"); }
    }

    private void RunAutomation(string trigger)
    {
        if (!Config.MasterEnabled)
        {
            if (trigger == "manual") Activity.Add("Enable AUTOMATION (MASTER) first");
            return;
        }
        try
        {
            EnsureEngine();
            // Charge the per-run service fee only on the once-a-day cadence (NewDay) + manual runs; never on the
            // frequent hourly ticks or the on-load pass, so the fee doesn't multiply during TURBO/skips/reloads.
            bool chargeFee = trigger == "manual" || trigger == "NewDay";
            int n = _engine.Tick(_state, _clock, Config, chargeServiceFee: chargeFee);
            BotStatus.LastRunDay = _snapshot.Day; BotStatus.LastRunHour = _snapshot.Hour; BotStatus.LastRunActions = n;

            string label = Label(trigger);
            string mode = Config.LiveWrites ? "LIVE" : "preview";
            string clock = $"day {_snapshot.Day} {_snapshot.Hour:00}:00";

            if (n > 0)
            {
                // Real work: always logged, clearly labelled auto-vs-manual with the in-game time.
                Activity.Add($"{label} [{mode}] {clock}: {n} action(s)");
            }
            else if (trigger == "manual")
            {
                Activity.Add($"{label} [{mode}] {clock}: nothing to do right now");
            }
            else if (_snapshot.Day != _lastHeartbeatDay)
            {
                // Idle auto-tick heartbeat: proves the bot is alive, throttled to once per in-game day so a
                // TURBO/skip burst of hourly ticks doesn't flood the log.
                _lastHeartbeatDay = _snapshot.Day;
                Activity.Add($"{label} [{mode}] {clock}: all caught up - watching");
            }
        }
        catch (Exception ex) { Debug.LogWarning($"[BA BOT] tick ({trigger}) failed: " + ex.Message); }
    }

    private static string Label(string trigger) => trigger switch
    {
        "manual" => "Manual",
        "NewDay" => "Auto (new day)",
        "load"   => "Auto (on load)",
        _        => "Auto (hour)",
    };
}
