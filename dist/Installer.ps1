<#  BA BOT - graphical installer (WinForms). Dark, branded, auto-detects the game.
    Everything ships in the zip: this installer copies the mod, and the "Install MelonLoader"
    button launches the bundled MelonLoader installer so there is nothing else to download. #>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }
$root = Split-Path -Parent $here   # zip root (this script lives in <root>\files)

# ---- palette ----
$cBg    = [Drawing.Color]::FromArgb(15,19,27)
$cPanel = [Drawing.Color]::FromArgb(22,29,43)
$cTxt   = [Drawing.Color]::FromArgb(233,238,246)
$cDim   = [Drawing.Color]::FromArgb(148,162,184)
$cCyan  = [Drawing.Color]::FromArgb(92,198,255)
$cGreen = [Drawing.Color]::FromArgb(84,220,142)
$cAmber = [Drawing.Color]::FromArgb(255,180,84)
$cRed   = [Drawing.Color]::FromArgb(224,90,79)
$cDark  = [Drawing.Color]::FromArgb(6,18,27)

# ---- locate bundled files no matter how the zip was extracted ----
function Find-Asset($name) {
    foreach ($d in @($here, $root, (Join-Path $here 'files'), (Join-Path $root 'files'))) {
        if ($d) { $p = Join-Path $d $name; if (Test-Path $p) { return $p } }
    }
    return $null
}
function Find-MLInstaller {
    foreach ($n in @('MelonLoader Installer.exe','MelonLoader.Installer.exe')) {
        $p = Find-Asset $n; if ($p) { return $p }
    }
    return $null
}

