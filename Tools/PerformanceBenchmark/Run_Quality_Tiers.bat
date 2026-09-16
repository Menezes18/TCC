@echo off
setlocal
title TCC Performance Benchmark - QUALITY TIERS
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Quality
set "exitCode=%ERRORLEVEL%"
echo.
pause
exit /b %exitCode%
