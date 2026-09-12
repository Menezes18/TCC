TCC PORTABLE PERFORMANCE BENCHMARK

No Unity installation or project checkout is required on the test PC.

1. Extract the entire ZIP to a local SSD. Do not run it inside the ZIP.
2. Close games, browsers, overlays, recording software, and battery-saving mode.
3. Plug laptops into power and let the machine reach a stable temperature.
4. Run Tools\PerformanceAudit\Portable\Run-Core.cmd for the three main risk scenes,
   or Run-All.cmd for all seven representative scenes.
5. Send back the Results folder. Each run records CPU/GPU names, graphics-memory
   indication, OS, Unity version, settings, medians, p95, p99, and maximum values.

The default workload is Ultra, 1920x1080, render scale 1.0, fullscreen, 2-second
warm-up, and 2,000 measured frames. Keep these settings fixed when comparing PCs.
For a same-PC before/after fix, run the same launcher once before and once after;
the second run creates comparison.md against the previous matching run.

Run-Queda-Raw.cmd also saves a Unity Profiler .raw file and can use hundreds of MB.
The other launchers save compact summaries only.

This is a one-player host benchmark. It does not prove 4/6-player scaling.

Advanced: run Tools\PerformanceAudit\Run-PerformanceAudit.ps1 directly to select
another resolution, quality tier, render scale, or -GraphicsApi d3d11. D3D12 is
the default because it matches the original audit. Never compare runs made with
different graphics APIs or quality/display settings as a code-change result.
