# Big Ambitions — API Map (discovery deliverable)

This file is the **source of truth** for the real game API. Each entry maps one adapter capability
(`IGameState` / `IGameCommands` / `IGameEvents` member) to the concrete `Il2CppBigAmbitions` type,
member, and access path discovered in the decompiler + live inspection. The adapter
(`BAA.Adapter`) is a mechanical transcription of the **CONFIRMED** rows here.

> Status legend: **TO-DISCOVER** (not started) · **TENTATIVE** (found, not yet live-verified;
> ship behind a default-off flag) · **CONFIRMED** (verified live) · **NOT-FOUND** (no API; adapter
> returns `CommandResult.Skipped("api-not-found")` so the rest of the mod still runs).

## How to discover (per entry)
1. **Static search** in dnSpyEx over `Il2CppBigAmbitions.dll` — anchors: `GameManager`, `BizMan`,
   `TimeMachine`; verbs: `Restock` `Buy` `Purchase` `Order` `Import` `Contract` `Deliver` `Tick`
   `Daily` `NewDay` `Hire` `Wage` `Rent` `Collect` `Income`.
2. **Map fields** on the data class (current/target/capacity stock, unit cost, due amounts). Note
   IL2CPP collection types (`Il2CppSystem.Collections.Generic.List<>`, `Il2CppArrayBase<>`).
3. **Live-confirm** in UnityExplorer: find the live instance, change something in-game, watch which
   field moves. This separates the real field from decoys.
4. **Fingerprints** to identify the right member:
   - the real **buy/restock** method internally calls `GameManager.Command_ChangeMoney(negative)`;
   - the **daily-tick** method is the one that *increments the in-game day* (hook it, log once/day).
5. Record below with **Status** + **game version** (so a future patch diff shows what to re-verify).

## Entry schema (copy per item)
```
### <IGame* member>
- Status:           TO-DISCOVER | TENTATIVE | CONFIRMED | NOT-FOUND
- Game type:        Il2CppBigAmbitions.<Type>
- Member:           <signature or field path>
- Singleton access: <e.g. GameManager.Instance / BizMan.Instance>
- Side effects:     <e.g. calls Command_ChangeMoney(-qty*cost)>
- Collections:      <IL2CPP collection types involved>
- Validated:        <date + how (UnityExplorer / probe patch)>
- Game version:     <EA x.y + build>
- Notes / risks:    <clamping, units, semantics of target vs capacity, ...>
```

---

## Discovered & compile-verified (current EA build, generated DLL)
Master access path for state: **`Il2Cpp.SaveGameManager.Current`** (static) → `Il2Cpp.GameInstance`
(null when no save is loaded). The `BAA.Mod` probe references all of the below and compiles clean.

- `Il2Cpp.GameManager : InstanceBehavior<GameManager>` (singleton `.Instance`):
  - `NewDay()` (private instance) — **daily-tick Harmony hook** = auto-restock trigger. Patched, compiles.
  - `RunMainGameTick(float)` — per-frame game tick.
  - `Command_ChangeMoney(float)`, `Command_SetMoney(float)`, static `ChangeMoneySafe(float, Transaction.TransactionType, …) → bool` — money mutation (ChangeMoneySafe is the gated one).
  - static `SetMinutesMultiplier(float)` + field `MinutesMultiplier`; also `Il2Cpp.GameSpeedController` / `GameSpeed` — time-skip/speed.
  - static `SaveGame(string,bool)`; static `Command_TeleportPlayerToBusiness(BuildingSize,int)` etc. — travel (sidesteps driving).
- `Il2Cpp.GameInstance` (the save/state object):
  - `Money` (float), `Day` (int), `Hour` (int), `Minute` (float), `NetWorth`, `Energy`, `Hunger`, `Happiness`.
  - `BuildingRegistrations` (`List<BuildingRegistration>`) — **player businesses + warehouses**.
  - `EmployeeInstances`, `CandidateEmployeeInstances` (`List<EmployeeInstance>`) — staff + recruits.
  - `Loans`, `Transactions`, `DeliveryContracts`, `RecruitmentCampaigns`.
  - `importPartnerships`, `logisticsManagerPlans`, `hrManagerPlans`, `headhunterPlans` — logistics/HR plans.
- `Il2Cpp.SaveGameManager` (static): `Current` (GameInstance get/set), `SavingGameInProgress`, `Save(…)`, `Load(…)`.

> Note: the research doc's `TimeMachine` and `BizMan` data-layer names are wrong. Time = GameManager/GameSpeedController; the BizMan* types are UI panels — real data is `GameInstance` + `Il2CppEntities.*`.

### Input / click-through (game uses the NEW Unity Input System)
- World clicks flow through **`Il2Cpp.MouseController.Run()`** (static, per-frame: click-to-move, interact, select). There is **no** `EventSystem.IsPointerOverGameObject` wrapper.
- Our IMGUI overlay blocks click-through by Harmony-**prefixing `MouseController.Run()`** to skip when the cursor is over the panel (`ModEntry.PointerOverPanel`, set in OnGUI vs `OverlayUI.PanelRect`). IMGUI (OnGUI/Event.current) is a separate path, so panel buttons still work. Implemented in `Hooks/MouseClickBlockPatch.cs`.
- Movement gate (for our own synthetic actions later): `Il2Cpp.PlayerController.SetNavigationBlocker(NavigationBlocker)` / `get_NavigationDisabled()`; global gates `GameManager.ShouldBlockKeyboardShortcuts()` / `HasInputSelected()`; major-panel flags like `FullMenu.IsOpen`, `PurchaseUI.IsPanelOpen`.
- Other click handlers: `MouseController.DetermineMouseHold()`, `InputHelper.GetClickedComponent(LayerMask,bool)`, `EntityController.OnIoLeftClick()` (virtual, per-object), `PlayerController.SetNewDestination(...)`.

