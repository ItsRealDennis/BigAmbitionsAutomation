using HarmonyLib;
using Il2Cpp;

namespace BAA.Mod.Hooks;

/// <summary>
/// Stops overlay clicks from leaking into the 3D world. <c>MouseController.Run()</c> is the game's
/// central per-frame world-input driver (click-to-move, object interaction, selection). When the
/// cursor is over our panel we skip it, so the click reaches only the IMGUI overlay — which lives on
/// a separate OnGUI/Event path and is unaffected. (Game uses the new Input System, so there is no
/// EventSystem "pointer over UI" gate to reuse; this is the correct choke point.)
/// </summary>
[HarmonyPatch(typeof(MouseController), "Run")]
internal static class MouseClickBlockPatch
{
    // HarmonyLib: a bool-returning prefix that returns false SKIPS the original method.
    private static bool Prefix()
    {
        try
        {
            return !ModEntry.PointerOverPanel; // over panel -> skip world input; else run normally
        }
        catch
        {
            return true; // never break the game's per-frame input loop
        }
    }
}
