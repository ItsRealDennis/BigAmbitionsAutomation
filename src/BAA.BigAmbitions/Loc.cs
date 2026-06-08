using System.Collections.Generic;

namespace BaBot;

internal enum Lang { En, Da }

/// <summary>Tiny EN/DA layer for the overlay. T(en) returns Danish if selected and present, else English.</summary>
internal static class Loc
{
    public static Lang Current = Lang.En;

    public static string T(string en) => Current == Lang.Da && Da.TryGetValue(en, out var v) ? v : en;

    private static readonly Dictionary<string, string> Da = new()
    {
        ["AUTOMATION CONTROL"] = "AUTOMATIONSKONTROL",
        ["NO SAVE LOADED"] = "INTET SPIL INDLÆST",
        ["QUICK ACTIONS"] = "HURTIGE HANDLINGER",
        ["ENERGY 100%"] = "ENERGI 100%",
        ["FEATURES"] = "FUNKTIONER",
        ["ACTIVITY"] = "AKTIVITET",
        ["ON"] = "TIL", ["OFF"] = "FRA", ["DAY"] = "DAG", ["NET"] = "NETTO",
        ["RESERVE FLOOR"] = "RESERVEGRÆNSE", ["F8 to toggle"] = "F8 skifter",
        ["Shops"] = "Butikker", ["Energy"] = "Energi", ["Happy"] = "Glad",
        ["RUN NOW"] = "KØR NU", ["RESTOCK TARGET"] = "GENOPFYLD MÅL", ["FEE / RUN"] = "GEBYR / KØRSEL",
        ["Tax due"] = "Skyldig skat", ["Loans"] = "Lån", ["Staff"] = "Personale", ["LIVE MODE"] = "LIVE-TILSTAND",
        ["AUTOMATION (MASTER)"] = "AUTOMATION (HOVED)", ["AUTO-RESTOCK"] = "AUTO-GENOPFYLD",
        ["LOGISTICS"] = "LOGISTIK", ["EMPLOYEES"] = "MEDARBEJDERE", ["FINANCE AUTO-PAY"] = "FINANS AUTO-BETAL",
        ["AUTO-WELLBEING"] = "AUTO-VELVÆRE", ["SERVICE FEE"] = "SERVICEGEBYR",
        ["ENERGY"] = "ENERGI", ["SKIP DAY"] = "SPRING DAG OVER", ["TURBO SPEED"] = "TURBOFART",
        ["No activity yet"] = "Ingen aktivitet endnu",
        ["Auto-sets each product's price to the game's own optimal price for its neighborhood, keeping price-satisfaction high. Previews unless Live mode is on."]
            = "Sætter automatisk hver vares pris til spillets egen optimale pris for kvarteret, så pristilfredsheden holdes høj. Forhåndsvisning medmindre Live-tilstand er slået til.",
    };
}
