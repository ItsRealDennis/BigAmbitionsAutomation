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
            var s = GameProbe.Read();
            if (!s.HasSave)
                return;
            ModEntry.Log.Msg($"[NewDay] Day {s.Day} | Money ${s.Money:N0} | NetWorth ${s.NetWorth:N0}");
            Diagnostics.Activity.Add($"Day {s.Day} began  -  ${s.Money:N0}");
        }
        catch (System.Exception ex)
        {
            ModEntry.Log?.Warning($"[NewDay] patch failed: {ex.Message}");
        }
    }
}
