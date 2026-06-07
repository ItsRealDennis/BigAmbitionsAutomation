using System.Collections.Generic;
using BAA.Core.Abstractions;
using BAA.Core.Config;
using Il2Cpp;
using Il2CppBigAmbitions.Items;

namespace BAA.Mod.Game;

/// <summary>
/// IGameCommands over the live game. Money-spending / state-changing actions (restock, pay taxes, staff
/// bonuses, finish training) honour <see cref="AutomationConfig.LiveWrites"/>: when it is OFF (default)
/// they only PREVIEW — they compute the real numbers and log exactly what they would do, but change
/// nothing — so the safety gate's budgeting stays realistic while the player verifies behaviour. Flip
/// Live mode on (panel toggle) to actually execute. Cosmetic helpers (energy/happiness/time-skip) live
/// in <see cref="GameActions"/> and are always live.
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
        Diagnostics.Activity.Add("[preview] " + what);
        return CommandResult.Applied(cashDelta);
    }

    // --- Restock ---

    public CommandResult RestockItem(BusinessId business, ItemId item, int quantity)
    {
        if (!_state.TryResolve(business, item, out var reg, out var name))
            return CommandResult.Failed("could not resolve business/item");

        decimal unitCost = 0m;
        try { unitCost = (decimal)ItemHelper.GetPrice(name, reg); } catch { }
        decimal spend = -(unitCost * quantity);

        Diagnostics.Activity.Add($"{(Live ? "Restock" : "[preview] restock")} {quantity}x {name}  ~${unitCost * quantity:N0}");

        // Report the intended spend either way so the safety gate's within-tick budget is realistic.
        if (!Live)
            return CommandResult.Applied(spend);

        // LIVE path is intentionally still a no-op: BuildingHelper.FillItemWithStock needs a real
        // CargoInstance (no per-item ItemName buy call exists), so it can't target a product safely.
        // Auto-restock therefore previews even in Live mode until a verified per-item buy is found.
        return CommandResult.Skipped("live restock pending verified per-item buy API");
    }

    // --- Finance ---

    public CommandResult PayTaxes(decimal amount)
    {
        if (amount <= 0m)
            return CommandResult.Skipped("no tax due");
        if (!Live)
            return Preview($"pay taxes ${amount:N0}", -amount);

        try
        {
            Il2CppHelpers.TaxHelper.Command_IRSForcePayment((float)amount);
            Diagnostics.Activity.Add($"Paid taxes ${amount:N0}");
            return CommandResult.Applied(-amount);
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failed("pay taxes: " + ex.Message);
        }
    }

    public CommandResult ChargeServiceFee(decimal amount)
    {
        if (amount <= 0m)
            return CommandResult.Skipped("no fee");
        if (!Live)
            return Preview($"automation service fee ${amount:N0}", -amount);

        try
        {
            GameManager.Command_ChangeMoney(-(float)amount);
            Diagnostics.Activity.Add($"Automation service fee -${amount:N0}");
            return CommandResult.Applied(-amount);
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failed("service fee: " + ex.Message);
        }
    }

    // --- Employees ---

    public CommandResult GiveBonus(EmployeeId employee)
    {
        if (!_state.TryResolveEmployee(employee, out var emp) || emp == null)
            return CommandResult.Failed("could not resolve employee");

        decimal cost = 0m;
        try { cost = (decimal)emp.GetBonusAmount(); } catch { }

        if (!Live)
            return Preview($"morale bonus ${cost:N0}", -cost);

        try
        {
            if (!emp.CanGiveBonus())
                return CommandResult.Skipped("bonus not available");
            emp.GiveBonus();
            Diagnostics.Activity.Add($"Gave morale bonus ${cost:N0}");
            return CommandResult.Applied(-cost);
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failed("give bonus: " + ex.Message);
        }
    }

    public CommandResult FinishTraining(EmployeeId employee)
    {
        if (!_state.TryResolveEmployee(employee, out var emp) || emp == null)
            return CommandResult.Failed("could not resolve employee");

        if (!Live)
            return Preview("finish training");

        try
        {
            emp.FinishTraining();
            Diagnostics.Activity.Add("Finished employee training");
            return CommandResult.Applied();
        }
        catch (System.Exception ex)
        {
            return CommandResult.Failed("finish training: " + ex.Message);
        }
    }

    // --- Logistics: previewed (no safe direct import/buy call verified yet) ---
    public CommandResult ConfigureImportContract(ImportContractSpec spec)
        => Preview($"import contract {spec.Quantity}x {spec.Item}", -0m);
    public CommandResult SetWarehouseTarget(WarehouseId w, ItemId i, int target) => Preview($"warehouse target {target}x {i}");
    public CommandResult AssignLogistics(BusinessId b, EmployeeId m, IReadOnlyList<EmployeeId> drivers) => Preview("assign logistics");

    // --- Not used by current managers ---
    public CommandResult SetStockTarget(BusinessId b, ItemId i, int target) => CommandResult.Skipped("n/a");
    public CommandResult SetItemPrice(BusinessId b, ItemId i, decimal price) => CommandResult.Skipped("n/a");
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
