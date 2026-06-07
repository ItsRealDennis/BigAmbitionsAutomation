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
}
