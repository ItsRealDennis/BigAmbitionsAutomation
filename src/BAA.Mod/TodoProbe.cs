using System;
using System.Collections.Generic;
using System.Reflection;

namespace BAA.Mod;

/// <summary>
/// Extends the game's "your stock is running low" to-do warning to fire more days ahead. The game adds
/// that note to the to-do list 2 in-game days before a shop/warehouse runs low; this raises the lead
/// time to <see cref="BAA.Core.Config.AutomationConfig.LowStockWarningDays"/>.
///
/// The game's to-do system isn't in the API map yet, so we locate the threshold by reflection instead
/// of hard-binding a guessed member. To keep false positives near-zero we only accept a *settable
/// static* numeric field, on a to-do/stock-named type, whose name reads like a day/lead/warning value
/// and which currently holds exactly the game's default of 2. If nothing matches (e.g. the 2 is a
/// hardcoded literal or an instance field) we change nothing and dump the to-do shape to Player.log so
/// the real member can be identified and wired. Fully guarded — never throws into the game. TENTATIVE.
/// </summary>
internal static class TodoProbe
{
    private const BindingFlags StaticFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    private const BindingFlags InstFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;

    /// <summary>The lead-days value the game ships with — the field we override must currently equal it.</summary>
    private const int GameDefaultDays = 2;

    private static readonly string[] TypeHints =
        { "todo", "task", "reminder", "objective", "notification", "alert", "restock", "lowstock", "stock", "inventory" };
    private static readonly string[] FieldHints =
        { "day", "lead", "threshold", "warn", "ahead", "advance", "notice", "remind", "before" };

    private static FieldInfo _field;
    private static bool _searched;
    private static int _lastApplied = -1;

    /// <summary>Set the game's low-stock to-do lead time to <paramref name="days"/>. Cached after the
    /// first search, so this is cheap to call every tick. Returns false when no field was found.</summary>
    public static bool Apply(int days)
    {
        try
        {
            if (days < 1) days = 1;
            if (!_searched)
            {
                _field = FindThresholdField();
                _searched = true;
                if (_field == null)
                {
                    LogShape();
                    Diagnostics.Activity.Add("Low-stock warning: game to-do field not found (see log)");
                }
            }
            if (_field == null) return false;

            _field.SetValue(null, Convert.ChangeType(days, _field.FieldType));
            if (_lastApplied != days)
            {
                _lastApplied = days;
                Diagnostics.Activity.Add($"Low-stock warning set to {days} days ahead");
            }
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.Log?.Warning($"TodoProbe apply failed: {ex.Message}");
            return false;
        }
    }

    private static FieldInfo FindThresholdField()
    {
        foreach (var t in GameTypes())
        {
            if (!ContainsAny(t.Name, TypeHints)) continue;
            foreach (var f in SafeFields(t, StaticFlags))
            {
                if (f.IsLiteral || f.IsInitOnly) continue;     // const/readonly: can't change at runtime
                if (!IsIntLike(f.FieldType)) continue;
                if (!ContainsAny(f.Name, FieldHints)) continue;
                int cur;
                try { cur = Convert.ToInt32(f.GetValue(null)); } catch { continue; }
                if (cur == GameDefaultDays) return f;            // strong match: it holds the game's "2"
            }
        }
        return null;
    }

    /// <summary>One-shot diagnostic: dumps to-do/stock-named types and their numeric members to
    /// Player.log so the real low-stock lead field/method can be identified and wired (or, if it's a
    /// literal, transpiled).</summary>
    private static void LogShape()
    {
        var log = ModEntry.Log;
        try
        {
            log?.Msg("================ BA BOT to-do shape (low-stock lead discovery) ================");
            int dumped = 0;
            foreach (var t in GameTypes())
            {
                if (!ContainsAny(t.Name, TypeHints)) continue;
                if (dumped++ > 40) { log?.Msg("    ... (more types omitted)"); break; }
                log?.Msg($"  {t.FullName}");
                foreach (var f in SafeFields(t, StaticFlags))
                {
                    if (!IsIntLike(f.FieldType)) continue;
                    object v = null; try { v = f.GetValue(null); } catch { }
                    log?.Msg($"    static {f.FieldType.Name} {f.Name} = {v}{(f.IsLiteral ? " (const)" : "")}");
                }
                foreach (var f in SafeFields(t, InstFlags))
                {
                    if (!IsIntLike(f.FieldType)) continue;
                    log?.Msg($"    {f.FieldType.Name} {f.Name}");
                }
            }
            log?.Msg("== add the real lead field to TodoProbe (or note the method to transpile) ==");
        }
        catch { }
    }

    private static IEnumerable<Type> GameTypes()
    {
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            // Il2CppInterop puts the game's generated proxies in assemblies named "Il2Cpp*".
            string name = null;
            try { name = asm.GetName().Name; } catch { }
            if (name == null || name.IndexOf("Il2Cpp", StringComparison.OrdinalIgnoreCase) < 0)
                continue;
            Type[] types = null;
            try { types = asm.GetTypes(); }
            catch (ReflectionTypeLoadException ex) { types = ex.Types; }
            catch { }
            if (types == null) continue;
            foreach (var t in types)
                if (t != null) yield return t;
        }
    }

    private static IEnumerable<FieldInfo> SafeFields(Type t, BindingFlags flags)
    {
        FieldInfo[] fs = null;
        try { fs = t.GetFields(flags); } catch { }
        return fs ?? Array.Empty<FieldInfo>();
    }

    private static bool IsIntLike(Type t)
        => t == typeof(int) || t == typeof(short) || t == typeof(byte) || t == typeof(long) || t == typeof(float);

    private static bool ContainsAny(string s, string[] hints)
    {
        if (string.IsNullOrEmpty(s)) return false;
        foreach (var h in hints)
            if (s.IndexOf(h, StringComparison.OrdinalIgnoreCase) >= 0) return true;
        return false;
    }
}
