namespace BAA.Core.Tests.Fakes;

/// <summary>
/// A mutable in-memory game world implementing <see cref="IGameState"/>. Paired with
/// <see cref="FakeGameCommands"/> (which mutates this same instance) it behaves like a tiny game,
/// so multi-tick orchestration tests are realistic without the real game.
/// </summary>
public sealed class FakeGameState : IGameState
{
    public bool WorldReady { get; set; } = true;
    public decimal Cash { get; set; }
    public decimal RentDue { get; set; }
    public decimal BillsDue { get; set; }
    public decimal LoanDue { get; set; }
    public decimal TaxDue { get; set; }
    public decimal NetWorth { get; set; }
    public bool AnyRentOverdue { get; set; }
    public GameTimeInfo Time { get; set; } = new(2023, 3, 10, 8, 0, DayOfWeek.Friday, 0);

    public List<BusinessInfo> Businesses { get; } = new();
    public Dictionary<BusinessId, List<InventoryLine>> Inventory { get; } = new();
    public Dictionary<BusinessId, List<EmployeeInfo>> Employees { get; } = new();
    public List<CandidateInfo> Candidates { get; } = new();
    public List<WarehouseInfo> Warehouses { get; } = new();
    public List<ImportContractInfo> ImportContracts { get; } = new();

    /// <summary>Income waiting to be collected per business (used by finance tests).</summary>
    public Dictionary<BusinessId, decimal> PendingIncome { get; } = new();

    public bool IsWorldReady() => WorldReady;

    public GameTimeInfo GetTime() => Time;

    public FinanceSnapshot GetFinances() => new(Cash, RentDue, BillsDue, LoanDue, AnyRentOverdue, TaxDue, NetWorth);

    /// <summary>Find an employee across all businesses by id (used by FakeGameCommands to cost a bonus).</summary>
    public EmployeeInfo? FindEmployee(EmployeeId id)
    {
        foreach (var list in Employees.Values)
            foreach (var e in list)
                if (e.Id == id)
                    return e;
        return null;
    }

    public IReadOnlyList<BusinessInfo> GetBusinesses() => Businesses;

    public IReadOnlyList<InventoryLine> GetInventory(BusinessId business)
        => Inventory.TryGetValue(business, out var lines) ? lines : new List<InventoryLine>();

    public IReadOnlyList<EmployeeInfo> GetEmployees(BusinessId? scope = null)
    {
        if (scope is null)
            return Employees.Values.SelectMany(x => x).ToList();
        return Employees.TryGetValue(scope.Value, out var lines) ? lines : new List<EmployeeInfo>();
    }

    public IReadOnlyList<CandidateInfo> GetCandidates() => Candidates;

    public IReadOnlyList<WarehouseInfo> GetWarehouses() => Warehouses;

    public IReadOnlyList<ImportContractInfo> GetImportContracts() => ImportContracts;

    public bool TryGetBusiness(BusinessId id, out BusinessInfo business)
    {
        foreach (var b in Businesses)
        {
            if (b.Id == id)
            {
                business = b;
                return true;
            }
        }
        business = default!;
        return false;
    }
}
