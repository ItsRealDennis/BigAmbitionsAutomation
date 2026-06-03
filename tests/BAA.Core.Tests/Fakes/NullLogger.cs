namespace BAA.Core.Tests.Fakes;

public sealed class NullLogger : IModLogger
{
    public void Debug(string message) { }
    public void Info(string message) { }
    public void Warn(string message) { }
    public void Error(string message) { }
}
