using System.Collections.Generic;

namespace BAA.Mod;

internal enum Lang { En, Da }

/// <summary>
/// Tiny localization layer. <see cref="T"/> takes the English text (which doubles as the lookup key),
/// so any missing translation falls back to English automatically. Switch language with <see cref="Current"/>.
/// </summary>
internal static class Loc
{
    public static Lang Current = Lang.En;

    public static string T(string en)
        => Current == Lang.Da && Da.TryGetValue(en, out var v) ? v : en;

    private static readonly Dictionary<string, string> Da = new()
    {
        // Panel chrome
        ["AUTOMATION CONTROL"] = "AUTOMATIONSKONTROL",
        ["NO SAVE LOADED"] = "INTET SPIL INDLÆST",
        ["QUICK ACTIONS"] = "HURTIGE HANDLINGER",
        ["ENERGY 100%"] = "ENERGI 100%",
        ["SCAN MY BUSINESSES"] = "SCAN MINE VIRKSOMHEDER",
        ["FEATURES"] = "FUNKTIONER",
        ["ACTIVITY"] = "AKTIVITET",
        ["ON"] = "TIL",
        ["OFF"] = "FRA",
        ["DAY"] = "DAG",
        ["NET"] = "NETTO",
        ["RESERVE FLOOR"] = "RESERVEGRÆNSE",
        ["F8 to toggle"] = "F8 skifter",

        // Feature names
        ["AUTOMATION (MASTER)"] = "AUTOMATION (HOVED)",
        ["AUTO-RESTOCK"] = "AUTO-GENOPFYLD",
        ["LOGISTICS"] = "LOGISTIK",
        ["EMPLOYEES"] = "MEDARBEJDERE",
        ["FINANCE AUTO-PAY"] = "FINANS AUTO-BETAL",
        ["TIME-SKIP (AFK)"] = "TIDSSPRING (AFK)",
        ["AUTO-WELLBEING"] = "AUTO-VELVÆRE",

        // Tooltips
        ["Instantly add $1,000 cash. Handy for testing the mod."]
            = "Tilføj straks $1.000 kontant. Nyttigt til at teste modden.",
        ["Instantly refill your energy to full."]
            = "Genopfyld straks din energi til fuld.",
        ["Lists each of your businesses and its current stock in the activity log (and MelonLoader console)."]
            = "Viser hver af dine virksomheder og dens nuværende lager i aktivitetsloggen (og MelonLoader-konsollen).",
        ["Master switch. Must be ON for anything below to run. Off = the mod does nothing."]
            = "Hovedkontakt. Skal være TIL for at noget nedenfor kører. FRA = modden gør intet.",
        ["Keeps shops stocked: buys products back up to target when shelves run low. (Coming soon)"]
            = "Holder butikker fyldt: køber varer op til målniveau når hylderne er lave. (Forhåndsvisning)",
        ["Auto-sets warehouse-to-store deliveries and repeating supplier imports. (Coming soon)"]
            = "Opsætter automatisk lager-til-butik leveringer og gentagne leverandørimporter. (Kommer snart)",
        ["Recruits staff and manages wages, schedules and training. (Coming soon)"]
            = "Rekrutterer personale og styrer løn, vagtplaner og oplæring. (Kommer snart)",
        ["Collects income and pays rent, bills and loans automatically. (Coming soon)"]
            = "Indsamler indtægter og betaler husleje, regninger og lån automatisk. (Kommer snart)",
        ["Fast-forwards in-game time while your businesses keep earning. Turn off for normal speed."]
            = "Spoler tiden frem mens dine virksomheder tjener penge. Slå fra for normal hastighed.",
        ["Automatically refills your energy so you never stop to sleep or eat."]
            = "Genopfylder automatisk din energi, så du aldrig stopper for at sove eller spise.",
        ["Automation never spends below this cash cushion. Use the minus / plus buttons to adjust."]
            = "Automatisering bruger aldrig under denne kontantbuffer. Brug minus / plus-knapperne for at justere.",
    };
}
