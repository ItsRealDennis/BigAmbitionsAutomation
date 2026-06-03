using BAA.Core.Config;
using BAA.Mod.UI;
using MelonLoader;
using UnityEngine;

namespace BAA.Mod;

/// <summary>
/// MelonLoader entry point. Hosts the shared automation config, refreshes a live snapshot, and draws
/// the F8 control-panel overlay. (Engine wiring that ACTS on the config comes in the M5 slice.)
/// </summary>
public sealed class ModEntry : MelonMod
{
    /// <summary>Shared logger for the static Harmony patches.</summary>
    internal static MelonLogger.Instance Log;

    /// <summary>The single config the overlay edits and the engine will read.</summary>
    internal static readonly AutomationConfig Config = new();

    private readonly OverlayUI _overlay = new();
    private bool _visible;
    private float _refreshTimer;
    private GameSnapshot _snapshot;

    public override void OnInitializeMelon()
    {
        Log = LoggerInstance;
        Log.Msg("================ BA BOT loaded ================");
        Log.Msg($"Core linked OK (master default = {Config.MasterEnabled}). Press F8 in-game for the panel.");
        Diagnostics.Activity.Add("BA BOT loaded - press F8 for the panel");
    }

    public override void OnUpdate()
    {
        _refreshTimer += Time.deltaTime;
        if (_refreshTimer >= 1f)
        {
            _refreshTimer = 0f;
            _snapshot = GameProbe.Read();
        }

        if (_visible)
        {
            // Keep the cursor usable while the panel is open.
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
    }

    public override void OnGUI()
    {
        // Toggle via IMGUI events so it works regardless of the active Input System.
        var e = Event.current;
        if (e != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.F8)
        {
            _visible = !_visible;
            e.Use();
        }

        if (_visible)
            _overlay.Draw(Config, _snapshot);
    }
}
