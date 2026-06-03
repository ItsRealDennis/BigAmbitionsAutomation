namespace BAA.Core.Abstractions;

// Inputs the engine hands to commands. Kept general so the adapter can map them onto
// whatever shape the real game API turns out to need.

public sealed record ShiftSpec(DayOfWeek Day, int StartHour, int EndHour);

public sealed record ScheduleSpec(IReadOnlyList<ShiftSpec> Shifts);

/// <summary>Where to send goods (a store or a warehouse), what item, how many, how often.</summary>
public sealed record ImportContractSpec(
    ItemId Item,
    int Quantity,
    DeliveryFrequency Frequency,
    BusinessId? TargetBusiness = null,
    WarehouseId? TargetWarehouse = null,
    string? Supplier = null);

/// <summary>A cooperative time-skip request, expressed in whole in-game hours.</summary>
public sealed record SkipSpec(int Hours);
