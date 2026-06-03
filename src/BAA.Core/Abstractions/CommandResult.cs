namespace BAA.Core.Abstractions;

/// <summary>
/// The result of an <see cref="IGameCommands"/> call. The adapter NEVER throws into the
/// engine — any failure becomes <see cref="CommandOutcome.Failed"/> with a reason. This keeps
/// a broken game-API call from taking down the tick (or the game).
/// </summary>
public sealed record CommandResult(CommandOutcome Outcome, string Reason, decimal CashDelta = 0m)
{
    public bool IsApplied => Outcome == CommandOutcome.Applied;

    public static CommandResult Applied(decimal cashDelta = 0m) => new(CommandOutcome.Applied, "", cashDelta);

    public static CommandResult Skipped(string reason) => new(CommandOutcome.Skipped, reason);

    public static CommandResult Failed(string reason) => new(CommandOutcome.Failed, reason);
}
