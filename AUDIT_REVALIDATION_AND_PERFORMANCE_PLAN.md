# Current audit revalidation and performance improvement plan

Date: 2026-09-15  
Unity: 6000.4.6f1  
Measured machine: Windows 11, Ryzen 7 5700G, RTX 3060 12 GB, D3D12  
Measured settings: Ultra, 1920x1080 fullscreen, render scale 1.0  

## Scope and decision boundary

This report revalidates the current project against `UNITY_AUDIT.md` and `PERFORMANCE_AUDIT.md`. It distinguishes source inspection, automated tests, functional multi-process evidence, and profiling evidence. No gameplay fix, architectural refactor, or quality-setting change was made. The only project-source changes made for this pass are opt-in diagnostic instrumentation in `PerformanceAuditBootstrap.cs` and a multiplayer benchmark runner. They are inert unless the performance-audit command-line arguments are present.

The current measurements are Development Player measurements. They are appropriate for relative comparisons and call-stack attribution, but Development-only IMGUI allocations must not be presented as shipping-build allocations. The RTX 3060 result is not representative of the entry-level target by itself.

## Executive result

- No critical performance blocker was measured for the current four-player target on the test PC.
- The 13 prior High findings in `UNITY_AUDIT.md` now have real implementation changes in the current source. They are not merely checked off in the document.
- The prior Medium findings also have corresponding targeted corrections. Two architectural remnants remain worth addressing incrementally: the base minigame lifecycle still does not enforce cleanup as a contract (M02), and an unused global ScriptableObject-event clearing method remains in `MyNetworkManager` after its dangerous call path was removed (M43).
- The current regression suite passed 21/21 EditMode tests. A two- and four-process KCP validation had already passed the functional flow. Those results do not prove Steam authentication/transport, dedicated-server results flow, adversarial command behavior, or six-player capacity.
- The measured `MN_Run` 10 KB/frame and `MN_Memoria` 17.4 KB/frame allocations are overwhelmingly Development-build Mirror/KCP `OnGUI` diagnostics. They are not evidence of a shipping GC problem.
- At Ultra/1080p, representative GPU p95 values were far below 16.67 ms. No graphics default should be reduced from this machine's data.
- Four-player scaling is healthy in absolute frame time, but host output traffic grows with fan-out as expected. It remains small in this test and should be monitored rather than optimized speculatively.
- Six players currently cannot connect because the startup NetworkManager in `offline.unity` has `maxConnections: 4`. This is correct for today's four-player target but is a concrete prerequisite for the proposed future target.
- A ten-transition soak did not show monotonic used-memory growth. After returning to the lobby and explicitly unloading unused assets, Unity allocated memory was 37.25 MB below the initial `MN_Run` snapshot. Unity reserved memory remained 81 MB above the initial value without corresponding used-memory growth, which is allocator retention, not proof of a leak.
- A separate all-scene soak reached `MN_BatataQ` and failed because the active match timer did not start in that repeated single-player sequence. A fresh single-player `MN_BatataQ` capture succeeds. This is suspicious lifecycle evidence, but the single-player fixture is not representative of the minigame's multiplayer rules; reproduce with four players before changing code.

## Audit-item revalidation

### Prior High findings H01-H13

