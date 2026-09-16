@echo off
setlocal
title TCC Performance Benchmark - TRANSITIONS
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Transition
set "exitCode=%ERRORLEVEL%"
echo.
pause
exit /b %exitCode%
