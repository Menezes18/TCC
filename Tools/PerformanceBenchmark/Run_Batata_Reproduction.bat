@echo off
setlocal
title TCC Performance Benchmark - BATATA REPRODUCTION
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Batata
set "exitCode=%ERRORLEVEL%"
echo.
type "%~dp0Results\LATEST.txt" 2>nul
pause
exit /b %exitCode%