| IDs | Current implementation evidence | Status / remaining proof |
|---|---|---|
| H01, H03 | `PlayerActiveFrame.CmdRequestPush` validates spawned identity, attacker state, cooldown, range, finite direction and duplicate victims. `ChatManager` requires an authenticated identity and enforces length/rate limits. | Implemented. Push surface-range regression passes. Adversarial/flood runtime tests remain useful. |
| H02 | `PlayerData.CmdSetPlayerInfo` ignores the submitted Steam ID. `MyNetworkManager.TryBindConnectionIdentity` binds Fizzy connections to the server-observed address and isolates KCP development IDs. | Implemented. Real Steam host/client authentication still needs runtime validation. |
| H04, H11 | `PlayerScript` owns a server SyncVar death state, server status expiries and a SmoothSync validation delegate with finite/timestamp/movement checks. | Implemented. Death/respawn and timestamp tests pass; malicious-client and production-latency tests are not covered. |
| H05, H08 | Sumo/floor/door authoritative collision state is applied on the server and persistent state is replicated. Hidden glass/door solution fields are no longer SyncVars. | Implemented. Glass late-join test passes; dedicated-server visual/collision separation still deserves one runtime check. |
| H06, H13 | Scene ACKs carry a transition ID, require authenticated eligible participants and phase state. The preloaded scene is committed through a Mirror message with `customHandling = true`. | Implemented. Two- and four-process KCP transitions pass; slow-disk and Steam runs remain. |
| H07 | Voting no longer mutates authoritative SyncLists on clients; client presentation uses replicated option/count state. | Implemented; multi-process voting UI should remain in the release checklist. |
| H09 | `MyNetworkManager` publishes server disconnects and minigame controllers remove/forfeit disconnected participants idempotently. | Implemented structurally; disconnect timing across every minigame is not exhaustively runtime-tested. |
| H10 | `PopupManager` no longer imports `UnityEditor`; Development Players build successfully. | Confirmed fixed. |
| H12 | `MatchManager` creates a server-owned replicated result deadline and the server performs the lobby transition. | Implemented. A true dedicated-server end-of-results run is still required. |

### Prior Medium findings M01-M44

Current source inspection found concrete corrections for every previously reported behavior: generation/coroutine cancellation (M01, M14, M17, M21, M30, M34), idempotent round/elimination state (M02, M04, M25, M29), bounded snapshot requests (M03), swept projectile collision (M05), replicated and phase-bound voting state (M06, M11-M13), refreshed customization (M07, M39), common briefing gate (M08), authoritative/owned moving-platform writes (M09), validated color persistence (M10), event-driven UI discovery and balanced subscriptions (M16, M18-M20, M22, M31-M33, M40), multi-collider occupancy (M23, M26), centralized floor detection (M24), all-client camera readiness with timeout (M27), server-only countdown lifecycle work (M28), actual lobby character instantiation (M35), allocation-free distance interaction checks (M36), minute-level phone-clock updates (M37), initialized toggle presentation (M38), owned native material/avatar cleanup (M41-M42), removal of the disconnect call that globally cleared SO listeners (M43), and unique-path prefab creation (M44).

Do not treat that paragraph as equivalent to full runtime proof. The highest-value caveats are:

- M02 is corrected in the affected Memory/Queda/Instructor paths, but `MinigameController.EndMatch` still does not enforce a reusable cleanup contract. This is an architectural gap, not a currently measured performance failure.
- M17/M22 depend on Steam callback timing; tests cover request identity/lifecycle helpers, not production Steam behavior.
- M27 now waits for all ready connections or a server timeout. It no longer has the original first-client behavior.
- M43's dangerous call path is gone, but `MyNetworkManager.ClearAllScriptableObjectEvents` remains as unused dead code and should not be reused.

## Current profiling evidence

### Representative single graphical Player (three repeats)

Values are medians across runs; p95/p99 are the across-run median of each run's percentile.

| Scene | Frame median / p95 / p99 | Main median | Render median | GPU median | Scripts median | GC median | SetPass median | Geometry median |
|---|---:|---:|---:|---:|---:|---:|---:|---:|
| `MN_Run` | 3.46 / 5.12 / 5.84 ms | 3.12 ms | 2.71 ms | 2.70 ms | 0.21 ms | 10,096 B | 72 | 583k tris / 442k verts |
| `MN_new_Rua` | 3.40 / 4.61 / 5.36 ms | 2.91 ms | 2.23 ms | 1.18 ms | 0.20 ms | 896 B | 80 | 875k tris / 946k verts |
| `MN_Queda` | 2.40 / 3.08 / 3.50 ms | 2.23 ms | 1.56 ms | 0.73 ms | 0.32 ms | 896 B | 66 | 176k tris / 214k verts |

