using System;
using BAA.Core.Config;
using BAA.Core.Engine;
using BAA.Core.Managers;
using BAA.Core.Safety;
using BAA.Core.Safety.Breakers;
using BAModAPI;
using BaBot.Diagnostics;
using BaBot.Game;
using BigAmbitions.Mods;

namespace BaBot;

/// <summary>
/// The mod's brain wiring. Registers BA BOT's controls through the game's OptionsService (toggles /
/// sliders / buttons that the game draws in Mods &gt; Options and auto-persists by id), and drives the
/// Core orchestration engine on GlobalEvents.onNewDay. Auto-wellbeing runs hourly. Everything is
/// safety-gated, default-OFF, and money writes only execute when LIVE MODE is on.
/// </summary>
public sealed class BaBotLogic
{
    private static readonly AutomationConfig Config = new();

    private ModContext _ctx;
    private OrchestrationEngine _engine;
    private GameStateAdapter _state;
    private GameClockAdapter _clock;
    private bool _subscribed;

    public void Initialize(ModContext ctx)
    {
        _ctx = ctx;
        try { Settings.Load(Config); } catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] settings load: " + ex.Message); }
        EnsureEngine();

        var options = new ModOptions()
            .AddHeader("babot_header")
            .AddToggle("babot_master", "babot_master", Config.MasterEnabled, v => { Config.MasterEnabled = v; Save(); })
            .AddToggle("babot_finance", "babot_finance", Config.FinanceEnabled, v => { Config.FinanceEnabled = v; Save(); })
            .AddToggle("babot_employees", "babot_employees", Config.EmployeesEnabled, v => { Config.EmployeesEnabled = v; Save(); })
            .AddToggle("babot_logistics", "babot_logistics", Config.LogisticsEnabled, v => { Config.LogisticsEnabled = v; Save(); })
            .AddToggle("babot_restock", "babot_restock", Config.RestockEnabled, v => { Config.RestockEnabled = v; Save(); })
            .AddToggle("babot_wellbeing", "babot_wellbeing", Config.WellbeingEnabled, v => { Config.WellbeingEnabled = v; Save(); })
            .AddToggle("babot_servicefee", "babot_servicefee", Config.ServiceFeeEnabled, v => { Config.ServiceFeeEnabled = v; Save(); })
            .AddToggle("babot_live", "babot_live", Config.LiveWrites, v => { Config.LiveWrites = v; Save(); })
            .AddSlider("babot_reserve", "babot_reserve", 0, 100000, (int)Config.CashReserveFloor, v => { Config.CashReserveFloor = v; Save(); }, "babot_dollars")
            .AddSlider("babot_target", "babot_target", 1, 100, Config.RestockTarget, v => { Config.RestockTarget = v; Save(); })
            .AddSlider("babot_fee", "babot_fee", 0, 5000, (int)Config.ServiceFeePerRun, v => { Config.ServiceFeePerRun = v; Save(); }, "babot_dollars")
            .AddButton("babot_run", () => RunAutomation("manual"))
            .AddButton("babot_cash", () => GameActions.AddMoney(1000f))
            .AddButton("babot_energy", () => GameActions.RefillEnergy())
            .AddSplitter();

        try { OptionsService.Register(ctx.ModId, options); } catch (Exception ex) { UnityEngine.Debug.LogError("[BA BOT] options register: " + ex.Message); }

        GlobalEvents.onNewDay += OnNewDay;
        GlobalEvents.onNewHour += OnNewHour;
        _subscribed = true;
        ctx.Logger.Info("BA BOT loaded - open Mods > Options to configure.");
    }

    public void Shutdown()
    {
        if (_subscribed)
        {
            GlobalEvents.onNewDay -= OnNewDay;
            GlobalEvents.onNewHour -= OnNewHour;
            _subscribed = false;
        }
        try { OptionsService.RemoveModOptions(_ctx.ModId); } catch { }
        try { Settings.SaveIfChanged(Config); } catch { }
    }

    private void Save() { try { Settings.SaveIfChanged(Config); } catch { } }

    private void EnsureEngine()
    {
        if (_engine != null) return;
        _state = new GameStateAdapter(Config);
        _clock = new GameClockAdapter();
        var commands = new GameCommandsAdapter(_state, Config);
        var breakers = new ISafetyBreaker[] { new LowFundsBreaker(), new UnpaidRentBreaker(), new EmptyInventoryBreaker() };
        var gate = new SafetyGate(breakers);
        var managers = new IAutomationManager[] { new FinanceManager(), new LogisticsManager(), new RestockManager(), new EmployeeManager() };
        _engine = new OrchestrationEngine(managers, gate, commands, new ModLog());
    }

    private void OnNewDay() => RunAutomation("NewDay");

    /// <summary>Auto-wellbeing: hourly top-up of energy (then happiness) when low.</summary>
    private void OnNewHour()
    {
        try
        {
            if (!(Config.MasterEnabled && Config.WellbeingEnabled)) return;
            var gi = SaveGameManager.Current;
            if (gi == null) return;
            if (gi.Energy < 30f) GameActions.RefillEnergy();
            else if (gi.Happiness < 30f) { GameActions.BoostHappiness(50); Activity.Add("Happiness boosted"); }
        }
        catch (Exception ex) { UnityEngine.Debug.LogWarning("[BA BOT] wellbeing failed: " + ex.Message); }
    }

    /// <summary>One safety-gated automation pass. Master switch required (engine enforces it too).</summary>
    private void RunAutomation(string trigger)
    {
        if (!Config.MasterEnabled)
        {
            if (trigger == "manual") Activity.Add("Enable AUTOMATION (MASTER) first");
            return;
        }
        try { EnsureEngine(); _engine.Tick(_state, _clock, Config); }
        catch (Exception ex) { UnityEngine.Debug.LogWarning($"[BA BOT] tick ({trigger}) failed: " + ex.Message); }
    }
}
