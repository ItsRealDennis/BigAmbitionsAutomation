# Big Ambitions Automation ("BA BOT")

An internal automation mod for **[Big Ambitions](https://store.steampowered.com/app/2127390/Big_Ambitions/)**
(Hovgaard Games) — a Unity **IL2CPP** business sim. Built as a **MelonLoader + HarmonyLib** mod in C#,
with an in-game control panel (press **F8**). The goal is to automate the game's tedious loops
(restocking, logistics, finance, employees, AFK time-skip) by reading true game state and calling the
game's own methods — not by screen-scraping.

> ⚠️ Early work-in-progress, single-player, for a game in Early Access. Mod at your own risk; back up
> saves. Not affiliated with Hovgaard Games.

## Status

**Working in-game today**
- F8 control panel styled to match the game (rounded slate panels, bold labels, ON/OFF switches).
- Live status: cash, day/time, net worth (read from `SaveGameManager.Current` / `GameInstance`).
- **Quick actions:** `+$1,000`, `Energy 100%` (instant, prove the write path).
- **Time-skip (AFK):** fast-forwards in-game time (`GameManager.MinutesMultiplier`), restores on off.
- **Auto-wellbeing:** keeps player energy topped up automatically.
- **Click-through blocking:** panel clicks don't leak into the 3D world (Harmony prefix on
  `MouseController.Run`).
- Settings persist across sessions (MelonPreferences).

**In progress**
- Auto-restock (the headline feature), then logistics, employees, finance, full safety-gated time-skip.

## Architecture

Split so the testable "brain" never touches the volatile game API:

| Project | TFM | Role |
|---|---|---|
| `src/BAA.Core` | net6.0 | The brain. **Zero game references.** Orchestration engine, safety gate + breakers, managers, config, adapter interfaces (`IGameState`/`IGameCommands`/`IGameEvents`/`IGameClock`). Pure + unit-tested. |
| `src/BAA.Mod` | net6.0 | The MelonLoader mod. The **only** project that references the game/IL2CPP — adapter, Harmony hooks, IMGUI overlay, write actions. |
| `tests/BAA.Core.Tests` | net8.0 | xUnit tests for the brain against in-memory fakes (18 green). Runs with no game installed. |
| `tools/ApiDump` | net8.0 | Dev helper: dumps the game's full type/method/field surface (via MetadataLoadContext) for API discovery. |

Safety model: **default-OFF**, plan→gate→apply with a reserve-floor shared budget, halt-all breakers
(low funds / unpaid rent / empty inventory), and every Harmony patch wrapped so a fault can never crash
the game. See `docs/API-MAP.md` (discovered game API) and `docs/UPDATE-RUNBOOK.md` (re-validating after
a game patch).

## Build & install

Requirements: Windows, Big Ambitions installed, **MelonLoader 0.7.x**, **.NET 8 SDK** (builds the
`net6.0` mod), and **Smart App Control OFF** (Windows blocks unsigned mod DLLs otherwise).

```powershell
# Build (set BIG_AMBITIONS_DIR if the game isn't at the default Steam path)
dotnet test  tests/BAA.Core.Tests/BAA.Core.Tests.csproj      # 18 tests, no game needed
dotnet build src/BAA.Mod/BAA.Mod.csproj -c Release

# Install: copy outputs into the game folder
#   bin/Release/net6.0/BAA.Mod.dll   -> <Big Ambitions>/Mods/
#   bin/Release/net6.0/BAA.Core.dll  -> <Big Ambitions>/UserLibs/
```

Launch the game once with MelonLoader installed first, so it generates
`MelonLoader/Il2CppAssemblies/Il2CppBigAmbitions.dll` (the mod references it). Press **F8** in-game.

## License

Personal project; no license granted yet.
