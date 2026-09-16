@echo off
setlocal
title TCC Performance Benchmark - QUICK TEST
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Quick
set "exitCode=%ERRORLEVEL%"
echo.
if "%exitCode%"=="0" (echo QUICK TEST PASS) else (echo QUICK TEST FAILED - results contain the reason.)
pause
exit /b %exitCode%
