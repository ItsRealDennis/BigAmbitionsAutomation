using BAA.Core.Config;
using MelonLoader;

namespace BAA.Mod;

/// <summary>
/// Persists the automation config across sessions via MelonPreferences (UserData/MelonPreferences.cfg).
/// Loaded once on startup; saved (debounced) whenever the user changes something in the panel.
/// </summary>
internal static class ModPreferences
{
    private static MelonPreferences_Category _cat;
    private static MelonPreferences_Entry<bool> _master, _restock, _logistics, _employees, _finance, _timeskip, _wellbeing, _serviceFee;
    private static MelonPreferences_Entry<float> _reserve, _serviceFeePerRun;
    private static MelonPreferences_Entry<int> _restockTarget;
    private static MelonPreferences_Entry<string> _lang;
    private static string _lastSig;

    public static void Load(AutomationConfig cfg)
    {
        _cat = MelonPreferences.CreateCategory("BA_BOT", "BA BOT");
        _master = _cat.CreateEntry("MasterEnabled", false);
        _restock = _cat.CreateEntry("RestockEnabled", false);
        _logistics = _cat.CreateEntry("LogisticsEnabled", false);
        _employees = _cat.CreateEntry("EmployeesEnabled", false);
        _finance = _cat.CreateEntry("FinanceEnabled", false);
        _timeskip = _cat.CreateEntry("TimeSkipEnabled", false);
        _wellbeing = _cat.CreateEntry("WellbeingEnabled", false);
        _reserve = _cat.CreateEntry("CashReserveFloor", 0f);
        _restockTarget = _cat.CreateEntry("RestockTarget", 20);
        _serviceFee = _cat.CreateEntry("ServiceFeeEnabled", false);
        _serviceFeePerRun = _cat.CreateEntry("ServiceFeePerRun", 250f);
        _lang = _cat.CreateEntry("Language", "en");

        cfg.MasterEnabled = _master.Value;
        cfg.RestockEnabled = _restock.Value;
        cfg.LogisticsEnabled = _logistics.Value;
        cfg.EmployeesEnabled = _employees.Value;
        cfg.FinanceEnabled = _finance.Value;
        cfg.TimeSkipEnabled = _timeskip.Value;
        cfg.WellbeingEnabled = _wellbeing.Value;
        cfg.CashReserveFloor = (decimal)_reserve.Value;
        cfg.RestockTarget = _restockTarget.Value;
        cfg.ServiceFeeEnabled = _serviceFee.Value;
        cfg.ServiceFeePerRun = (decimal)_serviceFeePerRun.Value;
        cfg.Language = _lang.Value;
        Loc.Current = cfg.Language == "da" ? Lang.Da : Lang.En;
        _lastSig = Sig(cfg);
    }

    /// <summary>Persist only if the config changed since the last save (cheap debounce for per-tick calls).</summary>
    public static void SaveIfChanged(AutomationConfig cfg)
    {
        if (_cat == null)
            return;
        var sig = Sig(cfg);
        if (sig == _lastSig)
            return;
        _lastSig = sig;

        _master.Value = cfg.MasterEnabled;
        _restock.Value = cfg.RestockEnabled;
        _logistics.Value = cfg.LogisticsEnabled;
        _employees.Value = cfg.EmployeesEnabled;
        _finance.Value = cfg.FinanceEnabled;
        _timeskip.Value = cfg.TimeSkipEnabled;
        _wellbeing.Value = cfg.WellbeingEnabled;
        _reserve.Value = (float)cfg.CashReserveFloor;
        _restockTarget.Value = cfg.RestockTarget;
        _serviceFee.Value = cfg.ServiceFeeEnabled;
        _serviceFeePerRun.Value = (float)cfg.ServiceFeePerRun;
        _lang.Value = cfg.Language;
        MelonPreferences.Save();
    }

    private static string Sig(AutomationConfig c)
        => $"{c.MasterEnabled}|{c.RestockEnabled}|{c.LogisticsEnabled}|{c.EmployeesEnabled}|" +
           $"{c.FinanceEnabled}|{c.TimeSkipEnabled}|{c.WellbeingEnabled}|{c.CashReserveFloor}|{c.RestockTarget}|" +
           $"{c.ServiceFeeEnabled}|{c.ServiceFeePerRun}|{c.Language}";
}
