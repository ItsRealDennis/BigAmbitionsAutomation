namespace BAA.Core.Managers;

/// <summary>
/// Keeps imports flowing on autopilot: any ENABLED wholesale delivery contract that is not yet set to
/// repeat is switched to a recurring weekly order, so stock keeps arriving without manual re-ordering.
/// It will not commit to a recurring bill that would drop cash below the reserve floor. Default-OFF
/// until <see cref="AutomationConfig.LogisticsEnabled"/>.
/// <para>Creating brand-new contracts is left to the player (it needs an in-game purchasing-agent visit);
/// this only automates the contracts they already have.</para>
/// </summary>
public sealed class ContractManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Logistics;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.LogisticsEnabled)
            return ActionPlan.Empty;

        var cash = ctx.State.GetFinances().Cash;
        var floor = ctx.Config.CashReserveFloor;

        var actions = new List<PlannedAction>();
        foreach (var c in ctx.State.GetContracts())
        {
            if (!c.Enabled || c.Repeating)
                continue;

            // Affordability: don't commit to a recurring weekly bill we couldn't cover and stay above the
            // reserve floor. (CashDelta is 0 - flipping the flag spends nothing now; the cost lands on
            // delivery day - so this check, not the budget gate, is what protects the player here.)
            if (c.CostPerDelivery > 0m && cash - c.CostPerDelivery < floor)
                continue;

            var id = c.Id;
            actions.Add(new PlannedAction(
                ManagerPriority.Logistics,
                $"Make import contract recurring ({c.ItemCount} items, ~${c.CostPerDelivery:N0}/delivery)",
                0m,
                c.Business,
                cmds => cmds.SetContractRepeating(id, true)));
        }

        return new ActionPlan(actions);
    }
}
