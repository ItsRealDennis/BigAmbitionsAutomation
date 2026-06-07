using System.Threading.Tasks;
using BAModAPI;
using UnityEngine;

[assembly: RegisterModClass(typeof(BaBot.ModMain))]

namespace BaBot;

/// <summary>
/// Official Big Ambitions mod entry (EA 0.11+). Registered via the assembly attribute above and
/// activated on city load. Spawns the persistent <see cref="BaBotBehaviour"/> host that runs the
/// panel + automation, and tears it down on unload.
/// </summary>
[ModEntryOnCityLoad]
public sealed class ModMain : ModBigAmbitionsBase
{
    private GameObject _go;

    public override Task OnLoadAsync(ModContext context)
    {
        try { context.Logger?.Info("BA BOT loading"); } catch { }
        if (_go == null)
        {
            _go = new GameObject("BA BOT");
            Object.DontDestroyOnLoad(_go);
            _go.AddComponent<BaBotBehaviour>();
        }
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync()
    {
        if (_go != null) { Object.Destroy(_go); _go = null; }
        return Task.CompletedTask;
    }
}
