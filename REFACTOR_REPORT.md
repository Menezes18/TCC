# Refactor audit — 2026-09-09

Changes cover first-party gameplay and networking scripts. At the user's request, Packages/manifest.json now references the existing local MCP for Unity package; the user started the server after automatic launch was rejected. MCP connectivity was subsequently verified against TCC@ff9c94878a6da380, including a live editor-state query, refresh, and test execution. Existing package upgrades, project settings, materials, scenes, and third-party code were not intentionally changed by this refactor. Script GUIDs and serialized UnityEvent method names were preserved. The already-invalid SceneTransitionManager prefab GUID was replaced and its bogus script reference corrected; neither invalid identifier had external references.

## Correctness and networking

- Fixed SceneTransitionManager.prefab metadata that caused Unity to ignore the asset: replaced a non-hexadecimal prefab GUID and pointed the component at the real script GUID.

- Floor state is allocated and tiles are bound in Awake, before client snapshot requests. Removed the unused tile dictionary and initialization coroutine.
- Flush queued floor progress before immediate destruction or initial snapshots. Snapshot chunks are sent within one server tick, avoiding interleaved stale snapshot data. Reset batches are flushed before yielding; repeated resets cancel the previous reset coroutine.
- Falling floors use server collision detection, synchronize collider state for late join, and cancel their previous fall coroutine on reset.
- Remote floor resets reactivate destroyed objects, restore their positions, and clear activation flags. Legacy networked breaking floors now explicitly update visuals on dedicated servers and on reset.
- Floor activation commands verify that the sending player's collider is actually in range. Glass tiles and finish triggers use server collision detection, matching the race finish implementation. Broken glass uses synchronized state for late joiners rather than transient RPCs alone.
- Votes derive the Steam ID from the sender's player object. Projectile requests validate ownership, finite direction, player state, and server cooldown; spawn origin comes from the server's muzzle transform.
- Briefing readiness resets each round and recalculates expected acknowledgements on disconnect. The old CmdAtivarPlayersNoServer UnityEvent name is preserved as a server-only compatibility wrapper.
- Roulette completion uses a server deadline covering spin and winner UI; missing UI callbacks cannot stall a round and clients cannot shorten it. Removed the unused duplicate roulette RPC and client-controlled standalone spin/overlay commands.
- Lobby countdown and global briefing-freeze operations that have only server callers are server methods rather than authority-free commands. Repeated match-start signals no longer restart an active/finalized match.
- Lobby voting delay is guarded against duplicate scheduling. Missing VotingManager uses the existing fallback instead of attempting to network-spawn an unregistered runtime object.
- Steam list requests use CallResult handles so a superseded response cannot complete the wrong search. Automatic matchmaking waits for completion with a realtime timeout and uses each lobby's capacity. Lobby entry checks the actual Steam response instead of treating the privacy flag as failure.
- Duplicate scene-preload requests are rejected while loading. Destroying the transition manager releases blocked scene activation. Post-activation acknowledgements use frame yields that also work without rendering. Solo player initialization has a bounded wait.

## Duplication and abstractions

- Removed the inherited empty observer method and no-op observer self-subscriptions from five minigame controllers.
- Removed unused DispatchPoints and FindController helpers, and the legacy breaking-floor adapter that could never bind to the manager's ChaoQuebrandoSimples array.
- Scoreboard presentation uses virtual controller capabilities for teams and percentages rather than concrete controller checks.
- Added a read-only minigame catalog accessor and replaced three reflection lookups. Stadium waves use a typed configuration method that updates the values cached in Awake.
- Kept the empty Obsolete/GameManager component because its GUID is still referenced by Assets/Scenes/Main.unity. Blanket removal of apparently unused Unity components would create broken scene references.

## CPU, allocations, and rendering

- Floor overlap queries reuse a buffer and grow it on saturation, avoiding lost hits in dense scenes. Activated server tiles stop scanning for players.
- Final ranking is built at match end, when it is consumed, instead of allocating/sorting every frame.
- Scoreboard notifications are coalesced to at most 10 updates per second. Player lookup and race progress bounds are computed once per refresh instead of scanning per row.
- Melting floors synchronize one start timestamp and removal state instead of sending cutoff RPCs every frame. Fixed hidden base Awake initialization, multi-collider occupancy, reset behavior, and double material instantiation; owned material instances are released on destruction.
- No measured CPU/GPU frame-time improvement is claimed. Rendering changes are limited to eliminating redundant material work; shader complexity, overdraw, draw calls, and GPU bottlenecks still require profiling representative scenes.

## Validation

- Runtime C# compilation: passed with zero errors; existing deprecation/unused-field warnings remain.
- Editor C# compilation including the six new regression tests: passed with zero errors.
- Tests cover remote reset/reuse, remote progress selection, early floor-state initialization with missing references, saturated overlap buffers, wave configuration after Awake, and broken-glass late join.
- Unity EditMode execution: six tests passed, zero failed; repeated successfully through MCP against the current editor at 2026-09-09 13:40 UTC (job 2e476bb1d2dc4324b7099a10d942e4ca). The first attempt found a missing collider in the test fixture, which was corrected.
- Unity compilation and Mirror weaving succeeded. Later command/readiness changes also passed C# compilation and the focused suite was rerun through MCP after refresh; two-client runtime validation remains outstanding.

Run the focused suite with **Tools > TCC > Run Refactor Regression Tests**, or create `Temp/run-refactor-regressions` while the editor is open. Results are written to `Temp/refactor-regressions.xml`. The runner does not enter Play Mode.

## Remaining runtime checks

Use a host and remote client to exercise join-by-code, matchmaking failure, reconnect, readiness, scene transitions, floor destruction/reset, late join, voting and projectile cooldown. Include a dedicated server check for collision/visual separation. Client-authoritative movement still limits cheat resistance: validating actions against server-observed transforms is not authoritative movement simulation.

Cinematic completion still uses client notifications, now gated by server briefing readiness and idempotent match start. Full server scheduling of cinematics, Steam authentication, transport behavior, and production multiplayer were not exercised here. This patch does not claim that every online path has been hardened.

The console also contains existing editor-package exceptions from vHierarchy (VHierarchyLibs.cs:370) and Unity AI Assistant tracing. These are separate from the refactored gameplay scripts and did not prevent the six focused tests from passing.
