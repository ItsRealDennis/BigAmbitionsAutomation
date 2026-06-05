using System.Collections.Generic;
using BAA.Core.Abstractions;
using BAA.Core.Config;
using Il2Cpp;
using Il2CppBigAmbitions.Items;

namespace BAA.Mod.Game;

/// <summary>
/// IGameState over the live game, focused on the auto-restock slice. Enumerates the player's
/// businesses and their per-product current stock via the game's own helpers, and exposes a
/// within-tick resolver so the commands adapter can map a (BusinessId, ItemId) back to the real
/// BuildingRegistration + ItemName. Caches are rebuilt each GetBusinesses/GetInventory call (the
/// engine reads then applies within one tick, so within-tick freshness is all that's needed).
/// </summary>
internal sealed class GameStateAdapter : IGameState
{
    private readonly AutomationConfig _config;
    private readonly Dictionary<BusinessId, BuildingRegistration> _regs = new();
    private readonly Dictionary<BusinessId, Dictionary<ItemId, ItemName>> _items = new();
    private readonly Dictionary<EmployeeId, Il2CppEntities.EmployeeInstance> _emps = new();

    public GameStateAdapter(AutomationConfig config) => _config = config;

    public bool IsWorldReady() => SaveGameManager.Current != null;

    public GameTimeInfo GetTime()
    {
        var gi = SaveGameManager.Current;
        if (gi == null)
            return new GameTimeInfo(0, 0, 0, 0, 0, System.DayOfWeek.Monday, 0);
        int d = gi.Day, h = gi.Hour, m = (int)gi.Minute;
        return new GameTimeInfo(0, 0, d, h, m, System.DayOfWeek.Monday, (long)d * 1440 + h * 60 + m);
    }

    public FinanceSnapshot GetFinances()
    {
        var gi = SaveGameManager.Current;
        if (gi == null)
            return new FinanceSnapshot(0m, 0m, 0m, 0m, false);

        decimal cash = (decimal)gi.Money;
        decimal netWorth = 0m;
        try { netWorth = (decimal)gi.NetWorth; } catch { }

        // Loans: sum the outstanding balance and the daily installment the game auto-deducts.
        decimal loanRemaining = 0m, loanDaily = 0m;
        try
        {
            var loans = gi.Loans;
            if (loans != null)
                for (int i = 0; i < loans.Count; i++)
                {
                    var loan = loans[i];
                    try { loanRemaining += (decimal)loan.remainingAmount; } catch { }
                    try { loanDaily += loan.dailyPayment; } catch { }
                }
        }
        catch { }

        // Unpaid taxes (the one finance chore the player must do by hand).
        decimal taxDue = 0m;
        try
        {
            var taxes = gi.currentUnpaidTaxes;
            if (taxes != null)
                taxDue = (decimal)taxes.totalToPay;
        }
        catch { }
        if (taxDue < 0m) taxDue = 0m;

        return new FinanceSnapshot(cash, 0m, 0m, loanDaily, false, taxDue, netWorth, loanRemaining);
    }

    public IReadOnlyList<BusinessInfo> GetBusinesses()
    {
        _regs.Clear();
        var list = new List<BusinessInfo>();
        var gi = SaveGameManager.Current;
        var regs = gi?.BuildingRegistrations;
        if (regs == null)
            return list;

        int idx = 0;
        for (int i = 0; i < regs.Count; i++)
        {
            var reg = regs[i];
            bool mine = false;
            try { mine = reg.RentedByPlayer || reg.BuildingOwnedByPlayer; } catch { }
            if (!mine)
                continue;
            var sale = reg.GetListOfItemsForSale();
            if (sale == null || sale.Count == 0)
                continue;

            var id = new BusinessId("b" + idx++);
            string type = "Business";
            try { type = reg.businessTypeName.ToString(); } catch { }
            _regs[id] = reg;
            list.Add(new BusinessInfo(id, type, type, true));
        }
        return list;
    }

