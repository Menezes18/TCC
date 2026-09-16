@echo off
setlocal
title TCC Performance Benchmark - FULL TEST
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0Scripts\Run-Benchmark.ps1" -Mode Full
set "exitCode=%ERRORLEVEL%"
echo.
if "%exitCode%"=="0" (echo FULL TEST PASS) else (echo FULL TEST FAILED - results contain the reason.)
pause
exit /b %exitCode%
