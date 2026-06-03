namespace BAA.Core.Tests.Safety;

public class BreakerTests
{
    private static BreakerContext Ctx(FakeGameState world, AutomationConfig config) => new(world, config);

    [Fact]
    public void LowFunds_trips_halt_all_when_cash_below_floor()
    {
        var world = WorldBuilder.New().Cash(5m).Build();
        var result = new LowFundsBreaker().Check(Ctx(world, new AutomationConfig { CashReserveFloor = 20m }));
        Assert.Equal(BreakerVerdict.Trip, result.Verdict);
        Assert.Equal(BreakerSeverity.HaltAll, result.Severity);
    }

    [Fact]
    public void LowFunds_passes_when_cash_at_or_above_floor()
    {
        var world = WorldBuilder.New().Cash(20m).Build();
        var result = new LowFundsBreaker().Check(Ctx(world, new AutomationConfig { CashReserveFloor = 20m }));
        Assert.Equal(BreakerVerdict.Pass, result.Verdict);
    }

    [Fact]
    public void UnpaidRent_trips_halt_all_when_rent_overdue()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        world.AnyRentOverdue = true;
        var result = new UnpaidRentBreaker().Check(Ctx(world, new AutomationConfig()));
        Assert.Equal(BreakerVerdict.Trip, result.Verdict);
        Assert.Equal(BreakerSeverity.HaltAll, result.Severity);
    }

    [Fact]
    public void UnpaidRent_passes_when_not_overdue()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        var result = new UnpaidRentBreaker().Check(Ctx(world, new AutomationConfig()));
        Assert.Equal(BreakerVerdict.Pass, result.Verdict);
    }

    [Fact]
    public void EmptyInventory_warns_when_active_business_has_depleted_item()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1").Item("b1", "milk", 0, 10, 12, 1m).Build();
        var result = new EmptyInventoryBreaker().Check(Ctx(world, new AutomationConfig()));
        Assert.Equal(BreakerVerdict.Warn, result.Verdict);
    }

    [Fact]
    public void EmptyInventory_passes_when_all_items_stocked()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1").Item("b1", "milk", 5, 10, 12, 1m).Build();
        var result = new EmptyInventoryBreaker().Check(Ctx(world, new AutomationConfig()));
        Assert.Equal(BreakerVerdict.Pass, result.Verdict);
    }
}
