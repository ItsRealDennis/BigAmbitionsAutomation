using HarmonyLib;
using Il2Cpp;

namespace BAA.Mod.Hooks;

/// <summary>
/// Hooks the in-game day boundary. GameManager.NewDay() runs once per in-game day — this is the
/// trigger auto-restock/upkeep will use. For now the patch only logs a snapshot (HookGuard: the body
/// is wrapped so a fault can never crash the game).
/// </summary>
[HarmonyPatch(typeof(GameManager), "NewDay")]
internal static class NewDayPatch
{
    private static void Postfix()
    {
        try
        {
            GameProbe.LogSnapshot("NewDay");
        }
        catch (System.Exception ex)
        {
            ModEntry.Log?.Warning($"[NewDay] patch failed: {ex.Message}");
        }
    }
}
