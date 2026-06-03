namespace BAA.Core.Tests.Managers;

public class RestockManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_buys_shortfall_up_to_target()
    {
        var world = WorldBuilder.New()
            .Cash(100m)
            .Business("b1")
            .Item("b1", "milk", current: 2, target: 10, cap: 12, unitCost: 1.5m)
            .Build();
        var config = new AutomationConfig { RestockEnabled = true };

        var plan = new RestockManager().Plan(Ctx(world, config));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(new BusinessId("b1"), action.Business);
        Assert.Equal(-12.0m, action.CashDelta); // shortfall 8 * unitCost 1.5
    }

    [Fact]
    public void Plan_is_empty_when_master_switch_off()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 2, 10, 12, 1.5m).Build();

        var plan = new RestockManager().Plan(Ctx(world, new AutomationConfig { RestockEnabled = false }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Plan_skips_business_opted_out()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 2, 10, 12, 1.5m).Build();
        var config = new AutomationConfig { RestockEnabled = true };
        config.PerBusiness[new BusinessId("b1")] = new PerBusinessConfig { RestockEnabled = false };

        var plan = new RestockManager().Plan(Ctx(world, config));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Plan_skips_item_already_at_target()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", current: 10, target: 10, cap: 12, unitCost: 1.5m).Build();

        var plan = new RestockManager().Plan(Ctx(world, new AutomationConfig { RestockEnabled = true }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Applying_plan_restocks_to_target_and_debits_exact_cash()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 2, 10, 12, 1.5m).Build();
        var commands = new FakeGameCommands(world);
        var plan = new RestockManager().Plan(Ctx(world, new AutomationConfig { RestockEnabled = true }));

        foreach (var action in plan.Actions)
            action.Apply(commands);

        Assert.Contains("RestockItem(b1,milk,8)", commands.Calls);
        Assert.Equal(88.0m, world.Cash); // 100 - (8 * 1.5)
        var milk = world.GetInventory(new BusinessId("b1")).Single(l => l.Item == new ItemId("milk"));
        Assert.Equal(10, milk.Current);
    }
}
