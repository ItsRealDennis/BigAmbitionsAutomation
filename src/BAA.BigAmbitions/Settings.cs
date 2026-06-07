using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using BAA.Core.Config;
using UnityEngine;

namespace BaBot;

/// <summary>
/// File-backed persistence for the automation config (replaces the old MelonPreferences path).
/// Stored as a tiny key=value file under the game's persistent data folder, so settings survive
/// across sessions and a mod reload. Save is debounced via a signature so per-frame calls are cheap.
/// </summary>
internal static class Settings
{
    private static string Path =>
        System.IO.Path.Combine(Application.persistentDataPath, "BABOT", "settings.cfg");

    private static string _lastSig;

    public static void Load(AutomationConfig cfg)
    {
        try
        {
            var path = Path;
            if (!File.Exists(path)) { _lastSig = Sig(cfg); return; }

            var kv = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(path))
            {
                var i = line.IndexOf('=');
                if (i <= 0) continue;
                kv[line.Substring(0, i).Trim()] = line.Substring(i + 1).Trim();
            }

            cfg.MasterEnabled     = B(kv, "MasterEnabled", cfg.MasterEnabled);
            cfg.RestockEnabled    = B(kv, "RestockEnabled", cfg.RestockEnabled);
            cfg.LogisticsEnabled  = B(kv, "LogisticsEnabled", cfg.LogisticsEnabled);
            cfg.EmployeesEnabled  = B(kv, "EmployeesEnabled", cfg.EmployeesEnabled);
            cfg.FinanceEnabled    = B(kv, "FinanceEnabled", cfg.FinanceEnabled);
            cfg.TimeSkipEnabled   = B(kv, "TimeSkipEnabled", cfg.TimeSkipEnabled);
            cfg.WellbeingEnabled  = B(kv, "WellbeingEnabled", cfg.WellbeingEnabled);
            cfg.ServiceFeeEnabled = B(kv, "ServiceFeeEnabled", cfg.ServiceFeeEnabled);
            cfg.LiveWrites        = B(kv, "LiveWrites", cfg.LiveWrites);
            cfg.CashReserveFloor  = M(kv, "CashReserveFloor", cfg.CashReserveFloor);
            cfg.ServiceFeePerRun  = M(kv, "ServiceFeePerRun", cfg.ServiceFeePerRun);
            cfg.RestockTarget     = I(kv, "RestockTarget", cfg.RestockTarget);
            cfg.PricingEnabled    = B(kv, "PricingEnabled", cfg.PricingEnabled);
            cfg.PricingTargetPercent = M(kv, "PricingTargetPercent", cfg.PricingTargetPercent);
            cfg.TurboPercent      = I(kv, "TurboPercent", cfg.TurboPercent);
            cfg.Language          = kv.TryGetValue("Language", out var lang) ? lang : cfg.Language;
            UiPrefs.Scale = F(kv, "UiScale", UiPrefs.Scale);
            UiPrefs.PosX  = F(kv, "UiPosX", UiPrefs.PosX);
            UiPrefs.PosY  = F(kv, "UiPosY", UiPrefs.PosY);
        }
        catch (Exception ex) { Debug.LogWarning("[BA BOT] settings load failed: " + ex.Message); }
        _lastSig = Sig(cfg);
    }

    public static void SaveIfChanged(AutomationConfig cfg)
    {
        var sig = Sig(cfg);
        if (sig == _lastSig) return;
        _lastSig = sig;
        try
        {
            var path = Path;
            Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path));
            File.WriteAllText(path, sig);
        }
        catch (Exception ex) { Debug.LogWarning("[BA BOT] settings save failed: " + ex.Message); }
    }

    // The signature IS the file content (key=value lines), so it doubles as the persisted form.
    private static string Sig(AutomationConfig c) => string.Join("\n", new[]
    {
        "MasterEnabled="     + c.MasterEnabled,
        "RestockEnabled="    + c.RestockEnabled,
        "LogisticsEnabled="  + c.LogisticsEnabled,
        "EmployeesEnabled="  + c.EmployeesEnabled,
        "FinanceEnabled="    + c.FinanceEnabled,
        "TimeSkipEnabled="   + c.TimeSkipEnabled,
        "WellbeingEnabled="  + c.WellbeingEnabled,
        "ServiceFeeEnabled=" + c.ServiceFeeEnabled,
        "LiveWrites="        + c.LiveWrites,
        "CashReserveFloor="  + c.CashReserveFloor.ToString(CultureInfo.InvariantCulture),
        "ServiceFeePerRun="  + c.ServiceFeePerRun.ToString(CultureInfo.InvariantCulture),
        "RestockTarget="     + c.RestockTarget.ToString(CultureInfo.InvariantCulture),
        "PricingEnabled="    + c.PricingEnabled,
        "PricingTargetPercent=" + c.PricingTargetPercent.ToString(CultureInfo.InvariantCulture),
        "TurboPercent="      + c.TurboPercent.ToString(CultureInfo.InvariantCulture),
        "Language="          + c.Language,
        "UiScale="           + UiPrefs.Scale.ToString(CultureInfo.InvariantCulture),
        "UiPosX="            + UiPrefs.PosX.ToString(CultureInfo.InvariantCulture),
        "UiPosY="            + UiPrefs.PosY.ToString(CultureInfo.InvariantCulture),
    });

    private static bool B(Dictionary<string, string> kv, string k, bool d)
        => kv.TryGetValue(k, out var v) && bool.TryParse(v, out var r) ? r : d;
    private static int I(Dictionary<string, string> kv, string k, int d)
        => kv.TryGetValue(k, out var v) && int.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : d;
    private static decimal M(Dictionary<string, string> kv, string k, decimal d)
        => kv.TryGetValue(k, out var v) && decimal.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : d;
    private static float F(Dictionary<string, string> kv, string k, float d)
        => kv.TryGetValue(k, out var v) && float.TryParse(v, NumberStyles.Any, CultureInfo.InvariantCulture, out var r) ? r : d;
}

/// <summary>Panel size + position prefs (persisted alongside settings; not part of the Core config).</summary>
internal static class UiPrefs
{
    public static float Scale = 1f;
    public static float PosX = 26f;
    public static float PosY = -26f;
}
