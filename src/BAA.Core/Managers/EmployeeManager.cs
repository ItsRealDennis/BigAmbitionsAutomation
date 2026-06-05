namespace BAA.Core.Managers;

/// <summary>
/// Staff upkeep. Keeps employees productive without micromanagement:
/// <list type="bullet">
/// <item>finishes a training course the moment it completes, so skills (and the businesses that need
/// them) come online immediately;</item>
/// <item>pays a morale bonus to an under-satisfied employee when the game allows one — cheap insurance
/// against resignations, and far less than the cost of re-hiring and re-training.</item>
/// </list>
/// Bonus spend is reserve-floor gated by the engine. Default-OFF until
/// <see cref="AutomationConfig.EmployeesEnabled"/>.
/// </summary>
public sealed class EmployeeManager : IAutomationManager
{
    public ManagerPriority Priority => ManagerPriority.Employee;

    public ActionPlan Plan(TickContext ctx)
    {
        if (!ctx.Config.EmployeesEnabled)
            return ActionPlan.Empty;

        var floor = (float)ctx.Config.EmployeeSatisfactionFloor;
        var actions = new List<PlannedAction>();

        foreach (var emp in ctx.State.GetEmployees())
        {
            var id = emp.Id;

            if (emp.TrainingComplete)
                actions.Add(new PlannedAction(
                    ManagerPriority.Employee,
                    $"Finish training for {emp.Name}",
                    0m,
                    emp.Assignment,
                    cmds => cmds.FinishTraining(id)));

            if (emp.BonusReady && emp.Satisfaction < floor)
                actions.Add(new PlannedAction(
                    ManagerPriority.Employee,
                    $"Morale bonus for {emp.Name} ({emp.Satisfaction:0.00})",
                    -emp.BonusCost,
                    emp.Assignment,
                    cmds => cmds.GiveBonus(id)));
        }

        return new ActionPlan(actions);
    }
}
