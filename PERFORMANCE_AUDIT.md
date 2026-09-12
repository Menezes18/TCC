# Performance Audit

Date: 2026-09-10; cross-scene continuation: 2026-09-11
Unity: 6000.4.6f1, Editor Play Mode plus Windows Development Player, Direct3D 12, 1920x1080
Machine: AMD Ryzen 7 5700G, NVIDIA GeForce RTX 3060 (12 GB), 32 GB RAM, Windows 11
Revision: `892ba36f3cebf85a5a489902c11dec2593ea93a5`

## Executive summary

Seven representative scenes were measured in a Windows Development Player with Deep Profile off, one local Mirror host/player, and deterministic movement. Every stable result below uses the most recent 2,000 post-warm-up frames. None was GPU-bound on the RTX 3060: GPU p99 ranged from 0.813 ms (`MN_Memoria`) to 1.679 ms (`MN_Run`). This does not establish entry-level performance.

`MN_Run` had the highest steady PlayerLoop median at 4.007 ms. `MN_Queda` retained the worst recurring CPU/physics tail: PlayerLoop p99 7.384 ms and Physics p99 2.168 ms, corroborating the earlier synchronized floor-query finding. `MN_Memoria` and `MN_Run` also showed recurring managed allocation of about 17.5 KB/frame and 11.4 KB/frame respectively. `MN_new_Rua` had the highest warmed total used memory (837.3 MB median), graphics allocator indicator (511.2 MB), mesh memory (57.4 MB), and submitted geometry, but not the highest GPU time.

Complete transition windows were retained for the four heavier-loading scenes. The largest observed transition was `MN_Queda`: 259.101 ms Player frame, 7.32 MB GC allocation in one frame, and a 281.7 MB increase in total used memory. `MN_new_Rua`, `MN_Memoria`, and `MN_BatataQ` also produced 109–121 ms transition frames and 155.8–324.3 MB total-memory increases. These are loading-window observations, not steady gameplay.

No gameplay or graphics code was changed during the audit capture. The local KCP host and deterministic-input bootstrap existed only in the diagnostic Player build and was removed from `Assets` after measurement. The captures do not substitute for entry-level-hardware tests or 4/6-player multi-process tests.

## Implementation follow-up — centralized Queda detection

On 2026-09-12, finding 1 was addressed by moving managed-scene server detection into one 0.1-second pass in `FloorBreakingManager`. The pass iterates connected server players, rejects distant or inactive tiles with a non-physics broad phase, and performs the existing exact overlap validation only for nearby candidates. `ChaoQuebrandoSimples` retains its original server polling as a fallback when no manager is assigned. Its overlap query now uses the explicit `Player` layer mask while still including trigger colliders.

A same-machine, same-build-settings pre-fix replay and three post-fix Development Player runs each sampled 2,000 warmed `MN_Queda` frames. Relative to the immediate pre-fix replay, post-fix Scripts p99 was 0.616–0.689 ms versus 4.398 ms (84.3–86.0% lower), main-thread p99 was 2.928–2.966 ms versus 6.344 ms (53.2–53.8% lower), and frame p99 was 3.612–3.729 ms versus 6.357 ms (41.3–43.2% lower). All three post-fix runs reached the benchmark READY and COMPLETE markers with no logged exception/error matches. Physics p99 remained 0.109–0.132 ms in these matched replays; the query cost is represented primarily inside the Scripts recorder rather than `Physics.Simulate`.

Unity compiled the change and 12/16 tests in `RefactorRegressionTests` passed, including all three floor-detection tests. The four failures are pre-existing Steam callback/movement-timestamp tests outside the changed files. This validation is still one local host with deterministic movement; real 4/6-player, Steam, and remote-client behavior remain unverified.

## Profiling capability established through Unity MCP

The Unity 6 MCP bridge exposed the following useful Profiler operations:

- counter summaries and per-frame counter tables;
- CPU main-thread, render-thread, GPU, GC allocation, SetPass, and triangle counters;
- frame-range threshold analysis and max/median frame selection;
- per-frame top-time and self-time sample analysis;
- sample/child drill-down, bottom-up analysis, and GC allocation summaries.

The separately installed MCP for Unity profiler API also advertised session recording, frame timing, counters, memory snapshots, and Frame Debugger event extraction. Its file-backed recording generated multi-gigabyte captures and destabilized the Editor, so the evidence below uses the Unity 6 bridge's rolling 2,000-frame Profiler buffer. Deep Profile was not enabled.

Two requested counters were not present in the captured Profiler stream: `Draw Calls Count` and `Batches Count`. Draw calls were therefore sampled from `UnityEditor.UnityStats` at the frozen Game view; batch count remains unavailable. Those snapshots are marked separately from the 2,000-frame distributions.

## Method and scope

Representative local-host flows were run through the normal offline scene, lobby, briefing-ready transition, and match start:

1. `MN_Sumo`: 2,000 stable gameplay frames, displayed frames 5708–7707.
2. `MN_Queda`: 2,000 stable gameplay frames, displayed frames 17538–19537.

Each flow ran a Mirror host plus one local player in the Editor. This exercises server and client work in one process, but it is not a dedicated-server, multi-client, Steam, or standalone-player benchmark. No automated player movement was injected, so the captures represent active match simulation at its starting gameplay state rather than a full-match stress test.

Reported averages and p99 values come from per-frame counter tables. `PlayerLoop` values are targeted samples from the median and worst inspected frames, because the bridge did not expose `PlayerLoop` as a range counter. Zero GPU samples at pause/capture boundaries are not interpreted as gameplay performance.

## Baseline measurements

