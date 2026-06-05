@echo off
REM Double-click me to install BA BOT (opens the BA BOT installer window).
start "" powershell -NoProfile -ExecutionPolicy Bypass -WindowStyle Hidden -File "%~dp0Installer.ps1"
