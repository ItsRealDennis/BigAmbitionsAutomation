namespace BAA.Core.Abstractions;

/// <summary>
/// Guarded, imperative mutations of the game. Every method returns a <see cref="CommandResult"/>
/// and never throws into the engine. Quantities and costs are decided by the brain BEFORE calling,
/// so the safety gate can veto an action using numbers it already holds — the adapter just executes.
/// </summary>
public interface IGameCommands
{
    // --- Restock / upkeep ---
    CommandResult RestockItem(BusinessId business, ItemId item, int quantity);
    CommandResult SetStockTarget(BusinessId business, ItemId item, int target);
    CommandResult SetItemPrice(BusinessId business, ItemId item, decimal price);

    // --- Logistics ---
    CommandResult ConfigureImportContract(ImportContractSpec spec);

    /// <summary>Make an existing wholesale delivery contract recur weekly (or stop) - the auto-logistics action.</summary>
    CommandResult SetContractRepeating(ContractId contract, bool repeating);
    CommandResult SetWarehouseTarget(WarehouseId warehouse, ItemId item, int target);
    CommandResult AssignLogistics(BusinessId business, EmployeeId manager, IReadOnlyList<EmployeeId> drivers);

    // --- Employees ---
    CommandResult HireCandidate(CandidateId candidate, BusinessId business);
    CommandResult SetWage(EmployeeId employee, decimal wage);
    CommandResult SetSchedule(EmployeeId employee, ScheduleSpec schedule);

    /// <summary>Ask the game's own auto-scheduler to fill this business's shifts from its assigned staff.</summary>
    CommandResult AutoFillSchedule(BusinessId business);
    CommandResult SetHealthPlan(EmployeeId employee, bool enabled);
    CommandResult GiveBonus(EmployeeId employee);
    CommandResult FinishTraining(EmployeeId employee);

    // --- Finance ---
    CommandResult PayRent(BusinessId business);
    CommandResult CollectIncome(BusinessId business);
    CommandResult PayLoanInstallment(LoanId loan);
    CommandResult PayTaxes(decimal amount);

    /// <summary>Charge the opt-in automation service fee (a flat cash cost per run that did work).</summary>
    CommandResult ChargeServiceFee(decimal amount);

    // --- Time ---
    CommandResult SetTimeSpeed(float multiplier);
    CommandResult RequestSkip(SkipSpec spec);
}
