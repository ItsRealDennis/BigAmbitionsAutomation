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
    bool AnyRentOverdue);

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
    BusinessId? Assignment);

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
