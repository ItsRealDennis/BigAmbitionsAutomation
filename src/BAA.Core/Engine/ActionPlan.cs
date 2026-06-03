namespace BAA.Core.Engine;

/// <summary>An ordered set of actions a manager proposes for one tick. Pure data — produced by
/// <see cref="IAutomationManager.Plan"/>, inspected by the safety gate, then selectively applied.</summary>
public sealed record ActionPlan(IReadOnlyList<PlannedAction> Actions)
{
    public static ActionPlan Empty { get; } = new(Array.Empty<PlannedAction>());

    public bool IsEmpty => Actions.Count == 0;
}
