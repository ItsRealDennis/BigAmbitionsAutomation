<#
  Builds the user-facing release zip with a clean, two-installer layout:

      BA-BOT-vX.zip
        Install BA BOT.bat          <- the only thing users click
        MelonLoader Installer.exe   <- bundled; the installer above can launch it
        READ ME FIRST.txt
        files\                       <- installer guts (kept out of the way)
          Installer.ps1  BAA.Mod.dll  BAA.Core.dll
          install.ps1  install-console.bat  uninstall.bat  uninstall.ps1

  Run from the repo root:  powershell -ExecutionPolicy Bypass -File tools\pack.ps1 -Version 0.5.0
  Requires the mod to be built in Release and the GitHub CLI (gh) for the first MelonLoader fetch.
#>
param([string]$Version = "0.5.0")
$ErrorActionPreference = 'Stop'

$repo  = Split-Path -Parent $PSScriptRoot
$dist  = Join-Path $repo "dist"
$out   = Join-Path $repo "build"
$modRel  = Join-Path $repo "src\BAA.Mod\bin\Release\net6.0\BAA.Mod.dll"
$coreRel = Join-Path $repo "src\BAA.Core\bin\Release\net6.0\BAA.Core.dll"
foreach ($d in @($modRel,$coreRel)) { if (-not (Test-Path $d)) { throw "Missing $d - build the mod in Release first." } }

$stage = Join-Path $out "stage"
$files = Join-Path $stage "files"
if (Test-Path $stage) { Remove-Item $stage -Recurse -Force }
New-Item -ItemType Directory -Force -Path $files | Out-Null

# --- zip root: the two installers + a friendly readme ---
Copy-Item (Join-Path $dist "Install BA BOT.bat") $stage -Force
Copy-Item (Join-Path $dist "READ-ME-FIRST.txt") (Join-Path $stage "READ ME FIRST.txt") -Force

# bundled MelonLoader installer (cached under build\ml; fetched once via gh)
$ml = Join-Path $out "ml\MelonLoader.Installer.exe"
if (-not (Test-Path $ml)) {
    New-Item -ItemType Directory -Force -Path (Split-Path $ml) | Out-Null
    gh release download --repo LavaGang/MelonLoader --pattern "MelonLoader.Installer.exe" --dir (Split-Path $ml) --clobber
}
Copy-Item $ml (Join-Path $stage "MelonLoader Installer.exe") -Force

# --- files\: installer guts, kept out of the way ---
Copy-Item (Join-Path $dist "Installer.ps1")        $files -Force
Copy-Item (Join-Path $dist "install.ps1")          $files -Force
Copy-Item (Join-Path $dist "install-console.bat")  $files -Force
Copy-Item (Join-Path $dist "uninstall.bat")        $files -Force
Copy-Item (Join-Path $dist "uninstall.ps1")        $files -Force
Copy-Item $modRel  $files -Force
Copy-Item $coreRel $files -Force

$zip = Join-Path $out "BA-BOT-v$Version.zip"
if (Test-Path $zip) { Remove-Item $zip -Force }
Compress-Archive -Path (Join-Path $stage '*') -DestinationPath $zip -Force
Write-Host "Built $zip" -ForegroundColor Green
Get-ChildItem -Recurse $stage | Select-Object FullName
