namespace BAA.Core.Managers;

/// <summary>
/// Keeps each product priced at the game's own neighborhood-optimal price (read from
/// <see cref="IGameState.GetPricing"/>), so price-satisfaction stays high without manual tuning.
/// <para>Pricing changes have NO immediate cash cost (<see cref="PlannedAction.CashDelta"/> = 0) — they
/// shape future revenue, not the wallet — so they always clear the budget gate. The LIVE-mode switch
/// still decides preview-vs-real at the adapter, exactly like every other feature.</para>
/// <para>A small dead-band stops the bot re-pricing for trivial day-to-day market drift, and
/// <see cref="AutomationConfig.PricingTargetPercent"/> lets the player undercut (e.g. 90%) or run a
/// premium (e.g. 110%) relative to optimal. Default 100% = exactly the game's optimal price.</para>
/// </summary>
public sealed class PricingManager : IAutomationManager
{
    // Only act once the gap to target is at least the larger of these (measured against the current
    // price). Without it, sub-cent market drift would re-propose a change every single day.
    private const decimal MinChangeAbsolute = 0.50m;
    private const decimal MinChangeFraction = 0.02m; // 2%

    public ManagerPriority Priority => ManagerPriority.Pricing;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.PricingEnabled)
            return ActionPlan.Empty;

        decimal pct = ctx.Config.PricingTargetPercent;
        if (pct <= 0m) pct = 100m;

        var actions = new List<PlannedAction>();
        foreach (var business in ctx.State.GetBusinesses())
        {
            if (!ctx.Config.For(business.Id).PricingEnabled)
                continue;

            var businessId = business.Id;
            foreach (var line in ctx.State.GetPricing(businessId))
            {
                // OptimalPrice == 0 means the game has no optimal for this item (e.g. a raw material
                // with no market price) — never touch those.
                if (line.OptimalPrice <= 0m)
                    continue;

                // Commercial round-half-up (AwayFromZero), not decimal's default banker's rounding.
                decimal target = decimal.Round(line.OptimalPrice * pct / 100m, 2, MidpointRounding.AwayFromZero);
                if (target <= 0m || target == line.CurrentPrice)
                    continue;

                // Dead-band only applies once a price already exists; an unpriced item (Current 0) is
                // always set so newly-stocked products don't sit at the default forever.
                if (line.CurrentPrice > 0m)
                {
                    decimal gap = Math.Abs(target - line.CurrentPrice);
                    decimal band = Math.Max(MinChangeAbsolute, line.CurrentPrice * MinChangeFraction);
                    if (gap < band)
                        continue;
                }

                var item = line.Item;
                string verb = line.CurrentPrice <= 0m
                    ? "Set"
                    : (target > line.CurrentPrice ? "Raise" : "Lower");

                actions.Add(new PlannedAction(
                    ManagerPriority.Pricing,
                    $"{verb} {line.ItemName} price ${line.CurrentPrice:0.00} -> ${target:0.00} in {business.Name}",
                    0m, // pricing changes future revenue, not current cash
                    businessId,
                    cmds => cmds.SetItemPrice(businessId, item, target)));
            }
        }

        return new ActionPlan(actions);
    }
}
