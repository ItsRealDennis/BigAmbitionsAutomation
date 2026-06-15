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
        ["SKILLS"] = "FÆRDIGHEDER", ["MAX"] = "MAKS",
        ["HOME"] = "HJEM", ["AUTO"] = "AUTO", ["STAFF"] = "STAB", ["LOG"] = "LOG",
        ["LOW-STOCK WARNING"] = "LAV-LAGER VARSEL", ["LOW-STOCK ALERT"] = "LAV-LAGER ALARM",
        ["Tap MAX to train a person's skills to 100%."] = "Tryk MAKS for at træne en persons færdigheder til 100%.",
        ["Warn that stock is running low when a product drops below this % of full. Higher = warned earlier. The game's default is 25%."]
            = "Advarer om lavt lager når en vare falder under denne % af fuldt. Højere = advaret tidligere. Spillets standard er 25%.",
        ["Hire staff to train their skills."] = "Ansæt personale for at træne deres færdigheder.",
        ["Click MAX to instantly train this person's skill to 100%."]
            = "Klik MAKS for straks at træne denne persons færdighed til 100%.",
        ["How many days ahead the game's to-do list warns that stock is running low. The game's default is 2 - bump it for more lead time."]
            = "Hvor mange dage i forvejen spillets to-do-liste advarer om at lageret er ved at slippe op. Spillets standard er 2 - sæt den op for mere varsel.",
        ["Tax due"] = "Skyldig skat", ["Loans"] = "Lån", ["Staff"] = "Personale", ["LIVE MODE"] = "LIVE-TILSTAND",
        ["AUTOMATION (MASTER)"] = "AUTOMATION (HOVED)", ["AUTO-RESTOCK"] = "AUTO-GENOPFYLD",
        ["LOGISTICS"] = "LOGISTIK", ["EMPLOYEES"] = "MEDARBEJDERE", ["FINANCE AUTO-PAY"] = "FINANS AUTO-BETAL",
        ["AUTO-WELLBEING"] = "AUTO-VELVÆRE", ["SERVICE FEE"] = "SERVICEGEBYR",
        ["ENERGY"] = "ENERGI", ["SKIP DAY"] = "SPRING DAG OVER", ["TURBO SPEED"] = "TURBOFART", ["TAXI"] = "TAXA",
        ["No activity yet"] = "Ingen aktivitet endnu",
        ["RUNNING"] = "KØRER", ["PREVIEW"] = "FORHÅNDSVISNING", ["LIVE"] = "LIVE",
        ["PAUSED - TURN ON MASTER"] = "PÅ PAUSE - SLÅ MASTER TIL", ["WAITING FOR SAVE"] = "VENTER PÅ SPIL",
        ["Auto-sets each product's price to the game's own optimal price for its neighborhood, keeping price-satisfaction high. Previews unless Live mode is on."]
            = "Sætter automatisk hver vares pris til spillets egen optimale pris for kvarteret, så pristilfredsheden holdes høj. Forhåndsvisning medmindre Live-tilstand er slået til.",
    };
}
