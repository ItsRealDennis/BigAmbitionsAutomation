namespace BAA.Core.Tests.Fakes;

/// <summary>A manager whose plan is supplied by a delegate, for exercising the engine's coordination.</summary>
public sealed class FakeManager : IAutomationManager
{
    private readonly Func<TickContext, ActionPlan> _plan;

    public FakeManager(ManagerPriority priority, Func<TickContext, ActionPlan> plan)
    {
        Priority = priority;
        _plan = plan;
    }

    public ManagerPriority Priority { get; }

    public ActionPlan Plan(TickContext ctx) => _plan(ctx);
}
