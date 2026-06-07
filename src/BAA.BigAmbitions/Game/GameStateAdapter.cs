using System.Collections.Generic;
using BAA.Core.Abstractions;
using BAA.Core.Config;

namespace BaBot.Game;

/// <summary>
/// IGameState over the live EA 0.11 (Mono) game. Enumerates the player's businesses + per-product
/// stock, finances (cash/net worth/loans/unpaid taxes) and the staff roster, into flat DTOs the
/// engine plans against. Within-tick caches (businessId -&gt; registration, employeeId -&gt; instance)
/// let the commands adapter act on the same objects the plan referenced. Item identity IS the item's
/// string name, so ItemId.Value == itemName (no item cache needed).
/// </summary>
internal sealed class GameStateAdapter : IGameState
{
    private readonly AutomationConfig _config;
    private readonly Dictionary<BusinessId, BuildingRegistration> _regs = new();
    private readonly Dictionary<EmployeeId, Entities.EmployeeInstance> _emps = new();

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

        decimal taxDue = 0m;
        try { if (gi.currentUnpaidTaxes != null) taxDue = (decimal)gi.currentUnpaidTaxes.totalToPay; } catch { }
        if (taxDue < 0m) taxDue = 0m;

        return new FinanceSnapshot(cash, 0m, 0m, loanDaily, false, taxDue, netWorth, loanRemaining);
    }

    public IReadOnlyList<BusinessInfo> GetBusinesses()
    {
        _regs.Clear();
        var list = new List<BusinessInfo>();
        var gi = SaveGameManager.Current;
        var regs = gi != null ? gi.BuildingRegistrations : null;
        if (regs == null)
            return list;

        int idx = 0;
        for (int i = 0; i < regs.Count; i++)
        {
            var reg = regs[i];
            bool mine = false;
            try { mine = reg.RentedByPlayer || reg.BuildingOwnedByPlayer; } catch { }
            if (!mine) continue;

            List<string> sale = null;
            try { sale = reg.GetListOfItemsForSale(); } catch { }
            if (sale == null || sale.Count == 0) continue;

            var id = new BusinessId("b" + idx++);
            string type = "Business";
            try { type = reg.businessTypeName; } catch { }
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

        List<string> sale = null;
        try { sale = reg.GetListOfItemsForSale(); } catch { }
        if (sale == null)
            return lines;

        int target = _config.RestockTarget;
        for (int j = 0; j < sale.Count; j++)
        {
            var name = sale[j];
            if (string.IsNullOrEmpty(name)) continue;
            int current = 0;
            try { current = Helpers.BuildingHelper.CountTotalResourcesInStock(reg, name, true, true); } catch { }
            decimal unitCost = 0m;
            try { unitCost = (decimal)ItemHelper.GetPrice(name, reg); } catch { }
            lines.Add(new InventoryLine(new ItemId(name), name, current, target, target, unitCost));
        }
        return lines;
    }

    public IReadOnlyList<PricingLine> GetPricing(BusinessId business)
    {
        var lines = new List<PricingLine>();
        if (!_regs.TryGetValue(business, out var reg) || reg == null)
            return lines;

        string hood = null;
        try { hood = reg.Neighborhood; } catch { }

        List<string> sale = null;
        try { sale = reg.GetListOfItemsForSale(); } catch { }
        if (sale == null)
            return lines;

        for (int j = 0; j < sale.Count; j++)
        {
            var name = sale[j];
            if (string.IsNullOrEmpty(name)) continue;

            // Current = the player's STORED retail price (0 if never set), read the same way we write it
            // in SetItemPrice, so the engine's compare/apply converges exactly. (ItemHelper.GetPrice would
            // fall back to default-market for unset items, which we deliberately want to treat as "unset".)
            decimal current = 0m;
            try
            {
                var rps = reg.retailPrices;
                if (rps != null)
                    for (int k = 0; k < rps.Count; k++)
                        if (rps[k] != null && rps[k].itemName == name) { current = (decimal)rps[k].price; break; }
            }
            catch { }

            decimal optimal = 0m;
            try { if (!string.IsNullOrEmpty(hood)) optimal = (decimal)ItemHelper.CalculateOptimalPriceByNeighborhood(name, hood); } catch { }
            if (optimal < 0m) optimal = 0m;

            lines.Add(new PricingLine(new ItemId(name), name, current, optimal));
        }
        return lines;
    }

    /// <summary>Resolve a (business, item) decided by the engine back to the live registration + name.
    /// Item identity is the string name, so it round-trips directly through ItemId.Value.</summary>
    public bool TryResolve(BusinessId b, ItemId item, out BuildingRegistration reg, out string itemName)
    {
        itemName = item.Value;
        return _regs.TryGetValue(b, out reg);
    }

    public IReadOnlyList<EmployeeInfo> GetEmployees(BusinessId? scope = null)
    {
        _emps.Clear();
        var list = new List<EmployeeInfo>();
        List<Entities.EmployeeInstance> all = null;
        try { all = Helpers.EmployeeHelper.GetEmployeeInstances(); }
        catch { return list; }
        if (all == null)
            return list;

        int idx = 0;
        for (int i = 0; i < all.Count; i++)
        {
            var e = all[i];
            if (e == null) continue;
            try { if (e.IsCandidate) continue; } catch { }

            float sat = 1f;
            try { sat = e.satisfaction; } catch { }
            if (sat > 1.5f) sat /= 100f; // normalise a 0-100 morale scale to 0-1

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

    public bool TryResolveEmployee(EmployeeId id, out Entities.EmployeeInstance emp)
        => _emps.TryGetValue(id, out emp);

    // --- Unused by the current slice ---
    public IReadOnlyList<CandidateInfo> GetCandidates() => System.Array.Empty<CandidateInfo>();
    public IReadOnlyList<WarehouseInfo> GetWarehouses() => System.Array.Empty<WarehouseInfo>();
    public IReadOnlyList<ImportContractInfo> GetImportContracts() => System.Array.Empty<ImportContractInfo>();

    public bool TryGetBusiness(BusinessId id, out BusinessInfo business)
    {
        if (_regs.ContainsKey(id)) { business = new BusinessInfo(id, id.ToString(), "Business", true); return true; }
        business = default;
        return false;
    }
}
