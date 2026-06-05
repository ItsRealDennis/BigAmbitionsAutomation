using System.Collections.Generic;
using BAA.Core.Abstractions;
using Il2Cpp;
using Il2CppBigAmbitions.Items;

namespace BAA.Mod.Game;

/// <summary>
/// IGameCommands over the live game. Restock currently runs in DRY-RUN: it computes the real cost
/// and logs exactly what it would buy, but does not yet spend (the cash-debiting fill call needs one
/// live verification). Everything stays safe and the safety gate's budgeting still works because the
/// returned CashDelta mirrors the intended spend. Flip <see cref="LiveWrites"/> once verified.
/// </summary>
internal sealed class GameCommandsAdapter : IGameCommands
{
    /// <summary>When false (default), restock only previews/logs — no game state is changed.</summary>
    public static bool LiveWrites = false;

    private readonly GameStateAdapter _state;

    public GameCommandsAdapter(GameStateAdapter state) => _state = state;

    public CommandResult RestockItem(BusinessId business, ItemId item, int quantity)
    {
        if (!_state.TryResolve(business, item, out var reg, out var name))
            return CommandResult.Failed("could not resolve business/item");

        decimal unitCost = 0m;
        try { unitCost = (decimal)ItemHelper.GetPrice(name, reg); } catch { }
        decimal spend = -(unitCost * quantity);

        Diagnostics.Activity.Add($"{(LiveWrites ? "Restock" : "[preview] restock")} {quantity}x {name}  ~${unitCost * quantity:N0}");

        // DRY-RUN (default): report the intended spend so the safety gate's within-tick budget is
        // realistic, but change nothing in the game.
        if (!LiveWrites)
            return CommandResult.Applied(spend);

        // LIVE path is intentionally NOT wired yet: BuildingHelper.FillItemWithStock(reg, amount,
        // CargoInstance) takes no ItemName and needs a real CargoInstance source, so it cannot
        // target a specific product safely. Until the correct per-item buy call is verified on a
        // live shop, live restock is a no-op so it can never spend incorrectly.
        return CommandResult.Skipped("live restock pending verified per-item API");
    }

    // --- Other commands: previewed/logged, no game mutation yet (managers wired, write paths pending) ---
    private static CommandResult Preview(string what) { Diagnostics.Activity.Add("[preview] " + what); return CommandResult.Applied(); }

    public CommandResult SetStockTarget(BusinessId b, ItemId i, int target) => CommandResult.Skipped("n/a");
    public CommandResult SetItemPrice(BusinessId b, ItemId i, decimal price) => CommandResult.Skipped("n/a");
    public CommandResult ConfigureImportContract(ImportContractSpec spec) => Preview($"import contract {spec.Quantity}x {spec.Item}");
    public CommandResult SetWarehouseTarget(WarehouseId w, ItemId i, int target) => Preview($"warehouse target {target}x {i}");
    public CommandResult AssignLogistics(BusinessId b, EmployeeId m, IReadOnlyList<EmployeeId> drivers) => Preview("assign logistics");
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
