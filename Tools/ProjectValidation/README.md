# TCC Project Validation

`Run-ProjectValidation.ps1` is the single entry point for the release validation matrix. It reuses the project's existing EditMode tests, scene rules, Windows Development Player builder, KCP development transport, and performance capture rather than maintaining parallel implementations.

## Profiles

Run from the repository root:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ProjectValidation\Run-ProjectValidation.ps1 -Profile EveryChange
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ProjectValidation\Run-ProjectValidation.ps1 -Profile PreRelease
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ProjectValidation\Run-ProjectValidation.ps1 -Profile Performance
```

- `EveryChange`: Unity compilation gate, all EditMode tests, and all enabled Build Settings scene rules. It does not build or launch players; Mirror weaving is covered by profiles that build a player.
- `PreRelease` (default): every-change gates, Windows Development Player build, then required localhost KCP smoke matrices with 2 and 4 player processes plus an informational 6-player capacity probe.
- `Performance`: pre-release gates plus the established 2,000-frame captures for `MN_Queda`, `MN_Run`, and `MN_new_Rua`.

The Unity menu `Tools > TCC > Run Project Validation` starts the default pre-release profile. When Unity is already open, the script sends the Editor a file-based request and waits for an explicit completion marker; it never opens a second Editor against the same Library. With Unity closed, it uses batch mode.

`-SkipBuild` reuses an existing player after still running the Editor gates. `-SkipEditor -SkipBuild` is intended only for rapid runtime-harness iteration against the exact freshly validated player; it is not a complete release result.

Six players are not part of the current supported configuration. The default run records that cluster as `optional-capacity-signal` and does not fail the release result when it cannot admit six players. Pass `-RequireSixPlayers` only after six-player support becomes a product requirement.

## Automated coverage

The Editor stage verifies script compilation, exercises Mirror weaving through the player build, runs all EditMode tests, validates every enabled scene, and produces a Development Player. The runtime stage uses KCP on localhost and checks:

- 2-player, current-target 4-player, and future-target 6-player admission;
- synchronized transition from `Offline`/hub flow into `MN_Run`;
- normal briefing Ready path and minigame start;
- one client leaving and reconnecting during the match;
- synchronized voting with accepted participant votes;
- server-authoritative non-permanent death, observer-visible death state, respawn, and presentation restoration;
- permanent death, synchronized spectator entry, and spectator-state exit on respawn;
- a same-scene minigame reload, second briefing/Ready cycle, and restarted match timer;
- results overlay loading on every process;
- per-process frame p95, managed/Unity memory start, end, growth, and peak values, best-effort process working-set peak, scene-load duration, Mirror totals, and a per-message-type byte/count breakdown.

Every run writes `Logs/ProjectValidationRuns/<timestamp>/validation-summary.json`. `Logs` is ignored by Git and, unlike Unity's project-local `Temp` directory, survives cold batch-Editor startup and shutdown. Process logs and machine-readable runtime summaries sit below that directory. Failures are preserved in `validation-failure.json`; unrelated failures are reported, not repaired by this runner.

The runtime error gate remains strict for Error, Exception, and Assert logs. Unity's built-in video module emits a small, fixed set of shader errors when a smoke process intentionally uses `NullGfxDevice`; the runner records those separately as `ignoredHeadlessGraphicsErrors` only when the device is exactly `Null Device`. The same messages on a graphical device, and every unrelated error, still fail the gate. A graphical D3D11 run is required when validating actual video rendering.

## Performance regression baselines

Pass a successful prior run directory or its `validation-summary.json`:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File .\Tools\ProjectValidation\Run-ProjectValidation.ps1 -Profile PreRelease -Baseline .\Logs\ProjectValidationRuns\20260914-120000 -RegressionTolerancePercent 10 -FailOnRegression
```

Runtime comparisons are only made for the same player count, role, CPU, and GPU. The compared signals are frame p95; managed and Unity-allocated memory peaks and growth; scene-load duration; and Mirror inbound/outbound bytes. A first run establishes a baseline. Treat a single regression as a signal to repeat and profile, not as proof of a code-level cause.

The `Performance` profile additionally uses `Tools/PerformanceAudit/Run-PerformanceAudit.ps1`, whose scene/settings/hardware matching rules remain authoritative for CPU/GPU/rendering comparisons. Its timestamped history is shared under `Logs/ProjectValidationRuns/_PerformanceCaptures`, allowing `-CompareWithPrevious` to select a genuinely prior matching run; the validation summary records the exact `performanceArtifact`. Raw Unity profiler captures can still be requested directly through that specialized runner.

## Scope boundary

Local KCP validates game logic and synchronization without Steam matchmaking. It cannot prove lobby callbacks, invitations, overlay behavior, Steam identity, host migration, or behavior across real WAN conditions. Complete `MANUAL_RELEASE_CHECKLIST.md` before release and attach its evidence to the automated run.
