using System;
using System.Collections.Generic;
using BAA.Core.Abstractions;
using BAA.Core.Config;
using BaBot.Diagnostics;
using BigAmbitions.Items;

namespace BaBot.Game;

/// <summary>
/// IGameCommands over the live EA 0.11 (Mono) game. Money-spending / state-changing actions honour
/// <see cref="AutomationConfig.LiveWrites"/>: OFF (default) only PREVIEWS (logs what it would do,
/// changes nothing) while reporting the intended cash delta so the safety gate's budgeting stays
/// realistic; ON actually executes. Cosmetic helpers (energy/happiness/test cash) are in
/// <see cref="GameActions"/> and always run.
/// </summary>
internal sealed class GameCommandsAdapter : IGameCommands
{
    private readonly GameStateAdapter _state;
    private readonly AutomationConfig _config;

    public GameCommandsAdapter(GameStateAdapter state, AutomationConfig config)
    {
        _state = state;
        _config = config;
    }

    private bool Live => _config.LiveWrites;

    private static CommandResult Preview(string what, decimal cashDelta = 0m)
    {
        Activity.Add("[preview] " + what);
        return CommandResult.Applied(cashDelta);
    }

    // --- Restock (live: debit cash + deliver cargo into the shop) ---
    public CommandResult RestockItem(BusinessId business, ItemId item, int quantity)
    {
        if (quantity <= 0) return CommandResult.Skipped("nothing to buy");
        if (!_state.TryResolve(business, item, out var reg, out var name) || reg == null)
            return CommandResult.Failed("could not resolve business/item");

        decimal unit = 0m;
        try { unit = (decimal)ItemHelper.GetPrice(name, reg); } catch { }
        decimal spend = -(unit * quantity);

        if (!Live)
            return Preview($"restock {quantity}x {name}  ~${unit * quantity:N0}", spend);

        try
        {
            // Deliver FIRST (this is the call that can throw), then debit — so a failed delivery
            // never charges the player for stock they didn't get (no money/stock desync).
            var cargo = new CargoInstance { itemName = name, amount = quantity, pricePerUnit = (float)unit, paid = true };
            ItemHelper.DeliverCargoToBuilding(cargo, reg, (Func<ItemInstance, bool>)null);
            GameManager.Command_ChangeMoney((float)spend); // negative = debit, only after stock is in
            Activity.Add($"Restocked {quantity}x {name}  -${unit * quantity:N0}");
            return CommandResult.Applied(spend);
        }
        catch (Exception ex) { return CommandResult.Failed("restock: " + ex.Message); }
    }

    // --- Finance ---
    public CommandResult PayTaxes(decimal amount)
    {
        if (amount <= 0m) return CommandResult.Skipped("no tax due");
        if (!Live) return Preview($"pay taxes ${amount:N0}", -amount);
        try
        {
            Helpers.TaxHelper.Command_IRSForcePayment((float)amount);
            Activity.Add($"Paid taxes ${amount:N0}");
            return CommandResult.Applied(-amount);
        }
        catch (Exception ex) { return CommandResult.Failed("pay taxes: " + ex.Message); }
    }

    public CommandResult ChargeServiceFee(decimal amount)
    {
        if (amount <= 0m) return CommandResult.Skipped("no fee");
        if (!Live) return Preview($"automation service fee ${amount:N0}", -amount);
        try
        {
            GameManager.Command_ChangeMoney(-(float)amount);
            Activity.Add($"Automation service fee -${amount:N0}");
            return CommandResult.Applied(-amount);
        }
        catch (Exception ex) { return CommandResult.Failed("service fee: " + ex.Message); }
    }

    // --- Employees ---
    public CommandResult GiveBonus(EmployeeId employee)
    {
        if (!_state.TryResolveEmployee(employee, out var emp) || emp == null)
            return CommandResult.Failed("could not resolve employee");
        decimal cost = 0m;
        try { cost = (decimal)emp.GetBonusAmount(); } catch { }
        if (!Live) return Preview($"morale bonus ${cost:N0}", -cost);
        try
        {
            if (!emp.CanGiveBonus()) return CommandResult.Skipped("bonus not available");
            emp.GiveBonus();
            Activity.Add($"Gave morale bonus ${cost:N0}");
            return CommandResult.Applied(-cost);
        }
        catch (Exception ex) { return CommandResult.Failed("give bonus: " + ex.Message); }
    }

    public CommandResult FinishTraining(EmployeeId employee)
    {
        if (!_state.TryResolveEmployee(employee, out var emp) || emp == null)
            return CommandResult.Failed("could not resolve employee");
        if (!Live) return Preview("finish training");
        try
        {
            emp.FinishTraining();
            Activity.Add("Finished employee training");
            return CommandResult.Applied();
        }
        catch (Exception ex) { return CommandResult.Failed("finish training: " + ex.Message); }
    }