The earlier synchronized-floor-query tail in `MN_Queda` is no longer present. Current physics p95 is about 0.09 ms and frame p99 is about 3.5 ms in the matched current capture. This confirms the centralized `FloorBreakingManager` change rather than relying on its “completed” label.

### Remaining scene inventory (one run; triage, not repeatability proof)

| Scene | Frame median / p95 / p99 | GPU median / p95 / p99 | Scripts p99 | Physics p99 | GC median | SetPass median |
|---|---:|---:|---:|---:|---:|---:|
| `MN_BatataQ` | 2.64 / 3.32 / 3.65 ms | 0.81 / 2.07 / 2.31 ms | 0.25 ms | 0.11 ms | 840 B | 55 |
| `MN_Memoria` | 1.92 / 2.46 / 2.77 ms | 0.50 / 0.55 / 0.57 ms | 0.29 ms | 0.12 ms | 17,400 B | 48 |
| `MN_Sumo` | 2.65 / 3.56 / 4.10 ms | 1.40 / 2.55 / 2.83 ms | 0.28 ms | 0.11 ms | 896 B | 60 |
| `RASCUNHO` | 1.59 / 2.02 / 2.31 ms | 0.65 / 0.67 / 0.69 ms | 0.23 ms | 0.13 ms | 5,232 B | 57 |

The previous isolated 17 ms Sumo GPU hitch did not recur, and there is no sustained GPU bottleneck. Because these are single runs, do not claim a statistically stable improvement for those four scenes.

### GC call-stack attribution

`MN_Run` exact recurring allocation (10,096 B/frame):

- 25 `Mirror.NetworkTransformBase.OnGUI` instances: 9,200 B.
- `Mirror.NetworkManager.OnGUI`: 368 B.
- `kcp2k.KcpTransport.OnGUI`: 368 B.
- match timer formatting/event delivery: 80 B.
- `CharacterController.Move` from `PlayerScript.Update`: 80 B.

`MN_Memoria` exact recurring allocation (17,400 B/frame):

- 45 `Mirror.NetworkTransformBase.OnGUI` instances: 16,560 B.
- `Mirror.NetworkManager.OnGUI`: 368 B.
- `kcp2k.KcpTransport.OnGUI`: 368 B.
- freeze HUD animation path: 24 B.
- `CharacterController.Move`: 80 B.

`NetworkTransformBase.OnGUI` and KCP `OnGUI` are Development/Editor-only. In `MN_Run`, 9,568 of 10,096 B/frame is diagnostic IMGUI. The remaining gameplay allocations are too small to justify risky changes without a release-like confirmation.

### Multiplayer scaling (three repeats per scenario)

Graphical host plus headless clients:

| Players | Frame median / p95 / p99 | Main | GPU | Scripts | GC | Unity memory | Network in/out per frame |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 1 | 3.36 / 4.36 / 4.93 ms | 3.06 ms | 1.43 ms | 0.21 ms | 10,096 B | 695.62 MB | 8.46 / 9.35 B |
| 2 | 3.45 / 4.33 / 4.75 ms | 3.09 ms | 1.30 ms | 0.22 ms | 10,256 B | 699.15 MB | 11.01 / 22.68 B |
| 4 | 3.92 / 5.09 / 6.01 ms | 3.54 ms | 2.37 ms | 0.26 ms | 10,973 B | 704.41 MB | 18.11 / 63.68 B |

Graphical client plus headless host/other clients:

| Players | Frame median / p95 / p99 | Main | GPU | Scripts | GC | Unity memory | Network in/out per frame |
|---:|---:|---:|---:|---:|---:|---:|---:|
| 2 | 3.46 / 4.48 / 5.13 ms | 3.06 ms | 2.08 ms | 0.19 ms | 10,016 B | 697.36 MB | 11.20 / 1.95 B |
| 4 | 3.84 / 5.11 / 5.73 ms | 3.40 ms | 2.37 ms | 0.21 ms | 10,033 B | 701.61 MB | 14.96 / 2.16 B |

Host 1→4 changes were frame +16.7%, main +15.7%, scripts +23.8%, and memory +1.3%. Client 2→4 changes were frame +11.0%, main +11.1%, scripts +10.5%, and memory +0.6%. Absolute costs remain well under a 16.67 ms frame on this machine. Network traffic is dominated by Mirror `EntityStateMessage` and `RpcMessage`; the measured rates are small, so there is no evidence-based reason to redesign replication now.

### Ten-transition soak

Rotation: `MN_Run`, `MN_Queda`, `MN_new_Rua`, `MN_Memoria`, `MN_Sumo`, repeated twice, then `RASCUNHO`.

- Gameplay-ready transition duration: p50 10.81 s, p95/p99 21.46 s, range 5.28–21.46 s.
- These durations include scene change, network/local-player readiness, briefing/start readiness and a two-second settle. They are not pure disk/asset load times.
- Unity allocated memory: 274.16 MB initially, peak 310.41 MB, 238.82 MB in lobby, 236.91 MB after explicit unload.
- Unity reserved memory: 434.30 MB initially, 515.30 MB after unload. Used memory did not track the retained reservation.
- Texture memory after unload was 73.24 MB below the initial scene. Mesh memory was 2.42 MB above it.
- Material count rose during the first cycle, then stabilized exactly for Memory/Sumo/Run on their repeat visits. Material runtime memory was below 1 MB. This is consistent with first-use caches plus scene composition, not continuing leakage.
- `processWorkingSetBytes` captured inside the Player was unavailable/zero; it is not used as evidence.

## Prioritized improvement plan

## 1. Critical — before release

No confirmed critical performance issue remains from the measured scope. Do not invent a critical optimization solely to populate this category.

Release can only retain that conclusion if the existing four-player functional gate stays green and the runtime logs remain error-free. Steam and dedicated-server requirements, if they are part of the release configuration, still need their own gates because KCP host/client evidence does not cover them.

## 2. High Priority

### H-P1 — Validate the real target hardware before changing graphics defaults

- **Found:** Ultra/1080p is inexpensive on an RTX 3060, but all current performance conclusions come from one relatively strong PC. Every quality tier has streaming mipmaps disabled. Warmed texture/graphics allocation is meaningful for an integrated or 2–4 GB GPU even though it is harmless here.
- **Where:** `ProjectSettings/QualitySettings.asset`; URP assets in `Assets/Settings`; representative gameplay scenes, especially `MN_new_Rua`, `MN_Run`, `MN_Memoria` and `MN_BatataQ`.
- **Why it matters:** The stated target includes entry-level gaming laptops/PCs. Fill rate, memory bandwidth, shared RAM and driver overhead can scale very differently from the RTX 3060.
- **Evidence:** GPU p95 stayed below roughly 4.3 ms here; warmed scene memory reached roughly 820 MB total in `MN_new_Rua`; streaming mipmaps are disabled for all tiers. This proves headroom only on the measured PC.
- **Expected impact:** Converts the largest release uncertainty into measured tier requirements; may identify a genuine low-tier GPU/memory issue without degrading defaults unnecessarily.
- **Complexity/risk:** Medium test effort; low product risk. Changing settings before results would be high risk.
- **Recommended solution:** Run the existing portable 2,000-frame package on at least one entry-level discrete laptop and one integrated/shared-memory machine. Use identical scene, route, resolution and warm-up; collect three repeats. Compare D3D11 and D3D12 only on matched hardware/settings. Add pass/fail budgets for frame p95/p99, GPU p95, peak memory and transition hitch.