| Metric | Sumo (2,000 frames) | Queda (2,000 frames) | Interpretation |
|---|---:|---:|---|
| CPU main active, average | 8.801 ms | 11.118 ms | Includes Editor and profiler overhead |
| CPU main active, median | 8.400 ms | 10.215 ms | Includes Editor and profiler overhead |
| CPU main active, p99 | 14.244 ms | 19.249 ms | Queda spike distribution is materially worse |
| CPU main active, maximum | 35.587 ms | 38.380 ms | Both maxima were Editor-dominated |
| Runtime `PlayerLoop`, sampled median frame | 2.892 ms | 3.835 ms | Runtime sample, not Editor total |
| Runtime `PlayerLoop`, inspected maximum frame | 4.333 ms | 10.453 ms | Queda includes synchronized floor checks |
| Render thread active, average | 7.860 ms | 9.041 ms | Editor Play Mode counter |
| Render thread active, p99 | 14.109 ms | 14.091 ms | Close to a 60 Hz budget in Editor only |
| GPU frame time, average | 2.880 ms | 3.678 ms | No sustained GPU limit measured |
| GPU frame time, median | 2.858 ms | 4.449 ms | No sustained GPU limit measured |
| GPU frame time, p99 | 5.612 ms | 6.292 ms | Good GPU headroom on RTX 3060 |
| GPU frame time, maximum | 17.176 ms | 9.292 ms | Sumo maximum is a single isolated spike |
| GC allocation/frame, median | 920 B | 952 B | Low steady allocation; Editor included |
| GC allocation/frame, maximum | 899,157 B | 472,110 B | Largest inspected bursts were not on `PlayerLoop` |
| Frames over 16.67 ms main active | 9 / 2,000 (0.5%) | 122 / 2,000 (6.1%) | Includes Editor overhead |
| SetPass, median | 64 | 59 | Profiler counter |
| Triangles, median | 376,198 | 98,718 | Profiler counter |
| Draw calls, frozen-frame snapshot | 140 | 222 | `UnityStats`, one frozen frame |
| SetPass, frozen-frame snapshot | 61 | 59 | `UnityStats`, one frozen frame |
| Triangles, frozen-frame snapshot | 293,890 | 98,280 | `UnityStats`, one frozen frame |
| Shadow casters, frozen-frame snapshot | 37 | 18 | `UnityStats`, one frozen frame |

The Profiler and Game-view triangle values differ for Sumo because they come from different instrumentation and frames; neither has been substituted for the other.

## Findings

### 1. Medium — Synchronized floor-tile overlap queries create Queda CPU spikes

**Measurement:** On three inspected Queda spike frames, `ChaoQuebrandoSimples.Update()` consumed 6.812 ms, 8.816 ms, and 11.057 ms. `Physics.OverlapSphereNonAlloc` consumed 2.433 ms, 3.048 ms, and 3.779 ms on those same frames. On the maximum frame, the script consumed 6.837 ms total with 4.415 ms self time and 2.422 ms in the overlap query. Queda main-thread p99 was 19.249 ms versus 14.244 ms in Sumo, and 122/2,000 Editor frames exceeded 16.67 ms versus 9/2,000 in Sumo.

