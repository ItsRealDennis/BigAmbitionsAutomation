namespace BAA.Core.Managers;

/// <summary>
/// Supply-chain upkeep. Restock buys shelves back up to target reactively; logistics is the proactive
/// layer: when a product is running short and no recurring import contract already covers it, it sets
/// up a repeating weekly import so the goods keep flowing without manual reordering. One contract per
/// item per tick (deduped against existing contracts and within the tick). Projected import cost is
/// reported so the engine's reserve floor applies. Default-OFF until
/// <see cref="AutomationConfig.LogisticsEnabled"/>.
/// </summary>
public sealed class LogisticsManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Logistics;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.LogisticsEnabled)
            return ActionPlan.Empty;

        // Items already served by a recurring import contract — don't propose a duplicate.
        var covered = new HashSet<string>();
        foreach (var contract in ctx.State.GetImportContracts())
            covered.Add(contract.Item.Value);

        var actions = new List<PlannedAction>();
        foreach (var business in ctx.State.GetBusinesses())
        {
            if (!business.IsActive)
                continue;

            foreach (var line in ctx.State.GetInventory(business.Id))
            {
                var shortfall = line.Target - line.Current;
                if (shortfall <= 0 || covered.Contains(line.Item.Value))
                    continue;

                covered.Add(line.Item.Value); // also avoids duplicate proposals across businesses this tick

                var spec = new ImportContractSpec(line.Item, shortfall, DeliveryFrequency.Weekly, business.Id);
                var cost = shortfall * line.UnitCost;
                actions.Add(new PlannedAction(
                    ManagerPriority.Logistics,
                    $"Set up weekly import: {shortfall}x {line.ItemName} for {business.Name}",
                    -cost,
                    business.Id,
                    cmds => cmds.ConfigureImportContract(spec)));
            }
        }

        return new ActionPlan(actions);
    }
}