### H-P2 — Resolve the future six-player capacity contract before advertising it

- **Found:** A six-process run fails admission because the startup NetworkManager has `maxConnections: 4`.
- **Where:** `Assets/Scenes/offline.unity` at the serialized NetworkManager; `MainMenu.unity` also contains a four-connection manager.
- **Why it matters:** Four players are supported today, but six cannot be profiled or played until this explicit cap and all dependent UI/spawn/roster assumptions are revised.
- **Evidence:** Two- and four-process KCP runs pass. Six processes fail; `offline.unity` serializes `maxConnections: 4`, and excess KCP connections produce invalid connection-ID send logs.
- **Expected impact:** Enables, but does not prove, the future six-player target.
- **Complexity/risk:** Medium/high. The numeric change is trivial; validating spawn slots, UI, scoring, minigame rosters, bandwidth and Steam lobby capacity is not.
- **Recommended solution:** Keep four for the current release unless six is promoted to a requirement. When it is, change capacity in one canonical prefab/manager, remove duplicate manager configuration, then run the full 1/2/4/6 matrix, all scenes, disconnect/reconnect, Steam lobby, and a soak. Do not only change the YAML value.

### H-P3 — Reproduce the repeated `MN_BatataQ` start failure with four players

- **Found:** A fresh single-player Batata capture completes, but the all-scene repeated-transition soak reached Batata and timed out waiting for the active match timer.
- **Where:** `MN_BatataQ`; briefing/match start lifecycle; `HotPotatoMinigameController`, `BriefingManager`, `MatchManager`.
- **Why it matters:** If this reproduces with the supported player count, a normal rotation can stall. If it only occurs in an unsupported one-player fixture, no gameplay change is warranted.
- **Evidence:** Failure marker: “the active match timer did not start” after prior scenes. The compatible five-scene soak then completed 10 transitions. Fresh Batata profiling also completes.
- **Expected impact:** Determines whether this is a release stability issue or a fixture limitation.
- **Complexity/risk:** Medium test effort; potentially high change risk if lifecycle code is modified without reproduction.
- **Recommended solution:** Run a four-process scripted rotation that reaches Batata both first and after at least three other scenes, three times. Record briefing ACK counts, ready states, controller start/end generation and match timer. Change code only if the supported scenario reproduces.

### H-P4 — Split transition latency before optimizing loading

- **Found:** Gameplay-ready transition p50 is 10.81 s and p95 21.46 s, with `MN_Run` repeatedly around 21.46 s.
- **Where:** `SceneTransitionManager`, `BriefingManager`, `MatchManager`, scene async load and asset activation.
- **Why it matters:** Twenty-one seconds may be a user-facing pacing issue, but the current metric combines disk load, network barriers, briefing readiness and the intentional settle.
- **Evidence:** Ten-transition soak timings; no corresponding monotonic memory growth.
- **Expected impact:** Identifies whether improvement belongs to asset loading, network synchronization, briefing timing or deliberate presentation.
- **Complexity/risk:** Low/medium instrumentation; high risk if the state machine is shortened blindly.
- **Recommended solution:** Add markers for preload start/90%, activation, local player ready, all-client ACK, briefing interactable, all ready, timer start and first interactive frame. Profile four players on SSD and a slower target laptop. Optimize only the dominant measured segment.

## 3. Medium Priority

### M-P1 — Make benchmarks release-aware and deterministic

- **Found:** Development IMGUI accounts for nearly all apparent 10–17 KB/frame allocations. Geometry/GPU results vary with spawn/camera state by more than 5% in some runs.
- **Where:** profiling harness; Mirror/KCP diagnostic `OnGUI`; camera/spawn route in representative scenes.
- **Why it matters:** Regressions can be hidden by scene variance or false-positive diagnostic GC.
- **Evidence:** Exact allocation call stacks; 25 vs 45 NetworkTransform `OnGUI` instances explain scene GC. Some per-run SetPass/triangle values varied materially while three-run CPU medians were stable.
- **Expected impact:** Trustworthy release regressions and fewer speculative optimizations.
- **Complexity/risk:** Medium; diagnostics only.
- **Recommended solution:** Keep a Development call-stack lane and add a non-Development profiling lane with profiler counters/markers compiled explicitly for benchmarking. Fix spawn/camera seed and scripted route. Require three repeats and flag >5% spread. Store hardware, API, resolved settings, roles and process counts in every result.

