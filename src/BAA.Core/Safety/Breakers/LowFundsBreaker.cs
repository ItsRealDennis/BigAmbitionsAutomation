namespace BAA.Core.Safety.Breakers;

/// <summary>Halts everything when cash is already below the reserve floor — the primary tripwire that
/// aborts a time-skip before it can spend the player into the ground. (Stub.)</summary>
public sealed class LowFundsBreaker : ISafetyBreaker
{
    public string Name => "LowFunds";

    public BreakerResult Check(BreakerContext ctx)
    {
        var cash = ctx.State.GetFinances().Cash;
        var floor = ctx.Config.CashReserveFloor;
        return cash < floor
            ? BreakerResult.Trip($"cash {cash} is below reserve floor {floor}", BreakerSeverity.HaltAll)
            : BreakerResult.Pass;
    }
}
