using System;
using System.Collections.Generic;
using System.Reflection;
using Il2Cpp;

namespace BAA.Mod;

/// <summary>One trainable person (employee) for the SKILLS panel: their display name and current
/// skill level as a 0-100 percent. <see cref="Index"/> is the slot in the game's
/// <c>EmployeeInstances</c> list, used to resolve the same person again when the player clicks to
/// train them to 100%.</summary>
internal struct PersonSkill
{
    public int Index;
    public string Name;
    public float Percent;
}

/// <summary>
/// Reads and maxes the skill of the player's staff — the "pick a person, train them to 100%" panel.
/// The employee SKILL field isn't in the API map yet, so we locate it by reflection (and log the
/// <c>EmployeeInstance</c> shape once, so the real field can be confirmed live) instead of
/// hard-binding a guessed member that might not even compile against the generated DLL. Fully
/// guarded — never throws into the game. TENTATIVE until the field is live-verified.
/// </summary>
internal static class SkillProbe
{
    // Candidate member names for an employee's skill/competence level, best guess first. Once the real
    // one is confirmed in Player.log (see LogShape), move it to the front so it always wins.
    private static readonly string[] SkillNames =
    {
        "skill", "Skill", "skillLevel", "SkillLevel", "skillPercentage", "SkillPercentage",
        "competence", "Competence", "expertise", "Expertise", "proficiency", "Proficiency",
        "experience", "Experience",
    };

    // Candidate no-arg methods that return a display name, best guess first.
    private static readonly string[] NameMethods =
        { "GetEmployeeNameWithInfo", "GetEmployeeName", "GetFullName", "GetName" };

    private const BindingFlags Flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
    private static bool _loggedShape;

    /// <summary>The player's hired staff (candidates excluded), each with their current skill percent.</summary>
    public static List<PersonSkill> Read()
    {
        var result = new List<PersonSkill>();
        try
        {
            var gi = SaveGameManager.Current;
            if (gi == null) return result;
            var emps = gi.EmployeeInstances;
            if (emps == null) return result;

            bool anySkill = false;
            object firstEmp = null;
            for (int i = 0; i < emps.Count; i++)
            {
                var e = emps[i];
                if (e == null) continue;
                try { if (IsCandidate(e)) continue; } catch { }

                firstEmp ??= e;
                float pct = ReadPercent(e, out bool found);
                anySkill |= found;
                result.Add(new PersonSkill { Index = i, Name = ReadName(e, i), Percent = pct });
            }

            // The skill field isn't mapped yet: dump the employee's numeric members once so it can be
            // identified in Player.log, then added to SkillNames and promoted to CONFIRMED.
            if (!anySkill && !_loggedShape && firstEmp != null)
                LogShape(firstEmp);
        }
        catch (Exception ex) { ModEntry.Log?.Warning($"SkillProbe read failed: {ex.Message}"); }
        return result;
    }

    /// <summary>Set one employee's skill to its maximum (100%). Returns false (and logs) when the
    /// person can't be resolved or no writable skill field is found.</summary>
    public static bool MaxOut(int index)
    {
        try
        {
            var gi = SaveGameManager.Current;
            var emps = gi?.EmployeeInstances;
            if (emps == null || index < 0 || index >= emps.Count) return false;
            var e = emps[index];
            if (e == null) return false;

            string name = ReadName(e, index);
            var prop = FindSkillProperty(e);
            if (prop == null || !prop.CanWrite)
            {
                Diagnostics.Activity.Add($"No skill field to train for {name}");
                return false;
            }

            float current = ToFloat(prop.GetValue(e));
            // A 0..1 game scale caps at 1.0; a 0..100 scale caps at 100. When the value is 0 we can't
            // tell, so assume 0..100 (the common case) — the game clamps an over-large write anyway.
            float max = current > 0f && current <= 1f ? 1f : 100f;
            prop.SetValue(e, Convert.ChangeType(max, prop.PropertyType));
            Diagnostics.Activity.Add($"Trained {name} to 100%");
            return true;
        }
        catch (Exception ex)
        {
            ModEntry.Log?.Warning($"SkillProbe maxout failed: {ex.Message}");
            return false;
        }
    }

    private static bool IsCandidate(object e)
    {
        var p = e.GetType().GetProperty("IsCandidate", Flags);
        return p != null && p.GetValue(e) is bool b && b;
    }

    private static string ReadName(object e, int i)
    {
        var t = e.GetType();
        foreach (var m in NameMethods)
        {
            try
            {
                var mi = t.GetMethod(m, Flags, null, Type.EmptyTypes, null);
                if (mi == null || mi.ReturnType != typeof(string)) continue;
                if (mi.Invoke(e, null) is string s && !string.IsNullOrWhiteSpace(s))
                    return s;
            }
            catch { }
        }
        return $"Employee {i + 1}";
    }

    private static float ReadPercent(object e, out bool found)
    {
        var prop = FindSkillProperty(e);
        found = prop != null;
        if (prop == null) return 0f;
        float v = ToFloat(prop.GetValue(e));
        return v > 0f && v <= 1f ? v * 100f : v; // normalise a 0..1 scale to a percent
    }

    private static PropertyInfo FindSkillProperty(object e)
    {
        var t = e.GetType();
        foreach (var name in SkillNames)
        {
            var p = t.GetProperty(name, Flags);
            if (p != null && p.CanRead && IsNumeric(p.PropertyType)) return p;
        }
        // Fallback: any readable numeric property whose name simply contains "skill".
        foreach (var p in t.GetProperties(Flags))
            if (p.CanRead && IsNumeric(p.PropertyType)
                && p.Name.IndexOf("skill", StringComparison.OrdinalIgnoreCase) >= 0)
                return p;
        return null;
    }

    private static bool IsNumeric(Type t)
        => t == typeof(float) || t == typeof(double) || t == typeof(int) || t == typeof(long);

    private static float ToFloat(object o)
    {
        try { return o == null ? 0f : Convert.ToSingle(o); } catch { return 0f; }
    }

    /// <summary>One-shot diagnostic: lists an employee's numeric members so the real skill field can be
    /// spotted in Player.log and promoted to a CONFIRMED entry in SkillNames / the API map.</summary>
    private static void LogShape(object e)
    {
        _loggedShape = true;
        try
        {
            var log = ModEntry.Log;
            log?.Msg("================ BA BOT employee shape (skill discovery) ================");
            foreach (var p in e.GetType().GetProperties(Flags))
            {
                if (!p.CanRead || !IsNumeric(p.PropertyType)) continue;
                object v = null;
                try { v = p.GetValue(e); } catch { }
                log?.Msg($"    {p.PropertyType.Name} {p.Name} = {v}");
            }
            log?.Msg("== add the real skill field to SkillProbe.SkillNames to make SKILLS live ==");
        }
        catch { }
    }
}
