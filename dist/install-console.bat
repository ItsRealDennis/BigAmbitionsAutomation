@echo off
REM Text-only installer (fallback if the graphical one won't open).
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0install.ps1"
echo.
pause
