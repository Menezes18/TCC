@echo off
setlocal
title TCC Performance Benchmark - PRE-FLIGHT
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Preflight
set "exitCode=%ERRORLEVEL%"
echo.
if "%exitCode%"=="0" (echo PRE-FLIGHT PASS) else (echo PRE-FLIGHT FAILED - see the message above.)
pause
exit /b %exitCode%
