<#  BA BOT - uninstaller. Removes the mod files from Big Ambitions. #>
$ErrorActionPreference = 'Stop'

function Find-Game {
    $libs = @()
    $steam = "C:\Program Files (x86)\Steam"
    if (Test-Path "$steam\steamapps\common") { $libs += "$steam\steamapps\common" }
    $vdf = "$steam\steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
        foreach ($line in Get-Content $vdf) {
            $m = [regex]::Match($line, '"path"\s*"(.+?)"')
            if ($m.Success) {
                $common = Join-Path ($m.Groups[1].Value -replace '\\\\', '\') "steamapps\common"
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

$game = Find-Game
if (-not $game) { $game = Read-Host "Paste your Big Ambitions folder" }
$removed = $false
foreach ($f in @("$game\Mods\BAA.Mod.dll", "$game\UserLibs\BAA.Core.dll")) {
    if (Test-Path $f) { Remove-Item $f -Force; Write-Host "Removed $f" -ForegroundColor Green; $removed = $true }
}
if (-not $removed) { Write-Host "Nothing to remove (BA BOT not found in $game)." -ForegroundColor Yellow }
Write-Host "Your settings file (UserData\MelonPreferences.cfg) was left untouched."
