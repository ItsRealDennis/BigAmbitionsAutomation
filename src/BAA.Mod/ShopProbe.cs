using Il2Cpp;

namespace BAA.Mod;

/// <summary>
/// Read-only inventory scan of the player's businesses — the groundwork for auto-restock.
/// Enumerates player-owned/rented buildings, lists each one's products and current stock via the
/// game's own helpers, and logs it. Once a shop exists this confirms the exact data the restocker
/// will read (current stock per product), before we wire the (cash-spending) fill step behind the
/// safety gate. Fully guarded — never throws into the game.
/// </summary>
internal static class ShopProbe
{
    public static void ScanAndLog()
    {
        var log = ModEntry.Log;
        try
        {
            var gi = SaveGameManager.Current;
            if (gi == null)
            {
                Diagnostics.Activity.Add("Scan: no save loaded");
                return;
            }

            var regs = gi.BuildingRegistrations;
            if (regs == null)
                return;

            int shops = 0;
            log.Msg("================ BA BOT inventory scan ================");
            for (int i = 0; i < regs.Count; i++)
            {
                var reg = regs[i];
                bool mine = false;
                try { mine = reg.RentedByPlayer || reg.BuildingOwnedByPlayer; } catch { }
                if (!mine)
                    continue;

                var items = reg.GetListOfItemsForSale();
                if (items == null || items.Count == 0)
                    continue;

                shops++;
                string type = "";
                try { type = reg.businessTypeName.ToString(); } catch { }
                log.Msg($"[{type}] - {items.Count} product(s):");

                for (int j = 0; j < items.Count; j++)
                {
                    var item = items[j];
                    int current = -1;
                    try { current = Il2CppHelpers.BuildingHelper.CountTotalResourcesInStock(reg, item, true, true); } catch { }
                    log.Msg($"    {item}  stock={current}");
                }
                Diagnostics.Activity.Add($"Scanned {type}: {items.Count} products");
            }

            if (shops == 0)
            {
                log.Msg("(no player-owned businesses with products yet - buy & stock a shop, then scan)");
                Diagnostics.Activity.Add("Scan: no businesses with products yet");
            }
            log.Msg("================ end scan ================");
        }
        catch (System.Exception ex)
        {
            log?.Warning($"ShopProbe failed: {ex.Message}");
        }
    }
}
