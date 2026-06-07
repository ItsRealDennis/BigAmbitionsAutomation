namespace BAA.Core.Tests.Fakes;

/// <summary>
/// Executes commands against a <see cref="FakeGameState"/> like a tiny game, and records every call
/// so tests can assert both the effect (stock/cash changed) and the exact arguments used.
/// </summary>
public sealed class FakeGameCommands : IGameCommands
{
    private readonly FakeGameState _world;

    public FakeGameCommands(FakeGameState world) => _world = world;

    /// <summary>Every command call, in order, formatted for assertions (integers only — culture-safe).</summary>
    public List<string> Calls { get; } = new();

    private CommandResult Record(string call)
    {
        Calls.Add(call);
        return CommandResult.Applied();
    }

    // --- Restock / upkeep (RestockItem fully simulated) ---

    public CommandResult RestockItem(BusinessId business, ItemId item, int quantity)
    {
        Calls.Add($"RestockItem({business},{item},{quantity})");
        if (!_world.Inventory.TryGetValue(business, out var lines))
            return CommandResult.Failed("no such business");

        var idx = lines.FindIndex(l => l.Item == item);
        if (idx < 0)
            return CommandResult.Failed("no such item");

        var line = lines[idx];
        var newCurrent = Math.Min(line.Current + quantity, line.ShelfCapacity);
        var addedUnits = newCurrent - line.Current;
        var cost = addedUnits * line.UnitCost;

        _world.Cash -= cost;
        lines[idx] = line with { Current = newCurrent };
        return CommandResult.Applied(-cost);
    }

    public CommandResult SetStockTarget(BusinessId business, ItemId item, int target)
        => Record($"SetStockTarget({business},{item},{target})");

    public CommandResult SetItemPrice(BusinessId business, ItemId item, decimal price)
        => Record($"SetItemPrice({business},{item})");

    // --- Logistics ---

    public CommandResult ConfigureImportContract(ImportContractSpec spec)
        => Record($"ConfigureImportContract({spec.Item},{spec.Quantity})");

    public CommandResult SetWarehouseTarget(WarehouseId warehouse, ItemId item, int target)
        => Record($"SetWarehouseTarget({warehouse},{item},{target})");

    public CommandResult AssignLogistics(BusinessId business, EmployeeId manager, IReadOnlyList<EmployeeId> drivers)
        => Record($"AssignLogistics({business},{manager},{drivers.Count})");

    // --- Employees ---

    public CommandResult HireCandidate(CandidateId candidate, BusinessId business)
        => Record($"HireCandidate({candidate},{business})");

    public CommandResult SetWage(EmployeeId employee, decimal wage)
        => Record($"SetWage({employee})");

    public CommandResult SetSchedule(EmployeeId employee, ScheduleSpec schedule)
        => Record($"SetSchedule({employee})");

    public CommandResult SetHealthPlan(EmployeeId employee, bool enabled)
        => Record($"SetHealthPlan({employee},{enabled})");

    public CommandResult GiveBonus(EmployeeId employee)
    {
        Calls.Add($"GiveBonus({employee})");
        var cost = _world.FindEmployee(employee)?.BonusCost ?? 0m;
        _world.Cash -= cost;
        return CommandResult.Applied(-cost);
    }

    public CommandResult FinishTraining(EmployeeId employee)
        => Record($"FinishTraining({employee})");

    // --- Finance (cash effects simulated) ---

    public CommandResult PayRent(BusinessId business)
    {
        Calls.Add($"PayRent({business})");
        var due = _world.RentDue;
        _world.Cash -= due;
        _world.RentDue = 0m;
        _world.AnyRentOverdue = false;
        return CommandResult.Applied(-due);
    }

    public CommandResult CollectIncome(BusinessId business)
    {
        Calls.Add($"CollectIncome({business})");
        var amount = _world.PendingIncome.TryGetValue(business, out var v) ? v : 0m;
        _world.Cash += amount;
        _world.PendingIncome[business] = 0m;
        return CommandResult.Applied(amount);
    }

    public CommandResult PayLoanInstallment(LoanId loan)
    {
        Calls.Add($"PayLoanInstallment({loan})");
        var due = _world.LoanDue;
        _world.Cash -= due;
        _world.LoanDue = 0m;
        return CommandResult.Applied(-due);
    }

    public CommandResult PayTaxes(decimal amount)
    {
        Calls.Add($"PayTaxes({(int)amount})");
        _world.Cash -= amount;
        _world.TaxDue = 0m;
        return CommandResult.Applied(-amount);
    }

    public CommandResult ChargeServiceFee(decimal amount)
    {
        Calls.Add($"ChargeServiceFee({(int)amount})");
        _world.Cash -= amount;
        return CommandResult.Applied(-amount);
    }

    // --- Time ---

    public CommandResult SetTimeSpeed(float multiplier)
        => Record($"SetTimeSpeed({(int)multiplier})");

    public CommandResult RequestSkip(SkipSpec spec)
        => Record($"RequestSkip({spec.Hours})");
}
