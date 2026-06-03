# Update Runbook — re-validating after a Big Ambitions patch

Each Early-Access update recompiles the game and **remaps the IL2CPP assembly** (member tokens /
obfuscated names shift). The Core brain and its tests never reference IL2CPP, so they stay green;
only the three game-referencing projects (Adapter / UI / Mod) can break. Recovery is mechanical:

1. **Back up saves** (copy the `SaveGames` folder).
2. **Regenerate assemblies:** delete `<Game>\MelonLoader\Il2CppAssemblies\`, launch the game once to
   the main menu (MelonLoader/Il2CppInterop regenerates the proxies), then quit.
3. **Re-reference:** refresh the DLL copies in `build/refs/` from the regenerated `Il2CppAssemblies\`
   (and `MelonLoader\net6\`).
4. **Run the health check:** start the game with the mod; `ApiHealthCheck` logs which `API-MAP.md`
   entries no longer resolve.
5. **Re-discover only the broken entries:** for each, re-run the discovery loop in `docs/API-MAP.md`
   (see "How to discover"); update type/member/field + Status + game version.
6. **Rebuild + verify:**
   - off-game: `dotnet test` (should stay green — proves the brain is intact);
   - in-game: money-invariant, toggle-off, and breaker drills (see the plan's Verification section).
7. **Commit** with the new game version in the message.

If an entry can't be re-found, mark it **NOT-FOUND** — the adapter returns
`CommandResult.Skipped("api-not-found")` and the rest of the mod keeps working.

## Environment note (this machine)
- **Smart App Control is OFF** (required — it blocks unsigned mod DLLs and MelonLoader). It cannot be
  re-enabled without resetting Windows. Standard Microsoft Defender AV remains on.
- Build with the **`dotnet` CLI** (`C:\Program Files\dotnet`); the VS2019 BuildTools are .NET-Framework-only.
- Tests target **net8.0** (run on the installed .NET 8 runtime) and reference the **net6.0** Core.
