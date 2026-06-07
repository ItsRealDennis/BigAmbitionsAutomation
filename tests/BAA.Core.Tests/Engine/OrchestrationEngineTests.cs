namespace BAA.Core.Tests.Engine;

public class OrchestrationEngineTests
{
    private static SafetyGate NoBreakers() => new(Array.Empty<ISafetyBreaker>());

    [Fact]
    public void Runs_managers_in_priority_order_and_rereads_state_between_them()
    {
        var b1 = new BusinessId("b1");
        var world = WorldBuilder.New()
            .Cash(0m)
            .Business("b1")
            .Item("b1", "milk", current: 0, target: 8, cap: 10, unitCost: 10m)
            .Build();
        world.PendingIncome[b1] = 100m;
        var commands = new FakeGameCommands(world);

        // Finance collects income (+100). Listed AFTER restock to prove the engine sorts by priority.
        var finance = new FakeManager(ManagerPriority.Finance, _ =>
            new ActionPlan(new[]
            {
                new PlannedAction(ManagerPriority.Finance, "collect", +100m, b1, c => c.CollectIncome(b1)),
            }));
        var restock = new RestockManager();

        var config = new AutomationConfig { MasterEnabled = true, RestockEnabled = true, CashReserveFloor = 10m };
        var engine = new OrchestrationEngine(
            new IAutomationManager[] { restock, finance }, // intentionally out of priority order
            NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), config);

