@echo off
:: Batch script to elevate and run the PowerShell installer
NET SESSION >nul 2>&1
if %errorLevel% == 0 (
    echo Administrator privileges confirmed.
    powershell.exe -ExecutionPolicy Bypass -NoProfile -File "%~dp0install.ps1"
) else (
    echo Requesting Administrator privileges...
    powershell -Command "Start-Process cmd -ArgumentList '/c %~dp0run_installer.bat' -Verb RunAs"
)
