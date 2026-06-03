namespace BAA.Core.Safety.Breakers;

/// <summary>Halts everything when rent is overdue (eviction risk) so the player can intervene. (Stub.)</summary>
public sealed class UnpaidRentBreaker : ISafetyBreaker
{
    public string Name => "UnpaidRent";

    public BreakerResult Check(BreakerContext ctx)
        => ctx.State.GetFinances().AnyRentOverdue
            ? BreakerResult.Trip("rent overdue - eviction risk", BreakerSeverity.HaltAll)
            : BreakerResult.Pass;
}
