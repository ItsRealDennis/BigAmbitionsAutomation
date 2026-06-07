using System.Threading.Tasks;
using BAModAPI;

[assembly: RegisterModClass(typeof(BaBot.ModMain))]

namespace BaBot;

/// <summary>
/// Official Big Ambitions mod entry (EA 0.11+). Loaded once at initialization. All the work lives in
/// <see cref="BaBotLogic"/>: it registers the control panel through the game's own OptionsService (so
/// the game renders + persists it — no custom UI, which a runtime-loaded mod assembly can't host) and
/// runs the automation engine on the in-game day/hour events.
/// </summary>
[ModEntryOnInitializationLoad]
public sealed class ModMain : ModBigAmbitionsBase
{
    private readonly BaBotLogic _logic = new();

    public override Task OnLoadAsync(ModContext context)
    {
        _logic.Initialize(context);
        return Task.CompletedTask;
    }

    public override Task OnUnloadAsync()
    {
        _logic.Shutdown();
        return Task.CompletedTask;
    }
}
