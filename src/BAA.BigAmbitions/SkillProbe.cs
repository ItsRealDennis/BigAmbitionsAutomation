using System;
using System.Collections.Generic;
using BaBot.Diagnostics;

namespace BaBot;

/// <summary>One trainable person for the SKILLS panel: their name, primary-skill percent (0-100, the
/// same number the game shows), and a reference to the live <see cref="Entities.EmployeeInstance"/>.</summary>
internal struct PersonSkill
{
    public Entities.EmployeeInstance Emp;
    public string Name;
    public float Percent;
}

/// <summary>
/// Reads and maxes the skills of the player's staff — the "pick a person, train them to 100%" panel,
/// over the EA 0.11 (Mono) game. Uses the game's OWN confirmed API (no reflection guessing): skills
/// live in <c>EmployeeInstance.characterData.skills</c> (each a <c>Skill</c> with public
/// <c>name</c>/<c>value</c>); <c>IncreaseSkill</c> raises a skill and the game clamps it at 100. An
/// employee has several skills (e.g. Customer Service + Cleaning) — "train to 100%" maxes them all.
/// Fully guarded — never throws into the game.
/// </summary>
internal static class SkillProbe
{
    /// <summary>The player's hired staff (candidates excluded), each with their primary-skill percent.</summary>
    public static List<PersonSkill> Read()
    {
        var result = new List<PersonSkill>();
        try
        {
            var emps = Staff();
            if (emps == null) return result;
            for (int i = 0; i < emps.Count; i++)
            {
                var e = emps[i];
                if (e == null) continue;
                try { if (e.IsCandidate) continue; } catch { continue; }
                result.Add(new PersonSkill { Emp = e, Name = NameOf(e, i), Percent = PrimaryPct(e) });
            }
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] SkillProbe read failed: " + ex.Message); }
        return result;
    }

    /// <summary>Max every skill of one employee to 100%. Returns true if the person was handled.</summary>
    public static bool MaxOut(Entities.EmployeeInstance e)
    {
        if (e == null) return false;
        try
        {
            string name = NameOf(e, 0);
            int changed = MaxAllSkills(e);
            if (changed == 0) { Activity.Add($"{name} is already fully trained"); return true; }
            try { SaveGameManager.MarkChange(); } catch { }
            Activity.Add($"Trained {name} - {changed} skill(s) to 100%");
            return true;
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("[BA BOT] SkillProbe maxout failed: " + ex.Message);
            return false;
        }
    }

    private static List<Entities.EmployeeInstance> Staff()
    {
        try { return Helpers.EmployeeHelper.GetEmployeeInstances(); } catch { return null; }
    }

    /// <summary>Raise every one of the employee's skills to 100 via the game's own IncreaseSkill (which
    /// clamps at 100). Returns how many skills were actually below 100 and got bumped.</summary>
    private static int MaxAllSkills(Entities.EmployeeInstance e)
    {
        int changed = 0;
        var cd = e.characterData;
        var skills = cd != null ? cd.skills : null;
        if (skills == null) return 0;
        for (int i = 0; i < skills.Count; i++)
        {
            var s = skills[i];
            if (s == null) continue;
            if (s.value < 100f) { e.IncreaseSkill(s, 100f - s.value); changed++; }
        }
        return changed;
    }

    /// <summary>The primary-skill percent (matches the game's own "(Skill: NN%)" readout).</summary>
    private static float PrimaryPct(Entities.EmployeeInstance e)
    {
        try { return e.GetSkillValue(e.GetPrimarySkill()); } catch { return 0f; }
    }

    private static string NameOf(Entities.EmployeeInstance e, int i)
    {
        try { var cd = e.characterData; if (cd != null && !string.IsNullOrWhiteSpace(cd.name)) return cd.name; } catch { }
        return $"Employee {i + 1}";
    }
}
