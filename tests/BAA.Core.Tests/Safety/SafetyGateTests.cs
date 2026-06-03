namespace BAA.Core.Tests.Safety;

public class SafetyGateTests
{
    private static PlannedAction Spend(decimal amount, string desc)
        => new(ManagerPriority.Restock, desc, -amount, null, _ => CommandResult.Applied(-amount));

    private static SafetyGate Gate(params ISafetyBreaker[] breakers) => new(breakers);

    [Fact]
    public void Approves_all_actions_when_cash_is_ample()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        var plan = new ActionPlan(new[] { Spend(10m, "A"), Spend(20m, "B") });

        var decision = Gate().Evaluate(plan, world, new AutomationConfig { CashReserveFloor = 0m });

        Assert.False(decision.Halted);
        Assert.Equal(2, decision.Approved.Count);
    }

    [Fact]
    public void Drops_action_that_would_breach_reserve_floor_but_keeps_affordable_ones()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        // floor 20: A(-50)->50 ok, B(-40)->10 (<20) DROP, C(-20)->30 ok
        var plan = new ActionPlan(new[] { Spend(50m, "A"), Spend(40m, "B"), Spend(20m, "C") });

        var decision = Gate().Evaluate(plan, world, new AutomationConfig { CashReserveFloor = 20m });

        Assert.Equal(new[] { "A", "C" }, decision.Approved.Select(x => x.Description).ToArray());
        Assert.Single(decision.Rejections);
    }

    [Fact]
    public void Halts_entire_plan_when_a_breaker_trips_halt_all()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        var plan = new ActionPlan(new[] { Spend(10m, "A") });
        var gate = Gate(new FakeBreaker("test", BreakerResult.Trip("boom", BreakerSeverity.HaltAll)));

        var decision = gate.Evaluate(plan, world, new AutomationConfig());

        Assert.True(decision.Halted);
        Assert.Empty(decision.Approved);
    }

    [Fact]
    public void CheckTripwires_returns_only_halt_all_trips()
    {
        var world = WorldBuilder.New().Cash(100m).Build();
        var gate = Gate(
            new FakeBreaker("ok", BreakerResult.Pass),
            new FakeBreaker("warn", BreakerResult.Warn("meh")),
            new FakeBreaker("bad", BreakerResult.Trip("rent overdue", BreakerSeverity.HaltAll)));

        var trips = gate.CheckTripwires(world, new AutomationConfig());

        var trip = Assert.Single(trips);
        Assert.Equal("rent overdue", trip.Reason);
    }
}
