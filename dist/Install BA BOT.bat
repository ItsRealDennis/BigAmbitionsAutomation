@echo off
REM ============================================================
REM   BA BOT - double-click me to install. A window will open.
REM ============================================================
set "PS=%~dp0files\Installer.ps1"
if not exist "%PS%" set "PS=%~dp0Installer.ps1"
start "" powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%PS%"
