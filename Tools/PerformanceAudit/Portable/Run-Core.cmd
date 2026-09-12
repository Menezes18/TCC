@echo off
setlocal
set "BENCH_ROOT=%~dp0"
if not exist "%BENCH_ROOT%Player\TCC.exe" set "BENCH_ROOT=%~dp0..\..\..\"
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%BENCH_ROOT%Tools\PerformanceAudit\Run-PerformanceAudit.ps1" -Scenes "MN_Queda,MN_Run,MN_new_Rua" -PlayerPath "%BENCH_ROOT%Player\TCC.exe" -OutputRoot "%BENCH_ROOT%Results" -SkipBuild -CompareWithPrevious -PauseAtEnd