        // Income (100) collected first -> restock 8*10=80 leaves cash 20 (>= floor 10) -> approved.
        // Had restock run first (cash 0), the gate would have dropped it and milk would stay 0.
        var milk = world.GetInventory(b1).Single();
        Assert.Equal(8, milk.Current);
        Assert.Equal(20m, world.Cash);
    }

    [Fact]
    public void Applies_nothing_when_a_halt_all_breaker_trips()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 0, 5, 10, 2m).Build();
        var commands = new FakeGameCommands(world);
        var gate = new SafetyGate(new[]
        {
            new FakeBreaker("rent", BreakerResult.Trip("rent overdue", BreakerSeverity.HaltAll)),
        });
        var engine = new OrchestrationEngine(
            new IAutomationManager[] { new RestockManager() }, gate, commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), new AutomationConfig { MasterEnabled = true, RestockEnabled = true });

        Assert.Empty(commands.Calls);
        Assert.Equal(100m, world.Cash);
    }

    [Fact]
    public void Does_nothing_when_world_not_ready()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 0, 5, 10, 2m).Build();
        world.WorldReady = false;
        var commands = new FakeGameCommands(world);
        var engine = new OrchestrationEngine(
            new IAutomationManager[] { new RestockManager() }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), new AutomationConfig { MasterEnabled = true, RestockEnabled = true });

        Assert.Empty(commands.Calls);
    }

    [Fact]
    public void Does_nothing_when_master_disabled()
    {
        var world = WorldBuilder.New().Cash(100m).Business("b1")
            .Item("b1", "milk", 0, 5, 10, 2m).Build();
        var commands = new FakeGameCommands(world);
        var engine = new OrchestrationEngine(
            new IAutomationManager[] { new RestockManager() }, NoBreakers(), commands, new NullLogger());

        // Feature is on, but the master switch is OFF -> the engine must do nothing.
        engine.Tick(world, new FakeGameClock(), new AutomationConfig { MasterEnabled = false, RestockEnabled = true });

        Assert.Empty(commands.Calls);
        Assert.Equal(100m, world.Cash);
    }

    [Fact]
    public void Charges_the_service_fee_once_when_enabled_and_the_tick_did_work()
    {
        var b1 = new BusinessId("b1");
        var world = WorldBuilder.New().Cash(1000m).Business("b1").Build();
        world.PendingIncome[b1] = 100m;
        var commands = new FakeGameCommands(world);

        var worker = new FakeManager(ManagerPriority.Finance, _ => new ActionPlan(new[]
        {
            new PlannedAction(ManagerPriority.Finance, "collect", +100m, b1, c => c.CollectIncome(b1)),
        }));
        var config = new AutomationConfig { MasterEnabled = true, ServiceFeeEnabled = true, ServiceFeePerRun = 250m };
        var engine = new OrchestrationEngine(new IAutomationManager[] { worker }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), config);

        Assert.Single(commands.Calls, c => c == "ChargeServiceFee(250)");
        // 1000 + 100 collected - 250 fee = 850.
        Assert.Equal(850m, world.Cash);
    }

    [Fact]
    public void Does_not_charge_the_service_fee_when_disabled()
    {
        var b1 = new BusinessId("b1");
        var world = WorldBuilder.New().Cash(1000m).Business("b1").Build();
        world.PendingIncome[b1] = 100m;
        var commands = new FakeGameCommands(world);

        var worker = new FakeManager(ManagerPriority.Finance, _ => new ActionPlan(new[]
        {
            new PlannedAction(ManagerPriority.Finance, "collect", +100m, b1, c => c.CollectIncome(b1)),
        }));
        var config = new AutomationConfig { MasterEnabled = true, ServiceFeeEnabled = false, ServiceFeePerRun = 250m };
        var engine = new OrchestrationEngine(new IAutomationManager[] { worker }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), config);

        Assert.DoesNotContain(commands.Calls, c => c.StartsWith("ChargeServiceFee"));
        Assert.Equal(1100m, world.Cash);
    }

    [Fact]
    public void Does_not_charge_the_service_fee_when_the_tick_did_no_work()
    {
        var world = WorldBuilder.New().Cash(1000m).Business("b1").Build();
        var commands = new FakeGameCommands(world);

        // Manager proposes nothing -> no work done -> no fee, even though the fee is enabled.
        var idle = new FakeManager(ManagerPriority.Finance, _ => ActionPlan.Empty);
        var config = new AutomationConfig { MasterEnabled = true, ServiceFeeEnabled = true, ServiceFeePerRun = 250m };
        var engine = new OrchestrationEngine(new IAutomationManager[] { idle }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), config);

        Assert.Empty(commands.Calls);
        Assert.Equal(1000m, world.Cash);
    }

    [Fact]
    public void Service_fee_respects_the_reserve_floor()
    {
        var b1 = new BusinessId("b1");
        var world = WorldBuilder.New().Cash(1000m).Business("b1").Build();
        world.PendingIncome[b1] = 100m;
        var commands = new FakeGameCommands(world);

        var worker = new FakeManager(ManagerPriority.Finance, _ => new ActionPlan(new[]
        {
            new PlannedAction(ManagerPriority.Finance, "collect", +100m, b1, c => c.CollectIncome(b1)),
        }));
        // Floor 1000 with cash 1100 after collection: a 250 fee would drop to 850 (< floor) -> rejected.
        var config = new AutomationConfig
        {
            MasterEnabled = true, ServiceFeeEnabled = true, ServiceFeePerRun = 250m, CashReserveFloor = 1000m,
        };
        var engine = new OrchestrationEngine(new IAutomationManager[] { worker }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), config);

        Assert.DoesNotContain(commands.Calls, c => c.StartsWith("ChargeServiceFee"));
        Assert.Equal(1100m, world.Cash);
    }

    [Fact]
    public void A_throwing_command_does_not_abort_the_rest_of_the_tick()
    {
        var b1 = new BusinessId("b1");
        var world = WorldBuilder.New().Cash(1000m).Business("b1").Build();
        var commands = new FakeGameCommands(world);

        // First action throws; second must still run.
        var thrower = new FakeManager(ManagerPriority.Finance, _ => new ActionPlan(new[]
        {
            new PlannedAction(ManagerPriority.Finance, "boom", 0m, b1,
                _ => throw new InvalidOperationException("kaboom")),
            new PlannedAction(ManagerPriority.Finance, "ok", 0m, b1, c => c.CollectIncome(b1)),
        }));
        var engine = new OrchestrationEngine(
            new IAutomationManager[] { thrower }, NoBreakers(), commands, new NullLogger());

        engine.Tick(world, new FakeGameClock(), new AutomationConfig { MasterEnabled = true });

        Assert.Contains("CollectIncome(b1)", commands.Calls);
    }
}
