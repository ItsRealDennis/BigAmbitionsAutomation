using BAA.Core.Abstractions;

namespace BaBot.Game;

/// <summary>Routes the Core engine's log calls to the Unity console (prefixed) so they show in the
/// player log / mod loader output. Kept trivial; the engine only logs warnings/errors in practice.</summary>
internal sealed class ModLog : IModLogger
{
    public void Debug(string message) { }
    public void Info(string message) => UnityEngine.Debug.Log("[BA BOT] " + message);
    public void Warn(string message) => UnityEngine.Debug.LogWarning("[BA BOT] " + message);
    public void Error(string message) => UnityEngine.Debug.LogError("[BA BOT] " + message);
}
