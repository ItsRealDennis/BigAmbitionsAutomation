namespace BAA.Core.Engine;

/// <summary>
/// A per-feature brain (restock, logistics, employees, finance, time-skip). It only PLANS — it
/// reads <see cref="TickContext"/> and returns the actions it would like to take. It decides
/// internally whether it is enabled (from config) and returns <see cref="ActionPlan.Empty"/> if not.
/// The engine gates and applies; the manager never touches the game directly.
/// </summary>
public interface IAutomationManager
{
    ManagerPriority Priority { get; }

    ActionPlan Plan(TickContext ctx);
}
