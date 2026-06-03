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

## Known anchors (from research; verify in the actual DLL)
- `GameManager` — central logic. Static `Command_*`: `Command_ChangeMoney(float)`,
  `Command_ChangeHappiness(int)`, `Command_ChangeHunger(int)`, `Command_SetEnergy(float)`.
- `TimeMachine` — time-skip / speed multiplier.
- `BizMan` — businesses, inventory/pricing, warehouses, real estate (the "BizPhone" data layer).
- Employee data model — wage, satisfaction, age, skills, candidate generation.

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

## Checklist — Lifecycle
- [ ] **IGameState.IsWorldReady** (a save is loaded & playable) — TO-DISCOVER
- [ ] **IGameEvents.SaveLoaded / SaveUnloading** (reset engine + load per-save config) — TO-DISCOVER
