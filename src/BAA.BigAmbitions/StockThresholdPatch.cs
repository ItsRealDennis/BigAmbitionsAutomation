using System;
using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;

namespace BaBot;

/// <summary>
/// Makes the game's "stock running low" to-do fire EARLIER. EA 0.11 adds a LowStock TodoTask when a
/// product drops to &lt;= 25% of its max shelf capacity — a hardcoded <c>0.25f</c> literal in two
/// methods (<c>ItemHelper.RunUpdateStockTodoTasks</c> and <c>Helpers.BusinessHelper.GenerateItemsWithoutStockTasks</c>).
/// There is NO "days ahead" lead constant, so the old reflection-based TodoProbe could never work. This
/// uses a Harmony transpiler to rewrite that single literal in each method to a configurable fraction
/// (<see cref="BAA.Core.Config.AutomationConfig.LowStockAlertPercent"/>): a higher percent warns sooner.
/// The replacement is a <c>call</c> to <see cref="Fraction"/>, so the value is read LIVE every stock
/// check — changing the panel value takes effect immediately, no re-patch.
///
/// Safety: scoped per-method and a strict no-op unless EXACTLY one <c>0.25f</c> is found in that method
/// body, so it can never corrupt unrelated math (other 0.25f literals live in CompetitionHelper). Fully
/// guarded — a failure leaves the game's vanilla behaviour untouched.
/// </summary>
internal static class StockThresholdPatch
{
    private static readonly Harmony H = new Harmony("babot.stockthreshold");
    private static bool _applied;

    /// <summary>Stock fraction at/below which the low-stock to-do fires. 0.25 = vanilla (25%).</summary>
    public static float Fraction()
    {
        try { return Mathf.Clamp(BaBotLogic.Config.LowStockAlertPercent / 100f, 0.1f, 0.95f); }
        catch { return 0.25f; }
    }

    /// <summary>Apply the patches once (idempotent). Harmony patches survive city reloads and the daily
    /// to-do recompute, so there is nothing to re-assert per frame.</summary>
    public static void Apply()
    {
        if (_applied) return;
        _applied = true;
        try
        {
            var tr = new HarmonyMethod(typeof(StockThresholdPatch), nameof(Transpiler));
            Patch("ItemHelper", "RunUpdateStockTodoTasks", tr);
            Patch("Helpers.BusinessHelper", "GenerateItemsWithoutStockTasks", tr);
        }
        catch (Exception ex) { Debug.LogWarning("[BA BOT] stock-threshold patch failed: " + ex.Message); }
    }

    private static void Patch(string typeName, string methodName, HarmonyMethod transpiler)
    {
        try
        {
            var t = AccessTools.TypeByName(typeName);
            if (t == null) { Debug.LogWarning($"[BA BOT] stock patch: type '{typeName}' not found"); return; }
            var m = AccessTools.Method(t, methodName);
            if (m == null) { Debug.LogWarning($"[BA BOT] stock patch: '{typeName}.{methodName}' not found"); return; }
            H.Patch(m, transpiler: transpiler);
        }
        catch (Exception ex) { Debug.LogWarning($"[BA BOT] stock patch '{typeName}.{methodName}': " + ex.Message); }
    }

    // Replace the SINGLE `ldc.r4 0.25` in this method body with `call Fraction()`. The IL context is
    // `(float)maxCapacity * 0.25f`, i.e. `... conv.r4; ldc.r4 0.25; mul` -> `... conv.r4; call Fraction; mul`,
    // which is stack-balanced (Fraction takes no args, returns one float).
    private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
    {
        var list = new List<CodeInstruction>(instructions);
        int idx = -1, count = 0;
        for (int i = 0; i < list.Count; i++)
            if (list[i].opcode == OpCodes.Ldc_R4 && list[i].operand is float f && Math.Abs(f - 0.25f) < 1e-6f) { idx = i; count++; }
        if (count != 1)
        {
            Debug.LogWarning($"[BA BOT] stock patch: expected one 0.25f, found {count}; method left unchanged");
            return list;
        }
        list[idx] = new CodeInstruction(OpCodes.Call, AccessTools.Method(typeof(StockThresholdPatch), nameof(Fraction)));
        return list;
    }
}
