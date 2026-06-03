namespace BAA.Core.Tests.Fakes;

/// <summary>A breaker that returns a pre-set result, for driving gate tests.</summary>
public sealed class FakeBreaker : ISafetyBreaker
{
    private readonly BreakerResult _result;

    public FakeBreaker(string name, BreakerResult result)
    {
        Name = name;
        _result = result;
    }

    public string Name { get; }

    public BreakerResult Check(BreakerContext ctx) => _result;
}