### Still to map for restock (M5) — via the live probe + `Il2CppEntities.*`
Businesses = `Il2Cpp.BuildingRegistration` / `Il2CppEntities.RealEstate`; display/storage shelves =
`Il2Cpp.ShelfController` / `StorageShelfController` (`fillState`, `IsFull()`); products =
`Il2Cpp.BusinessProduct` + `Il2CppBigAmbitions.Items.ItemName` enum; market = `Il2CppHelpers.ProductMarketHelper`.
Need: per-business product **current/target stock + unit cost** and the **buy/restock** call. Plan: have the
probe walk a BuildingRegistration at runtime and log its inventory shape (more reliable than static guessing).

## Checklist — Clock / Time  (`IGameClock`, `IGameEvents` time hooks)
- [ ] **IGameClock.Now** (in-game date/time fields) — TO-DISCOVER
- [ ] **IGameClock.SpeedMultiplier / IsPaused** (TimeMachine) — TO-DISCOVER
- [ ] **IGameCommands.SetTimeSpeed** (TimeMachine setter) — TO-DISCOVER
- [ ] **IGameCommands.RequestSkip** (advance N hours cooperatively) — TO-DISCOVER
- [ ] **IGameEvents.DailyTick** (day-boundary method to Harmony-patch) — TO-DISCOVER
- [ ] **IGameEvents.HourTick** (optional finer cadence) — TO-DISCOVER

## Checklist — Finance  (`IGameState.GetFinances`, finance commands)
- [ ] **Cash** (read; cross-check vs `Command_ChangeMoney`) — TO-DISCOVER
- [ ] **RentDue / AnyRentOverdue** — TO-DISCOVER
- [ ] **BillsDue**, **LoanDue** — TO-DISCOVER
- [ ] **IGameCommands.PayRent / PayLoanInstallment / CollectIncome** — TO-DISCOVER

## Checklist — Businesses & Inventory  (`IGameState`, restock commands)
- [ ] **GetBusinesses** (id, name, type, active) — TO-DISCOVER
- [ ] **GetInventory** (per-item Current / Target / ShelfCapacity / UnitCost) — TO-DISCOVER
- [ ] **IGameCommands.RestockItem** (the buy method; fingerprint: debits cash) — TO-DISCOVER
- [ ] **IGameCommands.SetStockTarget / SetItemPrice** — TO-DISCOVER
- [ ] **IGameEvents.ShelfDepleted** (item-hits-zero hook, if one exists) — TO-DISCOVER

## Checklist — Logistics  (warehouses, importer contracts, delivery)
- [ ] **GetWarehouses** (id, name, bays) — TO-DISCOVER
- [ ] **GetImportContracts** (item, qty, frequency, supplier) — TO-DISCOVER
- [ ] **IGameCommands.ConfigureImportContract** — TO-DISCOVER
- [ ] **IGameCommands.SetWarehouseTarget** — TO-DISCOVER
- [ ] **IGameCommands.AssignLogistics** (manager + drivers/trucks) — TO-DISCOVER
- [ ] **IGameEvents.DeliveryCompleted** (2 AM delivery hook) — TO-DISCOVER

## Checklist — Employees
- [ ] **GetEmployees** (id, name, role, wage, satisfaction, age, skill, assignment) — TO-DISCOVER
- [ ] **GetCandidates** (recruitment pool) — TO-DISCOVER
- [ ] **IGameCommands.HireCandidate / SetWage / SetSchedule / SetHealthPlan** — TO-DISCOVER
- [ ] **IGameEvents.EmployeeResigned** — TO-DISCOVER

### Employee skill (SKILLS panel — "train a person to 100%")
- Status:           TENTATIVE (reflection-based; ships behind the manual SKILLS panel, no automation)
- Game type:        `Il2Cpp.EmployeeInstance` (elements of `GameInstance.EmployeeInstances`)
- Member:           the per-employee skill/competence level field — **field name not yet confirmed**.
                    `SkillProbe` (in `BAA.Mod`) locates it by reflection over the candidate names in
                    `SkillProbe.SkillNames` (`skill`, `skillLevel`, `competence`, `expertise`, …) and,
                    if none match, logs every numeric member of the first employee to Player.log under
                    `BA BOT employee shape (skill discovery)` so the real one can be identified.
- Read path:        `SaveGameManager.Current.EmployeeInstances[i]` → skill property (normalised: a
                    0..1 value is shown as a percent; a 0..100 value is used as-is). Candidates are
                    skipped via `IsCandidate`. Display name via `GetEmployeeNameWithInfo()` (guarded).
- Write path:       set the skill property to its max (1.0 for a 0..1 scale, else 100). The game is
                    expected to clamp; verify no side-effects (e.g. payroll/role gates) before LIVE.
- To CONFIRM:       read the discovery dump in Player.log, move the real field name to the front of
                    `SkillProbe.SkillNames`, then live-verify the in-game skill bar fills to full.
- Notes / risks:    EA build moved Mono→IL2CPP; the old `Entities.EmployeeInstance.skill` access in the
                    legacy `BAA.BigAmbitions` adapter returned `0f` (never wired), so the field is
                    genuinely unconfirmed on the current DLL — hence the reflection + discovery log.

## Checklist — Lifecycle
- [ ] **IGameState.IsWorldReady** (a save is loaded & playable) — TO-DISCOVER
- [ ] **IGameEvents.SaveLoaded / SaveUnloading** (reset engine + load per-save config) — TO-DISCOVER
