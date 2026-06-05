<#  BA BOT - graphical installer (WinForms). Dark, branded, auto-detects the game. #>
$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
[System.Windows.Forms.Application]::EnableVisualStyles()

$here = if ($PSScriptRoot) { $PSScriptRoot } else { Split-Path -Parent $MyInvocation.MyCommand.Path }

# ---- palette ----
$cBg    = [Drawing.Color]::FromArgb(15,19,27)
$cPanel = [Drawing.Color]::FromArgb(22,29,43)
$cTxt   = [Drawing.Color]::FromArgb(233,238,246)
$cDim   = [Drawing.Color]::FromArgb(148,162,184)
$cCyan  = [Drawing.Color]::FromArgb(92,198,255)
$cGreen = [Drawing.Color]::FromArgb(84,220,142)
$cRed   = [Drawing.Color]::FromArgb(224,90,79)
$cDark  = [Drawing.Color]::FromArgb(6,18,27)

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
$form.Text = "BA BOT Installer"; $form.Size = New-Object Drawing.Size(500,500)
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
$title.Text = "BA BOT"; $title.Location = '72,16'; $title.AutoSize = $true
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

Add-Label "GAME FOLDER" 24 90 $cCyan 8 $true | Out-Null
$txtPath = New-Object Windows.Forms.TextBox
$txtPath.Location = '24,112'; $txtPath.Size = '350,26'; $txtPath.BackColor = $cPanel; $txtPath.ForeColor = $cTxt
$txtPath.BorderStyle = 'FixedSingle'
$form.Controls.Add($txtPath)
$btnBrowse = New-Object Windows.Forms.Button
$btnBrowse.Text = "Browse"; $btnBrowse.Location = '384,111'; $btnBrowse.Size = '92,28'
$btnBrowse.FlatStyle='Flat'; $btnBrowse.FlatAppearance.BorderColor=$cDim; $btnBrowse.BackColor=$cPanel; $btnBrowse.ForeColor=$cTxt
$form.Controls.Add($btnBrowse)

Add-Label "REQUIREMENTS" 24 156 $cCyan 8 $true | Out-Null
$lblMelon = Add-Label "" 24 178 $cDim 9.5 $false
$lblIl2   = Add-Label "" 24 202 $cDim 9.5 $false
$lblSac   = Add-Label "" 24 226 $cDim 9.5 $false

$btnInstall = New-Object Windows.Forms.Button
$btnInstall.Text = "Install BA BOT"; $btnInstall.Location = '24,268'; $btnInstall.Size = '452,46'
$btnInstall.FlatStyle='Flat'; $btnInstall.FlatAppearance.BorderSize=0; $btnInstall.BackColor=$cCyan; $btnInstall.ForeColor=$cDark
$btnInstall.Font = New-Object Drawing.Font("Segoe UI",12,[Drawing.FontStyle]::Bold); $btnInstall.Cursor='Hand'
$form.Controls.Add($btnInstall)

$status = New-Object Windows.Forms.Label
$status.Location = '24,330'; $status.Size = '452,90'; $status.ForeColor = $cDim
$status.Font = New-Object Drawing.Font("Segoe UI",9)
$form.Controls.Add($status)

$foot = Add-Label "github.com/ItsRealDennis/BigAmbitionsAutomation   -   press F8 in-game" 24 452 ([Drawing.Color]::FromArgb(95,108,128)) 8 $false

function Refresh-Reqs {
    $g = $txtPath.Text
    $okGame = (Test-Path (Join-Path $g "Big Ambitions.exe"))
    if ((Test-Path (Join-Path $g "MelonLoader")) -and ((Test-Path (Join-Path $g "version.dll")) -or (Test-Path (Join-Path $g "winhttp.dll")))) {
        $lblMelon.Text = [char]0x2713 + "  MelonLoader installed"; $lblMelon.ForeColor = $cGreen
    } else { $lblMelon.Text = [char]0x2715 + "  MelonLoader NOT found - install it first"; $lblMelon.ForeColor = $cRed }
    if (Test-Path (Join-Path $g "MelonLoader\Il2CppAssemblies\Il2CppBigAmbitions.dll")) {
        $lblIl2.Text = [char]0x2713 + "  Game assemblies generated"; $lblIl2.ForeColor = $cGreen
    } else { $lblIl2.Text = [char]0x2715 + "  Launch the game once with MelonLoader first"; $lblIl2.ForeColor = [Drawing.Color]::FromArgb(255,180,84) }
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

$btnInstall.Add_Click({
    $g = $txtPath.Text
    if (-not (Test-Path (Join-Path $g "Big Ambitions.exe"))) {
        $status.ForeColor = $cRed; $status.Text = "That folder has no 'Big Ambitions.exe'. Use Browse to pick the game folder."
        return
    }
    try {
        $mods = Join-Path $g "Mods"; $userlibs = Join-Path $g "UserLibs"
        New-Item -ItemType Directory -Force -Path $mods,$userlibs | Out-Null
        Copy-Item (Join-Path $here "BAA.Mod.dll")  (Join-Path $mods "BAA.Mod.dll") -Force
        Copy-Item (Join-Path $here "BAA.Core.dll") (Join-Path $userlibs "BAA.Core.dll") -Force
        $status.ForeColor = $cGreen
        $status.Text = ([char]0x2713 + " Installed!  Launch Big Ambitions and press F8 to open BA BOT.`r`n   BAA.Mod.dll -> Mods\   |   BAA.Core.dll -> UserLibs\")
        $btnInstall.Text = "Installed  -  press F8 in-game"; $btnInstall.BackColor = $cGreen
    } catch {
        $status.ForeColor = $cRed; $status.Text = "Install failed: $($_.Exception.Message)`r`n(Close the game if it's running, then retry.)"
    }
})

# init
$detected = Find-Game
if ($detected) { $txtPath.Text = $detected } else { $status.Text = "Couldn't auto-detect the game - click Browse to pick your Big Ambitions folder." }
Refresh-Reqs
[void]$form.ShowDialog()
