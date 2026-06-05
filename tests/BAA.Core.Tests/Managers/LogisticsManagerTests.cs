namespace BAA.Core.Tests.Managers;

public class LogisticsManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_sets_up_import_for_short_item_without_contract()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Item("b1", "milk", current: 2, target: 10, cap: 12, unitCost: 1.5m)
            .Build();

        var plan = new LogisticsManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(-12m, action.CashDelta); // shortfall 8 * 1.5
        Assert.Contains("import", action.Description, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Plan_skips_item_already_covered_by_contract()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Item("b1", "milk", 2, 10, 12, 1.5m)
            .ImportContract("milk")
            .Build();

        var plan = new LogisticsManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Plan_proposes_each_item_once_across_businesses()
    {
        var world = WorldBuilder.New().Cash(10000m)
            .Business("b1").Item("b1", "milk", 2, 10, 12, 1.5m)
            .Business("b2").Item("b2", "milk", 0, 10, 12, 1.5m)
            .Build();

        var plan = new LogisticsManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true }));

        Assert.Single(plan.Actions); // milk deduped to one contract proposal
    }

    [Fact]
    public void Plan_is_empty_when_feature_off()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Item("b1", "milk", 2, 10, 12, 1.5m).Build();

        var plan = new LogisticsManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = false }));

        Assert.True(plan.IsEmpty);
    }
}
