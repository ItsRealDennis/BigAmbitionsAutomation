using Il2Cpp;

namespace BAA.Mod;

/// <summary>
/// The write path into the game (the only place we mutate state directly, for instant-testable
/// features). All calls are static GameManager commands and are wrapped so a failure never escapes.
/// Economic automation (restock/finance) will instead flow through the Core engine + safety gate.
/// </summary>
internal static class GameActions
{
    public static void AddMoney(float amount)
    {
        try
        {
            GameManager.Command_ChangeMoney(amount);
            Diagnostics.Activity.Add($"{(amount >= 0 ? "+" : "")}${amount:N0} cash (manual)");
        }
        catch (System.Exception ex) { ModEntry.Log?.Warning($"AddMoney failed: {ex.Message}"); }
    }

    public static void RefillEnergy()
    {
        try
        {
            GameManager.Command_SetEnergy(100f);
            Diagnostics.Activity.Add("Energy refilled to 100%");
        }
        catch (System.Exception ex) { ModEntry.Log?.Warning($"RefillEnergy failed: {ex.Message}"); }
    }

    public static void BoostHappiness(int amount)
    {
        try { GameManager.Command_ChangeHappiness(amount); }
        catch (System.Exception ex) { ModEntry.Log?.Warning($"BoostHappiness failed: {ex.Message}"); }
    }

    public static float GetTimeMultiplier()
    {
        try { return GameManager.MinutesMultiplier; }
        catch { return 1f; }
    }

    public static void SetTimeMultiplier(float value)
    {
        try { GameManager.MinutesMultiplier = value; }
        catch (System.Exception ex) { ModEntry.Log?.Warning($"SetTimeMultiplier failed: {ex.Message}"); }
    }
}
