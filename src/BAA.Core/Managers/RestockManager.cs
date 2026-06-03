namespace BAA.Core.Managers;

/// <summary>
/// Plans purchases to bring each business's shelf stock up to its target.
/// (Stub — real planning logic is filled in test-first.)
/// </summary>
public sealed class RestockManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Restock;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.RestockEnabled)
            return ActionPlan.Empty;

        var actions = new List<PlannedAction>();
        foreach (var business in ctx.State.GetBusinesses())
        {
            if (!ctx.Config.For(business.Id).RestockEnabled)
                continue;

            var businessId = business.Id;
            foreach (var line in ctx.State.GetInventory(businessId))
            {
                var quantity = line.Target - line.Current;
                if (quantity <= 0)
                    continue;

                var item = line.Item;
                var cost = quantity * line.UnitCost;
                actions.Add(new PlannedAction(
                    ManagerPriority.Restock,
                    $"Restock {quantity}x {line.ItemName} in {business.Name}",
                    -cost,
                    businessId,
                    cmds => cmds.RestockItem(businessId, item, quantity)));
            }
        }

        return new ActionPlan(actions);
    }
}
