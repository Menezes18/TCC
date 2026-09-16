# TCC portable performance benchmark

This folder is a self-contained Windows benchmark. You do not need to open Unity or understand the project.

## Requirements

- Unity installed: **NO**
- Steam installed: **NO**
- Administrator rights: **NO**
- Internet connection: **NO**
- Manual Windows Firewall approval during PRE-FLIGHT: **YES, only if Windows displays the prompt**

Do not add firewall rules manually. The PRE-FLIGHT stage opens the same local KCP listeners used by the tests and is never included in measured transition times.

## Exact steps on another PC

1. Extract or copy the complete `PerformanceBenchmark` folder to a local drive. Do not run it inside the ZIP.
2. Plug a laptop into power.
3. Select the Windows power mode that you want recorded for this machine and keep it unchanged between runs.
4. Close games, browsers, launchers, downloads, recording software, overlays, and other unnecessary programs.
5. Double-click `Run_Preflight.bat` **first**.
6. Read the yellow message. If Windows asks for network access, click **Allow access** now. This interaction is not benchmarked.
7. Wait for `PRE-FLIGHT PASS`. If it fails, read the displayed path and open the corresponding `errors.log`.
8. Double-click `Run_Quick_Test.bat`.
9. Do not touch the mouse or keyboard while a game window says the measured test is running. Do not move, resize, minimize, or cover the window.
10. Wait for `QUICK TEST PASS` and the displayed result-folder path.
11. Run `Run_Full_Test.bat` only when the developer requests the complete matrix.
12. Open `Results`. The newest folder path is also written to `Results\LATEST.txt`.
13. ZIP the entire timestamped result folder and send that ZIP back to the developer. Do not send only `summary.md`.

If either Player executable changes, the next test refuses to start until PRE-FLIGHT is run again. This prevents a new Firewall prompt from contaminating a measurement.

## What each launcher does

- `Run_Preflight.bat`: opens Release-like and Diagnostic KCP listeners, confirms a local client can connect, gives Windows Firewall prompts a chance to appear, then cleans up. User interaction is allowed.
- `Run_Quick_Test.bat`: runs three fixed repeats of `MN_Run`, `MN_new_Rua`, and `MN_Queda`, plus a four-player `MN_Run` host scenario. It uses the Release-like Player. No interaction is allowed during measured portions.
- `Run_Full_Test.bat`: runs all seven scenes, three Release-like repeats, a separate Diagnostic lane, the 1/2/4-player matrix, Batata paths, phased transitions, and the two-cycle rotation/memory scenario. No interaction is allowed during measured portions.
- `Run_Batata_Reproduction.bat`: runs four players through Lobby-to-Batata, three-scenes-to-Batata, and a longer normal rotation-to-Batata, three times each. Its summary says `BATATA_REPRODUCTION: PASS` or `FAILED` with the reason.
- `Run_Transition_Test.bat`: records phase timestamps for a four-player scene rotation. The timer begins after KCP is already operational; Firewall approval is outside the trace.
- `Run_Quality_Tiers.bat`: optionally tests every existing tier on `MN_Run` with the same route, camera behavior, resolution, seed, and frame count. It records the resolved tier/settings and does not edit project quality assets.
- `Compare_Results.bat`: scans timestamped folders placed in `Results` and creates `Results\Comparison.md` plus `Results\Comparison.csv`. If it finds an RTX 3060 result, that result becomes the percentage baseline. A developer can also invoke the script with an explicit baseline folder.

## Successful output

Every measured execution creates a folder such as:

```text
Results\
  2026-09-20_21-30-15_DESKTOP-XYZ_Quick\
    hardware.json
    benchmark.csv
    benchmark.json
    transitions.csv
    transitions.json
    multiplayer.csv
    errors.log
    summary.md
    Runs\...
```

`benchmark.csv` and `benchmark.json` contain machine-readable frame, CPU, render, GPU, scripts, physics, GC, geometry, memory, network, build, role, scene, and run data. A counter that Unity or the current graphics backend does not expose is written as `N/A`; it is never invented.

`summary.md` gives the numbers first. It does not call a PC good or bad because this package does not define a performance budget.

## If a test fails

The runner has bounded timeouts and terminates every Player it launched. A failed run records its reason in `errors.log` and preserves all Player logs under `Runs`. Re-run PRE-FLIGHT if Windows displayed a network prompt, the executables changed, or the preflight marker is missing. Do not approve a Firewall prompt during a measured run; stop the run, approve it through PRE-FLIGHT, and start again.

## What to send back

Send the whole newest timestamped folder from `Results` as a ZIP. It must include `hardware.json`, all CSV/JSON files, `summary.md`, `errors.log`, and the `Runs` subfolder. The raw logs are needed to diagnose disconnects, missing counters, and Batata lifecycle failures.
