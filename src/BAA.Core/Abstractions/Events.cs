namespace BAA.Core.Abstractions;

// Payloads for the push notifications in IGameEvents. Raised by the adapter's Harmony
// hooks; consumed by the engine.

public sealed record DailyTickArgs(GameTimeInfo Time);

public sealed record HourTickArgs(GameTimeInfo Time);

public sealed record ShelfDepletionArgs(BusinessId Business, ItemId Item);

public sealed record DeliveryArgs(BusinessId? Business, WarehouseId? Warehouse, ItemId Item, int Quantity);

public sealed record EmployeeEventArgs(EmployeeId Employee, string Kind);

public sealed record SaveArgs(string SaveId);