### M-P2 — Add automated performance and capacity gates

- **Found:** Functional regressions have 21 passing tests, but there is no CI-style budget gate for frame tails, GC, memory after transitions, or 4/6-player admission.
- **Where:** `RefactorRegressionTests`, `ProjectValidation`, `Tools/PerformanceAudit`, build/validation workflow.
- **Why it matters:** The centralized floor fix and lifecycle corrections can regress without compile failures.
- **Evidence:** The old Queda p99 issue was measurable and is now fixed; the six-player cap and Batata soak issue were found only by process-level tests.
- **Expected impact:** Earlier detection of performance and scalability regressions.
- **Complexity/risk:** Medium. Hardware-sensitive budgets require a designated benchmark machine.
- **Recommended solution:** Add: allocation-source regression tests for timer caching; floor broad-phase/query-count tests; repeated enable/disable subscription-count tests; 10-transition memory plateau test; 2/4-player nightly smoke; optional 6-player gate once supported; fixed-hardware p95/p99 budgets with a tolerance band rather than universal absolute CI thresholds.

### M-P3 — Clarify and validate quality-tier semantics

- **Found:** Standalone defaults to index 0 `Ultra`; `High Fidelity` has materially higher shadow distance/cascades than `Ultra`; `Very Low  ` has a trailing-space name and uses the same render scale, HDR, shadows and additional-light mode as Ultra. `Performant` is the only clearly reduced URP asset. Streaming mipmaps are off everywhere.
- **Where:** `ProjectSettings/QualitySettings.asset`, `Ultra_PipelineAsset`, `URP-HighFidelity`, `Very Low  _PipelineAsset`, `Performant_PipelineAsset`, `SettingsGraphics`.
- **Why it matters:** Names do not communicate an ordered cost ladder, and users may select a “Very Low” tier that does not materially reduce the main GPU costs.
- **Evidence:** Current serialized quality/URP values. No matched low-tier benchmark exists.
- **Expected impact:** Predictable settings behavior across target PCs.
- **Complexity/risk:** Medium; quality changes can affect visuals and saved PlayerPrefs.
- **Recommended solution:** First benchmark every tier on target hardware with resolved settings logged. Then define a documented ladder for shadows, additional lights, render scale, post FX, texture limit, LOD bias and optional mip streaming. Rename/migrate saved tier identifiers carefully. Do not enable mip streaming or lower render scale without testing visual and transition behavior.

### M-P4 — Decompose large runtime classes incrementally, behind tests

- **Found:** `PlayerScript` is 2,178 lines with roughly 122 method-like declarations; `SceneTransitionManager` is 1,255 lines; `MyNetworkManager` is 1,252 lines. `PlayerScript` combines movement, camera, input, combat/status, death/respawn, networking, menus and customization. `MyNetworkManager` combines identity, scoring, lobby ownership, transport startup, scene flow, telemetry and cleanup.
- **Where:** those three classes; related `MatchManager`, `SteamLobby`, `VotingManager` boundaries.
- **Why it matters:** High coupling increases regression risk and makes profiling ownership/lifecycle harder to reason about.
- **Evidence:** Current line/method counts and responsibility regions/call paths. The prior audit fixes frequently touched the same central files for unrelated concerns.
- **Expected impact:** Easier testing and safer future six-player/transport work; little direct FPS gain is claimed.
- **Complexity/risk:** High if performed as a rewrite; medium when extracted one responsibility at a time.
- **Recommended solution:** Do not rewrite the working scene-transition state machine. First introduce characterization tests, then extract pure validators/state objects: movement validation, player life/status state, connection identity registry, score/roster service and transition telemetry. Preserve NetworkBehaviour ownership and serialized fields through adapters until runtime parity is proven.

