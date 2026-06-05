namespace BAA.Core.Tests.Managers;

public class EmployeeManagerTests
{
    private static TickContext Ctx(FakeGameState world, AutomationConfig config)
        => new(world, new FakeGameClock(), config);

    [Fact]
    public void Plan_bonuses_only_unhappy_eligible_staff()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Employee("b1", "e_sad", name: "Sad", satisfaction: 0.2f, bonusReady: true, bonusCost: 300m)
            .Employee("b1", "e_happy", name: "Happy", satisfaction: 0.9f, bonusReady: true, bonusCost: 300m)
            .Employee("b1", "e_cooldown", name: "Cooldown", satisfaction: 0.1f, bonusReady: false, bonusCost: 300m)
            .Build();

        var plan = new EmployeeManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(-300m, action.CashDelta);
        Assert.Contains("Sad", action.Description);
    }

    [Fact]
    public void Plan_finishes_completed_training()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Employee("b1", "e1", satisfaction: 1f, trainingComplete: true)
            .Build();

        var plan = new EmployeeManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = true }));

        var action = Assert.Single(plan.Actions);
        Assert.Equal(0m, action.CashDelta);
        Assert.Contains("training", action.Description);
    }

    [Fact]
    public void Plan_is_empty_when_feature_off()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Employee("b1", "e1", satisfaction: 0.1f, bonusReady: true, bonusCost: 300m, trainingComplete: true)
            .Build();

        var plan = new EmployeeManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = false }));

        Assert.True(plan.IsEmpty);
    }

    [Fact]
    public void Applying_bonus_debits_cost_and_records_call()
    {
        var world = WorldBuilder.New().Cash(10000m).Business("b1")
            .Employee("b1", "e_sad", satisfaction: 0.2f, bonusReady: true, bonusCost: 300m)
            .Build();
        var commands = new FakeGameCommands(world);
        var plan = new EmployeeManager().Plan(Ctx(world, new AutomationConfig { EmployeesEnabled = true }));

        foreach (var action in plan.Actions)
            action.Apply(commands);

        Assert.Contains("GiveBonus(e_sad)", commands.Calls);
        Assert.Equal(9700m, world.Cash);
    }
}
