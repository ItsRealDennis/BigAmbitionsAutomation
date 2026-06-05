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
        decimal cash = gi != null ? (decimal)gi.Money : 0m;
        return new FinanceSnapshot(cash, 0m, 0m, 0m, false);
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

    // --- Unused by the restock slice (stubs) ---
    public IReadOnlyList<EmployeeInfo> GetEmployees(BusinessId? scope = null) => System.Array.Empty<EmployeeInfo>();
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