### M-P5 — Formalize the minigame round lifecycle contract

- **Found:** affected controllers now clean up correctly, but the base `MinigameController.EndMatch` still only notifies; it does not guarantee cancellation/reset/idempotence for derived controllers.
- **Where:** `MinigameController`, Memory/Queda/HotPotato/Race/Sumo/Street controllers, Instructor.
- **Why it matters:** New or restarted minigames can reintroduce stale coroutines, rosters and subscriptions; the Batata sequence signal makes lifecycle characterization especially valuable.
- **Evidence:** Current base method versus per-controller cleanup implementations.
- **Expected impact:** Stability across repeated rotations, not a claimed steady-frame improvement.
- **Complexity/risk:** Medium/high due to inheritance and scene serialization.
- **Recommended solution:** Add a server-owned round generation and idempotent template lifecycle (`Prepare`, `Start`, `End`, `Cleanup`) only after characterization tests for every minigame. Migrate one controller at a time; do not force a broad inheritance rewrite before the Batata reproduction.

### M-P6 — Add targeted ownership/subscription lifecycle tests

- **Found:** many prior issues were listener/callback/native-object lifetime defects. Source now balances the relevant subscriptions and destroys owned materials/avatar resources, but most paths lack recreation tests.
- **Where:** HUD/BlindPanel/SpecOverlay, Steam avatar consumers, contributor UI, team-color material, lobby screens and SO event assets.
- **Why it matters:** reconnect and scene reload defects often appear only after the second lifecycle.
- **Evidence:** Current balanced methods; only a subset is represented in the 21-test suite. Soak material count stabilizes, supporting but not proving all ownership paths.
- **Expected impact:** Prevents duplicated callbacks and native-memory regressions.
- **Complexity/risk:** Low/medium.
- **Recommended solution:** Instantiate, enable/disable/destroy, recreate and assert exactly one callback; repeat contributor refresh and compare native object count after unload; spawn/despawn team-color objects and verify owned material count returns to baseline.

## 4. Low Priority / Nice to Have

### L-P1 — Ignore Development-only IMGUI GC as a shipping optimization target

- **Found/evidence:** Mirror/KCP diagnostic `OnGUI` produces nearly all recurring Development allocation.
- **Impact:** Removing it from Development builds would make captures cleaner but would not improve release performance.
- **Risk/solution:** Low; prefer a release-like capture lane or disable diagnostic GUI only in benchmark builds. Do not modify third-party networking code solely for this number.

### L-P2 — Measure before changing the remaining 80 B timer and 80 B movement allocations

- **Found:** timer formatting/event delivery and `CharacterController.Move` each allocate about 80 B/frame in the focused Development capture.
- **Where:** `CustomMath.FormatTimer` → HUD event path; `PlayerScript.Update` → `CharacterController.Move`.
- **Why/impact:** At 60 FPS these are small. Timer text can be updated only when its displayed value changes, but the movement allocation may be Unity/version behavior.
- **Complexity/risk:** Low for timer caching, potentially high for movement-path work.
- **Recommended solution:** Confirm both in a release-like build. Optimize the timer only if it remains recurring; do not replace CharacterController or restructure movement for 80 B without a larger measured cost.

### L-P3 — Keep current rendering architecture unless low-end evidence says otherwise

- **Found:** SRP Batcher is enabled, static batching is enabled, and current SetPass/GPU costs are low. Scenes use existing LOD assets selectively; coverage effectiveness is not measured.
- **Evidence:** RTX 3060 GPU/frame results and current project settings.
- **Expected impact:** Avoids asset churn and visual regressions.
- **Recommended solution:** On entry-level hardware, use Frame Debugger/RenderDoc and fixed-camera comparisons to identify actual shadow, overdraw, transparent, post-processing or LOD problems. Do not add LODs to every object or merge meshes indiscriminately.