**Evidence:** Profiler self-time and bottom-up samples identify `Assembly-CSharp.dll!::ChaoQuebrandoSimples.Update()` and `Physics.OverlapSphereNonAlloc`. The scene contains 1,047 serialized instances of the component. The targeted source inspection shows every untriggered server tile scheduling the same 0.1-second interval from an initially identical timer and issuing an unfiltered overlap sphere ([`ChaoQuebrandoSimples.cs`](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs#L66), [`DetectColliders`](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs#L291)).

**Cause:** The tile timers are phase-aligned. Large groups of active tiles therefore run broad physics queries in the same frame instead of spreading the work across frames. The host capture pays both the server tile loop and local client/render work.

**Recommended optimization:** Batch detection centrally in `FloorBreakingManager`, spatially restrict candidate tiles around each player, and apply an explicit player layer mask. If a centralized design is not immediately possible, stagger each tile's initial `_nextDetectionTime` and measure again; staggering reduces spikes but does not reduce total query work.

**Expected benefit:** Lower `ChaoQuebrandoSimples.Update` and `Physics.OverlapSphereNonAlloc` time on spike frames; lower main-thread p99 and fewer frames over the target budget. No FPS gain is asserted.

**Confidence:** Confirmed by profiler. The synchronization and scale explanation is strongly indicated by the measured cadence plus the 1,047 scene instances.

### 2. Low — Editor overhead, not gameplay, dominates the largest recorded CPU frames

**Measurement:** Sumo's 35.587 ms maximum contained 30.688 ms of `EditorLoop` and 4.333 ms of `PlayerLoop`. Queda's 38.383 ms maximum contained 27.478 ms of `EditorLoop` and 10.453 ms of `PlayerLoop`. A Queda median frame contained 6.012 ms `EditorLoop`, 3.835 ms `PlayerLoop`, and 0.309 ms profiler flushing.

**Cause:** Scene/Game view repaint, Editor services, and Profiler collection execute in the same captured process. They inflate CPU main-active and render-thread counters without representing a player build.

**Recommended optimization:** Do not optimize gameplay for `EditorLoop`. Capture the same scenarios in a development standalone player connected to the Profiler, with the Editor's Scene view closed or idle, before setting a release frame-time budget.

**Expected benefit:** Cleaner attribution and a trustworthy player CPU baseline; this is a measurement-quality improvement, not a claimed runtime speedup.

**Confidence:** Confirmed by profiler.

### 3. Low — Render-thread cost has narrower headroom than GPU cost, but no player bottleneck is confirmed

**Measurement:** Render-thread averages were 7.860 ms (Sumo) and 9.041 ms (Queda), with p99 values of 14.109 ms and 14.091 ms. GPU p99 was much lower at 5.612 ms and 6.292 ms. Median sampled runtime rendering work included `RenderPlayModeViewCameras` at 1.198 ms in Sumo and 1.869 ms in Queda.

**Cause:** The split render thread is doing appreciable CPU submission work while the RTX 3060 has substantial GPU headroom. Editor Game/Scene-view rendering can contribute, so the exact player-side submission cost remains unresolved.

**Recommended optimization:** First repeat in a standalone development player. If render-thread p99 remains near the target, use Frame Debugger plus render-thread samples to rank passes, material/state changes, shadows, and camera stacking before changing assets or URP settings.

**Expected benefit:** If reproduced in-player, fewer CPU render submissions and lower render-thread p99. The current evidence does not justify a specific asset reduction or FPS claim.

**Confidence:** Strongly indicated in Editor; requires additional player measurement.

### 4. Low — Large GC bursts are capture/background-thread events, not a confirmed gameplay allocator

**Measurement:** Steady allocation was 920 B/frame median in Sumo and 952 B/frame in Queda. There were isolated maxima of 899,157 B and 472,110 B. Only 9/2,000 Queda frames exceeded 8 KB. On the 472,110 B Queda frame, the allocation summary attributed 588,537 B to thread 71 while `PlayerLoop` accounted for only 1,312 B; the profiler explicitly notes that Editor threads are present.

**Cause:** The largest bursts are on worker/Editor threads and are not attributable to a gameplay callstack from this capture. The counter can exceed the displayed frame total when thread samples straddle the frame boundary.

**Recommended optimization:** Do not change gameplay allocation code from these bursts. In the standalone-player follow-up, enable GC.Alloc call stacks only for a short focused capture and investigate any recurring allocation above the steady sub-1 KB baseline.

**Expected benefit:** Avoids optimizing Editor noise and, if a player allocation is reproduced, identifies the exact allocation site to reduce GC pressure.

**Confidence:** Confirmed that the inspected burst was not mainly `PlayerLoop`; gameplay cause requires additional measurement.

### 5. Low — Sumo has one unclassified GPU hitch, not a sustained GPU bottleneck

**Measurement:** Sumo GPU median was 2.858 ms and p99 5.612 ms, but one frame reached 17.176 ms. The next-highest recorded Sumo GPU frame was 7.883 ms. Queda's maximum was 9.292 ms. Sumo's steady rendering load was approximately 64 SetPass calls and 376,198 Profiler triangles per frame; its frozen Game-view snapshot showed 140 draws, 61 SetPass calls, 293,890 triangles, and 37 shadow casters.

**Cause:** The single GPU event was not reproduced often enough to classify. Possible shader/driver/resource-upload or Editor interference explanations remain hypotheses, not conclusions.

**Recommended optimization:** Reproduce in a development player and trigger a GPU Profiler/Frame Debugger capture on recurrence. Inspect the actual pass or upload responsible before modifying shaders, shadows, meshes, or post-processing.

**Expected benefit:** Identification and removal of a genuine hitch if it reproduces; steady GPU frame time is not expected to change from investigation alone.

**Confidence:** The spike is confirmed by the GPU counter; its cause requires additional measurement.

## Baseline

The conservative baseline is the slower of the two measured scenarios, with both values shown where useful:

- **CPU frame time:** 8.801 ms Sumo / 11.118 ms Queda average main-active time, Editor-inclusive. Runtime `PlayerLoop` sampled medians: 2.892 / 3.835 ms.
- **GPU frame time:** 2.880 ms Sumo / 3.678 ms Queda average; p99 5.612 / 6.292 ms.
- **Main Thread:** 8.801 / 11.118 ms average; p99 14.244 / 19.249 ms, Editor-inclusive.
- **Render Thread:** 7.860 / 9.041 ms average; p99 14.109 / 14.091 ms, Editor-inclusive.
- **GC allocation/frame:** 920 / 952 B median; isolated maxima 899,157 / 472,110 B, largely non-`PlayerLoop` in the inspected case.
- **Draw calls:** 140 Sumo / 222 Queda, single frozen-frame Game-view snapshots.
- **Batches:** Not available from the MCP Profiler capture or this UnityStats API.
- **SetPass:** 64 Sumo / 59 Queda median.
- **Triangles:** 376,198 Sumo / 98,718 Queda median Profiler counter.
- **Average FPS:** Not reported. CPU-active time in an Editor capture is not wall-clock frame duration, so converting it to FPS would be misleading.
- **1% low FPS:** Not measurable reliably from the available capture. Slow-frame proxies are main-active p99 14.244 ms (Sumo) and 19.249 ms (Queda), not FPS.

## Top 5 bottlenecks / risks

1. Queda's synchronized `ChaoQuebrandoSimples` overlap-query bursts — confirmed runtime CPU hotspot.
2. Editor overhead contaminating main/render timing — confirmed measurement limitation; capture a development player next.
3. CPU render-thread headroom is narrower than GPU headroom — strongly indicated, player reproduction required.
4. Isolated background-thread GC bursts — measured, but not a confirmed gameplay allocator.
5. One isolated 17.176 ms Sumo GPU frame — measured hitch, cause not yet classified.

No sustained GPU bottleneck and no general Sumo gameplay CPU bottleneck were confirmed in this single-host, one-local-player pass.

## Next measurement pass

The Windows Development Player cross-scene pass requested after the original Editor baseline is now complete; its authoritative results begin at **Scene Risk Matrix** below. Remaining evidence-gathering work, before any optimization, is:

1. Run the planned real multi-process 1/4/6-player comparison for `MN_Queda`, `MN_Run`, and `MN_Sumo`.
2. Enable GC.Alloc call stacks only for short focused `MN_Memoria` and `MN_Run` captures to identify their persistent per-frame allocations.
3. Re-capture complete loading windows for `MN_Run` and `MN_Sumo`; their steady-state windows are complete, but their rolling transition buffers retained only the late tail.
4. Repeat a fixed subset on representative entry-level/discrete and integrated/shared-memory hardware, then perform controlled graphics-setting sweeps without changing defaults in this audit.

## Scene Risk Matrix

Continuation date: 2026-09-11. This phase is a cross-scene triage, not a claim that a scene has a shipping performance defect.

### Scope and measurement method

The actual gameplay rotation is defined by `Assets/Scripts/Data/ScriptableObjects/Minigames/MinigameCatalog.asset`. It contains eight minigames: `MN_new_Rua`, `MN_Sumo`, `MN_BatataQ`, `MN_Memoria`, `MN_Queda`, `MN_Run`, `MN_Round6`, and `MN_Fut`. `RASCUNHO` is also included because it is the persistent lobby/gameplay scene from which the minigame flow starts. `MN_Barco` is enabled in Build Settings but is not in the active catalog; `MN_Corrida`, `MN_Rua`, and other prototype/test scenes are not treated as actual gameplay in this matrix.

Each scoped scene was opened read-only in the Unity Editor and returned to `offline.unity` without saving. Counts are for active-in-hierarchy objects/components in the serialized scene. Dynamically spawned players, runtime-only objects, and the additive `ResultsOverlay`/`SpecOverlay` scenes are not included. Geometry totals count mesh instances used by active renderers, so a shared mesh rendered 100 times contributes its vertices/triangles 100 times; the mesh column is the distinct active mesh count. Materials are distinct materials used by active renderers. Transparent renderers were classified by transparent render queue or `RenderType` tag.

Texture memory is an Editor-side estimate from textures directly referenced by active-renderer materials, the skybox, and lightmaps, using `Profiler.GetRuntimeMemorySizeLong`. Mesh memory uses distinct active meshes. These are useful relative indicators, not total player allocations or measured VRAM residency. Texture streaming is disabled in every current quality level, which increases the importance of validating these estimates in a player. No serialized camera targets a RenderTexture; URP intermediate buffers and runtime result/preview RenderTextures are outside this static count.

| Scene | Active GO | Renderers | Meshes | Instanced vertices | Instanced triangles | Materials | Static SetPass/draw risk |
|---|---:|---:|---:|---:|---:|---:|---|
| `RASCUNHO` | 1,188 | 288 | 47 | 141,376 | 187,071 | 22 | Medium |
| `MN_new_Rua` | 4,857 | 1,306 | 96 | 1,387,760 | 1,302,434 | 28 | Very High |
| `MN_Sumo` | 1,121 | 189 | 86 | 87,744 | 83,350 | 14 | Medium |
| `MN_BatataQ` | 2,384 | 314 | 94 | 378,702 | 427,751 | 11 | Medium-High |
| `MN_Memoria` | 415 | 270 | 20 | 92,825 | 69,082 | 19 | High: transparency/particles |
| `MN_Queda` | 2,620 | 1,221 | 35 | 607,712 | 452,291 | 23 | Very High |
| `MN_Run` | 5,255 | 1,046 | 103 | 210,487 | 259,830 | 11 | Very High: shadows/object count |
| `MN_Round6` | 230 | 105 | 25 | 65,141 | 56,074 | 10 | Low-Medium |
| `MN_Fut` | 1,918 | 58 | 13 | 74,756 | 64,986 | 14 | Low-Medium |

The SetPass/draw column is a triage estimate based on renderer/material state-change opportunities, transparency, shadow casters, and camera configuration. It is not a measured draw-call count. Measured Development Player SetPass and geometry distributions appear later; the remote D3D12 stream did not expose a trustworthy draw-call or batch counter.

| Scene | Lights (real-time / mixed / shadowing) | Shadow-casting renderers | Particles | Transparent renderers | Post FX camera / Volumes | Cameras / stack | Serialized RT targets | Physics (colliders / RB / dynamic RB) | Texture / mesh estimate |
|---|---:|---:|---:|---:|---:|---:|---:|---:|---:|
| `RASCUNHO` | 1 / 1 / 0 | 283 | 13 | 21 | 1 / 1 | 1 / 0 | 0 | 259 / 18 / 3 | 58.6 / 8.1 MB |
| `MN_new_Rua` | 0 / 1 / 1 | 1,295 | 0 | 15 | 1 / 1 | 1 / 0 | 0 | 377 / 0 / 0 | 77.3 / 10.0 MB |
| `MN_Sumo` | 1 / 0 / 1 | 183 | 13 | 20 | 1 / 1 | 1 / 0 | 0 | 35 / 0 / 0 | 171.4 / 5.3 MB |
| `MN_BatataQ` | 1 / 0 / 1 | 313 | 1 | 3 | 1 / 1 | 1 / 0 | 0 | 58 / 0 / 0 | 203.1 / 12.0 MB |
| `MN_Memoria` | 0 / 2 / 2 | 177 | 32 | 41 | 0 / 1 | 1 / 0 | 0 | 99 / 0 / 0 | 198.6 / 2.3 MB |
| `MN_Queda` | 1 / 0 / 1 | 140 | 22 | 24 | 1 / 1 | 1 / 0 | 0 | 2,108 / 0 / 0 | 70.7 / 5.3 MB |
| `MN_Run` | 2 / 0 / 2 | 1,046 | 0 | 1 | 1 / 1 | 1 / 0 | 0 | 1,001 / 45 / 15 | 77.6 / 4.0 MB |
| `MN_Round6` | 1 / 0 / 1 | 93 | 0 | 5 | 1 / 1 | 1 / 0 | 0 | 73 / 0 / 0 | 175.3 / 3.2 MB |
| `MN_Fut` | 1 / 0 / 1 | 57 | 1 | 3 | 1 / 0 | 1 / 0 | 0 | 10 / 3 / 1 | 170.0 / 4.3 MB |

All scoped scenes use one active camera, no camera stack, and no serialized camera RenderTexture target. All had zero active 3D Joint components. The post-processing pair is `camera renderPostProcessing flag / active Volume count`; it does not prove that every Volume override has a non-zero visual cost.

### Triage ranking

| Scene | GPU risk | CPU risk | Memory/VRAM risk | Physics risk | Primary reason for rank |
|---|---|---|---|---|---|
| `MN_new_Rua` | Very High | High | Medium | Medium | Highest active renderer count and approximately 1.30 M instanced triangles; 1,295 potential shadow casters |
| `MN_Run` | Very High | Very High | Medium | Very High | Highest active GO count, 1,046 renderers/shadow casters, two real-time shadowing lights, 1,001 colliders, and 15 dynamic rigidbodies |
| `MN_Queda` | High | Very High | Medium | Very High | 1,221 renderers and 2,108 colliders; synchronized tile physics work is already confirmed in the Editor capture |
| `MN_Memoria` | High | Medium-High | Very High | Medium | Highest particle and transparent-renderer counts, two mixed shadowing lights, 297 active Animators, and about 198.6 MB directly referenced textures |
| `MN_BatataQ` | Medium-High | Medium-High | Very High | Low | Highest direct texture estimate, highest mesh-memory estimate, and approximately 428 k instanced triangles |
| `MN_Sumo` | Medium | Low-Medium | High | Low | Moderate render state/transparent load and about 171.4 MB direct textures; prior RTX 3060 GPU capture was inexpensive |
| `RASCUNHO` | Medium | Medium | Low-Medium | Medium | Normal persistent gameplay/lobby load with 18 rigidbodies and 259 colliders |
| `MN_Round6` | Low-Medium | Low-Medium | High | Low | Small live hierarchy/render load but about 175.3 MB directly referenced textures |
| `MN_Fut` | Low-Medium | Medium | High | Low | Low visible renderer/physics count but 1,918 active objects and about 170.0 MB directly referenced textures |

These ranks are relative priorities for measurement. Asset or object count alone is not treated as proof of a bottleneck; visibility, batching, overdraw, shader cost, update cadence, and runtime spawning still need player evidence.

### Representative scene selection

| Selected scene | Coverage reason |
|---|---|
| `MN_new_Rua` | Heaviest static rendering case: highest renderer, triangle, and shadow-caster counts. |
| `MN_Run` | Highest active-object scene and strongest combined lighting/shadow/physics candidate. |
| `MN_Memoria` | Particle/transparency-heavy representative and one of the two highest direct texture estimates. |
| `MN_BatataQ` | Highest direct texture and mesh-memory estimate; separates memory pressure from raw renderer count. |
| `MN_Queda` | Explicitly required, highest collider count, and contains the already measured synchronized floor-query CPU hotspot. |
| `MN_Sumo` | Normal minigame baseline with an existing Editor capture for continuity. |
| `RASCUNHO` | Normal persistent lobby/gameplay baseline and the only selected non-minigame scene. |

Seven scenes cover the requested worst and representative workloads without deeply profiling every scene. `MN_Round6` and `MN_Fut` remain in the inventory but are deferred because their main distinguishing risk is texture residency already represented by `MN_BatataQ`/`MN_Memoria`; their live render and physics counts are otherwise lower.

## Standalone Cross-Scene Baseline

### Capture method and limits

The final build was Windows x86-64 Development, Direct3D 12, 1920x1080, `Ultra`, render scale 1.0, on the Ryzen 7 5700G / RTX 3060 machine. Deep Profile was off. A temporary command-line-only bootstrap used the existing Mirror flow to start a KCP host, load `RASCUNHO`, perform the synchronized scene transition, acknowledge the briefing, ready the local player, and wait for an active match. It then drove the local player's normal `PlayerControlsSO.Move` event in a repeating forward/right/back/left sequence. The bootstrap was removed after the build.

Each steady result is the most recent 2,000 frames after a one-second post-save warm-up and a second buffer clear. Values in the tables are **median / p95 / p99 / maximum**. The fixed input improves camera/visibility and floor-contact coverage over a stationary capture, but one local player cannot reproduce player-player pushes, passing, multi-player tile distribution, or every scripted interaction. `MN_Memoria` therefore does not include a guaranteed peak reveal/effect cycle, and `MN_BatataQ`/`MN_Sumo` do not represent multi-player exchange/contact stress. These are representative moving one-player baselines, not full-match worst cases.

The resolved Player settings were logged in every run: `Ultra`, 1920x1080 fullscreen, render scale 1.0. PlayerPrefs overrode the requested windowed command-line flag, so the logged resolution/mode—not the launch arguments—is authoritative. The build completed with 0 errors; existing obsolete/compiler warnings remained. Raw captures and Player logs are in `ProfilerCaptures/`.

### CPU and allocation distributions

All time values are milliseconds; GC is bytes allocated per frame.

| Scene | PlayerLoop | Main Thread active | Render Thread active | Scripts | Physics | GC B/frame |
|---|---:|---:|---:|---:|---:|---:|
| `RASCUNHO` | 2.114 / 2.705 / 3.066 / 5.773 | 2.121 / 2.724 / 3.087 / 5.786 | 0.906 / 1.402 / 1.643 / 2.256 | 0.790 / 1.047 / 1.194 / 1.492 | 0.019 / 0.246 / 0.300 / 0.371 | 5,232 / 5,352 / 5,352 / 5,472 |
| `MN_new_Rua` | 3.409 / 4.124 / 4.607 / 6.748 | 3.421 / 4.140 / 4.557 / 6.769 | 1.659 / 2.414 / 2.719 / 3.812 | 1.015 / 1.280 / 1.466 / 1.945 | 0.016 / 0.199 / 0.240 / 0.452 | 896 / 896 / 896 / 2,720 |
| `MN_Run` | 4.007 / 4.966 / 5.511 / 7.260 | 4.019 / 4.983 / 5.527 / 7.269 | 2.133 / 2.961 / 3.373 / 4.003 | 1.204 / 1.578 / 1.769 / 2.231 | 0.021 / 0.310 / 0.365 / 0.971 | 11,360 / 11,360 / 12,684 / 23,284 |
| `MN_Memoria` | 2.578 / 3.399 / 4.021 / 6.342 | 2.584 / 3.412 / 4.040 / 6.354 | 1.000 / 1.630 / 1.973 / 2.477 | 0.941 / 1.254 / 1.481 / 1.863 | 0.008 / 0.154 / 0.184 / 0.385 | 17,524 / 17,524 / 17,556 / 19,348 |
| `MN_BatataQ` | 2.886 / 3.568 / 3.953 / 6.481 | 2.898 / 3.579 / 4.010 / 6.491 | 1.332 / 2.027 / 2.331 / 3.274 | 1.040 / 1.312 / 1.525 / 2.023 | 0.018 / 0.156 / 0.183 / 0.747 | 816 / 976 / 1,008 / 90,040 |
| `MN_Queda` | 2.607 / 3.424 / 7.384 / 13.358 | 2.616 / 3.436 / 7.397 / 13.368 | 0.951 / 1.531 / 1.851 / 2.457 | 0.953 / 1.294 / 3.297 / 4.751 | 0.015 / 0.165 / 2.168 / 2.876 | 736 / 816 / 928 / 144,728 |
| `MN_Sumo` | 2.520 / 3.166 / 3.576 / 4.176 | 2.454 / 3.101 / 3.503 / 4.181 | 1.210 / 1.864 / 2.270 / 3.313 | 0.932 / 1.234 / 1.417 / 1.881 | 0.010 / 0.142 / 0.178 / 0.253 | 896 / 896 / 928 / 2,752 |

`MN_Run` has the highest median PlayerLoop and render-thread cost. `MN_Queda` is the clear tail-risk outlier: its Physics p99 is 2.168 ms despite a 0.015 ms median, and the Scripts and PlayerLoop distributions rise with it. This reproduces the earlier periodic floor-query shape outside the Editor. `MN_Memoria` and `MN_Run` have persistent GC allocation rather than isolated capture noise; the exact allocation sites require a short GC call-stack capture before any optimization decision.

### GPU and submission distributions

| Scene | GPU frame ms | SetPass | Triangles | Vertices | Draw calls / batches |
|---|---:|---:|---:|---:|---|
| `RASCUNHO` | 0.699 / 0.943 / 0.984 / 1.075 | 57 / 58 / 58 / 58 | 195,601 / 195,605 / 195,607 / 195,609 | 143,957 / 143,965 / 143,969 / 143,973 | unavailable / unavailable |
| `MN_new_Rua` | 1.019 / 1.107 / 1.145 / 1.319 | 79 / 80 / 80 / 81 | 863,960 / 870,612 / 871,800 / 872,882 | 908,648 / 913,084 / 913,088 / 913,812 | unavailable / unavailable |
| `MN_Run` | 1.148 / 1.585 / 1.679 / 2.018 | 71 / 72 / 72 / 73 | 583,520 / 583,522 / 583,522 / 583,524 | 444,400 / 444,404 / 444,404 / 444,408 | unavailable / unavailable |
| `MN_Memoria` | 0.670 / 0.779 / 0.813 / 0.846 | 49 / 51 / 52 / 53 | 109,706 / 111,348 / 111,358 / 111,362 | 124,155 / 125,561 / 125,581 / 125,589 | unavailable / unavailable |
| `MN_BatataQ` | 0.866 / 0.922 / 0.964 / 0.986 | 69 / 71 / 72 / 73 | 541,336 / 542,988 / 543,820 / 544,262 | 449,555 / 450,947 / 451,433 / 451,605 | unavailable / unavailable |
| `MN_Queda` | 0.912 / 1.019 / 1.074 / 1.459 | 63 / 64 / 65 / 66 | 72,994 / 117,018 / 130,780 / 133,016 | 64,342 / 123,255 / 141,821 / 144,841 | unavailable / unavailable |
| `MN_Sumo` | 1.352 / 1.412 / 1.457 / 1.493 | 64 / 65 / 65 / 66 | 385,470 / 385,482 / 385,484 / 385,488 | 242,463 / 242,487 / 242,491 / 242,499 | unavailable / unavailable |

Unity 6's remote D3D12 stream did not expose `Draw Calls Count`; `Batches Count` returned zero while SetPass and geometry were non-zero, so zero is treated as unavailable rather than a real measurement. No draw estimate is substituted into the 2,000-frame distributions. The result also demonstrates why the static inventory was triage only: `MN_new_Rua` submits the most geometry and SetPass work, while `MN_Run` has the highest GPU p95/p99/max and `MN_Sumo` the highest GPU median. None is close to a 16.67 ms GPU budget on the RTX 3060.

### Remote Player Frame Debugger

Frame Debugger snapshots were captured from Development Players for the three selected render-risk scenes:

| Scene | Total frame events | Opaque SRP-batcher events | Main-light shadow events | Transparent events | Repeated post/intermediate work |
|---|---:|---:|---:|---:|---|
| `MN_new_Rua` | 85 | 35 | 3 | 4 SRP-batcher + 2 Canvas sub-batch | Bloom: 1 prefilter, 10 downsample, 5 upsample; Gaussian DoF: 5; final post blit: 1 |
| `MN_Run` | 73 | 25 | 3 | 2 SRP-batcher + 2 Canvas sub-batch | Same Bloom/DoF/final-blit chain |
| `MN_Queda` | 71 | 16 | 3 | 2 SRP-batcher + 5 non-SRP + 2 Canvas sub-batch | Same Bloom/DoF/final-blit chain |

Frame events include clears, blits, procedural events, UI, and draws, so the totals are not draw-call counts. The snapshots confirm one main-light shadow-map pass and the full Bloom + Gaussian DoF + final post-processing chain in all three scenes. The GPU Profiler categorized most time as `Other`, so it did not provide trustworthy per-pass GPU timings; the Frame Debugger establishes pass presence and event composition, not that any individual pass is a confirmed bottleneck.

## Memory / VRAM Analysis

### Warmed Development Player memory

Values are MB. Total and resident are shown as median / maximum over the same 2,000 frames; the other columns are medians because they were effectively flat after warm-up.

| Scene | Total used | Texture | Mesh | RenderTexture | Gfx used indicator | App resident |
|---|---:|---:|---:|---:|---:|---:|
| `RASCUNHO` | 545.544 / 547.132 | 185.162 | 9.889 | 64.986 | 289.842 | 824.934 / 824.934 |
| `MN_new_Rua` | 837.255 / 840.165 | 295.789 | 57.439 | 103.571 | 511.159 | 935.477 / 935.504 |
| `MN_Run` | 722.084 / 723.945 | 297.244 | 7.496 | 103.571 | 402.929 | 910.371 / 910.371 |
| `MN_Memoria` | 700.481 / 702.157 | 289.493 | 6.201 | 85.321 | 390.808 | 899.121 / 899.289 |
| `MN_BatataQ` | 669.452 / 670.496 | 238.009 | 11.465 | 103.571 | 349.429 | 953.027 / 953.777 |
| `MN_Queda` | 792.675 / 797.201 | 233.294 | 7.677 | 103.571 | 454.522 | 956.977 / 957.125 |
| `MN_Sumo` | 715.659 / 716.553 | 296.584 | 9.503 | 103.571 | 401.243 | 902.316 / 902.316 |

`MN_new_Rua` is the highest warmed Unity allocation and graphics-allocator case, and it is the clear mesh-memory outlier. `MN_Queda` and `MN_BatataQ` have the highest process resident readings even though their Unity texture counters are not the highest; process residency includes code, managed/native heaps, loaded shared assets, and other allocations, so it is not a VRAM measurement. Likewise, `Gfx Used Memory` is Unity's graphics allocator indicator, not physical VRAM residency or the driver's complete budget.

The player results differ from the static scene estimates because the running host includes persistent lobby assets, spawned player/UI content, URP buffers, and shared resources. This is expected and is why static texture counts were not used as proof of pressure. No serialized camera targets a RenderTexture, yet the warmed runtime uses 65–104 MB of RenderTextures, primarily consistent with URP/post-processing intermediates at 1080p.

### Observed transition/loading spikes

The transition profile was collected separately from the steady window. Deltas are minimum→maximum within the retained buffer and can include both loading and unloading; they are not sums and are not equivalent to peak physical VRAM residency.

| Scene | Max Player frame | Max GC allocation | Total used delta | Texture delta | Mesh delta | RT delta | Gfx-used delta | Coverage |
|---|---:|---:|---:|---:|---:|---:|---:|---|
| `RASCUNHO` | 5.297 ms | 5,472 B | +3.134 MB | 0 | 0 | 0 | 0 | Late host/lobby window; startup already resident |
| `MN_new_Rua` | 109.650 ms | 3,434,897 B | +324.283 MB | +138.587 MB | +52.275 MB | +66.534 MB | +249.267 MB | Complete retained scene transition |
| `MN_Run` | 14.612 ms | 56,736 B | +4.912 MB | +1.000 MB | +0.012 MB | 0 | +1.009 MB | Rolling buffer retained only the late transition/warm-up tail |
| `MN_Memoria` | 121.259 ms | 3,503,533 B | +206.750 MB | +150.557 MB | +6.494 MB | +66.534 MB | +147.131 MB | Complete retained scene transition |
| `MN_BatataQ` | 116.352 ms | 3,494,923 B | +155.788 MB | +126.664 MB | +5.292 MB | +66.534 MB | +126.816 MB | Complete retained scene transition |
| `MN_Queda` | 259.101 ms | 7,322,352 B | +281.717 MB | +126.038 MB | +5.025 MB | +66.534 MB | +191.977 MB | Complete retained scene transition |
| `MN_Sumo` | 9.278 ms | 60,376 B | +4.091 MB | +1.000 MB | +0.011 MB | 0 | +1.016 MB | Rolling buffer retained only the late transition/warm-up tail |

The strongest measured load-pressure candidates are `MN_Queda`, `MN_new_Rua`, `MN_Memoria`, and `MN_BatataQ`. The `MN_Run` and `MN_Sumo` rows must not be interpreted as proof that their full scene loads are cheap: their high lobby frame rate filled the 3,000-frame rolling history before the complete transition could be retained. Re-running those two with an in-player marker capture or a larger history is the remaining loading-only follow-up; their steady-state measurements are complete.

The project disables streaming mipmaps in every current quality level. A 12 GB RTX 3060 can absorb these warmed allocations and uploads without proving safety for integrated graphics or a 2–4 GB card. Shared graphics memory competes with system RAM, and the observed 126–151 MB texture jumps plus 66.5 MB RenderTexture jumps can be materially more disruptive on those systems.

## Entry-Level Hardware Risks

The RTX 3060 standalone results establish comfortable headroom only on the measured machine: GPU p99 ranged from 0.813 ms in `MN_Memoria` to 1.679 ms in `MN_Run`. They do not predict integrated graphics, low-power laptop GPUs, or 2–4 GB discrete cards. The measured and static evidence indicates these likely poor-scaling characteristics:

- Entry-level GPU/render submission: `MN_Run` has the highest measured GPU p99/max (1.679/2.018 ms) and render-thread p99/max (3.373/4.003 ms). `MN_new_Rua` has the highest measured SetPass and geometry load. The Frame Debugger also shows a shadow pass plus Bloom, Gaussian DoF, and final post-processing in all three inspected scenes. Those costs can scale much worse with fill rate, bandwidth, and driver overhead than they did on the RTX 3060.
- Limited/shared graphics memory: warmed texture memory is 185–297 MB, RenderTexture memory 65–104 MB, and Unity's graphics-used indicator 290–511 MB across the selected scenes. Complete transitions added 126–151 MB of texture memory and 66.5 MB of RenderTextures in the strongest observed cases. These are allocator indicators rather than physical VRAM residency, but the lack of streaming mipmaps and competition with system RAM make the load/upload peaks the main low-memory concern.
- Weaker CPUs: `MN_Run` has the highest measured median PlayerLoop at 4.007 ms. `MN_Queda` has the clearest tail sensitivity, with PlayerLoop p99/max of 7.384/13.358 ms and Physics p99/max of 2.168/2.876 ms. `MN_Memoria` and `MN_Run` also allocate persistently at about 17.5 KB and 11.4 KB per frame in these captures; allocation sites are not yet attributed.
- Multiplayer CPU/network growth: per-player physics contacts, movement/animation, network serialization/interest work, and player-triggered scene logic. Static scene counts do not measure that growth.

Current project controls that could provide meaningful scalability, without changing them in this audit:

| Control | Current evidence | Likely value / validation need |
|---|---|---|
| Shadows | Quality levels and `SettingsGraphics` can disable, hard-only, or all shadows; URP assets vary shadow distance/cascades | Highest-value candidate for `MN_Run`, `MN_new_Rua`, and `MN_Queda`; validate GPU and render-thread deltas |
| Render scale | `SettingsMenu` applies the URP render-scale slider; pipeline assets default to 1.0 | Strong bandwidth/fill-rate lever for entry GPUs; validate UI range and image quality |
| Texture quality | UI changes `globalTextureMipmapLimit`; `Very Low` uses limit 3 | Strong VRAM/shared-memory lever; validate that all important textures respect mip limits and measure loading/residency |
| Anti-aliasing | Default `Ultra` pipeline asset uses 1x MSAA, but `Very Low` and `Performant` assets use 4x; a separate UI command also changes `QualitySettings.antiAliasing` | Current tiers are internally inconsistent, so lower tier does not necessarily mean cheaper AA; verify resolved player setting before relying on it |
| Post-processing | Cameras generally enable post-processing; UI exposes DoF and motion blur, while the Bloom toggle application is commented out | Measure Bloom/DoF/motion blur and intermediate blits per selected scene; current user-facing scalability is partial |
| Particle quality | Quality tiers vary `particleRaycastBudget`, but no project-wide emission/render-quality control was found | Useful mainly for `MN_Memoria`/`MN_Queda`; a future setting needs evidence from peak-effect captures |
| LOD/draw distance | UI exposes LOD bias 0.5/1/2/4; `Performant` is 0.4. No general project-specific draw-distance control was found | Potentially useful for `MN_new_Rua`/`MN_Run`; effectiveness depends on actual LODGroup coverage and camera visibility |
| Resolution/fullscreen | Existing UI changes output resolution and fullscreen mode | Reliable fallback when render scale alone is insufficient; benchmark fixed 1080p first |
| Anisotropic filtering/reflections | Existing texture-filtering UI; lower tiers disable real-time reflection probes | Secondary levers for bandwidth and texture sampling; validate visual impact |

The measured runs resolved to `Ultra`, 1920x1080 fullscreen, and render scale 1.0. Project assets define that tier with full-resolution textures, LOD bias 1, 20 m shadow distance, one cascade, and 1x MSAA, but PlayerPrefs/UI can override parts of the configuration at startup. Future hardware and settings comparisons must log the resolved quality level, render scale, resolution, AA, shadow mode/distance, texture mip limit, LOD bias, and enabled post effects. No quality default or graphics setting was changed in this audit.

## Multiplayer Scaling Candidates

The three candidates most likely to change with player count are:

1. `MN_Queda`: server-authoritative tile checks, 2,108 active colliders, 1,047 serialized floor-tile scripts, tile state replication, and player distribution across the floor.
2. `MN_Run`: dynamic rigidbodies/vehicles, 1,001 colliders, high active-object/script counts, and a heavy rendering baseline that can expose combined CPU/render contention.
3. `MN_Sumo`: representative player-player contact, push/projectile/ball interaction, animation, and network fan-out, with an existing one-host Editor baseline for comparison.

The completed moving one-player host baselines provide the comparison anchor:

| Scene | PlayerLoop ms (median / p95 / p99 / max) | Physics ms (median / p95 / p99 / max) | GPU ms (median / p95 / p99 / max) |
|---|---:|---:|---:|
| `MN_Queda` | 2.607 / 3.424 / 7.384 / 13.358 | 0.015 / 0.165 / 2.168 / 2.876 | 0.912 / 1.019 / 1.074 / 1.459 |
| `MN_Run` | 4.007 / 4.966 / 5.511 / 7.260 | 0.021 / 0.310 / 0.365 / 0.971 | 1.148 / 1.585 / 1.679 / 2.018 |
| `MN_Sumo` | 2.520 / 3.166 / 3.576 / 4.176 | 0.010 / 0.142 / 0.178 / 0.253 | 1.352 / 1.412 / 1.457 / 1.493 |

Test only 1, 4, and 6 players. The one-player anchor is complete; the 4- and 6-player runs remain planned. Use one host plus 0/3/5 remote clients; capture the host process and at least one remote client separately because host-mode combines server and local rendering work. Keep resolution, quality, camera path, match phase, and movement script/input sequence fixed. After warm-up, capture the same duration and report PlayerLoop/Main/Render/Scripts/Physics/GC, GPU/render counters, memory, and network traffic/serialization where available. Compare absolute med/p95/p99/max values and the incremental cost from 1→4 and 4→6 players. Six players is a considered capacity target, not authorization to test beyond six.

No multiplayer scaling result is claimed until those real multi-process captures exist.

## Reproducible Benchmark Automation

An opt-in Development Player harness and Windows runner now reproduce the one-player cross-scene workflow without changing normal gameplay. The harness is inert unless launched with `--performance-audit-scene` and is excluded from non-development builds. It starts the existing KCP host flow, performs the synchronized scene transition and briefing-ready sequence, waits for the active match, drives the same deterministic movement pattern, warms up, and samples a fixed frame count.

From an open Editor, use **Tools > Performance Audit** to build the Player, run the three core scenes, run all seven representative scenes, or create the portable ZIP. Terminal usage and parameters are documented in `Tools/PerformanceAudit/README.md`. Each output records machine/graphics identity, resolved quality/resolution/render scale, metric source, median/p95/p99/max, missing counters, Player log, and optionally a Unity `.raw` capture. `-CompareWithPrevious` compares only runs with matching scene sets, frame count, warm-up, quality, resolution, render scale, fullscreen mode, CPU, and GPU.

The portable artifact is `Builds/PerformanceAuditPortable/TCC-Performance-Benchmark.zip`. After extraction it runs on a Windows PC without Unity or the source checkout and writes a transferable `Results` directory. Cross-PC runs measure hardware scalability; code-change claims still require before/after builds on the same PC under matched power, thermal, display, quality, and workload conditions. This remains a one-player host benchmark and does not replace the planned real 4/6-player captures.
