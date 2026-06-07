namespace BAA.Core.Tests.Managers;

public class SchedulingManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_fills_when_a_business_has_unscheduled_staff()
    {
        var world = WorldBuilder.New().Business("b1").Staffing("b1", assigned: 3, unscheduled: 1).Build();

        var plan = new SchedulingManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(new BusinessId("b1"), action.Business);
        Assert.Equal(0m, action.CashDelta); // scheduling never spends cash
    }

    [Fact]
    public void Applying_plan_fills_schedule_then_converges()
    {
        var world = WorldBuilder.New().Business("b1").Staffing("b1", 3, 1).Build();
        var commands = new FakeGameCommands(world);
        var config = new AutomationConfig { EmployeesEnabled = true };

        foreach (var a in new SchedulingManager().Plan(Ctx(world, config)).Actions)
            a.Apply(commands);

        Assert.Contains("AutoFillSchedule(b1)", commands.Calls);
        // Once filled, a second pass proposes nothing (no daily churn).
        Assert.True(new SchedulingManager().Plan(Ctx(world, config)).IsEmpty);
    }

    [Fact]
    public void Plan_is_empty_when_fully_scheduled()
    {
        var world = WorldBuilder.New().Business("b1").Staffing("b1", 3, 0).Build();

        Assert.True(new SchedulingManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = true })).IsEmpty);
    }

    [Fact]
    public void Plan_is_empty_when_employees_automation_off()
    {
        var world = WorldBuilder.New().Business("b1").Staffing("b1", 3, 2).Build();

        Assert.True(new SchedulingManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = false })).IsEmpty);
    }
}
