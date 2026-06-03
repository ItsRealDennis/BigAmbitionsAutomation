using Il2Cpp;

namespace BAA.Mod;

/// <summary>Immutable snapshot of the live game for the UI + logs.</summary>
internal struct GameSnapshot
{
    public bool HasSave;
    public int Day;
    public int Hour;
    public float Minute;
    public float Money;
    public float NetWorth;
    public float Energy;
    public float Happiness;
    public int CityBuildings;
    public int Employees;
    public int Candidates;
    public int Loans;
    public int ImportPartnerships;
}

/// <summary>
/// Reads a read-only snapshot of the live game via SaveGameManager.Current / GameInstance.
/// Null-safe: returns HasSave=false when no save is loaded. Never throws into callers.
/// </summary>
internal static class GameProbe
{
    public static GameSnapshot Read()
    {
        var s = new GameSnapshot();
        try
        {
            var gi = SaveGameManager.Current;
            if (gi == null)
                return s;

            s.HasSave = true;
            s.Day = gi.Day;
            s.Hour = gi.Hour;
            s.Minute = gi.Minute;
            s.Money = gi.Money;
            s.NetWorth = gi.NetWorth;
            s.Energy = gi.Energy;
            s.Happiness = gi.Happiness;
            s.CityBuildings = gi.BuildingRegistrations != null ? gi.BuildingRegistrations.Count : 0;
            s.Employees = gi.EmployeeInstances != null ? gi.EmployeeInstances.Count : 0;
            s.Candidates = gi.CandidateEmployeeInstances != null ? gi.CandidateEmployeeInstances.Count : 0;
            s.Loans = gi.Loans != null ? gi.Loans.Count : 0;
            s.ImportPartnerships = gi.importPartnerships != null ? gi.importPartnerships.Count : 0;
        }
        catch (System.Exception ex)
        {
            ModEntry.Log?.Warning($"probe read failed: {ex.Message}");
        }
        return s;
    }

    public static void LogSnapshot(string tag)
    {
        var s = Read();
        if (!s.HasSave)
            return;
        ModEntry.Log.Msg(
            $"[{tag}] Day {s.Day} {s.Hour:00}:{(int)s.Minute:00} | Money ${s.Money:N0} | " +
            $"NetWorth ${s.NetWorth:N0} | CityBuildings {s.CityBuildings} | Employees {s.Employees}");
    }
}
