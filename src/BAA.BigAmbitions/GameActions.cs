using System;
using BaBot.Diagnostics;

namespace BaBot;

/// <summary>
/// Direct, always-live helpers for the instant quick-actions and auto-wellbeing. These are additive /
/// cosmetic (cash test button, energy, happiness) and use the game's static debug commands, so they're
/// safe and don't go through the LiveWrites gate. Economic automation flows through the Core engine.
/// </summary>
internal static class GameActions
{
    public static void AddMoney(float amount)
    {
        try
        {
            GameManager.Command_ChangeMoney(amount);
            Activity.Add($"{(amount >= 0 ? "+" : "")}${amount:N0} cash (manual)");
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] AddMoney failed: " + ex.Message); }
    }

    public static void RefillEnergy()
    {
        try
        {
            GameManager.Command_SetEnergy(100f);
            Activity.Add("Energy refilled to 100%");
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] RefillEnergy failed: " + ex.Message); }
    }

    public static void BoostHappiness(int amount)
    {
        try { GameManager.Command_ChangeHappiness(amount); }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] BoostHappiness failed: " + ex.Message); }
    }

    // --- AFK time control (pure QoL: no cash, no LiveWrites gate; uses the game's own time commands) ---

    private static bool _turbo;
    private static int _turboPercent = 300;

    /// <summary>True while the AFK time accelerator is engaged.</summary>
    public static bool TurboOn => _turbo;

    /// <summary>Engage/disengage the AFK accelerator via the game's own time-speed command
    /// (TimeHelper.Command_SetTimeSpeed -> GameManager.SetMinutesMultiplier). Off restores 100% (normal).</summary>
    public static void SetTurbo(bool on, int percent)
    {
        try
        {
            int p = on ? (percent < 100 ? 100 : (percent > 1000 ? 1000 : percent)) : 100;
            TimeHelper.Command_SetTimeSpeed(p);
            _turbo = on;
            if (on) _turboPercent = p;
            Activity.Add(on ? $"Turbo ON ({p}%)" : "Turbo OFF (normal speed)");
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] SetTurbo failed: " + ex.Message); }
    }

    /// <summary>Restore normal speed - called on shutdown so the game is never left stuck fast.</summary>
    public static void ResetTurbo()
    {
        if (!_turbo) return;
        try { TimeHelper.Command_SetTimeSpeed(100); } catch { }
        _turbo = false;
    }

    /// <summary>Fast-forward to the next morning (08:00) via the game's TimeMachine. Crossing midnight fires
    /// the daily automation cycle, so the bot works while you skip.</summary>
    public static void SkipToMorning()
    {
        try
        {
            var gi = SaveGameManager.Current;
            if (gi == null) { Activity.Add("Skip: no save loaded"); return; }
            int hoursAhead = ((8 - gi.Hour) % 24 + 24) % 24;
            if (hoursAhead == 0) hoursAhead = 24; // already 08:00 -> skip a full day
            string skipArg = hoursAhead == 24 ? "1d" : hoursAhead + "h"; // "1d" avoids ambiguity vs "24h"

            // The TimeMachine resets game speed to normal when it finishes, which would silently drop an
            // active turbo to 1x. Re-stamp the turbo speed once the skip ends (one-shot listener).
            if (_turbo)
            {
                Action reapply = null;
                reapply = () => { try { TimeHelper.Command_SetTimeSpeed(_turboPercent); } catch { } GlobalEvents.onTimeMachineEnded -= reapply; };
                GlobalEvents.onTimeMachineEnded += reapply;
            }

            Timemachine.TimeMachine.SkipTime(skipArg);
            Activity.Add($"Skipping to next morning (+{hoursAhead}h)");
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] SkipToMorning failed: " + ex.Message); }
    }
}
