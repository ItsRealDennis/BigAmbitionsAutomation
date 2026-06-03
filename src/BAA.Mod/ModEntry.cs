using BAA.Core.Config;
using BAA.Mod.UI;
using MelonLoader;
using UnityEngine;

namespace BAA.Mod;

/// <summary>
/// MelonLoader entry point. Hosts the shared automation config, refreshes a live snapshot, draws the
/// F8 control panel, and runs the instant features (time-skip fast-forward). Economic automation that
/// acts on the feature toggles (restock/finance) arrives in the M5 slice.
/// </summary>
public sealed class ModEntry : MelonMod
{
    internal static MelonLogger.Instance Log;
    internal static readonly AutomationConfig Config = new();

    /// <summary>True when the cursor is over the open panel — read by the click-through-blocking patch.</summary>
    internal static bool PointerOverPanel;

    private const float FastFactor = 8f;

    private readonly OverlayUI _overlay = new();
    private bool _visible;
    private float _refreshTimer;
    private GameSnapshot _snapshot;

    private bool _fastActive;
    private float _savedMultiplier = 1f;
    private bool _drawFaulted;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        ModPreferences.Load(Config);
        Log.Msg("================ BA BOT loaded ================");
        Log.Msg($"Settings loaded (master = {Config.MasterEnabled}). Press F8 in-game for the panel.");
        Diagnostics.Activity.Add("BA BOT loaded - press F8");
    }

    public override void OnUpdate()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= 1f)
        {
            _refreshTimer = 0f;
            _snapshot = GameProbe.Read();
            MaybeRefillEnergy();
            ModPreferences.SaveIfChanged(Config);
        }

        ApplyTimeSkip();

        if (_visible)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    /// <summary>Auto-wellbeing: refill energy when it dips, so the player never has to stop to sleep.</summary>
    private void MaybeRefillEnergy()
    {
        if (Config.MasterEnabled && Config.WellbeingEnabled && _snapshot.HasSave && _snapshot.Energy < 30f)
            GameActions.RefillEnergy();
    }

    /// <summary>Fast-forwards in-game time while Time-skip is on; restores the original speed when off.</summary>
    private void ApplyTimeSkip()
    {
        bool wantFast = Config.MasterEnabled && Config.TimeSkipEnabled && _snapshot.HasSave;
        if (wantFast)
        {
            if (!_fastActive)
            {
                _savedMultiplier = GameActions.GetTimeMultiplier();
                if (_savedMultiplier <= 0f)
                    _savedMultiplier = 1f;
                _fastActive = true;
                Diagnostics.Activity.Add($"Time-skip ON ({FastFactor:0}x)");
            }
            GameActions.SetTimeMultiplier(_savedMultiplier * FastFactor);
        }
        else if (_fastActive)
        {
            GameActions.SetTimeMultiplier(_savedMultiplier);
            _fastActive = false;
            Diagnostics.Activity.Add("Time-skip OFF");
        }
    }

    public override void OnGUI()
    {
        var e = Event.current;
        if (e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.F8)
        {
            _visible = !_visible;
            e.Use();
        }

        // Track whether the cursor is over the panel so the world-click block patch can read it.
        PointerOverPanel = _visible && e != null && OverlayUI.PanelRect.Contains(e.mousePosition);

        if (!_visible)
            return;

        try
        {
            _overlay.Draw(Config, _snapshot);
        }
        catch (System.Exception ex)
        {
            if (!_drawFaulted)
            {
                _drawFaulted = true; // log once, never spam
                Log?.Error($"overlay draw failed (disabled): {ex}");
            }
        }
    }
}
