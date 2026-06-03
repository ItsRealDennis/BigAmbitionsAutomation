namespace BAA.Core.Safety;

/// <summary>The outcome of one breaker's check.</summary>
public sealed record BreakerResult(BreakerVerdict Verdict, string Reason, BreakerSeverity Severity)
{
    public static readonly BreakerResult Pass = new(BreakerVerdict.Pass, "", BreakerSeverity.SkipAction);

    public static BreakerResult Trip(string reason, BreakerSeverity severity) =>
        new(BreakerVerdict.Trip, reason, severity);

    public static BreakerResult Warn(string reason) =>
        new(BreakerVerdict.Warn, reason, BreakerSeverity.SkipAction);
}
