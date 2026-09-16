# Building or updating the portable package

This file is for the project developer. Testers should use `README_FIRST.md`.

The package is generated from the enabled Windows scenes and contains two Players:

- `Builds\Release\TCC.exe`: non-Development, primary Release-like measurement lane.
- `Builds\Diagnostic\TCC.exe`: Development lane for Profiler counters, allocation attribution, and failure diagnosis.

Both Players define `TCC_PERFORMANCE_BENCHMARK`. That compiles the opt-in benchmark bootstrap without enabling it during an ordinary launch. Normal shipping builds do not define it, so the bootstrap is absent. Even in a benchmark Player, the bootstrap is inert unless `--performance-audit-scene` is supplied.

## Unity menu

With the project open in Unity 6000.4.6f1, choose:

`Tools > Performance Benchmark > Build Complete Portable Package`

Outputs:

```text
Builds\PerformanceBenchmark\PerformanceBenchmark\
Builds\PerformanceBenchmark\TCC-PerformanceBenchmark.zip
```

## Command line

Close the Unity Editor for this project, then run the installed Unity executable with:

```powershell
Unity.exe -batchmode -quit -projectPath "C:\path\to\TCC" -executeMethod PerformanceAuditBuild.BuildCompletePortablePackageFromCommandLine -logFile "C:\path\to\TCC\Logs\PerformanceBenchmarkBuild.log"
```

After updating the package, run `Run_Preflight.bat` again because executable hashes change. Validate the quick test from an extracted copy of the ZIP, not only from the source staging folder.
