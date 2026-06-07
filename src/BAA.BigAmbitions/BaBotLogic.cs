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
/// (which Unity can't instantiate from a runtime-loaded mod). Automation runs on GlobalEvents.onNewDay,
/// gated by the Core safety gate + the LIVE MODE switch; everything is default-OFF and preview-first.
/// </summary>
public sealed class BaBotLogic
{
    internal static readonly AutomationConfig Config = new();

    private readonly PanelView _panel = new();
    private GameSnapshot _snapshot;
    private bool _visible;
    private float _refreshTimer;
    private float _sinceWellbeing = 99f;
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
        GlobalEvents.onNewDay += OnNewDay;
        _subscribed = true;

        ctx.Logger.Info("BA BOT loaded - press F8 in-game for the panel.");
        Activity.Add("BA BOT loaded - press F8");
    }

    public void Shutdown()
    {
        if (_subscribed)
        {
            UnityLifecycleProvider.OnUpdate -= OnFrame;
            GlobalEvents.onNewDay -= OnNewDay;
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
            if (_refreshTimer >= 1f)
            {
                _refreshTimer = 0f;
                _snapshot = GameProbe.Read();
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

    private void OnNewDay() => RunAutomation("NewDay");

    private void RunAutomation(string trigger)
    {
        if (!Config.MasterEnabled)
        {
            if (trigger == "manual") Activity.Add("Enable AUTOMATION (MASTER) first");
            return;
        }
        try { EnsureEngine(); _engine.Tick(_state, _clock, Config); }
        catch (Exception ex) { Debug.LogWarning($"[BA BOT] tick ({trigger}) failed: " + ex.Message); }
    }
}
