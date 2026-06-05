using BAA.Core.Abstractions;

namespace BAA.Mod.Game;

/// <summary>IModLogger over MelonLogger.</summary>
internal sealed class ModLogger : IModLogger
{
    public void Debug(string message) => ModEntry.Log?.Msg(message);
    public void Info(string message) => ModEntry.Log?.Msg(message);
    public void Warn(string message) => ModEntry.Log?.Warning(message);
    public void Error(string message) => ModEntry.Log?.Error(message);
}
