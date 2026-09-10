# Unity project audit

Updated: 2026-09-09. Scope: architecture/inheritance/polymorphism, networking correctness/security, async/concurrency, and serious CPU/GPU bottlenecks.

This is a static audit of the current working tree, including pre-existing uncommitted changes. No gameplay code or assets were modified. No Unity play mode, player build, exploit reproduction or profiler run was performed; an editor-generated solution build was used only as a static compile check. Unity MCP was unnecessary for the source-level findings. Severity describes likely impact; conditions and unverified runtime wiring are stated explicitly.

## Inspection coverage

- Inventory: **248 C# files under Assets/Scripts**.
- **236 fully read files / 248 = 95.16%**.
- **12 additional files received focused section review**.
- **248 files inspected at least partly / 248 = 100.00%**.
- Remaining: **0 files not substantively reviewed**. Pattern searches alone do not count as inspection.
- This is file-based coverage of the main script directory, not a percentage of all assets, source lines, runtime paths or the entire Unity project. Third-party libraries, most editor scripts outside this directory, shaders and scenes are outside the denominator. No defensible whole-project validation percentage is claimed.
- The previous conversational ~10% estimate was approximate and included focused reads; this checklist replaces it with explicit full/partial counts. A checked box does not mean a file is bug-free.

## 1. Critical

None confirmed in the inspected scope.

## 2. High

### H01 — Unchecked combat command permits remote attacks

