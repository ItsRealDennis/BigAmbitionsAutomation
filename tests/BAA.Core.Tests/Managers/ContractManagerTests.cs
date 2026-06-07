namespace BAA.Core.Tests.Managers;

public class ContractManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_makes_active_nonrepeating_contract_recurring()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Contract("c1", "b1", enabled: true, repeating: false, costPerDelivery: 500m)
            .Build();

        var plan = new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(new BusinessId("b1"), action.Business);
        Assert.Equal(0m, action.CashDelta); // flipping the flag spends nothing now
    }

    [Fact]
    public void Applying_plan_sets_repeating_then_converges()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Contract("c1", "b1", true, false, 500m).Build();
        var commands = new FakeGameCommands(world);
        var config = new AutomationConfig { LogisticsEnabled = true };

        foreach (var a in new ContractManager().Plan(Ctx(world, config)).Actions)
            a.Apply(commands);

        Assert.Contains("SetContractRepeating(c1,True)", commands.Calls);
        Assert.True(new ContractManager().Plan(Ctx(world, config)).IsEmpty); // already recurring now
    }

    [Fact]
    public void Plan_skips_already_repeating_contract()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Contract("c1", "b1", true, true, 500m).Build();

        Assert.True(new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_skips_disabled_contract()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Contract("c1", "b1", enabled: false, repeating: false, costPerDelivery: 500m).Build();

        Assert.True(new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_skips_when_recurring_bill_would_breach_reserve_floor()
    {
        // cash 400 - cost 500 = -100, below the (default 0) reserve floor -> don't commit
        var world = WorldBuilder.New().Cash(400m).Business("b1")
            .Contract("c1", "b1", true, false, 500m).Build();

        Assert.True(new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_stops_once_cumulative_recurring_cost_would_breach_floor()
    {
        // cash 900, floor 0; two contracts at 600 each. The first fits (900-600=300); the second must NOT
        // (300-600=-300 < 0). Only one action - proves per-tick running affordability, not just per-contract.
        var world = WorldBuilder.New().Cash(900m).Business("b1")
            .Contract("c1", "b1", true, false, 600m)
            .Contract("c2", "b1", true, false, 600m)
            .Build();

        var plan = new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = true }));

        Assert.Single(plan.Actions);
    }

    [Fact]
    public void Plan_is_empty_when_feature_off()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Contract("c1", "b1", true, false, 500m).Build();

        Assert.True(new ContractManager().Plan(Ctx(world, new AutomationConfig { LogisticsEnabled = false })).IsEmpty);
    }
}
