namespace BAA.Core.Tests.Fakes;

/// <summary>Fluent setup for a <see cref="FakeGameState"/> so test arrangements read clearly:
/// <code>WorldBuilder.New().Cash(100m).Business("b1").Item("b1","milk",2,10,12,1.5m).Build()</code></summary>
public sealed class WorldBuilder
{
    private readonly FakeGameState _world = new();

    public static WorldBuilder New() => new();

    public WorldBuilder Cash(decimal cash)
    {
        _world.Cash = cash;
        return this;
    }

    public WorldBuilder Business(string id, string? name = null, string type = "Shop", bool active = true)
    {
        var bid = new BusinessId(id);
        _world.Businesses.Add(new BusinessInfo(bid, name ?? id, type, active));
        if (!_world.Inventory.ContainsKey(bid))
            _world.Inventory[bid] = new List<InventoryLine>();
        return this;
    }

    public WorldBuilder Item(string businessId, string item, int current, int target, int cap, decimal unitCost)
    {
        var bid = new BusinessId(businessId);
        _world.Inventory[bid].Add(new InventoryLine(new ItemId(item), item, current, target, cap, unitCost));
        return this;
    }

    /// <summary>Add a sellable product with a current retail price and the game's optimal price (for pricing tests).</summary>
    public WorldBuilder Price(string businessId, string item, decimal current, decimal optimal)
    {
        var bid = new BusinessId(businessId);
        if (!_world.Pricing.ContainsKey(bid))
            _world.Pricing[bid] = new List<PricingLine>();
        _world.Pricing[bid].Add(new PricingLine(new ItemId(item), item, current, optimal));
        return this;
    }

    public WorldBuilder Tax(decimal due)
    {
        _world.TaxDue = due;
        return this;
    }

    public WorldBuilder Employee(string businessId, string id, string name = "Staff",
        float satisfaction = 1f, bool bonusReady = false, decimal bonusCost = 0m, bool trainingComplete = false)
    {
        var bid = new BusinessId(businessId);
        if (!_world.Employees.ContainsKey(bid))
            _world.Employees[bid] = new List<EmployeeInfo>();
        _world.Employees[bid].Add(new EmployeeInfo(
            new EmployeeId(id), name, "Cashier", 12m, satisfaction, 30, 0.5f, bid,
            bonusReady, trainingComplete, bonusCost));
        return this;
    }

    public WorldBuilder ImportContract(string item, int quantity = 10, string supplier = "Wholesaler")
    {
        _world.ImportContracts.Add(new ImportContractInfo(
            new ImportContractId("ic" + _world.ImportContracts.Count),
            new ItemId(item), quantity, DeliveryFrequency.Weekly, supplier));
        return this;
    }

    public FakeGameState Build() => _world;
}
