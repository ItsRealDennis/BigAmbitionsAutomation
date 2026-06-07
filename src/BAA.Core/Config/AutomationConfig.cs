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

    /// <summary>Master switch for auto-pricing: keep each product at the game's neighborhood-optimal
    /// price. Off by default. Reads/writes the same retail price the in-game pricing UI uses.</summary>
    public bool PricingEnabled { get; set; }

    /// <summary>Keep the player's energy topped up automatically (instant QoL, no business needed).</summary>
    public bool WellbeingEnabled { get; set; }

    /// <summary>Target shelf level each product is restocked up to.</summary>
    public int RestockTarget { get; set; } = 20;

    /// <summary>Auto-pricing target as a percentage of the game's optimal price. 100 = exactly optimal;
    /// below 100 undercuts rivals, above 100 runs a premium. Clamped to a sane floor of 100 if &lt;= 0.</summary>
    public decimal PricingTargetPercent { get; set; } = 100m;

    /// <summary>
    /// Gate for money-spending / state-changing game writes (auto-pay taxes, staff bonuses, restock).
    /// <b>Off by default</b>: every such action only previews (logs what it WOULD do) until the player
    /// turns this on while watching. Cosmetic helpers (energy, happiness, time-skip) ignore this flag.
    /// </summary>
    public bool LiveWrites { get; set; }

    /// <summary>Employees below this morale (0–1) get a bonus when the game allows one. Conservative
    /// default so automation only spends on clearly unhappy staff.</summary>
    public decimal EmployeeSatisfactionFloor { get; set; } = 0.4m;

    /// <summary>UI language code: "en" or "da".</summary>
    public string Language { get; set; } = "en";

    /// <summary>Automation will not spend cash below this floor.</summary>
    public decimal CashReserveFloor { get; set; }

    /// <summary>
    /// Opt-in difficulty balance: when on, each automation run that actually does work charges a
    /// <see cref="ServiceFeePerRun"/> cash fee, so leaning on the bot is no longer free. <b>Off by
    /// default</b> — the mod stays free until the player chooses the extra challenge.
    /// </summary>
    public bool ServiceFeeEnabled { get; set; }

    /// <summary>Cash charged per automation run when <see cref="ServiceFeeEnabled"/> is on. Routed
    /// through the safety gate, so it never spends below <see cref="CashReserveFloor"/>.</summary>
    public decimal ServiceFeePerRun { get; set; } = 250m;

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

    /// <summary>Whether this business participates when auto-pricing is enabled globally. Defaults true.</summary>
    public bool PricingEnabled { get; set; } = true;
}
