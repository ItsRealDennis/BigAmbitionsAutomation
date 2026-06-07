namespace BAA.Core.Managers;

/// <summary>
/// Keeps each business's shift schedule filled. When a business has employees assigned to it that are
/// not on any work shift, it asks the game's own auto-scheduler to fill them in (no cash cost). The
/// "needs filling" check means it acts ONLY when something is actually unscheduled, so it never churns
/// an already-staffed schedule day after day. Gated by <see cref="AutomationConfig.EmployeesEnabled"/>.
/// </summary>
public sealed class SchedulingManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Scheduling;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.EmployeesEnabled)
            return ActionPlan.Empty;

        var actions = new List<PlannedAction>();
        foreach (var business in ctx.State.GetBusinesses())
        {
            var staffing = ctx.State.GetStaffing(business.Id);
            if (staffing == null || !staffing.NeedsSchedule)
                continue;

            var businessId = business.Id;
            actions.Add(new PlannedAction(
                ManagerPriority.Scheduling,
                $"Auto-fill schedule for {business.Name} ({staffing.UnscheduledEmployees} unscheduled)",
                0m, // scheduling never spends cash
                businessId,
                cmds => cmds.AutoFillSchedule(businessId)));
        }

        return new ActionPlan(actions);
    }
}
