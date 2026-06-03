namespace BAA.Core.Abstractions;

/// <summary>
/// Logging abstraction so Core carries no MelonLoader dependency. The mod wires this to MelonLogger;
/// tests use a no-op or capturing implementation.
/// </summary>
public interface IModLogger
{
    void Debug(string message);
    void Info(string message);
    void Warn(string message);
    void Error(string message);
}