    // --- Scheduling (live: hand the business to the game's own auto-scheduler) ---
    public CommandResult AutoFillSchedule(BusinessId business)
    {
        if (!_state.TryResolveBusiness(business, out var reg) || reg == null)
            return CommandResult.Failed("could not resolve business");
        if (!Live) return Preview("auto-fill schedule");
        try
        {
            // warnIfUnassigned:false avoids the in-game confirm popup; inhibitSuccessNotification:true keeps
            // it quiet; the game fills on a background thread. No cash cost.
            Helpers.ScheduleAutoFillerHelper.AutoFillSchedule(reg, null, null, false, true, true);
            Activity.Add("Auto-filled employee schedule");
            return CommandResult.Applied();
        }
        catch (Exception ex) { return CommandResult.Failed("schedule: " + ex.Message); }
    }

    // --- Logistics: previewed (recurring import contracts are stateful; kept preview for now) ---
    public CommandResult ConfigureImportContract(ImportContractSpec spec)
        => Preview($"import contract {spec.Quantity}x {spec.Item}");

    public CommandResult SetContractRepeating(ContractId contract, bool repeating)
    {
        if (!_state.TryResolveContract(contract, out var c) || c == null)
            return CommandResult.Failed("could not resolve contract");
        if (!Live) return Preview(repeating ? "make import contract recurring" : "stop import contract recurring");
        try
        {
            // Match the game's own UI guard: contracts can't be modified during the Sun 20:00 - Mon 08:00 lock.
            if (!Entities.DeliveryHelper.CanModifyContract(c.nextDeliveryDay))
                return CommandResult.Skipped("contract in lock period");
            c.repeatingOrder = repeating;
            try { SaveGameManager.MarkChange(); } catch { }
            Activity.Add(repeating ? "Import contract set to recurring" : "Import contract recurring off");
            return CommandResult.Applied();
        }
        catch (Exception ex) { return CommandResult.Failed("contract: " + ex.Message); }
    }
    public CommandResult SetWarehouseTarget(WarehouseId w, ItemId i, int target) => Preview($"warehouse target {target}x {i}");
    public CommandResult AssignLogistics(BusinessId b, EmployeeId m, IReadOnlyList<EmployeeId> drivers) => Preview("assign logistics");

    // --- Pricing (live: write the business's stored retail price the same way the in-game UI does) ---
    public CommandResult SetItemPrice(BusinessId business, ItemId item, decimal price)
    {
        if (price <= 0m) return CommandResult.Skipped("invalid price");
        if (!_state.TryResolve(business, item, out var reg, out var name) || reg == null)
            return CommandResult.Failed("could not resolve business/item");

        if (!Live) return Preview($"set {name} price ${price:0.00}");

        try
        {
            var list = reg.retailPrices;
            if (list == null) return CommandResult.Failed("no retail price list");

            RetailPrice rp = null;
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].itemName == name) { rp = list[i]; break; }

            if (rp != null) rp.price = (float)price;
            else list.Add(new RetailPrice { itemName = name, price = (float)price });

            // Deliberately do NOT call BusinessHelper.UpdateSatisfaction here: it recomputes pricing-
            // satisfaction from the LAST DAY's completed orders (each priced at the OLD price), so it
            // can't reflect a not-yet-sold price and would only stale-overwrite. The game recomputes it
            // correctly on the next daily tick (BusinessHelper.RunDaily), reading the new price live.
            try { SaveGameManager.MarkChange(); } catch { }
            Activity.Add($"Set {name} price ${price:0.00}");
            return CommandResult.Applied();
        }
        catch (Exception ex) { return CommandResult.Failed("set price: " + ex.Message); }
    }

    // --- Not used by current managers ---
    public CommandResult SetStockTarget(BusinessId b, ItemId i, int target) => CommandResult.Skipped("n/a");
    public CommandResult HireCandidate(CandidateId c, BusinessId b) => Preview("hire candidate");
    public CommandResult SetWage(EmployeeId e, decimal wage) => Preview("set wage");
    public CommandResult SetSchedule(EmployeeId e, ScheduleSpec schedule) => CommandResult.Skipped("n/a");
    public CommandResult SetHealthPlan(EmployeeId e, bool enabled) => CommandResult.Skipped("n/a");
    public CommandResult PayRent(BusinessId b) => Preview("pay rent");
    public CommandResult CollectIncome(BusinessId b) => Preview("collect income");
    public CommandResult PayLoanInstallment(LoanId l) => Preview("pay loan");
    public CommandResult SetTimeSpeed(float m) => CommandResult.Skipped("n/a");
    public CommandResult RequestSkip(SkipSpec s) => CommandResult.Skipped("n/a");
}
