namespace BAA.Core.Safety.Breakers;

/// <summary>Warns when an active business has a fully depleted item — a signal that a store is losing
/// sales and needs attention/restock. (Stub.)</summary>
public sealed class EmptyInventoryBreaker : ISafetyBreaker
{
    public string Name => "EmptyInventory";

    public BreakerResult Check(BreakerContext ctx)
    {
        foreach (var business in ctx.State.GetBusinesses())
        {
            if (!business.IsActive)
                continue;

            foreach (var line in ctx.State.GetInventory(business.Id))
            {
                if (line.Current <= 0)
                    return BreakerResult.Warn($"{business.Name}: '{line.ItemName}' is out of stock");
            }
        }

        return BreakerResult.Pass;
    }
}
