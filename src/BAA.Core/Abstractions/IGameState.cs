namespace BAA.Core.Abstractions;

/// <summary>
/// Read-only snapshot of the game world. Implementations return immutable DTOs only — the
/// engine never holds a live game object, which Il2CppInterop can invalidate across frames.
/// </summary>
public interface IGameState
{
    /// <summary>True only when a save is actually loaded and playable.</summary>
    bool IsWorldReady();

    GameTimeInfo GetTime();
    FinanceSnapshot GetFinances();

    IReadOnlyList<BusinessInfo> GetBusinesses();
    IReadOnlyList<InventoryLine> GetInventory(BusinessId business);

    /// <summary>Per-product current vs. neighborhood-optimal price for a business (drives auto-pricing).</summary>
    IReadOnlyList<PricingLine> GetPricing(BusinessId business);

    /// <summary>Staffing state for a business (drives auto-scheduling). Never null.</summary>
    StaffingInfo GetStaffing(BusinessId business);
    IReadOnlyList<EmployeeInfo> GetEmployees(BusinessId? scope = null);
    IReadOnlyList<CandidateInfo> GetCandidates();
    IReadOnlyList<WarehouseInfo> GetWarehouses();
    IReadOnlyList<ImportContractInfo> GetImportContracts();

    bool TryGetBusiness(BusinessId id, out BusinessInfo business);
}
