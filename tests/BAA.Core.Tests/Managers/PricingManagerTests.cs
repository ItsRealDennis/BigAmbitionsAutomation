namespace BAA.Core.Tests.Managers;

public class PricingManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_targets_optimal_with_no_cash_effect()
    {
        var world = WorldBuilder.New()
            .Business("b1")
            .Price("b1", "milk", current: 19.00m, optimal: 21.00m)
            .Build();

        var plan = new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(new BusinessId("b1"), action.Business);
        Assert.Equal(0m, action.CashDelta); // pricing shapes future revenue, never spends cash
    }

    [Fact]
    public void Applying_plan_writes_optimal_price_then_converges()
    {
        var world = WorldBuilder.New()
            .Business("b1")
            .Price("b1", "milk", 19.00m, 21.00m)
            .Build();
        var commands = new FakeGameCommands(world);
        var config = new AutomationConfig { PricingEnabled = true };

        foreach (var a in new PricingManager().Plan(Ctx(world, config)).Actions)
            a.Apply(commands);

        Assert.Contains("SetItemPrice(b1,milk,21.00)", commands.Calls);
        Assert.Equal(21.00m, world.GetPricing(new BusinessId("b1")).Single().CurrentPrice);

        // Now that the price equals optimal, a second pass proposes nothing (no daily churn).
        Assert.True(new PricingManager().Plan(Ctx(world, config)).IsEmpty);
    }

    [Fact]
    public void Plan_is_empty_when_master_switch_off()
    {
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", 10m, 20m).Build();

        Assert.True(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = false })).IsEmpty);
    }

    [Fact]
    public void Plan_skips_business_opted_out()
    {
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", 10m, 20m).Build();
        var config = new AutomationConfig { PricingEnabled = true };
        config.PerBusiness[new BusinessId("b1")] = new PerBusinessConfig { PricingEnabled = false };

        Assert.True(new PricingManager().Plan(Ctx(world, config)).IsEmpty);
    }

    [Fact]
    public void Plan_skips_item_with_no_optimal_price()
    {
        // Raw materials / non-retail goods have a 0 market price -> never priced.
        var world = WorldBuilder.New().Business("b1").Price("b1", "barley", current: 0m, optimal: 0m).Build();

        Assert.True(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_ignores_trivial_drift_within_deadband()
    {
        // gap 0.10 < band max($0.50, 2% of $21.00 = $0.42) => skip
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 21.00m, optimal: 21.10m).Build();

        Assert.True(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_sets_initial_price_for_unpriced_item_ignoring_deadband()
    {
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 0m, optimal: 5.00m).Build();

        Assert.Single(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true })).Actions);
    }

    [Fact]
    public void Plan_respects_target_percent()
    {
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 20.00m, optimal: 20.00m).Build();
        var commands = new FakeGameCommands(world);
        var config = new AutomationConfig { PricingEnabled = true, PricingTargetPercent = 90m };

        foreach (var a in new PricingManager().Plan(Ctx(world, config)).Actions)
            a.Apply(commands);

        Assert.Contains("SetItemPrice(b1,milk,18.00)", commands.Calls); // 20.00 * 90% = 18.00
    }

    [Fact]
    public void Plan_acts_when_gap_exactly_equals_deadband()
    {
        // gap 0.50 == band max($0.50, 2% of $21.00 = $0.42) -> must ACT (dead-band is a strict '<').
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 21.00m, optimal: 21.50m).Build();

        Assert.Single(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true })).Actions);
    }

    [Fact]
    public void Plan_skips_when_gap_just_below_deadband()
    {
        // gap 0.49 < band 0.50 -> skip (boundary sibling of the test above).
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 21.00m, optimal: 21.49m).Build();

        Assert.True(new PricingManager().Plan(Ctx(world, new AutomationConfig { PricingEnabled = true })).IsEmpty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public void Plan_clamps_nonpositive_target_percent_to_optimal(int pct)
    {
        // pct <= 0 must behave like 100% (target == optimal), NOT collapse target to 0 and skip everything.
        var world = WorldBuilder.New().Business("b1").Price("b1", "milk", current: 19.00m, optimal: 21.00m).Build();
        var commands = new FakeGameCommands(world);
        var config = new AutomationConfig { PricingEnabled = true, PricingTargetPercent = pct };

        foreach (var a in new PricingManager().Plan(Ctx(world, config)).Actions)
            a.Apply(commands);

        Assert.Contains("SetItemPrice(b1,milk,21.00)", commands.Calls);
    }
}
