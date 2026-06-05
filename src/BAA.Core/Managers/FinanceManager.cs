namespace BAA.Core.Managers;

/// <summary>
/// Finance upkeep. In Big Ambitions rent, wages and loan installments already settle automatically
/// at midnight — the one finance chore the player must do by hand is paying <b>taxes</b> (miss them
/// and you get penalties). So this manager's job is simple and high-value: when taxes are due, pay
/// them. The engine's safety gate still enforces the cash reserve floor, so it never empties the bank.
/// Runs first (<see cref="ManagerPriority.Finance"/>) so income/settlement is accounted for before
/// other managers spend. Default-OFF until <see cref="AutomationConfig.FinanceEnabled"/>.
/// </summary>
public sealed class FinanceManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Finance;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.FinanceEnabled)
            return ActionPlan.Empty;

        var fin = ctx.State.GetFinances();
        if (fin.TaxDue <= 0m)
            return ActionPlan.Empty;

        var amount = fin.TaxDue;
        var action = new PlannedAction(
            ManagerPriority.Finance,
            $"Pay taxes ${amount:N0}",
            -amount,
            null,
            cmds => cmds.PayTaxes(amount));

        return new ActionPlan(new[] { action });
    }
}
