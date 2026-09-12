# Repeatable Performance Audit Runner

This runner reproduces the audit's one-player Development Player workflow. It starts a local KCP host through the normal network flow, loads the requested scene with the synchronized transition, acknowledges and completes the briefing, waits for the live match, drives deterministic movement, warms up, and records a fixed frame window. The bootstrap is completely inert without its command-line flag and is excluded from non-development builds.

## Easiest use while Unity is open

Use either Editor menu command:

- **Tools > Performance Audit > Run Core Scenes (raw captures)**: `MN_Queda`, `MN_Run`, and `MN_new_Rua`, including Unity `.raw` Profiler files.
- **Tools > Performance Audit > Run All 7 Scenes (summaries)**: all representative scenes, without multi-gigabyte raw captures.
- **Tools > Performance Audit > Create Portable Benchmark ZIP**: builds a self-contained package for PCs without Unity or the repository.

Both commands rebuild first. A visible PowerShell window runs the Players sequentially. If a previous run exists for the same scenes, `comparison.md` compares p99 values and labels changes beyond ±5% as signals to investigate—not proof from a single run.

The reusable local Development Player is stored at `Builds/PerformanceAuditPlayer/`. It intentionally lives outside Unity's `Temp` folder because batch-mode Unity deletes `Temp` during shutdown.

## Terminal use

Close this Unity project before asking the script to rebuild:

```powershell
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -Scenes MN_Queda -Tag before-fix
```

After the fix:

```powershell
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -Scenes MN_Queda -Tag after-fix -CompareWithPrevious -RawCapture
```

Useful variants:

```powershell
# All representative scenes, summary data only
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -All -CompareWithPrevious

# Reuse the already-built Player while Unity remains open
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -Scenes MN_Run,MN_Queda -SkipBuild -CompareWithPrevious

# Short smoke validation of the automation itself
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -Scenes MN_Sumo -Frames 120 -WarmupSeconds 1 -SkipBuild

# Compatibility run for an older Direct3D 11 PC (compare only with other D3D11 runs)
.\Tools\PerformanceAudit\Run-PerformanceAudit.ps1 -Scenes MN_Queda -GraphicsApi d3d11 -Quality Balanced -Width 1280 -Height 720
```

Outputs are written under `Temp/PerformanceAuditRuns/<timestamp>-<tag>/`:

- `run-summary.json`: combined machine-readable results;
- `<scene>/summary.json`: median, p95, p99, and max per available metric;
- `<scene>/Player.log`: exact startup, resolved settings, READY, and completion evidence;
- `<scene>/<scene>.raw`: optional Unity Profiler capture when `-RawCapture` is used;
- `comparison.md`: automatic p99 comparison against the previous matching run.

Keep hardware, resolution, quality, scene list, frame count, warm-up, and gameplay phase fixed between before/after runs. Repeat any apparent improvement or regression at least three times before treating it as real. Draw calls, batches, Scripts, or Physics are retained only when Unity exposes their recorder on the current backend; unavailable counters are listed explicitly in each summary.

## Portable low-end-PC package

Choose **Tools > Performance Audit > Create Portable Benchmark ZIP**. The result is `Builds/PerformanceAuditPortable/TCC-Performance-Benchmark.zip`. Copy it to another Windows PC, extract it to a local SSD, and use the included `.cmd` launchers. Unity and the source project are not required. Results go beside the Player in the package's `Results` folder and include the detected CPU, GPU, graphics-memory indicator, OS, Unity version, and resolved test settings.

Cross-PC results describe hardware scalability; they must not be mixed into a same-hardware code-change comparison. For before/after validation, run both builds on the same PC under the same power mode, resolution, quality, render scale, scene set, and thermal conditions.
