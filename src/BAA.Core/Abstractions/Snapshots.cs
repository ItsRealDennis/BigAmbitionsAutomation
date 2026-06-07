namespace BAA.Core.Abstractions;

// Immutable, flat snapshots that cross the adapter boundary into the engine.
// No game/IL2CPP type ever appears here. Money is decimal for correct currency math;
// the adapter converts the game's float at the boundary.

/// <summary>In-game calendar plus a monotonic minute counter for boundary detection.</summary>
public sealed record GameTimeInfo(
    int Year,
    int Month,
    int Day,
    int Hour,
    int Minute,
    DayOfWeek DayOfWeek,
    long AbsoluteMinutes);

public sealed record FinanceSnapshot(
    decimal Cash,
    decimal RentDue,
    decimal BillsDue,
    decimal LoanDue,
    bool AnyRentOverdue,
    decimal TaxDue = 0m,
    decimal NetWorth = 0m,
    decimal LoanRemaining = 0m);

public sealed record BusinessInfo(
    BusinessId Id,
    string Name,
    string Type,
    bool IsActive);

/// <summary>
/// One stockable line in a business: how much is on the shelf now, the desired target,
/// the physical shelf capacity, and what one unit costs to buy. The heart of auto-restock.
/// </summary>
public sealed record InventoryLine(
    ItemId Item,
    string ItemName,
    int Current,
    int Target,
    int ShelfCapacity,
    decimal UnitCost);

public sealed record EmployeeInfo(
    EmployeeId Id,
    string Name,
    string Role,
    decimal Wage,
    float Satisfaction,
    int Age,
    float Skill,
    BusinessId? Assignment,
    bool BonusReady = false,
    bool TrainingComplete = false,
    decimal BonusCost = 0m);

public sealed record CandidateInfo(
    CandidateId Id,
    string Name,
    string Role,
    decimal ExpectedWage,
    float Skill);

public sealed record WarehouseInfo(
    WarehouseId Id,
    string Name,
    int BayCount);

public sealed record ImportContractInfo(
    ImportContractId Id,
    ItemId Item,
    int Quantity,
    DeliveryFrequency Frequency,
    string Supplier);

/// <summary>
/// One sellable product line for auto-pricing: what it currently sells for (the player's stored retail
/// price, 0 if never set) and the game's own neighborhood-optimal price. <see cref="OptimalPrice"/> of 0
/// means the game has no optimal for it (e.g. a raw material with no market price) — leave it alone.
/// </summary>
public sealed record PricingLine(
    ItemId Item,
    string ItemName,
    decimal CurrentPrice,
    decimal OptimalPrice);

/// <summary>Per-business staffing state for auto-scheduling: how many employees are assigned to the
/// business and how many of those are not on any work shift (so the schedule needs filling).</summary>
public sealed record StaffingInfo(int AssignedEmployees, int UnscheduledEmployees)
{
    public bool NeedsSchedule => UnscheduledEmployees > 0;
}

/// <summary>A wholesale delivery contract belonging to a player business. <see cref="CostPerDelivery"/>
/// is the projected per-delivery charge, used to decide whether committing to a recurring bill is
/// affordable (drives auto-logistics).</summary>
public sealed record ContractInfo(
    ContractId Id,
    BusinessId Business,
    bool Enabled,
    bool Repeating,
    decimal CostPerDelivery,
    int ItemCount);
