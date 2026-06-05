<#
  BA BOT - installer for Big Ambitions.
  Copies the mod into your Big Ambitions install. Auto-detects the game via Steam;
  falls back to asking for the path. Checks the prerequisites and tells you if any are missing.
#>

$ErrorActionPreference = 'Stop'
$here = $PSScriptRoot

function Write-Step($m) { Write-Host "  $m" -ForegroundColor Cyan }
function Write-Ok($m)   { Write-Host "  [OK] $m" -ForegroundColor Green }
function Write-Warn2($m){ Write-Host "  [!]  $m" -ForegroundColor Yellow }
function Write-Err2($m) { Write-Host "  [X]  $m" -ForegroundColor Red }

Write-Host ""
Write-Host "============================================" -ForegroundColor White
Write-Host "   BA BOT  -  Big Ambitions automation mod"  -ForegroundColor White
Write-Host "============================================" -ForegroundColor White
Write-Host ""

function Find-Game {
    $libs = @()
    $steam = "C:\Program Files (x86)\Steam"
    if (Test-Path "$steam\steamapps\common") { $libs += "$steam\steamapps\common" }
    $vdf = "$steam\steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
        foreach ($line in Get-Content $vdf) {
            $m = [regex]::Match($line, '"path"\s*"(.+?)"')
            if ($m.Success) {
                $p = $m.Groups[1].Value -replace '\\\\', '\'
                $common = Join-Path $p "steamapps\common"
                if (Test-Path $common) { $libs += $common }
            }
        }
    }
    foreach ($lib in ($libs | Select-Object -Unique)) {
        $g = Join-Path $lib "Big Ambitions"
        if (Test-Path (Join-Path $g "Big Ambitions.exe")) { return $g }
    }
    return $null
}

# 1. Locate the game
Write-Step "Locating Big Ambitions..."
$game = Find-Game
if (-not $game) {
    Write-Warn2 "Could not auto-detect the game."
    $game = Read-Host "  Paste your Big Ambitions folder (the one containing 'Big Ambitions.exe')"
}
if (-not (Test-Path (Join-Path $game "Big Ambitions.exe"))) {
    Write-Err2 "That folder doesn't contain 'Big Ambitions.exe'. Aborting."
    exit 1
}
Write-Ok "Game: $game"

# 2. Prerequisite checks (warn, don't hard-fail except the game itself)
$melon = Test-Path (Join-Path $game "MelonLoader")
$proxy = (Test-Path (Join-Path $game "version.dll")) -or (Test-Path (Join-Path $game "winhttp.dll"))
if ($melon -and $proxy) { Write-Ok "MelonLoader detected." }
else {
    Write-Warn2 "MelonLoader not detected. Install it first:"
    Write-Warn2 "  https://github.com/LavaGang/MelonLoader.Installer  (point it at 'Big Ambitions.exe')"
}

$il2 = Test-Path (Join-Path $game "MelonLoader\Il2CppAssemblies\Il2CppBigAmbitions.dll")
if (-not $il2) {
    Write-Warn2 "Il2Cpp assemblies not generated yet. Launch the game once with MelonLoader, then re-run this."
}

try {
    $sac = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name VerifiedAndReputablePolicyState -ErrorAction Stop).VerifiedAndReputablePolicyState
    if ($sac -eq 1) {
        Write-Warn2 "Windows Smart App Control is ON - it will BLOCK this (unsigned) mod."
        Write-Warn2 "  Turn it off: Windows Security > App & browser control > Smart App Control > Off."
    }
} catch { }

# 3. Copy the mod
Write-Step "Installing mod files..."
$mods = Join-Path $game "Mods"
$userlibs = Join-Path $game "UserLibs"
New-Item -ItemType Directory -Force -Path $mods, $userlibs | Out-Null

$modDll  = Join-Path $here "BAA.Mod.dll"
$coreDll = Join-Path $here "BAA.Core.dll"
if (-not (Test-Path $modDll) -or -not (Test-Path $coreDll)) {
    Write-Err2 "BAA.Mod.dll / BAA.Core.dll not found next to this script. Extract the whole zip and run again."
    exit 1
}
Copy-Item $modDll  (Join-Path $mods "BAA.Mod.dll") -Force
Copy-Item $coreDll (Join-Path $userlibs "BAA.Core.dll") -Force
Write-Ok "Copied BAA.Mod.dll  -> Mods\"
Write-Ok "Copied BAA.Core.dll -> UserLibs\"

Write-Host ""
Write-Host "  Done! Launch Big Ambitions and press F8 in-game to open BA BOT." -ForegroundColor Green
Write-Host ""
