namespace BAA.Core.Tests.Managers;

public class FinanceManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_pays_taxes_when_due()
    {
        var world = WorldBuilder.New().Cash(5000m).Tax(800m).Build();

        var plan = new FinanceManager().Plan(Ctx(world, new AutomationConfig { FinanceEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(-800m, action.CashDelta);
    }

    [Fact]
    public void Plan_is_empty_when_no_tax_due()
    {
        var world = WorldBuilder.New().Cash(5000m).Tax(0m).Build();

        var plan = new FinanceManager().Plan(Ctx(world, new AutomationConfig { FinanceEnabled = true }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Plan_is_empty_when_feature_off()
    {
        var world = WorldBuilder.New().Cash(5000m).Tax(800m).Build();

        var plan = new FinanceManager().Plan(Ctx(world, new AutomationConfig { FinanceEnabled = false }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Applying_plan_pays_exact_tax_and_clears_due()
    {
        var world = WorldBuilder.New().Cash(5000m).Tax(800m).Build();
        var commands = new FakeGameCommands(world);
        var plan = new FinanceManager().Plan(Ctx(world, new AutomationConfig { FinanceEnabled = true }));

        foreach (var action in plan.Actions)
            action.Apply(commands);

        Assert.Contains("PayTaxes(800)", commands.Calls);
        Assert.Equal(4200m, world.Cash);
        Assert.Equal(0m, world.TaxDue);
    }
}
