namespace BAA.Core.Config;

/// <summary>
/// Root automation config. <b>Default-OFF</b> is the safety stance: every feature master-switch
/// defaults false, so a freshly installed mod does nothing until the user opts in.
/// </summary>
public sealed class AutomationConfig
{
    /// <summary>Master switch for ALL automation. Off by default.</summary>
    public bool MasterEnabled { get; set; }

    /// <summary>Master switch for auto-restock. Off by default.</summary>
    public bool RestockEnabled { get; set; }

    /// <summary>Per-feature master switches (all off by default).</summary>
    public bool LogisticsEnabled { get; set; }
    public bool EmployeesEnabled { get; set; }
    public bool FinanceEnabled { get; set; }
    public bool TimeSkipEnabled { get; set; }

    /// <summary>Keep the player's energy topped up automatically (instant QoL, no business needed).</summary>
    public bool WellbeingEnabled { get; set; }

    /// <summary>Automation will not spend cash below this floor.</summary>
    public decimal CashReserveFloor { get; set; }

    /// <summary>Behavior applied to any business without an explicit override.</summary>
    public PerBusinessConfig DefaultBusiness { get; set; } = new();

    /// <summary>Per-business overrides keyed by business id.</summary>
    public Dictionary<BusinessId, PerBusinessConfig> PerBusiness { get; } = new();

    /// <summary>Resolve the effective config for a business (override if present, else default).</summary>
    public PerBusinessConfig For(BusinessId id)
        => PerBusiness.TryGetValue(id, out var cfg) ? cfg : DefaultBusiness;
}

/// <summary>Per-business automation toggles.</summary>
public sealed class PerBusinessConfig
{
    /// <summary>Whether this business participates when restock is enabled globally. Defaults true,
    /// so flipping the master switch affects all businesses unless one is explicitly opted out.</summary>
    public bool RestockEnabled { get; set; } = true;
}