- **Area:** Networking/security.
- **Files:** [Gameplay/Player/Core/PlayerActiveFrame.cs:64](Assets/Scripts/Gameplay/Player/Core/PlayerActiveFrame.cs#L64); [Gameplay/Minigames/HotPotato/HotPotatoMinigameController.cs:319](Assets/Scripts/Gameplay/Minigames/HotPotato/HotPotatoMinigameController.cs#L319); [Gameplay/Player/Core/PlayerScript.cs:1138](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1138).
- **Cause/impact:** `CmdRequestPush` trusts target identity, damage type and direction. It checks neither distance, attack cooldown, alive/frozen state, nor valid targets. The hot-potato distance check is commented out. An owning client can repeatedly push/blind remote targets or transfer its potato across the map; null/non-damageable identities also throw.
- **Recommended fix:** Resolve and validate the target on the server, enforce attack timing/state/range, derive the permitted damage type and direction, validate finite vectors, and deduplicate victims per attack. The command is attached to `Assets/Prefab/Player/Player.prefab`.

### H02 — Client-supplied Steam identity controls score registration

- **Area:** Networking/security.
- **Files:** [Gameplay/Lobby/PlayerData.cs:104](Assets/Scripts/Gameplay/Lobby/PlayerData.cs#L104); [Infrastructure/Network/MyNetworkManager.cs:152](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L152); [FizzySteamworks/NextServer.cs:216](Assets/Mirror/Runtime/Transports/FizzySteamworks/NextServer.cs#L216); [offline.unity:42645](Assets/Scenes/offline.unity#L42645).
- **Cause/impact:** `CmdSetPlayerInfo` accepts an arbitrary Steam ID on every call. Registration uses that ID to retrieve another player's stored score/alias or add a new scoreboard entry. Command ownership proves ownership of the network object, not ownership of the submitted Steam ID. The configured NetworkManager has no Mirror authenticator. The active Steam transport already maps each Mirror connection to the remote Steam ID and exposes it through `ServerGetClientAddress`; application code ignores that binding. The KCP development fallback has no equivalent Steam identity, so it must use a separate development identity policy.
- **Recommended fix:** On FizzySteamworks, parse and bind the connection's transport-provided address once on server admission; never accept the Steam ID from the command. Reject duplicate identities and subsequent identity changes, bound/escape display names, and define a clearly isolated identity scheme for KCP development sessions.

### H03 — Chat bypasses authentication and amplifies message floods

- **Area:** Networking/security; CPU.
- **Files:** [UI/Chat/ChatManager.cs:119](Assets/Scripts/UI/Chat/ChatManager.cs#L119); [UI/Chat/ChatManager.cs:143](Assets/Scripts/UI/Chat/ChatManager.cs#L143); [UI/Chat/ChatManager.cs:508](Assets/Scripts/UI/Chat/ChatManager.cs#L508).
- **Cause/impact:** The server handler explicitly disables authentication and rebroadcasts every nonblank message without a server-side size/rate limit. The 140-character limit exists only in the client UI. Each delivery rebuilds retained chat text and forces canvas updates, so flooding amplifies network traffic and client CPU work. Rich text and newlines also allow message formatting/spoofing.
- **Recommended fix:** Require authenticated, admitted players; enforce message length and per-connection rate limits before broadcasting; escape rich text/newlines and avoid a forced canvas rebuild for every incoming packet. Magnitude of frame-time impact requires profiling.

### H04 — Death and status authority are split between server and owner

- **Area:** Architecture; networking/security.
- **Files:** [Gameplay/Player/Core/PlayerScript.cs:100](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L100); [Gameplay/Player/Core/PlayerScript.cs:1328](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1328); [Gameplay/Player/Core/PlayerScript.cs:1355](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1355); [Gameplay/Player/Core/PlayerScript.cs:1694](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1694); [Gameplay/Hazards/ContextualKillZone.cs:9](Assets/Scripts/Gameplay/Hazards/ContextualKillZone.cs#L9); [Gameplay/Combat/PrefabInstancer.cs:27](Assets/Scripts/Gameplay/Combat/PrefabInstancer.cs#L27).
- **Cause/impact:** `IsDead` reads a local, non-SyncVar state. Contextual death reporting is owner-only; `ServerForceSpectate` instructs the owner but does not set server death state. Separate commands let the owner clear stagger/blind/spectator flags. Consequently the server's dead-player combat gate can remain false for remote dead players, and a modified owner can suppress the client-controlled death path. Server hazard UnityEvents may still eliminate a player separately; that does not repair the state split.
- **Recommended fix:** Maintain authoritative death/status state and expiry on the server, update it before sending effects, and use one validated transition API. Keep prediction/presentation separate from gameplay state.

### H05 — Authoritative obstacle availability is changed only inside client RPCs

- **Area:** Networking correctness.
- **Files:** [Gameplay/Minigames/Sumo/SumoMinigameController.cs:202](Assets/Scripts/Gameplay/Minigames/Sumo/SumoMinigameController.cs#L202); [Gameplay/Minigames/Sumo/SumoMinigameController.cs:209](Assets/Scripts/Gameplay/Minigames/Sumo/SumoMinigameController.cs#L209); [Gameplay/Minigames/FloorBreaking/ChaoSumindo.cs:88](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoSumindo.cs#L88); [Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs:171](Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs#L171); [Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs:197](Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs#L197).
- **Cause/impact:** Sumo disables floor objects only in `RpcDisableStep`; memory tiles similarly use `RpcTiraChao`/`RpcPoeChao`. A real Fall Guys door sets `opened` on the server, but makes its rigidbody dynamic and later disables both colliders only inside ClientRpc bodies. A dedicated server therefore keeps the opened door physically closed. These event-only changes also omit persistent presentation state for late joiners. Sumo's `RpcEnsureVisible` is an ordinary method despite its name, so remote clients do not receive that visibility restoration.
- **Recommended fix:** Apply collider/availability state on the server and replicate persistent state with client hooks. Keep visual physics/effects separate and synchronize missing visibility/open-state presentation.

### H06 — Scene-transition ACKs are not bound to participants or transition phase

- **Area:** Networking/security; concurrency.
- **Files:** [Infrastructure/Network/SceneTransitionManager.cs:129](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L129); [Infrastructure/Network/SceneTransitionManager.cs:630](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L630); [Infrastructure/Network/SceneTransitionManager.cs:1054](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L1054); [Infrastructure/Network/SceneTransitionManager.cs:1094](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L1094).
- **Cause/impact:** ACK handlers bypass authentication and add any sender connection ID while `_isTransitioning`. They do not require membership in `_playerLoadStates`, the expected preload/activation phase, or a transition identifier. Counts are compared against the tracked participant count. An out-of-set sender can count toward the barrier, and delayed/early acknowledgements can be attributed to the wrong phase/transition.
- **Recommended fix:** Require authenticated participant membership, attach a transition ID to both messages and ACKs, enforce phase ordering, and count only eligible participants. Reject ACKs for disconnected/timed-out participants.

### H07 — Voting RPC mutates server-owned SyncLists on remote clients

- **Area:** Networking correctness.
- **Files:** [VotingManager.cs:274](Assets/Scripts/Minigames/Voting/VotingManager.cs#L274); [SyncList.cs:71](Assets/Mirror/Core/SyncList.cs#L71); [SyncList.cs:273](Assets/Mirror/Core/SyncList.cs#L273); [NetworkBehaviour.cs:244](Assets/Mirror/Core/NetworkBehaviour.cs#L244).
- **Cause/impact:** `RpcSyncVotingOptions` calls Clear/Add on replicated option lists specifically on pure clients. In the bundled Mirror implementation, spawned server-to-client SyncLists are not client-writable. Clear empties the underlying local list and then throws, aborting the RPC before option reconstruction. Host-only testing avoids this branch and hides the defect.
- **Recommended fix:** Choose one replication path: let SyncLists deserialize and rebuild a separate client cache, or send an RPC snapshot into ordinary client collections. Never mutate authoritative SyncLists on receiving clients; test with a separate client process.

### H08 — Hidden minigame solutions are replicated to clients

- **Area:** Networking/security.
- **Files:** [GlassTile.cs:14](Assets/Scripts/Gameplay/Minigames/Glass/GlassTile.cs#L14); [GlassMinigameController.cs:105](Assets/Scripts/Gameplay/Minigames/Glass/GlassMinigameController.cs#L105); [FallGuysDoor.cs:32](Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs#L32); [FallGuysDoorRow.cs:19](Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoorRow.cs#L19).
- **Cause/impact:** Every glass tile's private `_isSafe` flag and every door's `isReal` flag are SyncVars. The server assigns both solutions before players test them. A modified observing client can read the safe glass side and the real door in every row; C# private visibility does not hide serialized network data. This is a confirmed information disclosure in the code, not a claim of a reproduced exploit.
- **Recommended fix:** Keep unrevealed solution flags server-only. Replicate only revealed/broken/opened presentation state and adjudicate interactions on the server.

### H09 — Disconnected contestants remain in elimination rosters

- **Area:** Networking lifecycle; architecture.
- **Files:** [MyNetworkManager.cs:207](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L207); [HotPotatoMinigameController.cs:73](Assets/Scripts/Gameplay/Minigames/HotPotato/HotPotatoMinigameController.cs#L73); [HotPotatoMinigameController.cs:151](Assets/Scripts/Gameplay/Minigames/HotPotato/HotPotatoMinigameController.cs#L151); [SumoMinigameController.cs:49](Assets/Scripts/Gameplay/Minigames/Sumo/SumoMinigameController.cs#L49); [MemoriaMinigameController.cs:50](Assets/Scripts/Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs#L50); [QuedaMinigameController.cs:39](Assets/Scripts/Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs#L39).
- **Cause/impact:** Disconnect removes the player from global lists and notifies briefing, but these minigames retain independent PlayerData snapshots without a disconnect update. Survivor counts, selection and final placement still include departed contestants. Hot potato can select a destroyed holder reference or count a departed player as the last survivor; remaining players can get incorrect completion/placement. Exact symptom depends on when the disconnect occurs.
- **Recommended fix:** Publish an authoritative participant-left event before destruction. Have each active controller remove or forfeit the stable player ID exactly once, update holder/survivor state, and re-evaluate completion. Keep immutable result data separately from live Unity object references.

### H10 — Runtime UI script imports an editor-only assembly

- **Area:** Build reliability.
- **Files:** [UI/Menus/PopupManager.cs:5](Assets/Scripts/UI/Menus/PopupManager.cs#L5).
- **Cause/impact:** `PopupManager`, a runtime script under `Assets/Scripts` with no editor-only assembly definition, unconditionally imports `UnityEditor`. Unity player compilation does not reference `UnityEditor`, so a standalone player build that includes this script fails to compile even though the editor-generated `.csproj` build succeeds. No player build was run in this audit.
- **Recommended fix:** Remove the unused import. If future editor APIs are required, move that code under an `Editor` folder/assembly or guard both the import and use with `#if UNITY_EDITOR`.

### H11 — Player transform authority accepts arbitrary owner states

- **Area:** Networking/security; movement authority.
- **Files:** [Player.prefab:2162](Assets/Prefab/Player/Player.prefab#L2162); [Player.prefab:2186](Assets/Prefab/Player/Player.prefab#L2186); [SmoothSyncMirror.cs:363](Assets/Smooth%20Sync/Mirror/Smooth%20Sync%20Asset/SmoothSyncMirror.cs#L363); [SmoothSyncMirror.cs:419](Assets/Smooth%20Sync/Mirror/Smooth%20Sync%20Asset/SmoothSyncMirror.cs#L419); [SmoothSyncMirror.cs:2378](Assets/Smooth%20Sync/Mirror/Smooth%20Sync%20Asset/SmoothSyncMirror.cs#L2378); [offline.unity:42646](Assets/Scenes/offline.unity#L42646).
- **Cause/impact:** The configured player prefab uses Smooth Sync with `transformSource = Owner` and full position/rotation synchronization. No gameplay code installs a validation delegate, and the bundled default validator returns `true`; the server therefore relays every state from the owning connection. A modified owner can report teleports, impossible speed or rotation and thereby bypass server trigger/range/placement logic. The configuration and acceptance path are confirmed; no exploit was executed.
- **Recommended fix:** Prefer server-simulated movement from bounded inputs. If owner prediction remains, install per-player server validation for finite values, speed/acceleration, collision/path bounds and state-specific movement, with explicit server-issued teleport tokens and correction/kick policy.

### H12 — Dedicated server has no authoritative results-exit path

- **Area:** Network lifecycle; async/concurrency.
- **Files:** [Minigames/Results/ResultsRow.cs:253](Assets/Scripts/Minigames/Results/ResultsRow.cs#L253); [Minigames/Results/ResultsRow.cs:276](Assets/Scripts/Minigames/Results/ResultsRow.cs#L276); [Gameplay/Match/MatchManager.cs:31](Assets/Scripts/Gameplay/Match/MatchManager.cs#L31); [Gameplay/Match/MatchManager.cs:58](Assets/Scripts/Gameplay/Match/MatchManager.cs#L58); [Gameplay/Match/MatchManager.cs:513](Assets/Scripts/Gameplay/Match/MatchManager.cs#L513).
- **Cause/impact:** The server sends results with a ClientRpc, and each client UI countdown directly tries to start `WaitAndReturnToLobby`, an iterator marked `[Server]`. Remote clients cannot execute that server-only method, while a dedicated server never runs the ClientRpc/UI countdown that calls it. Host mode masks the defect because the host client and server share the process. A dedicated match can remain stuck after results. This is a confirmed call-graph defect; dedicated runtime reproduction was not performed.
- **Recommended fix:** Start one server-owned results deadline when results are finalized, then transition idempotently on the server. Replicate the deadline for UI only; clients must not own scene progression.

### H13 — Custom preload is followed by a second standard Mirror scene load

- **Area:** Network/scene lifecycle; async/concurrency.
- **Files:** [Infrastructure/Network/SceneTransitionManager.cs:725](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L725); [Infrastructure/Network/SceneTransitionManager.cs:742](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L742); [Infrastructure/Network/SceneTransitionManager.cs:754](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L754); [Infrastructure/Network/SceneTransitionManager.cs:922](Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs#L922); [Infrastructure/Network/MyNetworkManager.cs:942](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L942); [Mirror/NetworkManager.cs:870](Assets/Mirror/Core/NetworkManager.cs#L870); [Mirror/NetworkManager.cs:877](Assets/Mirror/Core/NetworkManager.cs#L877); [Mirror/NetworkManager.cs:892](Assets/Mirror/Core/NetworkManager.cs#L892); [Mirror/NetworkManager.cs:931](Assets/Mirror/Core/NetworkManager.cs#L931).
- **Cause/impact:** Each remote client first starts its own single-mode `SceneManager.LoadSceneAsync` and blocks activation at 90%. After broadcasting the custom activation message, the server calls Mirror's ordinary `ServerChangeScene`; Mirror then sends a non-custom `SceneMessage`, and each remote client starts another single-mode `LoadSceneAsync` for the same scene. The two load invocations and missing `customHandling` flag are confirmed. Depending on message/activation timing, the second queued load can stall, reload the scene, or make Mirror finish against a different operation, disrupting spawned state and transition completion; this runtime symptom was not reproduced.
- **Recommended fix:** Use one scene-load owner. Either drive the transition entirely through Mirror, or send Mirror a custom-handled scene message and connect the existing preload operation to Mirror's completion path. Attach a transition ID and test host plus remote clients under delayed loading.

## 3. Medium

### M01 — Delayed death work can execute after respawn

- **Area:** Async/concurrency.
- **Files:** [Gameplay/Player/Core/PlayerScript.cs:1681](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1681); [Gameplay/Player/Core/PlayerScript.cs:1764](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1764); [Gameplay/Player/Core/PlayerScript.cs:1818](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1818).
- **Cause/impact:** Spectator and model-hide coroutines are not cancelled or tied to the life that created them. Respawning before a delay expires can hide the new life or enter spectator mode again.
- **Recommended fix:** Cancel owned death coroutines on respawn/despawn and check a life-generation token plus current death state before applying delayed effects.

### M02 — Minigame base lifecycle does not enforce round cleanup

- **Area:** Architecture/inheritance; concurrency.
- **Files:** [Gameplay/Minigames/Shared/MinigameController.cs:36](Assets/Scripts/Gameplay/Minigames/Shared/MinigameController.cs#L36); [Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs:34](Assets/Scripts/Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs#L34); [Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs:27](Assets/Scripts/Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs#L27); [Minigames/Instrutor/Instrutor.cs:77](Assets/Scripts/Minigames/Instrutor/Instrutor.cs#L77).
- **Cause/impact:** Memory and Queda inherit an `EndMatch` that only emits notifications, leaving their `_matchEnded` false on timeout. Memory's instructor runs an infinite coroutine with no stop method. Their roster/results are initialized at server spawn rather than each match, so restarting the same controller retains round state. The delayed `AddPlayer` also overwrites the alive roster independently of match state.
- **Recommended fix:** Make a common, idempotent lifecycle own active/ended state; use protected setup/cleanup hooks. Reset each round's roster/results at start, cancel delayed roster replacement, and stop/reset the instructor cycle at end.

### M03 — Floor snapshots can be requested repeatedly

- **Area:** Networking/security; CPU.
- **Files:** [Gameplay/Minigames/FloorBreaking/FloorBreakingManager.cs:269](Assets/Scripts/Gameplay/Minigames/FloorBreaking/FloorBreakingManager.cs#L269); [Gameplay/Minigames/FloorBreaking/FloorBreakingManager.cs:279](Assets/Scripts/Gameplay/Minigames/FloorBreaking/FloorBreakingManager.cs#L279).
- **Cause/impact:** Every request from a ready client flushes pending updates and scans the full tile array, allocating/serializing batches. There is no per-connection cooldown or initial-snapshot guard. Cost scales with tile count and request frequency.
- **Recommended fix:** Allow one initial snapshot per connection/scene and rate-limit subsequent recovery requests. Cache/version state where appropriate. Normal gameplay performance impact is not measured.

### M04 — Repeated elimination corrupts placement lists

- **Area:** Architecture/state correctness.
- **Files:** [Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs:79](Assets/Scripts/Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs#L79); [Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs:63](Assets/Scripts/Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs#L63); [Gameplay/Player/Core/HitKillDetection.cs:35](Assets/Scripts/Gameplay/Player/Core/HitKillDetection.cs#L35).
- **Cause/impact:** Both elimination methods append even when removal from `alivePlayers` fails. Repeated trigger/periodic hazard notifications can add the same player more than once while more than one survivor remains. Final scoring then consumes duplicate placement entries and can overwrite that player's awarded points.
- **Recommended fix:** Return unless the player is a valid current survivor and removal succeeds; use a set or explicit per-player elimination state. Make hazard/death processing idempotent.

### M05 — Projectile collision can tunnel and hit a victim repeatedly

- **Area:** Networking/gameplay correctness.
- **Files:** [Gameplay/Combat/Projectile/ProjectileScript.cs:33](Assets/Scripts/Gameplay/Combat/Projectile/ProjectileScript.cs#L33).
- **Cause/impact:** The server moves the projectile then tests only its new position with `OverlapSphere`, so fast motion/long frames can skip targets. After a hit, the current collider loop keeps running: multiple colliders on one victim can cause repeated damage/VFX despite `_hasHit`.
- **Recommended fix:** Sweep from the previous position to the next with the projectile radius. For a single-hit projectile, choose the first valid hit and stop processing; otherwise deduplicate by victim identity. Keep simulation on a consistent physics tick.

### M06 — Vote-zone labels and counts are never replicated

- **Area:** Networking correctness.
- **Files:** [Minigames/Voting/ZoneVoteInputProvider.cs:80](Assets/Scripts/Minigames/Voting/ZoneVoteInputProvider.cs#L80); [Minigames/Voting/ZoneVoteInputProvider.cs:227](Assets/Scripts/Minigames/Voting/ZoneVoteInputProvider.cs#L227); [Minigames/Voting/VoteZone.cs:44](Assets/Scripts/Minigames/Voting/VoteZone.cs#L44); [Minigames/Voting/VoteZone.cs:75](Assets/Scripts/Minigames/Voting/VoteZone.cs#L75).
- **Cause/impact:** The provider creates and initializes zones only on the server. Option data, labels and count are ordinary fields/UI assignments with no SyncVars/RPCs or client initialization. Remote clients get the spawned prefab but not its chosen option or updated count.
- **Recommended fix:** Replicate an option ID/index and vote count; use client hooks to resolve catalog assets and render labels/icons, including initial state for late joiners.

### M07 — Remote customization becomes permanently stale after first apply

- **Area:** Networking correctness; initialization race.
- **Files:** [Gameplay/Player/PlayerCustomizationSync.cs:69](Assets/Scripts/Gameplay/Player/PlayerCustomizationSync.cs#L69); [Gameplay/Player/PlayerCustomizationSync.cs:82](Assets/Scripts/Gameplay/Player/PlayerCustomizationSync.cs#L82).
- **Cause/impact:** `customizationApplied` prevents all later applications, even after the JSON hook invalidates cached data. The 0.1-second initial invocation may apply default indices before real customization arrives and latch the flag. Later updates sent by `UpdateCustomization` then never update that remote visual.
- **Recommended fix:** Apply each new customization revision, reset the applied state on changes, and wait for explicit data/renderer readiness instead of a fixed delay.

### M08 — Alternate ready command bypasses the briefing ACK gate

- **Area:** Networking correctness.
- **Files:** [Gameplay/Lobby/PlayerData.cs:364](Assets/Scripts/Gameplay/Lobby/PlayerData.cs#L364); [Gameplay/Minigames/Shared/BriefingManager.cs:79](Assets/Scripts/Gameplay/Minigames/Shared/BriefingManager.cs#L79); [Gameplay/Minigames/Shared/BriefingManager.cs:305](Assets/Scripts/Gameplay/Minigames/Shared/BriefingManager.cs#L305).
- **Cause/impact:** `CmdMarkClientReady` checks whether all briefing ACKs arrived, but `Cmd_ToggleReady` sets the same flag without that check and invokes `CheckAllReady`, which only checks ready counts. When all flags are set via this alternate path, briefing can finish before its intended ACK barrier.
- **Recommended fix:** Route every ready mutation through one server validation method and enforce the ACK/phase barrier inside the final transition check as well.

### M09 — Moving-platform motion has multiple writers across peers

- **Area:** Networking correctness; runtime wiring to verify.
- **Files:** [Gameplay/Minigames/Street/MovingPlatform.cs:84](Assets/Scripts/Gameplay/Minigames/Street/MovingPlatform.cs#L84); [Gameplay/Minigames/Street/VehicleLane.cs:50](Assets/Scripts/Gameplay/Minigames/Street/VehicleLane.cs#L50).
- **Cause/impact:** `MovingPlatform` moves every tracked enabled CharacterController without ownership/server checks. `VehicleLane` drives remote platform positions directly from a stepped SyncVar timer. Where remote controllers remain enabled, platform code modifies replica positions alongside transform replication, causing correction/jitter risk. Runtime component enablement was not verified.
- **Recommended fix:** Apply platform displacement in the authoritative/predicted movement path only, leave remote replicas to interpolation, and derive smooth platform motion from synchronized time. Confirm with host plus remote client before changing wiring.

### M10 — Color command persists the unvalidated request

- **Area:** Networking correctness/security.
- **Files:** [PlayerData.cs:188](Assets/Scripts/Gameplay/Lobby/PlayerData.cs#L188); [PlayerList.cs:126](Assets/Scripts/Gameplay/Lobby/PlayerList.cs#L126); [MyNetworkManager.cs:174](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L174).
- **Cause/impact:** The server chooses an available color via `ServerRequestColor`, then the command writes the original client-supplied `value` into `pointsBoard`. An unavailable or out-of-range request therefore produces different live and stored colors; re-registration restores the invalid/unavailable stored value. Rendering through Database.GetColor clamps invalid indices, but that does not repair stored state or color ownership.
- **Recommended fix:** Store only the validated assigned color, verify the player's registration, and centralize color allocation/persistence. Handle empty pools explicitly instead of indexing an empty list.

### M11 — Voting finalization has no common replicated lifecycle

- **Area:** Architecture; networking correctness.
- **Files:** [VotingManager.cs:385](Assets/Scripts/Minigames/Voting/VotingManager.cs#L385); [UIVoteInputProvider.cs:324](Assets/Scripts/Minigames/Voting/UIVoteInputProvider.cs#L324).
- **Cause/impact:** `OnVotingEnded` is raised only on the server, while remote UI cleanup subscribes to that ordinary C# event. The synchronized active flag has no end hook, so remote panels are not cleaned up by finalization (scene destruction may hide this). The one-option return also skips the common stop-timer/unfreeze block. Repeated EndVoting calls can recompute a tie winner and emit completion again because the result is not latched.
- **Recommended fix:** Use one idempotent finalize method for all option counts; store the winner, stop the timer, release the voting freeze, and replicate the ended phase/result so clients apply cleanup exactly once.

### M12 — Server accepts votes outside the active voting window

- **Area:** Networking correctness/security.
- **Files:** [VotingManager.cs:305](Assets/Scripts/Minigames/Voting/VotingManager.cs#L305); [UIVoteInputProvider.cs:290](Assets/Scripts/Minigames/Voting/UIVoteInputProvider.cs#L290).
- **Cause/impact:** RegisterVote checks only the option index. Neither it nor the command checks the active phase, deadline, or round ID. The option list remains populated after voting ends, allowing commands to keep changing counts; a delayed old-round command can also target the same index in a new round. The UI's local active check does not protect the server.
- **Recommended fix:** Validate active phase, server deadline, participant eligibility and round ID in RegisterVote. Freeze results before finalization and reject stale/late commands.

### M13 — Dedicated-server vote providers miss the start event

- **Area:** Architecture; networking correctness.
- **Files:** [VotingManager.cs:263](Assets/Scripts/Minigames/Voting/VotingManager.cs#L263); [VotingManager.cs:555](Assets/Scripts/Minigames/Voting/VotingManager.cs#L555); [ZoneVoteInputProvider.cs:48](Assets/Scripts/Minigames/Voting/ZoneVoteInputProvider.cs#L48).
- **Cause/impact:** StartVotingRound reaches OnVotingStarted through a ClientRpc/client-option rebuild. A dedicated server neither executes that RPC body nor subscribes to the client SyncList callbacks, so an already attached ZoneVoteInputProvider never gets the round-start notification and does not spawn zones. Attaching a provider after options exist can mask this through its catch-up branch.
- **Recommended fix:** Emit a server-side round-start notification directly after authoritative initialization, and a separate client presentation notification after replication. Avoid firing the same host subscriber twice.

### M14 — Race respawn work outlives its player or round

- **Area:** Async/concurrency.
- **Files:** [RaceMinigameController.cs:99](Assets/Scripts/Gameplay/Minigames/Race/RaceMinigameController.cs#L99); [RaceMinigameController.cs:221](Assets/Scripts/Gameplay/Minigames/Race/RaceMinigameController.cs#L221).
- **Cause/impact:** Every death starts an untracked delayed respawn. After the yield, it dereferences PlayerData and teleports/respawns without checking player lifetime, connection readiness, active round or generation. EndMatch removes event listeners but does not cancel already-running coroutines. A disconnect can cause a missing-object exception; a match ending or restarting before the delay expires can receive a stale teleport/respawn.
- **Recommended fix:** Own one respawn operation per participant, cancel it on leave/end/despawn, and validate player/connection and round generation after the wait. Avoid duplicate death notifications scheduling multiple respawns.

### M15 — Repeated dropoff binding can orphan owned delivery zones

- **Area:** Architecture/state ownership.
- **Files:** [StreetMinigameController.cs:21](Assets/Scripts/Gameplay/Minigames/Street/StreetMinigameController.cs#L21); [StreetMinigameController.cs:36](Assets/Scripts/Gameplay/Minigames/Street/StreetMinigameController.cs#L36); [StreetMinigameController.cs:199](Assets/Scripts/Gameplay/Minigames/Street/StreetMinigameController.cs#L199).
- **Cause/impact:** Setup and StartMatch both bind unowned dropoffs. The binding loop does not skip players with an existing assignment. If spare zones remain on the second call, it overwrites the player's map entry but leaves the earlier zone owned. Multiple zones then belong to one player while only the newest gets respawn/highlight management, reducing capacity for other participants. The defect is confirmed for that zone-count/call-sequence condition; scene configuration was not inspected.
- **Recommended fix:** Make binding idempotent: retain valid assignments, allocate only unassigned players, and explicitly release old zone ownership when reassignment is intended.

### M16 — Re-enabling player controls does not restore subscriptions

- **Area:** Input lifecycle; async state transitions.
- **Files:** [PlayerControls.cs:21](Assets/Scripts/Gameplay/Player/Core/PlayerControls.cs#L21); [PlayerControls.cs:84](Assets/Scripts/Gameplay/Player/Core/PlayerControls.cs#L84).
- **Cause/impact:** Input subscription happens only in Start, while OnDisable unsubscribes. There is no OnEnable resubscription. Disabling and re-enabling the local component or its GameObject therefore leaves input callbacks disconnected. This is a confirmed lifecycle defect when that transition occurs; which gameplay/UI transitions disable it was not inspected.
- **Recommended fix:** Subscribe when enabled and local-player ownership is ready; unsubscribe using the stored subscription flag regardless of current ownership. Handle authority acquisition/loss explicitly and make both operations idempotent.

### M17 — Lobby creation/join callbacks can survive leave or overlap another request

- **Area:** Network lifecycle; async/concurrency.
- **Files:** [SteamLobby.cs:219](Assets/Scripts/Infrastructure/Network/SteamLobby.cs#L219); [SteamLobby.cs:295](Assets/Scripts/Infrastructure/Network/SteamLobby.cs#L295); [SteamLobby.cs:342](Assets/Scripts/Infrastructure/Network/SteamLobby.cs#L342); [SteamLobby.cs:400](Assets/Scripts/Infrastructure/Network/SteamLobby.cs#L400); [RoomMenuController.cs:48](Assets/Scripts/UI/Menus/RoomMenuController.cs#L48).
- **Cause/impact:** Creation can launch multiple delayed routines that share mutable pending code/capacity fields. Leave clears state/stops the current network session but does not invalidate pending create/join work. A successful late creation callback unconditionally starts a host; a late lobby-enter callback can start a client after leave. Overlapping creations can also apply the newest request's metadata to an older lobby. Search CallResult replacement already addresses stale list searches, but not these operations.
- **Recommended fix:** Use one lobby-operation state machine with request generation IDs and immutable request parameters. Reject duplicate creation, invalidate operations on leave, and ignore/leave late successful lobby callbacks instead of starting a new session. Disable every conflicting create/join action while an operation is active; the current UI only toggles `joinButton`, so the create path can still be clicked repeatedly.

### M18 — Lobby UI performs scene-wide discovery and allocations every frame

- **Area:** CPU/GC.
- **Files:** [UI/Menus/LobbyUI.cs:20](Assets/Scripts/UI/Menus/LobbyUI.cs#L20); [UI/Menus/LobbyUI.cs:27](Assets/Scripts/UI/Menus/LobbyUI.cs#L27); [UI/Menus/LobbyUI.cs:47](Assets/Scripts/UI/Menus/LobbyUI.cs#L47).
- **Cause/impact:** Every frame calls `FindObjectsByType<PlayerData>`, allocates a new `HashSet`, copies all dictionary keys into a new `List`, and refreshes every lobby slot. While this component is enabled, cost and garbage scale with players and scene objects even when lobby data is unchanged. Exact frame-time impact requires profiling.
- **Recommended fix:** Drive slot add/remove/update from replicated roster callbacks, cache stable collections, and perform a single reconciliation only when roster data changes.

### M19 — Party menu emits an error log every frame for normal non-owner states

- **Area:** CPU/GC; UI lifecycle.
- **Files:** [UI/Menus/PartyMenuUIManager.cs:18](Assets/Scripts/UI/Menus/PartyMenuUIManager.cs#L18); [UI/Menus/PartyMenuUIManager.cs:29](Assets/Scripts/UI/Menus/PartyMenuUIManager.cs#L29).
- **Cause/impact:** `Update` logs `Erro localLobbyPlayer` whenever the local player is absent or is not party owner. Those are normal initialization/client states, so an enabled component can emit roughly one log per rendered frame, creating avoidable formatting, console/file I/O, and retained editor-console entries. Runtime magnitude depends on logger/build settings.
- **Recommended fix:** Remove the per-frame log. Log a state transition once if diagnostically needed, and enable owner-only input when the local player reference/ownership changes.

### M20 — Leaving the room screen can strand its join action disabled

- **Area:** Reconnect/UI lifecycle; async concurrency.
- **Files:** [UI/Menus/RoomMenuController.cs:43](Assets/Scripts/UI/Menus/RoomMenuController.cs#L43); [UI/Menus/RoomMenuController.cs:74](Assets/Scripts/UI/Menus/RoomMenuController.cs#L74); [UI/Menus/RoomMenuController.cs:147](Assets/Scripts/UI/Menus/RoomMenuController.cs#L147); [UI/Menus/RoomMenuController.cs:159](Assets/Scripts/UI/Menus/RoomMenuController.cs#L159).
- **Cause/impact:** Join disables `joinButton`, but `OnDisable` unsubscribes from the completion/failure events and `OnEnable` does not restore the button from authoritative operation state. If a join fails or completes while this screen is disabled, its handler is missed; reopening the same object leaves the action disabled. This requires the callback to arrive while the screen is inactive.
- **Recommended fix:** Keep operation state outside the transient screen. On enable, render from that state; on cancellation/leave invalidate the request, and always restore controls in a completion/finally path that is not dependent on the screen remaining subscribed.

### M21 — Spectator exit leaves additive overlay work and scene alive

- **Area:** UI lifecycle; async concurrency.
- **Files:** [Gameplay/Spectator/SpectatorManager.cs:49](Assets/Scripts/Gameplay/Spectator/SpectatorManager.cs#L49); [Gameplay/Spectator/SpectatorManager.cs:57](Assets/Scripts/Gameplay/Spectator/SpectatorManager.cs#L57); [Gameplay/Spectator/SpectatorManager.cs:75](Assets/Scripts/Gameplay/Spectator/SpectatorManager.cs#L75).
- **Cause/impact:** Entering spectator mode starts an additive overlay load. Exiting clears local state but neither cancels nor invalidates the load, and it does not unload/disable the overlay. An exit during loading therefore still completes the overlay load, and an exit within the same main scene leaves the spectator overlay scene resident until a later scene replacement. Visible stale UI/input depends on the overlay's own behavior, which was not inspected.
- **Recommended fix:** Tie the load to a spectator-state generation/cancellation token, verify state after the await loop, and explicitly unload or deactivate the overlay on exit. Make repeated enter/exit idempotent.

### M22 — Destroyed avatar consumers leave Steam callbacks registered until GC

- **Area:** UI lifecycle; memory/GC.
- **Files:** [Network/Steam/FriendItem.cs:20](Assets/Scripts/Network/Steam/FriendItem.cs#L20); [Network/Steam/FriendListManager.cs:43](Assets/Scripts/Network/Steam/FriendListManager.cs#L43); [Gameplay/Player/Core/CharacterSkinElement.cs:135](Assets/Scripts/Gameplay/Player/Core/CharacterSkinElement.cs#L135); [Gameplay/Player/Core/CharacterSkinElement.cs:188](Assets/Scripts/Gameplay/Player/Core/CharacterSkinElement.cs#L188); [Steamworks.NET/CallbackDispatcher.cs:247](Assets/com.rlabrecque.steamworks.net/Runtime/CallbackDispatcher.cs#L247).
- **Cause/impact:** Every friend row and initialized character-skin element creates an `AvatarImageLoaded_t` callback, while destruction removes their UI objects/markers without disposing those callback handles. Steamworks.NET provides deterministic `Dispose`/unregister, but these consumers rely on eventual finalization instead. Refreshing rows or destroying lobby character elements can retain native callback registrations and destroyed-object delegates until GC; matching avatar callbacks in that window can invoke stale handlers. Frequency and retained duration depend on refresh/player churn and GC timing; the character path additionally depends on the currently broken handler in M35 being made operational or another caller initializing an element.
- **Recommended fix:** Dispose and null each callback in `OnDestroy`/`OnDisable`, or centralize avatar callbacks in a lifetime-owned service and reuse/pool consumers.

### M23 — Multi-player tile occupancy is collapsed into one Boolean

- **Area:** Minigame state correctness.
- **Files:** [Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs:14](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs#L14); [Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs:17](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs#L17); [Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs:25](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs#L25).
- **Cause/impact:** Any player enter sets `jogadorNoTile = true`, and any player exit sets it false. With two players overlapping, the first exit stops server progression even though another player remains on the tile. The defect is conditional on overlapping players/colliders.
- **Recommended fix:** Track unique server-side player identities (or a validated occupancy count) and progress while the set is nonempty; make duplicate collider enter/exit events idempotent.

### M24 — Every intact simple floor tile runs a physics query every frame

- **Area:** CPU.
- **Files:** [Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs:64](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs#L64); [Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs:96](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs#L96); [Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs:279](Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs#L279).
- **Cause/impact:** Each unstepped tile polls `OverlapSphereNonAlloc` from `Update`; dedicated/host servers do so for all players, while pure clients repeat the query for local detection. The nonalloc buffer avoids steady garbage but not the O(active tiles × frames) broadphase work, and saturation can resize buffers. Severity is based on the algorithmic path; actual cost depends on scene tile count and needs profiling.
- **Recommended fix:** Prefer server trigger events or a centralized, spatially bounded player-to-tile query. If client prediction is retained, query near the local player rather than once per tile and keep the server authoritative.

### M25 — Roulette spin exclusion exists only in client presentation state

- **Area:** Minigame networking; concurrency.
- **Files:** [Gameplay/Minigames/HotPotato/SlotRoleta.cs:98](Assets/Scripts/Gameplay/Minigames/HotPotato/SlotRoleta.cs#L98); [Gameplay/Minigames/HotPotato/SlotRoleta.cs:135](Assets/Scripts/Gameplay/Minigames/HotPotato/SlotRoleta.cs#L135); [Gameplay/Minigames/HotPotato/SlotRoleta.cs:371](Assets/Scripts/Gameplay/Minigames/HotPotato/SlotRoleta.cs#L371).
- **Cause/impact:** `ServerStartSpin` checks `_girando` but never sets it. The flag is set only inside client presentation (`SpinToIndex`), so host mode incidentally shares the client flag while a dedicated server remains false and accepts overlapping starts. Whether duplicate calls occur in normal wiring was not established; the server guard itself is ineffective on dedicated server.
- **Recommended fix:** Own an explicit server spin state, set it before the RPC, clear it from a tracked server completion routine, and keep client animation state separate. Make repeated start requests idempotent.

### M26 — Finish-platform progress counts colliders instead of players

- **Area:** Network lifecycle; state correctness.
- **Files:** [Gameplay/Match/Flow/CheckMudarCena.cs:17](Assets/Scripts/Gameplay/Match/Flow/CheckMudarCena.cs#L17); [Gameplay/Match/Flow/CheckMudarCena.cs:23](Assets/Scripts/Gameplay/Match/Flow/CheckMudarCena.cs#L23); [Gameplay/Match/Flow/CheckMudarCena.cs:31](Assets/Scripts/Gameplay/Match/Flow/CheckMudarCena.cs#L31).
- **Cause/impact:** Every tagged collider enter increments an integer and every exit decrements it, then exact equality with `allClients.Count` triggers the scene change. A player with multiple colliders can advance the count more than once; unmatched exits can underflow it; disconnects and destroyed colliders are not reconciled. The transition can fire early or never fire. The counter defect is confirmed; actual collider layout was not inspected.
- **Recommended fix:** Track unique admitted player identities in a server-side set, remove them idempotently on exit/disconnect, and re-evaluate against the eligible participant set using an explicit completion policy.

### M27 — The first client camera event can start the match for everyone

- **Area:** Networking lifecycle.
- **Files:** [Gameplay/Minigames/Shared/ActionFrameCamera.cs:9](Assets/Scripts/Gameplay/Minigames/Shared/ActionFrameCamera.cs#L9); [Gameplay/Match/MatchManager.cs:266](Assets/Scripts/Gameplay/Match/MatchManager.cs#L266); [Gameplay/Match/MatchManager.cs:279](Assets/Scripts/Gameplay/Match/MatchManager.cs#L279).
- **Cause/impact:** Any admitted client can call the authority-free `CmdStartMatchAfterCamera`; the server verifies only that the sender has an identity. Once briefing is finished, the first client-side animation event unfreezes all players, even if other clients have not completed the camera sequence. A modified client can also send the command immediately at that phase. The command gate is confirmed; whether all current camera timelines finish simultaneously was not inspected.
- **Recommended fix:** Make match start a server-owned phase/deadline, or collect one generation-bound camera-ready ACK per eligible participant with timeout/disconnect handling. Do not let one arbitrary client own the transition.

### M28 — Countdown SyncVar hooks perform global lifecycle work on every peer

- **Area:** Network lifecycle; state ownership.
- **Files:** [Gameplay/Match/Time/ContadorTempo.cs:100](Assets/Scripts/Gameplay/Match/Time/ContadorTempo.cs#L100); [Gameplay/Match/Time/ContadorTempo.cs:131](Assets/Scripts/Gameplay/Match/Time/ContadorTempo.cs#L131); [Infrastructure/Network/MyNetworkManager.cs:583](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L583).
- **Cause/impact:** When the replicated timer reaches zero, its SyncVar hook notifies observers, calls `ReiniciarJogo`, and chooses scene behavior on every peer. `ReiniciarJogo` is not server-gated and clears scoreboard/list/victory state locally; pure clients then take a separate loading-UI fallback while the server starts the synchronized transition. This splits authoritative reset from presentation and can erase client caches or invoke observers in the wrong role. The code path is confirmed; it matters only in scenes still using this legacy timer.
- **Recommended fix:** Perform timeout, scoring/reset and scene selection once in a server method. Replicate a terminal timer/phase state whose hook updates UI only.

### M29 — Legacy podium scoring can award the same placements repeatedly

- **Area:** Minigame state correctness.
- **Files:** [Minigames/Chegada/ChegadaPodio.cs:22](Assets/Scripts/Minigames/Chegada/ChegadaPodio.cs#L22); [Minigames/Chegada/ChegadaPodio.cs:37](Assets/Scripts/Minigames/Chegada/ChegadaPodio.cs#L37); [Minigames/Chegada/ChegadaPodio.cs:51](Assets/Scripts/Minigames/Chegada/ChegadaPodio.cs#L51).
- **Cause/impact:** Every newly arriving player calls `VerificarFimDeJogo`, and every call at or above the threshold runs `DistribuirPontos`; there is no completed/awarded guard. In a race with `N` players, arrival `N-1` awards the current ranking and arrival `N` awards it again, duplicating points for earlier finishers. This arithmetic path is confirmed; current scene wiring was not inspected.
- **Recommended fix:** Finalize once through the common match lifecycle, guard scoring idempotently, freeze an immutable ranking snapshot, and define explicitly whether the last finisher is included.

### M30 — Re-showing results can strand its coroutine completion counter

- **Area:** Async/concurrency; UI lifecycle.
- **Files:** [Minigames/Results/ResultsRow.cs:119](Assets/Scripts/Minigames/Results/ResultsRow.cs#L119); [Minigames/Results/ResultsRow.cs:122](Assets/Scripts/Minigames/Results/ResultsRow.cs#L122); [Minigames/Results/ResultsRow.cs:226](Assets/Scripts/Minigames/Results/ResultsRow.cs#L226); [Minigames/Results/ResultsRow.cs:236](Assets/Scripts/Minigames/Results/ResultsRow.cs#L236).
- **Cause/impact:** `Show` stops all coroutines but neither resets `_pendingNumberAnims` nor cancels outstanding LeanTween callbacks. Stopped number routines never decrement the counter, while old callbacks can start new routines during the next sequence. A second results delivery can therefore wait forever before its exit countdown or mix old/new rows. The defect is conditional on overlapping/repeated `Show` calls; normal delivery count was not runtime-tested.
- **Recommended fix:** Give each presentation a generation, cancel both coroutines and tweens owned by the previous generation, reset counters deterministically, and ignore stale completion callbacks.

### M31 — Spectator overlay can permanently miss its manager subscription

- **Area:** Reconnect/UI lifecycle.
- **Files:** [UI/SpecOverlay/SpecOverlayController.cs:27](Assets/Scripts/UI/SpecOverlay/SpecOverlayController.cs#L27); [UI/SpecOverlay/SpecOverlayController.cs:38](Assets/Scripts/UI/SpecOverlay/SpecOverlayController.cs#L38).
- **Cause/impact:** `OnEnable` returns when `SpectatorManager.Instance` is not ready and never retries. Conversely, `OnDisable` consults the current singleton rather than the instance originally subscribed, so manager replacement/nulling can leave delegates attached to the old manager. The overlay can stay hidden/stale after additive-load or reconnect ordering changes and can retain a destroyed controller. These ordering conditions were not reproduced.
- **Recommended fix:** Bind through an explicit manager-ready lifecycle, retain the subscribed instance, unbind that exact instance, and render current state whenever binding succeeds.

### M32 — HUD teardown leaves a persistent potato-holder listener

- **Area:** UI lifecycle; memory/GC.
- **Files:** [UI/HUD/HUDManager.cs:42](Assets/Scripts/UI/HUD/HUDManager.cs#L42); [UI/HUD/HUDManager.cs:81](Assets/Scripts/UI/HUD/HUDManager.cs#L81).
- **Cause/impact:** `Start` subscribes `OnPotatoHolderUpdated` to the HUD ScriptableObject, but `OnDestroy` removes every neighboring HUD listener except that one. Repeated HUD scene creation retains destroyed managers and accumulates callbacks. Unity's destroyed-object null behavior may suppress visible text writes, but it does not remove the delegate retention or invocation overhead.
- **Recommended fix:** Unsubscribe the potato-holder handler during teardown and make all subscriptions symmetric/idempotent, preferably in `OnEnable`/`OnDisable`.

### M33 — Distance HUD searches the whole scene every frame and can select the wrong player

- **Area:** CPU; multiplayer UI correctness.
- **Files:** [Minigames/Chegada/RecordeDistancia.cs:29](Assets/Scripts/Minigames/Chegada/RecordeDistancia.cs#L29); [Minigames/Chegada/RecordeDistancia.cs:31](Assets/Scripts/Minigames/Chegada/RecordeDistancia.cs#L31); [Minigames/Chegada/RecordeDistancia.cs:61](Assets/Scripts/Minigames/Chegada/RecordeDistancia.cs#L61).
- **Cause/impact:** `Update` calls `FindGameObjectWithTag("Player")` every frame. If Unity returns a remote player first, the method exits and repeats the same global search next frame instead of retaining the local player. This creates recurring scene traversal and can leave the local distance display frozen in multiplayer. Exact frame cost depends on scene size and requires profiling.
- **Recommended fix:** Resolve the local player from the network spawn/ownership lifecycle once, cache it, clear it on despawn, and update UI only while that reference is valid.

### M34 — Cinematic camera transitions are duplicated and pending opposites cannot supersede each other

- **Area:** Network/UI lifecycle; async/concurrency.
- **Files:** [Core/Camera/CinematicCameraController.cs:75](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L75); [Core/Camera/CinematicCameraController.cs:110](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L110); [Core/Camera/CinematicCameraController.cs:145](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L145); [Core/Camera/CinematicCameraController.cs:179](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L179); [Core/Camera/CinematicCameraController.cs:225](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L225); [Core/Camera/CinematicCameraController.cs:290](Assets/Scripts/Core/Camera/CinematicCameraController.cs#L290); [Mirror/Core/NetworkBehaviour.cs:529](Assets/Mirror/Core/NetworkBehaviour.cs#L529).
- **Cause/impact:** Each server state change is delivered through both a SyncVar hook and a ClientRpc, so every client executes the same activation/deactivation and UnityEvent twice; Mirror also invokes the hook immediately on a host. Separately, a delayed activation can only cancel another activation: a deactivation request sees the still-false state and returns, leaving the activation pending (and vice versa). Listener side effects can therefore run twice, and a superseded camera/freeze transition can still fire later. The exact visible consequence depends on configured UnityEvent listeners; the duplicate calls and stale-delay ordering are confirmed statically.
- **Recommended fix:** Use the SyncVar hook as the single replicated presentation path, make local application idempotent, and represent pending target state explicitly. Any new request should cancel the opposite pending coroutine before evaluating current/effective state.

### M35 — Lobby character handler never creates or assigns its character entries

- **Area:** Lobby/UI lifecycle.
- **Files:** [Gameplay/Player/Core/CharacterSkinHandler.cs:25](Assets/Scripts/Gameplay/Player/Core/CharacterSkinHandler.cs#L25); [Gameplay/Player/Core/CharacterSkinHandler.cs:52](Assets/Scripts/Gameplay/Player/Core/CharacterSkinHandler.cs#L52); [Gameplay/Lobby/PlayerData.cs:134](Assets/Scripts/Gameplay/Lobby/PlayerData.cs#L134).
- **Cause/impact:** `Start` allocates two empty arrays, but neither the readiness coroutine nor `SpawnCharacterMesh` instantiates `characterSkinPrefab` or assigns either array. Local players return because slot 0 is null; remote players reach the same null entry and return. When an active `CharacterSkinHandler` is present, `PlayerData` calls this path, but it cannot initialize the intended lobby model, nametag or avatar. Scene presence and whether another system supplies equivalent presentation were not validated.
- **Recommended fix:** Instantiate the local and remote entries at validated spawn points, store both the GameObject and `CharacterSkinElement`, bind each stable player ID to one slot, and clean that mapping on disconnect. Remove the unused readiness coroutine/array if this system has been superseded.

### M36 — Every interaction zone allocates a physics result array every frame

- **Area:** CPU/GC; UI interaction lifecycle.
- **Files:** [Core/Tools/RangeInteractZone.cs:33](Assets/Scripts/Core/Tools/RangeInteractZone.cs#L33); [Core/Tools/RangeInteractZone.cs:45](Assets/Scripts/Core/Tools/RangeInteractZone.cs#L45); [Core/Tools/RangeInteractZone.cs:101](Assets/Scripts/Core/Tools/RangeInteractZone.cs#L101).
- **Cause/impact:** Each enabled zone runs `Physics.SphereCastAll` in `Update`, allocating a new hit array even though the preceding distance check already establishes most interactions. Until a local player is found, every zone also performs a scene-wide player search each frame and logs once per nonlocal player encountered. Cost scales with enabled zone count, colliders and connection/spawn delay; actual frame and GC impact requires profiling.
- **Recommended fix:** Prefer trigger enter/exit state or a non-allocating overlap query at a bounded cadence, skip the cast when the distance decision is sufficient, and inject/cache the local player from its spawn lifecycle instead of polling and logging.

### M37 — Phone clock rebuilds identical UI text on every physics tick

- **Area:** CPU/GC.
- **Files:** [Core/Utilities/ManagerUICelular.cs:10](Assets/Scripts/Core/Utilities/ManagerUICelular.cs#L10).
- **Cause/impact:** `FixedUpdate` calls `DateTime.Now`, formats a new `HH:mm` string and assigns it to TMP on every physics step, although the displayed value changes only once per minute. This creates avoidable managed allocations and text rebuild checks while the phone UI object is enabled. Magnitude depends on object lifetime and fixed timestep and is not profiler-measured.
- **Recommended fix:** Update when the displayed minute changes (or on a one-second unscaled timer), cache the last string/value, and skip TMP assignment when unchanged.

### M38 — Legacy minigame toggles do not apply or render their serialized initial state

- **Area:** Minigame selection; UI lifecycle.
- **Files:** [UI/Celular/ButtonToggle.cs:31](Assets/Scripts/UI/Celular/ButtonToggle.cs#L31); [UI/Celular/ButtonToggle.cs:47](Assets/Scripts/UI/Celular/ButtonToggle.cs#L47); [UI/Celular/AtribuiEventos.cs:10](Assets/Scripts/UI/Celular/AtribuiEventos.cs#L10); [Infrastructure/Network/MyNetworkManager.cs:637](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L637).
- **Cause/impact:** `Awake` first animates from `_isOn`, then forcibly paints the control off and assigns `isOn = _isOn`; the property setter returns because the value is unchanged, so it neither restores the on visuals nor emits the event that updates the active minigame set. A toggle serialized on can therefore look off while remaining logically on, and its configured selection is not applied until a click. This affects scenes/prefabs still using `ButtonToggle` plus `AtribuiEventos`; their wiring was not inspected.
- **Recommended fix:** Separate initialization from change notification: render the serialized value directly, then explicitly reconcile selection state once after listeners and the catalog manager are ready. Keep one source of truth rather than parallel `_isOn` and visual state.

### M39 — Customization fallback can repaint every remote avatar with local data

- **Area:** Networking presentation; UI lifecycle.
- **Files:** [UI/SimpleCustomizationUI.cs:122](Assets/Scripts/UI/SimpleCustomizationUI.cs#L122); [Gameplay/Player/CustomizationApplier.cs:54](Assets/Scripts/Gameplay/Player/CustomizationApplier.cs#L54); [Gameplay/Player/Core/PlayerScript.cs:1887](Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs#L1887).
- **Cause/impact:** After a local selection, `ApplyToPlayer` uses nondeterministic `FindAnyObjectByType<PlayerScript>()`. If that returns a remote player, the local-player branch is skipped and the fallback calls `ApplyCurrentCustomization` on every `CustomizationApplier`; that method reads the local singleton customization, so all visible remote avatars can be repainted locally with the owner's cosmetics until later synchronization corrects them. The bad call path is confirmed; whether it occurs in a session depends on object-search ordering and which appliers are active.
- **Recommended fix:** Resolve the local player explicitly (`NetworkClient.localPlayer` or a stored local reference), apply local data only to that player, and remove the all-appliers fallback. Remote visuals must use their replicated `PlayerData` values.

### M40 — Blind HUD subscriber survives teardown

- **Area:** Reconnect/UI lifecycle.
- **Files:** [UI/HUD/BlindPanel.cs:11](Assets/Scripts/UI/HUD/BlindPanel.cs#L11); [Data/ScriptsSO/HUDSO.cs:21](Assets/Scripts/Data/ScriptsSO/HUDSO.cs#L21).
- **Cause/impact:** `BlindPanel.Start` subscribes to the `HUDSO` ScriptableObject event but has no matching unsubscribe. When the panel is destroyed while the asset remains loaded, the asset retains the destroyed component delegate; later blind updates can invoke stale UI work, retain managed objects and accumulate duplicate listeners after scene/reconnect cycles. The missing lifecycle pairing is confirmed; actual accumulation depends on this panel and the same HUDSO asset being recreated/reused.
- **Recommended fix:** Subscribe in `OnEnable` and unsubscribe in `OnDisable` (or pair `Start` with `OnDestroy`), null-check the asset, and add a recreation test that verifies exactly one callback after reconnect/scene reload.

### M41 — Team-color controller assumes a second material and never releases its clone

- **Area:** Runtime correctness; native-memory lifetime.
- **Files:** [Utils/TeamColorMaterialController.cs:21](Assets/Scripts/Utils/TeamColorMaterialController.cs#L21); [Utils/TeamColorMaterialController.cs:32](Assets/Scripts/Utils/TeamColorMaterialController.cs#L32).
- **Cause/impact:** `EnsureSetup` accepts any nonempty material array, then unconditionally accesses index `1`. A renderer with one material throws before team color/visibility is applied. On a valid renderer it may instantiate a replacement `Material`, but the component has no teardown ownership and never destroys that clone, so repeated player spawn/despawn or scene cycles can retain native material allocations. The code paths are confirmed; affected prefab cardinality and repetition were not inspected.
- **Recommended fix:** Serialize/validate the target material index, require `mats.Length > index`, prefer `MaterialPropertyBlock` for per-renderer color/alpha, or explicitly destroy any owned runtime material during teardown.

### M42 — Contributor refresh leaks generated avatar sprites and textures

- **Area:** Async/UI lifecycle; native-memory lifetime.
- **Files:** [GitHubIntegration/GitHubContributorsDisplay.cs:173](Assets/Scripts/GitHubIntegration/GitHubContributorsDisplay.cs#L173); [GitHubIntegration/GitHubContributorsDisplay.cs:356](Assets/Scripts/GitHubIntegration/GitHubContributorsDisplay.cs#L356); [GitHubIntegration/GitHubContributorsDisplay.cs:446](Assets/Scripts/GitHubIntegration/GitHubContributorsDisplay.cs#L446); [GitHubIntegration/ContributorUIItem.cs:77](Assets/Scripts/GitHubIntegration/ContributorUIItem.cs#L77).
- **Cause/impact:** Each avatar download creates a runtime `Texture2D`, and `SetAvatarTexture` creates another runtime `Sprite`. `RefreshRanking` destroys only the item GameObjects; neither generated object is tracked or destroyed. Repeated refreshes or recreation of the display therefore accumulate native image allocations until an external unload happens. The ownership gap is confirmed; severity depends on refresh frequency and contributor/avatar sizes and is not profiler-measured.
- **Recommended fix:** Give each item explicit ownership and destroy the generated sprite and texture on replacement/teardown, or use a bounded URL cache with reference counting; cancel/replace the active refresh through one tracked coroutine/request generation.

### M43 — Client teardown clears unrelated ScriptableObject subscribers globally

- **Area:** Reconnect/UI lifecycle.
- **Files:** [Infrastructure/Network/MyNetworkManager.cs:302](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L302); [Infrastructure/Network/MyNetworkManager.cs:341](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L341); [Infrastructure/Network/MyNetworkManager.cs:373](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L373); [Infrastructure/Network/MyNetworkManager.cs:399](Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs#L399); [Data/ScriptsSO/PlayerControlsSO.cs:60](Assets/Scripts/Data/ScriptsSO/PlayerControlsSO.cs#L60); [Data/ScriptsSO/HUDSO.cs:189](Assets/Scripts/Data/ScriptsSO/HUDSO.cs#L189).
- **Cause/impact:** Both client-stop paths call `CleanupClientLocalState`, which enumerates every loaded input/control/HUD/player-data ScriptableObject and assigns all of their events to null. This removes subscribers owned by surviving menu/UI systems as well as stale network objects. The global deletion is confirmed; which controls or panels remain broken after disconnect/reconnect depends on scene persistence and whether every surviving consumer re-subscribes.
- **Recommended fix:** Make each subscriber pair registration with `OnDisable`/`OnDestroy`, and remove only that subscriber. If a session-wide event hub must reset, scope it to a session generation and rebuild all owners explicitly instead of clearing every loaded asset delegate.

### M44 — Editor setup tools silently overwrite the shared FeatureCard prefab

- **Area:** Editor tooling; asset integrity.
- **Files:** [ProjectFeatures/Editor/FeaturesPanelSetup.cs:39](Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelSetup.cs#L39); [ProjectFeatures/Editor/FeaturesPanelSetup.cs:44](Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelSetup.cs#L44); [ProjectFeatures/Editor/FeaturesPanelSetup.cs:52](Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelSetup.cs#L52); [ProjectFeatures/Editor/FeaturesPanelWizard.cs:966](Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelWizard.cs#L966); [ProjectFeatures/Editor/FeaturesPanelWizard.cs:1147](Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelWizard.cs#L1147).
- **Cause/impact:** Both editor commands save a newly generated card to the fixed path `Assets/Prefabs/FeatureCard.prefab` without checking whether a customized prefab already exists or asking to replace it. Invoking either tool can therefore overwrite project-owned prefab contents and propagate the replacement to existing references. The fixed-path write is confirmed; whether the current prefab contains unique manual work was not inspected.
- **Recommended fix:** Refuse to overwrite by default, use `GenerateUniqueAssetPath` or an explicit save panel, and require a clear replacement confirmation with Undo/version-control guidance when an existing prefab is intentionally updated.

## Performance conclusions and ruled-out suspicions

- No measured serious GPU bottleneck is claimed. Renderer/material counts, draw calls, overdraw and GPU timings were not profiled.
- H03 and M03 are concrete client-triggerable amplification paths, not measured normal-play frame-time regressions. M18, M19, M24, M33, M36 and M37 are confirmed recurring work/logging/query/allocation paths whose actual frame-time cost remains unprofiled. M41 and M42 are native-object ownership gaps whose real accumulation requires runtime/profiler measurement.
- Projectile lifetime is **not unbounded in the two inspected prefabs**: both `Assets/Prefab/Projectile.prefab` and `Assets/Prefab/VFX_PROJ.prefab` attach `Assets/Prefab/Destruc.cs` with `Time: 4`. A leak inferred only from ProjectileScript would be a false positive.
- PrefabInstancer already validates sender/owner, rejects nonfinite/zero directions, uses the server origin and enforces a throw cooldown. These protections do not cover the separate unchecked push command (H01).
- ScoreboardUI already coalesces notifications before sending updates; do not infer one full scoreboard broadcast per UpdateScores call from the observer pattern alone.
- Scene loading already guards duplicate preloads and has disconnect cleanup/activation waits. H06 concerns ACK eligibility and phase identity, not the absence of all transition handling.
- `VictoryDataManagerSpawner` would create an unspawned local manager on clients if wired, but no scene/prefab reference to that spawner script was found; it is not reported as an active finding. `Vitoria.unity` directly contains `VictoryDataManager` instead.

## Suggested verification after fixes

1. Host plus two remote clients: reject out-of-range/repeated push commands, forged identities, and dead/frozen attacks.
2. Send unauthenticated/oversized/rate-exceeding chat and out-of-set/stale transition ACKs; verify rejection without fan-out or barrier advancement.
3. Dedicated server plus clients: verify Sumo/memory floor colliders and late-join state, and vote-zone labels/counts.
4. Respawn before death-effect delays expire; end/restart memory and Queda rounds; submit duplicate eliminations.
5. Test projectile collision during long server frames and with multi-collider players; change customization repeatedly after spawn.
6. Profile CPU/network behavior under bounded chat/snapshot load and separately measure representative scene GPU costs. No test scripts were added during this audit.
7. Build a standalone player after removing the editor-only import; exercise lobby-screen disable/re-enable, spectator enter/exit during additive loading, repeated roulette starts on a dedicated server, and multi-player floor occupancy.
8. Run an owner client against server-side movement checks: attempt teleport/speed/collision violations and verify corrections while legitimate server teleports still work.
9. On a dedicated server, finish a round and verify the server-owned results deadline returns everyone exactly once; replay results delivery and reconnect the spectator/HUD overlays to validate cancellation and subscriptions.
10. Exercise delayed cinematic activate/deactivate reversals on host and remote client while counting UnityEvent invocations; enter/leave every interaction zone under profiler allocation recording; recreate lobby character rows and verify deterministic Steam callback disposal.

## File checklist

Legend: **FULL** = entire file read for the requested concerns; **PARTIAL** = selected relevant sections traced; **PENDING** = not substantively inspected. Both FULL and PARTIAL have checked boxes. Checklist paths are relative to the repository root.

- [x] **FULL** — `Assets/Scripts/Core/Camera/CinematicCameraController.cs`
- [x] **FULL** — `Assets/Scripts/Core/CustomizationInitializer.cs`
- [x] **FULL** — `Assets/Scripts/Core/CustomizationManager.cs`
- [x] **FULL** — `Assets/Scripts/Core/CustomizationSceneManager.cs`
- [x] **FULL** — `Assets/Scripts/Core/Extensions/ArrayExtension.cs`
- [x] **FULL** — `Assets/Scripts/Core/Extensions/MonoBehaviourExtensions.cs`
- [x] **FULL** — `Assets/Scripts/Core/Extensions/SteamHelper.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/AnimatorIntToggle.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/BillboardBehaviourTool.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/ButtonTextNormal.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/ClickRelayIntegerTool.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/CustomMath.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/DelayedEventTool.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/DisableToolGameObject.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/OnEnableDisableTool.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/PlayerEventPanel.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/RangeInteractZone.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/RangeInteractor.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/RelayTriggerEvents.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/RotateAroundTool.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/SequentialAnimationController.cs`
- [x] **FULL** — `Assets/Scripts/Core/Tools/StartCursor.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/CutsceneAnimationBridge.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/DelayedEventCaller.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/FPSDisplay.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/ManagerCutscene.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/ManagerUICelular.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/Markers/Marker.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/Markers/MarkerDefinition.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/Markers/MarkerHandler.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/Markers/NametagMarker.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/NetworkPingDisplay.cs`
- [x] **FULL** — `Assets/Scripts/Core/Utilities/WaypointLooper.cs`
- [x] **FULL** — `Assets/Scripts/Data/PlayerCustomizationData.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/BriefingScreenSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/CustomizationDatabase.cs`
- [x] **PARTIAL** — `Assets/Scripts/Data/ScriptsSO/Database.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/HUDSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/PanelCameraSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/PlayerControlsSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/PlayerDataSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/PlayerInputSO.cs`
- [x] **FULL** — `Assets/Scripts/Data/ScriptsSO/TempoSo.cs`
- [x] **FULL** — `Assets/Scripts/Editor/CustomizationAttachmentSetup.cs`
- [x] **FULL** — `Assets/Scripts/Editor/MinigameSettingsWindow.cs`
- [x] **FULL** — `Assets/Scripts/Environment/AirshipWaypointMover.cs`
- [x] **FULL** — `Assets/Scripts/Environment/KiteAmbientAnimator.cs`
- [x] **FULL** — `Assets/Scripts/Examples/MinigameMusicExample.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Combat/PrefabInstancer.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Combat/Projectile/ProjectileScript.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Hazards/ContextualKillZone.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Lobby/LobbySlot.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Lobby/PlayerData.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Lobby/PlayerInputReadyHandler.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Lobby/PlayerList.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Lobby/PlayerNameplate.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Match/Flow/CheckMudarCena.cs`
- [x] **PARTIAL** — `Assets/Scripts/Gameplay/Match/MatchManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Match/Score/IObserverPontos.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Match/Score/ISubjectPontos.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Match/Time/ContadorTempo.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoCaindo.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoDerrete.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoMae.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoMaeSo.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrando.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoQuebrandoSimples.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/ChaoSumindo.cs`
- [x] **PARTIAL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/FloorBreakingManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/MemoriaMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/FloorBreaking/QuedaMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Glass/Editor/GlassPathEditorWindow.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Glass/GlassFinishTrigger.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Glass/GlassMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Glass/GlassPathData.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Glass/GlassTile.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/HotPotato/HotPotatoMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/HotPotato/SlotRoleta.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/HotPotato/UiSlotBatata.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/CollisionManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/DriftSettings.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/ExampleInput.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/VehicleBehaviour.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/VehicleBehaviourSettings.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/VehicleEffects.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/Scripts/enums.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Race/RaceCheckpoint.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Race/RaceFinishTrigger.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Race/RaceMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/ActionFrameCamera.cs`
- [x] **PARTIAL** — `Assets/Scripts/Gameplay/Minigames/Shared/BriefingManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/DeathZoneTrigger.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/IScoreRule.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/MinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/BouncePad.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/ConveyorZone.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoor.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/Doors/FallGuysDoorRow.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/IceZone.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/Obstaculos/RotatingHammer.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/ServerFinishLine.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Shared/SlotBriefing.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Soccer/BallPhysics.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Soccer/GoalTrigger.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Soccer/SoccerHUD.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Soccer/SoccerMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/LaneAtrribuition.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/MovingPlatform.cs`
- [x] **PARTIAL** — `Assets/Scripts/Gameplay/Minigames/Street/StreetCourierZone.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/StreetMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/TrainSignalController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/VehicleLane.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Street/WagonFab.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Minigames/Sumo/SumoMinigameController.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/CharacterSkinElement.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/CharacterSkinHandler.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/DeathCause.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/DeathEffectsSO.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/HitKillDetection.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/IDamageable.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/IHitKillable.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/PermaHitKillDetection.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/PlayerActiveFrame.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/PlayerControls.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/PlayerMesh.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/Core/PlayerRespawn.cs`
- [x] **PARTIAL** — `Assets/Scripts/Gameplay/Player/Core/PlayerScript.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/CustomizationApplier.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/PlayerCustomizationIntegration.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Player/PlayerCustomizationSync.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Spectator/SpectatorManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Victory/VictoryDataManager.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Victory/VictoryDataManagerSpawner.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Victory/VictoryDisplay.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Victory/VictoryPlayerData.cs`
- [x] **FULL** — `Assets/Scripts/Gameplay/Victory/VictoryPodiumManager.cs`
- [x] **FULL** — `Assets/Scripts/GitHubIntegration/ContributorUIItem.cs`
- [x] **FULL** — `Assets/Scripts/GitHubIntegration/GitHubConfig.cs`
- [x] **FULL** — `Assets/Scripts/GitHubIntegration/GitHubContributor.cs`
- [x] **FULL** — `Assets/Scripts/GitHubIntegration/GitHubContributorsDisplay.cs`
- [x] **FULL** — `Assets/Scripts/GitHubIntegration/GitHubContributorsWrapper.cs`
- [x] **FULL** — `Assets/Scripts/Infrastructure/Network/LobbyController.cs`
- [x] **PARTIAL** — `Assets/Scripts/Infrastructure/Network/MyNetworkManager.cs`
- [x] **FULL** — `Assets/Scripts/Infrastructure/Network/PoupNetwork.cs`
- [x] **PARTIAL** — `Assets/Scripts/Infrastructure/Network/SceneTransitionManager.cs`
- [x] **FULL** — `Assets/Scripts/Infrastructure/Network/SceneTransitionSetup.cs`
- [x] **FULL** — `Assets/Scripts/Infrastructure/Network/SteamLobby.cs`
- [x] **FULL** — `Assets/Scripts/LevelEditor/HexagonLevelData.cs`
- [x] **FULL** — `Assets/Scripts/LevelEditor/HexagonTile.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Chegada/ChegadaPodio.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Chegada/RecordeDistancia.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/DataMinigame/MinigameCatalog.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/DataMinigame/SceneReference.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/DataMinigame/SettingsMiniGameData.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Instrutor/IObserver.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Instrutor/ISubject.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Instrutor/Instrutor.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Pontos/ScoreboardSlot.cs`
- [x] **PARTIAL** — `Assets/Scripts/Minigames/Pontos/ScoreboardUI.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Results/Editor/ResultsBannerModelEditor.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Results/Editor/ResultsModelPreviewWindow.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Results/ResultsBannerModel.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Results/ResultsRow.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Results/SimpleResultsRow.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/IVoteInputProvider.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/MinigameOptionRuntime.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/MinigameRotationState.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/UIVoteInputProvider.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/VoteCard.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/VoteZone.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/VotingManager.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/VotingSystemExample.cs`
- [x] **FULL** — `Assets/Scripts/Minigames/Voting/VotingTimerUI.cs`
- [x] **PARTIAL** — `Assets/Scripts/Minigames/Voting/ZoneVoteInputProvider.cs`
- [x] **FULL** — `Assets/Scripts/Network/Steam/FriendItem.cs`
- [x] **FULL** — `Assets/Scripts/Network/Steam/FriendListManager.cs`
- [x] **FULL** — `Assets/Scripts/Network/Steamworks.NET/SteamManager.cs`
- [x] **FULL** — `Assets/Scripts/Obsolete/GameManager.cs`
- [x] **PARTIAL** — `Assets/Scripts/OptimizedBounceManager.cs`
- [x] **FULL** — `Assets/Scripts/Player/Movement.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/Editor/FeaturesEditor.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelSetup.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/Editor/FeaturesPanelWizard.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/Editor/PopulateFeaturesDatabase.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeatureCard.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeatureCategory.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeatureDetailPopup.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeatureEntry.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeatureStatus.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeaturesDatabase.cs`
- [x] **FULL** — `Assets/Scripts/ProjectFeatures/FeaturesPanel.cs`
- [x] **FULL** — `Assets/Scripts/StadiumWaveManager.cs`
- [x] **FULL** — `Assets/Scripts/TCA/PreBuilder/PreFBXsetup.cs`
- [x] **FULL** — `Assets/Scripts/TCA/VfxClone.cs`
- [x] **FULL** — `Assets/Scripts/UI/ButtonHoverAnimator.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/AtribuiEventos.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/ButtonToggle.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/CelularTag.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/AntiAliasingCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/ISettingCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/InvertYCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/QualityCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/ResolutionCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/SensitivityCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/ShowFPSCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/ShowPingCommand.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/Command/UISlider_FontSize.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/SettingsManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UIDropdown_AntiAliasing.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UIDropdown_Quality.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UIDropdown_Resolution.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UISlider_Sensitivity.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UIToggle_ShowFPS.cs`
- [x] **FULL** — `Assets/Scripts/UI/Celular/Settings/UIToggle_ShowPing.cs`
- [x] **PARTIAL** — `Assets/Scripts/UI/Chat/ChatManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/ColorChangePanel.cs`
- [x] **FULL** — `Assets/Scripts/UI/CooldownUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/CustomButton.cs`
- [x] **FULL** — `Assets/Scripts/UI/FriendListPanelUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/BlindPanel.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/DynamicVideoFit.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/HUDManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/InteractHintUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/MostrarPontosLobby.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/NetworkStatsDisplay.cs`
- [x] **FULL** — `Assets/Scripts/UI/HUD/ScoreUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/LinkOpener.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/LoadingScreenUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/LobbyUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/MainMenu.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/MenuController.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/PartyMenuUIManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/PopupManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/RoomCodeDisplay.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/RoomListUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/RoomMenuController.cs`
- [x] **FULL** — `Assets/Scripts/UI/Menus/UIManager.cs`
- [x] **FULL** — `Assets/Scripts/UI/MinigamePanelActions.cs`
- [x] **FULL** — `Assets/Scripts/UI/MinigameSelectionPanel.cs`
- [x] **FULL** — `Assets/Scripts/UI/Minigames/MinigameSelectorItem.cs`
- [x] **FULL** — `Assets/Scripts/UI/Minigames/MinigameSelectorUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/SimpleCustomizationUI.cs`
- [x] **FULL** — `Assets/Scripts/UI/SpecOverlay/SpecOverlayController.cs`
- [x] **FULL** — `Assets/Scripts/UI/SpecOverlay/SpectatorListItem.cs`
- [x] **FULL** — `Assets/Scripts/Utils/BobAndSway.cs`
- [x] **FULL** — `Assets/Scripts/Utils/LoopRotate.cs`
- [x] **FULL** — `Assets/Scripts/Utils/TeamColorMaterialController.cs`
- [x] **FULL** — `Assets/Scripts/VerticalBounce.cs`

## Supporting files inspected outside the denominator

- [x] `Packages/manifest.json` — package inventory.
- [x] `Assets/Prefab/Destruc.cs` and its .meta — full lifetime helper read and GUID resolution.
- [x] `Assets/Prefab/Projectile.prefab`, `Assets/Prefab/VFX_PROJ.prefab` — only script bindings, lifetime and network components.
- [x] `Assets/Prefab/Player/Player.prefab` — push binding plus Smooth Sync transform-source/axis configuration only; confirms H11.
- [x] `Assets/Scripts/Gameplay/Player/Core/PlayerActiveFrame.cs.meta`, `Assets/Scripts/Gameplay/Combat/Projectile/ProjectileScript.cs.meta` — GUID lookup only.
- [x] `Assets/Mirror/Core/NetworkServer.cs` — handler signature/requireAuthentication semantics only.
- [x] `Assets/Mirror/Core/NetworkIdentity.cs` — targeted destruction symbol search only; not a substantive library review.

- [x] `Assets/Mirror/Core/SyncList.cs` — Clear/access-check implementation only; confirms H07.
- [x] `Assets/Mirror/Core/NetworkBehaviour.cs` — SyncObject write-ownership implementation only; confirms H07.
- [x] `Assets/Mirror/Runtime/Transports/FizzySteamworks/NextServer.cs` and `LegacyServer.cs` — remote Steam-ID/address mapping only; materially strengthens H02.
- [x] `Assets/com.rlabrecque.steamworks.net/Runtime/CallbackDispatcher.cs` — callback disposal/finalizer contract only; confirms M22.
- [x] `Assets/Scripts/Gameplay/Minigames/Kart/VehicleController/VehiclePrefabs/Car_Box.prefab` and its `.meta` — component/script bindings only; confirms the reviewed kart controller is wired only in its sample scene and has no network components on this prefab.
- [x] `Assets/Smooth Sync/Mirror/Smooth Sync Asset/SmoothSyncMirror.cs` and `Assets/Smooth Sync/Common/Required Internal Use Scripts/SyncMode.cs` — owner/server source enum, default validator and receive-handler sections only; confirms H11.
- [x] `Assets/Mirror/Core/NetworkManager.cs` — standard server/client scene-change implementation only; confirms H13.
- [x] `Assets/Scenes/offline.unity` — NetworkManager authenticator and player-prefab references only; strengthens H02 and confirms H11 uses the configured player prefab.
- [x] `Assets/Scenes/Vitoria.unity` plus victory-manager/spawner script GUIDs — targeted binding check only; the scene directly contains `VictoryDataManager`, and no scene/prefab reference to `VictoryDataManagerSpawner` was found.

## Remaining audit scope

The requested bounded continuation is complete. No PENDING file remains under `Assets/Scripts`; 12 large files remain PARTIAL because this pass revisited only their risk-relevant sections and did not count focused reads as FULL. Runtime/editor validation is still required for scene-transition ordering, reconnect UI behavior, transform authority, recurring CPU/GC and native-memory paths. Transport identity remains the documented application-layer binding failure rather than an absence of a FizzySteamworks identity. This report is an audit snapshot, not a clean bill of health or runtime proof.

## Audit continuation log

- 2026-09-09, lobby/voting pass: fully read 14 files (12 previously pending, 2 previously partial). Added H07 and M10–M13. Verified Mirror's actual SyncList access rules rather than inferring behavior from framework names. Current coverage: 39 FULL, 14 PARTIAL, 195 PENDING (21.37% touched, 15.73% fully read).

- 2026-09-09, bounded network/movement/minigame pass: fully read 22 previously PENDING files and completed 2 directly relevant PARTIAL controller reviews. Added H08–H09 and M14–M17 (secret glass-route replication, disconnected participants, stale race respawns, duplicate Street assignments, input re-enable lifecycle, and late Steam lobby callbacks). Coverage: 63 FULL, 12 PARTIAL, 173 PENDING; 30.24% touched and 25.40% fully read. Static review only; no gameplay changes, Unity MCP, new asset inspection, or profiler validation. Stopped at the requested batch boundary; transport authentication and reconnect UI still need further review.

- 2026-09-09, bounded transport/UI/kart/floor pass: fully read 30 previously PENDING files. Added H10 and M18–M25; materially strengthened H02 and M17 with Fizzy transport identity and room-screen evidence. Coverage: 93 FULL, 12 PARTIAL, 143 PENDING; 42.34% touched and 37.50% fully read. A static `dotnet build TCC.slnx --no-restore` completed with 0 errors and 177 warnings; no player build, play mode, profiler, gameplay edits, or Unity MCP were used. One kart prefab/sample-scene binding and two narrow third-party implementation sections were inspected only to validate concrete authority/disposal questions. Stopped at the 30-file batch boundary.

- 2026-09-09, bounded transform/results/reconnect pass: fully read 27 previously PENDING files. Added H11–H12 and M26–M33; materially extended H02, H05 and H08 with configured authenticator/player-transform and door evidence. Coverage: 120 FULL, 12 PARTIAL, 116 PENDING; 53.23% touched and 48.39% fully read. Static review only; no gameplay changes, Unity MCP, play mode, profiler or broad asset inspection. Targeted prefab/scene and Smooth Sync sections were read only to validate concrete authority/wiring questions. Stopped at the 27-file batch boundary.

- 2026-09-09, bounded customization/interaction/UI utility pass: fully read 56 previously PENDING files. Added M34–M38 and materially extended M22 with the character-avatar callback lifecycle. Coverage: 176 FULL, 12 PARTIAL, 60 PENDING; 75.81% touched and 70.97% fully read. Static review only; no gameplay changes, Unity MCP, play mode, profiler, assets/scenes/prefabs or broad third-party review. Revisited only narrow PlayerData, MyNetworkManager and Mirror SyncVar-setter sections required by newly discovered call paths. Stopped at the requested 56-file batch boundary.

- 2026-09-09, bounded remaining-runtime pass: fully read 51 previously PENDING non-editor files. Added M39–M42 (local customization overwriting remote presentation, stale blind-HUD subscription, team-material index/ownership, and contributor-avatar native allocations). Coverage: 227 FULL, 12 PARTIAL, 9 PENDING; 96.37% touched and 91.53% fully read. Static review only; no gameplay changes, Unity MCP, play mode, profiler, assets/scenes/prefabs or third-party code. Revisited only narrow `MyNetworkManager`, `CustomizationApplier` and `PlayerScript` sections directly required to validate new call paths. Stopped at the requested 51-file boundary; the nine remaining PENDING files are editor-only.

- 2026-09-09, bounded final-pending/network-lifecycle pass: fully read the 9 remaining PENDING editor files and performed focused continuation review of 12 directly relevant PARTIAL files. Added H13 and M43–M44 (double scene load after custom preload, global ScriptableObject event clearing on disconnect, and silent editor prefab overwrite). Coverage: 236 FULL, 12 PARTIAL, 0 PENDING; 100.00% touched and 95.16% fully read. The requested 50–60 PENDING target was impossible from the source-of-truth state, so no files or grep hits were invented. Static review only; no gameplay changes, Unity MCP, play mode, profiler, assets/scenes/prefabs, or broad third-party review. A narrow Mirror `NetworkManager` scene-loading section was inspected only to validate H13.
