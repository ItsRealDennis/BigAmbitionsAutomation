namespace BAA.Core.Abstractions;

// Strongly-typed identifiers. Wrapping the game's raw id prevents accidentally
// passing, say, an ItemId where a BusinessId is expected. The underlying value is
// a string so it can hold whatever the game uses (name, int-as-string, or GUID).

public readonly record struct BusinessId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct ItemId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct EmployeeId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct CandidateId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct WarehouseId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct LoanId(string Value) { public override string ToString() => Value ?? ""; }
public readonly record struct ImportContractId(string Value) { public override string ToString() => Value ?? ""; }
