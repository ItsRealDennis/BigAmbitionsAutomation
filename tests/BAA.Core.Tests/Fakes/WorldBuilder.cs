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

    public FakeGameState Build() => _world;
}