### L-P4 — Remove dead cleanup code and normalize names when touching nearby systems

- **Found:** unused `ClearAllScriptableObjectEvents` remains in `MyNetworkManager`; `Very Low  ` has trailing spaces; Portuguese/English names and `Rpc...` method names that are no longer RPCs remain in places.
- **Why it matters:** Readability and accidental reuse, not runtime cost.
- **Expected impact:** Small maintenance improvement.
- **Complexity/risk:** Low, but serialized quality-name migration needs care.
- **Recommended solution:** Remove/rename only with reference searches and serialization/PlayerPrefs migration tests. Do not bundle this cleanup with performance fixes.

## Areas already well structured — do not refactor without evidence

- Centralized Queda floor detection: one manager owns cadence/broad phase, reuses/grows non-alloc buffers and stops scanning activated tiles. It is measured and regression-tested.
- Scene transition participant/phase validation: transition IDs, eligible-state checks, custom Mirror scene handling and disconnect cleanup form a coherent state machine. Add markers/tests around it; do not rewrite it for style.
- Server authority boundaries for push, death/status, hidden solutions, voting and results are materially stronger and should be preserved.
- UI/event teardown fixes use local ownership (`OnEnable`/`OnDisable` or paired destruction) rather than global event clearing. Preserve that model.
- Native resource ownership in team-color and contributor avatar components is explicit. Do not replace it with blanket `Resources.UnloadUnusedAssets` calls.
- Existing portable benchmark, multi-process runner, project validation and focused regression suite are valuable foundations. Extend them instead of creating unrelated one-off harnesses.
- Current SRP/static batching and measured rendering headroom provide no reason for a broad shader/material/mesh refactor.

## Logical implementation order

1. Freeze a release candidate and run the existing 2/4-player functional/log gates plus the 21 regression tests.
2. Reproduce or dismiss the repeated Batata start failure with four processes and transition-phase telemetry.
3. Instrument transition phase splits; measure loading on the RTX machine and one slower target machine.
4. Run the fixed three-repeat benchmark on representative entry-level discrete and integrated/shared-memory PCs, across current quality tiers.
5. Define release budgets and add automated fixed-hardware performance/soak gates.
6. If six players become a requirement, centralize capacity configuration and validate 1/2/4/6 before any network optimization.
7. Make only measured graphics/loading/allocation changes, one subsystem at a time, with before/after captures and unchanged scenarios.
8. Add lifecycle/ownership characterization tests, then extract the first narrow responsibility from `PlayerScript` or `MyNetworkManager` without changing network ownership.
9. Formalize the minigame lifecycle after Batata and repeated-rotation behavior is understood.
10. Finish low-risk naming/dead-code cleanup separately from performance work.

## Validation performed in this pass

- Three-repeat standalone captures for `MN_Run`, `MN_new_Rua`, `MN_Queda`.
- One-run inventory for `MN_BatataQ`, `MN_Memoria`, `MN_Sumo`, `RASCUNHO`.
- Raw GC call-stack captures for `MN_Run` and `MN_Memoria`.
- Three repeats for graphical host at 1/2/4 players.
- Three repeats for graphical client at 2/4 players.
- Six-player admission attempt, including configuration root cause.
- Ten compatible scene transitions plus lobby/unload memory snapshots.
- Current Unity compilation with no console errors after instrumentation.
- `RefactorRegressionTests`: 21 passed, 0 failed, 0 skipped.

Not performed/proven: Steam transport/authentication, true dedicated server, real six-player gameplay, entry-level hardware, release-build allocation call stacks, pure asset-load time, exhaustive all-minigame four-player stress, or adversarial security testing.