    public IReadOnlyList<InventoryLine> GetInventory(BusinessId business)
    {
        var lines = new List<InventoryLine>();
        if (!_regs.TryGetValue(business, out var reg))
            return lines;
        var sale = reg.GetListOfItemsForSale();
        if (sale == null)
            return lines;

        int target = _config.RestockTarget;
        var map = new Dictionary<ItemId, ItemName>();
        for (int j = 0; j < sale.Count; j++)
        {
            var name = sale[j];
            int current = 0;
            try { current = Il2CppHelpers.BuildingHelper.CountTotalResourcesInStock(reg, name, true, true); } catch { }
            decimal unitCost = 0m;
            try { unitCost = (decimal)ItemHelper.GetPrice(name, reg); } catch { }

            var itemId = new ItemId(name.ToString());
            map[itemId] = name;
            lines.Add(new InventoryLine(itemId, name.ToString(), current, target, target, unitCost));
        }
        _items[business] = map;
        return lines;
    }

    /// <summary>Resolve a (business, item) decided by the engine back to the live game objects.</summary>
    public bool TryResolve(BusinessId b, ItemId item, out BuildingRegistration reg, out ItemName name)
    {
        name = default;
        if (_regs.TryGetValue(b, out reg) && _items.TryGetValue(b, out var map) && map.TryGetValue(item, out name))
            return true;
        reg = null;
        return false;
    }

    /// <summary>
    /// Live employee roster via EmployeeHelper. Maps each hired employee (candidates excluded) to a flat
    /// snapshot the EmployeeManager can plan against: morale (normalised to 0–1), whether a bonus is
    /// available now (the game decides eligibility via CanGiveBonus) and what it would cost. Caches the
    /// IL2CPP instance by a tick-stable id so the commands adapter can act on the same employee.
    /// TrainingComplete is left false for now — finishing training live awaits a verified completion check.
    /// </summary>
    public IReadOnlyList<EmployeeInfo> GetEmployees(BusinessId? scope = null)
    {
        _emps.Clear();
        var list = new List<EmployeeInfo>();
        Il2CppSystem.Collections.Generic.List<Il2CppEntities.EmployeeInstance> all;
        try { all = Il2CppHelpers.EmployeeHelper.GetEmployeeInstances(); }
        catch { return list; }
        if (all == null)
            return list;

        int idx = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null)
                continue;
            try { if (e.IsCandidate) continue; } catch { }

            float sat = 1f;
            try { sat = e.satisfaction; } catch { }
            if (sat > 1.5f) sat /= 100f; // normalise a 0–100 morale scale to 0–1

            decimal wage = 0m;
            try { wage = (decimal)e.hourlyWage; } catch { }

            bool bonusReady = false;
            try { bonusReady = e.CanGiveBonus(); } catch { }
            decimal bonusCost = 0m;
            try { bonusCost = (decimal)e.GetBonusAmount(); } catch { }

            string name = "Employee";
            try { name = e.GetEmployeeNameWithInfo(); } catch { }

            var id = new EmployeeId("e" + idx++);
            _emps[id] = e;
            list.Add(new EmployeeInfo(id, name, "Staff", wage, sat, 0, 0f, null, bonusReady, false, bonusCost));
        }
        return list;
    }

    /// <summary>Resolve an EmployeeId decided by the engine back to the live IL2CPP instance.</summary>
    public bool TryResolveEmployee(EmployeeId id, out Il2CppEntities.EmployeeInstance emp)
        => _emps.TryGetValue(id, out emp);

    // --- Unused by the current slice (stubs) ---
    public IReadOnlyList<CandidateInfo> GetCandidates() => System.Array.Empty<CandidateInfo>();
    public IReadOnlyList<WarehouseInfo> GetWarehouses() => System.Array.Empty<WarehouseInfo>();
    public IReadOnlyList<ImportContractInfo> GetImportContracts() => System.Array.Empty<ImportContractInfo>();

    public bool TryGetBusiness(BusinessId id, out BusinessInfo business)
    {
        if (_regs.ContainsKey(id)) { business = new BusinessInfo(id, id.ToString(), "Business", true); return true; }
        business = default!;
        return false;
    }
}
