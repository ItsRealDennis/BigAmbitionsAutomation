using System;
using BAA.Core.Config;
using BAA.Core.Engine;
using BAA.Core.Managers;
using BAA.Core.Safety;
using BAA.Core.Safety.Breakers;
using BaBot.Diagnostics;
using BaBot.Game;
using BaBot.UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BaBot;

/// <summary>
/// The live host: a MonoBehaviour created when a city loads. Draws the F8 panel (OnGUI), polls the
/// F8 hotkey, runs auto-wellbeing, and drives the Core orchestration engine on each in-game day
/// (GlobalEvents.onNewDay) and on the panel's RUN NOW button. All economic writes are gated by the
/// engine's safety gate + the LiveWrites switch.
/// </summary>
public sealed class BaBotBehaviour : MonoBehaviour
{
    internal static readonly AutomationConfig Config = new();
    private static bool _settingsLoaded;
    private static BaBotBehaviour _instance;

    private readonly OverlayUI _overlay = new();
    private GameSnapshot _snapshot;
    private bool _visible;
    private float _refreshTimer;
    private float _sinceRefill = 99f;
    private bool _loggedUpdateError;
    private bool _wasVisible;
    private CursorLockMode _savedLock;
    private bool _savedCursorVisible;

    private OrchestrationEngine _engine;
    private GameStateAdapter _state;
    private GameClockAdapter _clock;

    private void Awake()
    {
        // Never run two hosts at once (would double-subscribe onNewDay / draw twice).
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;

        if (!_settingsLoaded) { Settings.Load(Config); _settingsLoaded = true; }
        _overlay.OnRunNow = () => RunAutomation("manual");
        EnsureEngine();
        GlobalEvents.onNewDay += OnNewDay;
        Activity.Add("BA BOT loaded - press F8");
    }

    private void OnDestroy()
    {
        if (_instance != this) return;
        _instance = null;
        GlobalEvents.onNewDay -= OnNewDay;
        Settings.SaveIfChanged(Config);
    }

    private void EnsureEngine()
    {
        if (_engine != null) return;
        _state = new GameStateAdapter(Config);
        _clock = new GameClockAdapter();
        var commands = new GameCommandsAdapter(_state, Config);
        var breakers = new ISafetyBreaker[] { new LowFundsBreaker(), new UnpaidRentBreaker(), new EmptyInventoryBreaker() };
        var gate = new SafetyGate(breakers);
        var managers = new IAutomationManager[]
        {
            new FinanceManager(),
            new LogisticsManager(),
            new RestockManager(),
            new EmployeeManager(),
        };
        _engine = new OrchestrationEngine(managers, gate, commands, new ModLog());
    }

    private void Update()
    {
        // Whole loop guarded: a per-frame throw must never spam-crash or destabilise the game.
        try
        {
            _refreshTimer += Time.deltaTime;
            _sinceRefill += Time.deltaTime;
            if (_refreshTimer >= 1f)
            {
                _refreshTimer = 0f;
                _snapshot = GameProbe.Read();
                MaybeWellbeing();
                Settings.SaveIfChanged(Config);
            }

            // F8 toggle (new Input System), ignored while a text field is focused.
            // Own guard, silent: a per-frame failure here must never spam the player log.
            try
            {
                var kb = Keyboard.current;
                if (kb != null && kb.f8Key.wasPressedThisFrame && !GameManager.HasInputSelected())
                    _visible = !_visible;
            }
            catch { }

            // Free the cursor while the panel is open; restore the game's cursor state on close.
            if (_visible)
            {
                if (!_wasVisible) { _savedLock = Cursor.lockState; _savedCursorVisible = Cursor.visible; _wasVisible = true; }
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else if (_wasVisible)
            {
                Cursor.lockState = _savedLock;
                Cursor.visible = _savedCursorVisible;
                _wasVisible = false;
            }
        }
        catch (Exception ex)
        {
            if (!_loggedUpdateError) { _loggedUpdateError = true; Debug.LogWarning("[BA BOT] update failed (logged once): " + ex.Message); }
        }
    }

    private void OnGUI()
    {
        if (!_visible) return;
        try { _overlay.Draw(Config, _snapshot); }
        catch (Exception ex) { Debug.LogError("[BA BOT] panel draw failed: " + ex); }
    }

    /// <summary>Auto-wellbeing: top up energy (then happiness) when low, debounced.</summary>
    private void MaybeWellbeing()
    {
        if (!(Config.MasterEnabled && Config.WellbeingEnabled && _snapshot.HasSave) || _sinceRefill <= 15f)
            return;
        if (_snapshot.Energy < 30f) { GameActions.RefillEnergy(); _sinceRefill = 0f; }
        else if (_snapshot.Happiness < 30f) { GameActions.BoostHappiness(50); Activity.Add("Happiness boosted"); _sinceRefill = 0f; }
    }

    private void OnNewDay() => RunAutomation("NewDay");

    /// <summary>One safety-gated automation pass. Master switch required (the engine enforces it too).</summary>
    private void RunAutomation(string trigger)
    {
        if (!Config.MasterEnabled)
        {
            if (trigger == "manual") Activity.Add("Enable AUTOMATION (MASTER) first");
            return;
        }
        try { EnsureEngine(); _engine.Tick(_state, _clock, Config); }
        catch (Exception ex) { Debug.LogWarning($"[BA BOT] automation tick ({trigger}) failed: " + ex.Message); }
    }
}
