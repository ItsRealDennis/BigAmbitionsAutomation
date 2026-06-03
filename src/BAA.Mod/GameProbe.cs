using Il2Cpp;

namespace BAA.Mod;

/// <summary>
/// Reads a read-only snapshot of the live game and logs it. This is our runtime "ground truth" for
/// API discovery: confirms SaveGameManager.Current / GameInstance access works before we build the
/// full adapter on top of it.
/// </summary>
internal static class GameProbe
{
    public static void LogSnapshot(string tag)
    {
        var log = ModEntry.Log;
        try
        {
            var gi = SaveGameManager.Current;
            if (gi == null)
                return; // no save loaded yet

            int buildings = gi.BuildingRegistrations != null ? gi.BuildingRegistrations.Count : -1;
            int employees = gi.EmployeeInstances != null ? gi.EmployeeInstances.Count : -1;
            int candidates = gi.CandidateEmployeeInstances != null ? gi.CandidateEmployeeInstances.Count : -1;
            int loans = gi.Loans != null ? gi.Loans.Count : -1;
            int imports = gi.importPartnerships != null ? gi.importPartnerships.Count : -1;

            log.Msg(
                $"[{tag}] Day {gi.Day} {gi.Hour:00}:{(int)gi.Minute:00} | " +
                $"Money ${gi.Money:N0} | NetWorth ${gi.NetWorth:N0} | " +
                $"Buildings {buildings} | Employees {employees} | Candidates {candidates} | " +
                $"Loans {loans} | ImportPartnerships {imports}");
        }
        catch (System.Exception ex)
        {
            log?.Warning($"[{tag}] probe failed: {ex.Message}");
        }
    }
}
