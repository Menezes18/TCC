@echo off
setlocal
title TCC Performance Benchmark - COMPARE RESULTS
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Compare-Results.ps1" -ResultsRoot "%~dp0Results"
set "exitCode=%ERRORLEVEL%"
echo.
if "%exitCode%"=="0" echo Comparison.md and Comparison.csv were created in Results.
pause
exit /b %exitCode%
