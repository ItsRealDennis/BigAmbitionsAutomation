<#
  Builds the official BA BOT mod and deploys it to the game's local mods folder for in-game testing.

  Layout required by Big Ambitions (EA 0.11):
    <persistentData>\ModsLocal\BA BOT\
        BAA.BigAmbitions.dll        <- EXACTLY ONE dll in the folder root (the mod assembly)
        Dependencies\
            BAA.Core.dll            <- everything else goes here

  persistentData = %USERPROFILE%\AppData\LocalLow\Hovgaard Games\Big Ambitions
  After deploying, alt-tab into the game (focus triggers re-discovery) or reload the city.

  Usage:  powershell -ExecutionPolicy Bypass -File tools\deploy-mod.ps1
#>
param([string]$Config = "Release")
$ErrorActionPreference = 'Stop'

$repo = Split-Path -Parent $PSScriptRoot
$modProj  = Join-Path $repo "src\BAA.BigAmbitions\BAA.BigAmbitions.csproj"
# The mod sets AppendTargetFrameworkToOutputPath=false (DLL straight in bin\Config); Core keeps the TFM subfolder.
$modDll   = Join-Path $repo "src\BAA.BigAmbitions\bin\$Config\BAA.BigAmbitions.dll"
$coreDll  = Join-Path $repo "src\BAA.Core\bin\$Config\netstandard2.1\BAA.Core.dll"

$dotnet = "C:\Program Files\dotnet\dotnet.exe"
if (-not (Test-Path $dotnet)) { $dotnet = "dotnet" }

Write-Host "Building $modProj ($Config)..." -ForegroundColor Cyan
& $dotnet build $modProj -c $Config --nologo
if ($LASTEXITCODE -ne 0) { throw "Build failed." }

foreach ($d in @($modDll, $coreDll)) { if (-not (Test-Path $d)) { throw "Missing build output: $d" } }

$dest = Join-Path $env:USERPROFILE "AppData\LocalLow\Hovgaard Games\Big Ambitions\ModsLocal\BA BOT"
$deps = Join-Path $dest "Dependencies"

# Fresh folder so the root never ends up with more than one dll (the discovery rule).
if (Test-Path $dest) { Remove-Item $dest -Recurse -Force }
New-Item -ItemType Directory -Force -Path $deps | Out-Null

Copy-Item $modDll  $dest -Force          # the single root mod assembly
Copy-Item $coreDll $deps -Force          # the brain, as a dependency

# Optional: thumbnail / locales / asset bundles get copied here later when they exist.
$loc = Join-Path $repo "src\BAA.BigAmbitions\Locales"
if (Test-Path $loc) { Copy-Item $loc (Join-Path $dest "Locales") -Recurse -Force }

Write-Host "Deployed to: $dest" -ForegroundColor Green
Get-ChildItem $dest -Recurse | Select-Object FullName
Write-Host "`nAlt-tab into Big Ambitions (or reload the city) to load the mod. Check the in-game mods list." -ForegroundColor Yellow
