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
        ["Shops"] = "Butikker",
        ["Energy"] = "Energi",
        ["Happy"] = "Glad",
        ["SCAN SHOPS"] = "SCAN BUTIKKER",
        ["RUN NOW"] = "KØR NU",
        ["RESTOCK TARGET"] = "GENOPFYLD MÅL",
        ["Tax due"] = "Skyldig skat",
        ["Loans"] = "Lån",
        ["Staff"] = "Personale",
        ["LIVE MODE"] = "LIVE-TILSTAND",

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
        ["Auto-pays your taxes the moment they come due — the one finance chore the game won't do for you. Rent, wages and loans already settle nightly. Respects your reserve floor."]
            = "Betaler automatisk din skat så snart den forfalder — den ene finansopgave spillet ikke klarer for dig. Husleje, løn og lån afregnes allerede hver nat. Respekterer din reservegrænse.",
        ["Keeps staff productive: pays a morale bonus to unhappy employees when the game allows one, and finishes completed training."]
            = "Holder personalet produktivt: giver en moralbonus til utilfredse medarbejdere når spillet tillader det, og afslutter færdig oplæring.",
        ["Sets up a repeating weekly import for any product running low so stock keeps flowing without manual reordering."]
            = "Opsætter en gentagen ugentlig import for varer der er ved at slippe op, så lageret holdes fyldt uden manuel genbestilling.",
        ["Buys products back up to target when shelves run low. Previews until a verified per-item buy call lands."]
            = "Køber varer op til målniveau når hylderne er lave. Forhåndsvises indtil et verificeret per-vare-køb er på plads.",
        ["Fast-forwards in-game time while your businesses keep earning. Turn off for normal speed."]
            = "Spoler tiden frem mens dine virksomheder tjener penge. Slå fra for normal hastighed.",
        ["Automatically refills your energy (and tops up happiness) so you never stop to sleep or eat."]
            = "Genopfylder automatisk din energi (og fylder humøret op), så du aldrig stopper for at sove eller spise.",
        ["OFF (default) = automation only PREVIEWS what it would do (safe). ON = it actually pays taxes and gives bonuses. Turn on only while watching the game."]
            = "FRA (standard) = automatisering FORHÅNDSVISER kun hvad den ville gøre (sikkert). TIL = den betaler faktisk skat og giver bonusser. Slå kun til mens du holder øje med spillet.",
        ["Automation never spends below this cash cushion. Use the minus / plus buttons to adjust."]
            = "Automatisering bruger aldrig under denne kontantbuffer. Brug minus / plus-knapperne for at justere.",
        ["Lists each of your businesses and its current stock in the activity log."]
            = "Viser hver af dine virksomheder og dens nuværende lager i aktivitetsloggen.",
        ["Runs one automation pass now. Turn on AUTOMATION (MASTER) and the features you want first. Previews unless Live mode is on; respects your reserve floor."]
            = "Kører én automatiseringsrunde nu. Slå AUTOMATION (HOVED) og de ønskede funktioner til først. Forhåndsviser medmindre Live-tilstand er slået til; respekterer din reservegrænse.",
        ["Enable AUTOMATION (MASTER) first"]
            = "Slå AUTOMATION (HOVED) til først",
        ["Auto-restock fills each product up to this many units."]
            = "Auto-genopfyld fylder hver vare op til så mange enheder.",
    };
}
