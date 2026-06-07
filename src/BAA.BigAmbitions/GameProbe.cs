using System;

namespace BaBot;

/// <summary>Immutable snapshot of the live game for the panel + logs (EA 0.11 Mono).</summary>
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
    public int PlayerBusinesses;
    public int Employees;
    public int Loans;
    public float TaxDue;
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
            try { s.Loans = gi.Loans != null ? gi.Loans.Count : 0; } catch { }
            try { s.Employees = gi.EmployeeInstances != null ? gi.EmployeeInstances.Count : 0; } catch { }
            try { if (gi.currentUnpaidTaxes != null) s.TaxDue = gi.currentUnpaidTaxes.totalToPay; } catch { }
            s.PlayerBusinesses = CountPlayerBusinesses(gi);
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("[BA BOT] probe read failed: " + ex.Message);
        }
        return s;
    }

    private static int CountPlayerBusinesses(GameInstance gi)
    {
        var regs = gi.BuildingRegistrations;
        if (regs == null) return 0;
        int n = 0;
        for (int i = 0; i < regs.Count; i++)
        {
            var r = regs[i];
            bool mine = false;
            try { mine = r.RentedByPlayer || r.BuildingOwnedByPlayer; } catch { }
            if (!mine) continue;
            try { var sale = r.GetListOfItemsForSale(); if (sale != null && sale.Count > 0) n++; } catch { }
        }
        return n;
    }
}
