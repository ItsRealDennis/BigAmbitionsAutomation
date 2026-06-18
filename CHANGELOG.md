# Changelog

All notable changes to **BA BOT** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Fixed
- **Auto-Restock now actually restocks mid/large shops.** The target was a flat **20 total units** per
  product (counting backroom stock), so any shop already holding ≥20 of an item bought nothing. Restock
  now fills each product up to the **combined capacity of the shelves that sell it** (read live from the
  game, same model as the low-stock warning); falls back to the flat target if capacity can't be read.
  New **RESTOCK FULL** toggle in the AUTO tab (on by default) — turn it off to keep the old flat
  Restock Target behaviour. Still preview-first and reserve-floor gated.

## [0.10.0] — 2026-06-17

### Added
- **Rebindable panel hotkey.** The overlay key is no longer hardcoded — change it in the panel
  (HOME tab → **Open panel** → **Rebind**, then press any key; Esc cancels). Defaults to **F8** and
  is persisted across sessions.

### Fixed
- **Employees feature now bonuses the staff who actually need it.** `GameStateAdapter` normalized the
  game's 0–100 `satisfaction` to the engine's 0–1 scale with a `sat > 1.5` guard, which skipped the
  divide for staff at 0–1.5% morale — the ones about to resign — so the engine read them as ~100%
  happy and never gave the morale bonus. Satisfaction is now always normalized and clamped to 0–1.

### Removed
- **Legacy MelonLoader/IL2CPP build (`BAA.Mod`)** and its end-user installer scripts (`dist/`,
  `tools/pack.ps1`). BA BOT now ships solely through the official Steam Workshop mod API
  (`BAA.BigAmbitions`); the old manual-install path is gone.

## [0.9.0] — 2026-06-15

### Added
- Official **Big Ambitions EA 0.11 (Mono) Workshop** mod build (`BAA.BigAmbitions`).
- **F8 control panel** with HOME / AUTO / STAFF / LOG tabs; preview-first automation behind a safety
  gate + breakers (low funds / unpaid rent / empty inventory), default-OFF, reserve-floor gated.
- Automation: **Finance auto-pay**, **Employees** (morale + training), **Auto-Restock**,
  **Logistics auto-import**, **Auto-Pricing**, **Auto-Wellbeing**, and **Turbo** time-skip.
- **SKILLS panel** (train staff to 100%), **earlier low-stock alert** (Harmony transpiler on the
  game's 25% threshold), and an opt-in **service-fee** challenge.
- English & Dansk; settings persisted outside the save (uninstall loads vanilla clean).

[Unreleased]: https://github.com/ItsRealDennis/BigAmbitionsAutomation/compare/v0.10.0...HEAD
[0.10.0]: https://github.com/ItsRealDennis/BigAmbitionsAutomation/compare/v0.9.0...v0.10.0
