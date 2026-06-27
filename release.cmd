@echo off
cd /d "%~dp0"
pwsh -ExecutionPolicy Bypass -File "%~dp0release.ps1"
pause
