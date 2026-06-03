namespace BAA.Core.Engine;

/// <summary>
/// One intended action a manager wants to take, decided during the PLAN phase but not yet executed.
/// <para><see cref="CashDelta"/> is the projected change to cash: negative spends, positive gains.
/// The safety gate inspects this to enforce the reserve floor and shared budget BEFORE anything runs.</para>
/// <para><see cref="Apply"/> performs the actual game mutation when (and only when) the gate approves.</para>
/// </summary>
public sealed record PlannedAction(
    ManagerPriority Source,
    string Description,
    decimal CashDelta,
    BusinessId? Business,
    Func<IGameCommands, CommandResult> Apply);