function Find-Game {
    $libs = @(); $steam = "C:\Program Files (x86)\Steam"
    if (Test-Path "$steam\steamapps\common") { $libs += "$steam\steamapps\common" }
    $vdf = "$steam\steamapps\libraryfolders.vdf"
    if (Test-Path $vdf) {
        foreach ($line in Get-Content $vdf) {
            $m = [regex]::Match($line, '"path"\s*"(.+?)"')
            if ($m.Success) {
                $c = Join-Path ($m.Groups[1].Value -replace '\\\\','\') "steamapps\common"
                if (Test-Path $c) { $libs += $c }
            }
        }
    }
    foreach ($lib in ($libs | Select-Object -Unique)) {
        $g = Join-Path $lib "Big Ambitions"
        if (Test-Path (Join-Path $g "Big Ambitions.exe")) { return $g }
    }
    return $null
}

# ---- form (borderless, draggable) ----
$form = New-Object Windows.Forms.Form
$form.Text = "BA BOT Installer"; $form.Size = New-Object Drawing.Size(500,548)
$form.FormBorderStyle = 'None'; $form.StartPosition = 'CenterScreen'
$form.BackColor = $cBg; $form.ForeColor = $cTxt
$form.Font = New-Object Drawing.Font("Segoe UI", 9.5)

# header
$header = New-Object Windows.Forms.Panel
$header.Size = New-Object Drawing.Size(500,70); $header.Location = '0,0'; $header.BackColor = $cPanel
$form.Controls.Add($header)
$logo = New-Object Windows.Forms.Label
$logo.Text = "BA"; $logo.Size = '38,38'; $logo.Location = '24,16'
$logo.TextAlign = 'MiddleCenter'; $logo.BackColor = $cCyan; $logo.ForeColor = $cDark
$logo.Font = New-Object Drawing.Font("Consolas",13,[Drawing.FontStyle]::Bold)
$header.Controls.Add($logo)
$title = New-Object Windows.Forms.Label
$title.Text = "BA BOT"; $title.Location = '72,15'; $title.AutoSize = $true
$title.Font = New-Object Drawing.Font("Segoe UI",15,[Drawing.FontStyle]::Bold); $title.ForeColor = $cTxt
$header.Controls.Add($title)
$tagline = New-Object Windows.Forms.Label
$tagline.Text = "Big Ambitions automation mod"; $tagline.Location = '74,42'; $tagline.AutoSize = $true
$tagline.ForeColor = $cDim; $tagline.Font = New-Object Drawing.Font("Segoe UI",8.5)
$header.Controls.Add($tagline)
$close = New-Object Windows.Forms.Label
$close.Text = [char]0x2715; $close.Size = '34,34'; $close.Location = '452,8'; $close.TextAlign = 'MiddleCenter'
$close.ForeColor = $cDim; $close.Font = New-Object Drawing.Font("Segoe UI",11)
$close.Add_Click({ $form.Close() })
$close.Add_MouseEnter({ $close.ForeColor = $cRed }); $close.Add_MouseLeave({ $close.ForeColor = $cDim })
$header.Controls.Add($close)
# drag
$script:drag = $false; $script:pt = New-Object Drawing.Point
$dragDown = { $script:drag = $true; $script:pt = New-Object Drawing.Point($_.X,$_.Y) }
$dragMove = { if ($script:drag) { $p=[Windows.Forms.Cursor]::Position; $form.Location = New-Object Drawing.Point(($p.X-$script:pt.X),($p.Y-$script:pt.Y)) } }
$dragUp   = { $script:drag = $false }
$header.Add_MouseDown($dragDown); $header.Add_MouseMove($dragMove); $header.Add_MouseUp($dragUp)
$title.Add_MouseDown($dragDown); $title.Add_MouseMove($dragMove); $title.Add_MouseUp($dragUp)

function Add-Label($text,$x,$y,$color,$size,$bold){
    $l = New-Object Windows.Forms.Label; $l.Text=$text; $l.Location="$x,$y"; $l.AutoSize=$true; $l.ForeColor=$color
    $st = if($bold){[Drawing.FontStyle]::Bold}else{[Drawing.FontStyle]::Regular}
    $l.Font = New-Object Drawing.Font("Segoe UI",$size,$st); $form.Controls.Add($l); return $l
}

Add-Label "GAME FOLDER" 24 88 $cCyan 8 $true | Out-Null
$txtPath = New-Object Windows.Forms.TextBox
$txtPath.Location = '24,110'; $txtPath.Size = '350,26'; $txtPath.BackColor = $cPanel; $txtPath.ForeColor = $cTxt
$txtPath.BorderStyle = 'FixedSingle'
$form.Controls.Add($txtPath)
$btnBrowse = New-Object Windows.Forms.Button
$btnBrowse.Text = "Browse"; $btnBrowse.Location = '384,109'; $btnBrowse.Size = '92,28'
$btnBrowse.FlatStyle='Flat'; $btnBrowse.FlatAppearance.BorderColor=$cDim; $btnBrowse.BackColor=$cPanel; $btnBrowse.ForeColor=$cTxt
$form.Controls.Add($btnBrowse)

Add-Label "REQUIREMENTS" 24 152 $cCyan 8 $true | Out-Null
$lblMelon = Add-Label "" 24 174 $cDim 9.5 $false
$lblIl2   = Add-Label "" 24 198 $cDim 9.5 $false
$lblSac   = Add-Label "" 24 222 $cDim 9.5 $false

# Step 1 - MelonLoader (launches the bundled installer)
$btnMelon = New-Object Windows.Forms.Button
$btnMelon.Location = '24,250'; $btnMelon.Size = '452,38'
$btnMelon.FlatStyle='Flat'; $btnMelon.FlatAppearance.BorderColor=$cCyan; $btnMelon.FlatAppearance.BorderSize=1
$btnMelon.BackColor=$cPanel; $btnMelon.ForeColor=$cCyan
$btnMelon.Font = New-Object Drawing.Font("Segoe UI",10.5,[Drawing.FontStyle]::Bold); $btnMelon.Cursor='Hand'
$btnMelon.Text = "1  -  Install MelonLoader"
$form.Controls.Add($btnMelon)

# Step 2 - BA BOT
$btnInstall = New-Object Windows.Forms.Button
$btnInstall.Text = "2  -  Install BA BOT"; $btnInstall.Location = '24,298'; $btnInstall.Size = '452,46'
$btnInstall.FlatStyle='Flat'; $btnInstall.FlatAppearance.BorderSize=0; $btnInstall.BackColor=$cCyan; $btnInstall.ForeColor=$cDark
$btnInstall.Font = New-Object Drawing.Font("Segoe UI",12,[Drawing.FontStyle]::Bold); $btnInstall.Cursor='Hand'
$form.Controls.Add($btnInstall)

$status = New-Object Windows.Forms.Label
$status.Location = '24,356'; $status.Size = '452,96'; $status.ForeColor = $cDim
$status.Font = New-Object Drawing.Font("Segoe UI",9)
$form.Controls.Add($status)

Add-Label "github.com/ItsRealDennis/BigAmbitionsAutomation   -   press F8 in-game" 24 510 ([Drawing.Color]::FromArgb(95,108,128)) 8 $false | Out-Null

function Refresh-Reqs {
    $g = $txtPath.Text
    $melonOk = (Test-Path (Join-Path $g "MelonLoader")) -and ((Test-Path (Join-Path $g "version.dll")) -or (Test-Path (Join-Path $g "winhttp.dll")))
    if ($melonOk) {
        $lblMelon.Text = [char]0x2713 + "  MelonLoader installed"; $lblMelon.ForeColor = $cGreen
        $btnMelon.Text = [char]0x2713 + "  MelonLoader installed"; $btnMelon.Enabled = $false
        $btnMelon.ForeColor = $cDim; $btnMelon.FlatAppearance.BorderColor = $cPanel
    } else {
        $lblMelon.Text = [char]0x2715 + "  MelonLoader NOT found - click step 1 below"; $lblMelon.ForeColor = $cRed
        $btnMelon.Text = "1  -  Install MelonLoader"; $btnMelon.Enabled = $true
        $btnMelon.ForeColor = $cCyan; $btnMelon.FlatAppearance.BorderColor = $cCyan
    }
    if (Test-Path (Join-Path $g "MelonLoader\Il2CppAssemblies\Il2CppBigAmbitions.dll")) {
        $lblIl2.Text = [char]0x2713 + "  Game assemblies generated"; $lblIl2.ForeColor = $cGreen
    } else { $lblIl2.Text = [char]0x2715 + "  Launch the game once after MelonLoader"; $lblIl2.ForeColor = $cAmber }
    try {
        $sac = (Get-ItemProperty 'HKLM:\SYSTEM\CurrentControlSet\Control\CI\Policy' -Name VerifiedAndReputablePolicyState -ErrorAction Stop).VerifiedAndReputablePolicyState
        if ($sac -eq 1) { $lblSac.Text = [char]0x2715 + "  Smart App Control is ON - turn it OFF or the mod is blocked"; $lblSac.ForeColor = $cRed }
        else { $lblSac.Text = [char]0x2713 + "  Smart App Control is off"; $lblSac.ForeColor = $cGreen }
    } catch { $lblSac.Text = [char]0x2713 + "  Smart App Control: not enforced"; $lblSac.ForeColor = $cGreen }
}

$btnBrowse.Add_Click({
    $dlg = New-Object Windows.Forms.FolderBrowserDialog
    $dlg.Description = "Select your 'Big Ambitions' folder"
    if ($dlg.ShowDialog() -eq 'OK') { $txtPath.Text = $dlg.SelectedPath; Refresh-Reqs }
})

$btnMelon.Add_Click({
    $ml = Find-MLInstaller
    if (-not $ml) {
        $status.ForeColor = $cRed
        $status.Text = "Couldn't find the MelonLoader installer next to this file. Extract the WHOLE zip, then try again.`r`n(Or get it from github.com/LavaGang/MelonLoader)"
        return
    }
    try {
        Start-Process -FilePath $ml | Out-Null
        $status.ForeColor = $cCyan
        $status.Text = "MelonLoader installer opened.`r`n  1) Pick 'Big Ambitions' and click INSTALL.`r`n  2) Launch the game once, then quit.`r`n  3) Come back here - the check turns green - and click step 2."
    } catch {
        $status.ForeColor = $cRed; $status.Text = "Couldn't launch MelonLoader installer: $($_.Exception.Message)"
    }
})

$btnInstall.Add_Click({
    $g = $txtPath.Text
    if (-not (Test-Path (Join-Path $g "Big Ambitions.exe"))) {
        $status.ForeColor = $cRed; $status.Text = "That folder has no 'Big Ambitions.exe'. Use Browse to pick the game folder."
        return
    }
    $modDll  = Find-Asset "BAA.Mod.dll"
    $coreDll = Find-Asset "BAA.Core.dll"
    if (-not $modDll -or -not $coreDll) {
        $status.ForeColor = $cRed; $status.Text = "BAA.Mod.dll / BAA.Core.dll not found. Extract the WHOLE zip (keep the 'files' folder next to this installer) and retry."
        return
    }
    try {
        $mods = Join-Path $g "Mods"; $userlibs = Join-Path $g "UserLibs"
        New-Item -ItemType Directory -Force -Path $mods,$userlibs | Out-Null
        Copy-Item $modDll  (Join-Path $mods "BAA.Mod.dll") -Force
        Copy-Item $coreDll (Join-Path $userlibs "BAA.Core.dll") -Force
        $melonOk = (Test-Path (Join-Path $g "MelonLoader")) -and ((Test-Path (Join-Path $g "version.dll")) -or (Test-Path (Join-Path $g "winhttp.dll")))
        $status.ForeColor = $cGreen
        if ($melonOk) {
            $status.Text = ([char]0x2713 + " Installed!  Launch Big Ambitions and press F8 to open BA BOT.")
        } else {
            $status.ForeColor = $cAmber
            $status.Text = ([char]0x2713 + " Mod copied - but MelonLoader isn't installed yet.`r`n   Click step 1 above (Install MelonLoader), or BA BOT won't load.")
        }
        $btnInstall.Text = "Installed  -  press F8 in-game"; $btnInstall.BackColor = $cGreen
    } catch {
        $status.ForeColor = $cRed; $status.Text = "Install failed: $($_.Exception.Message)`r`n(Close the game if it's running, then retry.)"
    }
})

# re-check requirements whenever the window regains focus (e.g. after using the MelonLoader installer)
$form.Add_Activated({ if ($txtPath.Text) { Refresh-Reqs } })

# init
$detected = Find-Game
if ($detected) { $txtPath.Text = $detected } else { $status.Text = "Couldn't auto-detect the game - click Browse to pick your Big Ambitions folder." }
Refresh-Reqs
[void]$form.ShowDialog()
