#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using kcp2k;
using Mirror;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;

/// <summary>
/// Opt-in multi-process KCP smoke harness. It uses normal network/gameplay APIs
/// and is inert unless --project-validation-role is supplied.
/// </summary>
public sealed class ProjectValidationBootstrap : MonoBehaviour
{
    [Serializable]
    private sealed class Check
    {
        public string name;
        public string status;
        public double durationSeconds;
        public string detail;
    }

    [Serializable]
    private sealed class RuntimeSummary
    {
        public string schema = "tcc-project-validation-runtime/v1";
        public string utc;
        public string role;
        public int clientIndex;
        public int expectedPlayers;
        public string scene;
        public string unityVersion;
        public string processor;
        public string graphicsDevice;
        public bool succeeded;
        public long networkBytesIn;
        public long networkBytesOut;
        public long networkMessagesIn;
        public long networkMessagesOut;
        public NetworkMessageTraffic[] networkMessageTraffic;
        public long peakManagedMemoryBytes;
        public long peakWorkingSetBytes;
        public long peakUnityAllocatedMemoryBytes;
        public long managedMemoryStartBytes;
        public long managedMemoryEndBytes;
        public long managedMemoryGrowthBytes;
        public long unityAllocatedMemoryStartBytes;
        public long unityAllocatedMemoryEndBytes;
        public long unityAllocatedMemoryGrowthBytes;
        public double sceneLoadSeconds;
        public double frameP95Milliseconds;
        public int runtimeErrorCount;
        public string[] runtimeErrors;
        public int ignoredHeadlessGraphicsErrorCount;
        public string[] ignoredHeadlessGraphicsErrors;
        public Check[] checks;
    }

    [Serializable]
    private sealed class NetworkMessageTraffic
    {
        public string messageType;
        public long messagesIn;
        public long bytesIn;
        public long messagesOut;
        public long bytesOut;
    }

    private readonly List<Check> _checks = new();
    private readonly List<double> _frameMilliseconds = new(4096);
    private readonly List<string> _runtimeErrors = new(20);
    private readonly List<string> _ignoredHeadlessGraphicsErrors = new(20);
    private readonly Dictionary<string, NetworkMessageTraffic> _networkTraffic = new();
    private string _role;
    private int _clientIndex;
    private int _expectedPlayers;
    private ushort _port;
    private string _scene;
    private string _outputDirectory;
    private bool _exerciseReconnect;
    private float _globalDeadline;
    private long _bytesIn;
    private long _bytesOut;
    private long _messagesIn;
    private long _messagesOut;
    private long _peakManagedMemory;
    private long _peakWorkingSet;
    private long _peakUnityAllocatedMemory;
    private long _managedMemoryStart;
    private long _unityAllocatedMemoryStart;
    private int _runtimeErrorCount;
    private int _ignoredHeadlessGraphicsErrorCount;
    private double _sceneLoadSeconds;
    private bool _sawVoting;
    private bool _sawDeath;
    private bool _sawRespawnAfterDeath;
    private uint _observedDeadNetId;
    private bool _sawSpectating;
    private bool _sawSpectateExit;
    private bool _sawResults;
    private bool _finished;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallWhenRequested()
    {
        if (!TryGetArgument("--project-validation-role", out _)) return;
        // Headless Development Players otherwise honor the scene's serialized
        // Mirror auto-start mode and can start FizzySteamworks before this
        // harness selects KCP. sceneLoaded runs after Awake but before Start.
        SceneManager.sceneLoaded += ConfigureInitialNetworkManager;
        var root = new GameObject("ProjectValidationBootstrap");
        DontDestroyOnLoad(root);
        root.AddComponent<ProjectValidationBootstrap>();
    }

    private static void ConfigureInitialNetworkManager(Scene scene, LoadSceneMode mode)
    {
        MyNetworkManager manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
        if (manager == null) return;
        manager.headlessStartMode = HeadlessStartOptions.DoNothing;
        if (TryGetArgument("--project-validation-port", out string value) && ushort.TryParse(value, out ushort port))
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();
            if (transport == null) transport = manager.gameObject.AddComponent<KcpTransport>();
            transport.Port = port;
        }
    }

    private void Awake()
    {
        _role = GetRequiredArgument("--project-validation-role").ToLowerInvariant();
        _clientIndex = GetIntArgument("--project-validation-client-index", 0, 0, 64);
        _expectedPlayers = GetIntArgument("--project-validation-player-count", 2, 1, 16);
        _port = (ushort)GetIntArgument("--project-validation-port", 7777, 1024, 65535);
        _scene = GetOptionalArgument("--project-validation-scene", "MN_Run");
        _outputDirectory = Path.GetFullPath(GetRequiredArgument("--project-validation-output"));
        _exerciseReconnect = GetBoolArgument("--project-validation-reconnect", true);
        float timeout = GetFloatArgument("--project-validation-timeout", 180f, 30f, 600f);
        _globalDeadline = Time.realtimeSinceStartup + timeout;
        _managedMemoryStart = GC.GetTotalMemory(false);
        _unityAllocatedMemoryStart = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        _peakManagedMemory = _managedMemoryStart;
        _peakUnityAllocatedMemory = _unityAllocatedMemoryStart;
        Directory.CreateDirectory(_outputDirectory);
        Application.runInBackground = true;
        NetworkDiagnostics.InMessageEvent += OnMessageIn;
        NetworkDiagnostics.OutMessageEvent += OnMessageOut;
        Application.logMessageReceived += OnLogMessage;
    }

    private IEnumerator Start()
    {
        // Mirror clears diagnostics delegates from its own runtime initializer.
        // Reattach after all runtime initializers have completed.
        NetworkDiagnostics.InMessageEvent -= OnMessageIn;
        NetworkDiagnostics.OutMessageEvent -= OnMessageOut;
        NetworkDiagnostics.InMessageEvent += OnMessageIn;
        NetworkDiagnostics.OutMessageEvent += OnMessageOut;
        yield return GuardedRun();
    }

    private void Update()
    {
        if (_finished) return;
        _frameMilliseconds.Add(Time.unscaledDeltaTime * 1000d);
        _peakManagedMemory = Math.Max(_peakManagedMemory, GC.GetTotalMemory(false));
        _peakUnityAllocatedMemory = Math.Max(_peakUnityAllocatedMemory, UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong());
        try { _peakWorkingSet = Math.Max(_peakWorkingSet, Process.GetCurrentProcess().WorkingSet64); }
        catch { /* Working set is best-effort telemetry. */ }

        foreach (PlayerScript player in FindObjectsByType<PlayerScript>())
        {
            if (player == null) continue;
            if (player.IsDead && !_sawDeath)
            {
                _sawDeath = true;
                _observedDeadNetId = player.netId;
            }
            else if (_sawDeath && player.netId == _observedDeadNetId && !player.IsDead)
            {
                _sawRespawnAfterDeath = true;
            }
        }
        PlayerData[] playerData = FindObjectsByType<PlayerData>();
        if (playerData.Any(player => player != null && player.isSpectating)) _sawSpectating = true;
        else if (_sawSpectating && playerData.Length > 0) _sawSpectateExit = true;
        if (VotingManager.Instance != null && VotingManager.Instance.IsVotingActive) _sawVoting = true;
        if (SceneManager.GetSceneByName("ResultsOverlay").isLoaded && FindAnyObjectByType<ResultsUI>() != null) _sawResults = true;

        if (Time.realtimeSinceStartup > _globalDeadline)
            FailAndQuit("global-timeout", $"Runtime smoke exceeded its timeout in role '{_role}'.");
    }

    private IEnumerator GuardedRun()
    {
        if (_role == "host") yield return RunHost();
        else if (_role == "client") yield return RunClient();
        else
        {
            FailAndQuit("invalid-role", $"Unknown project validation role '{_role}'.");
            yield break;
        }

        CompleteAndQuit();
    }

    private IEnumerator RunHost()
    {
        yield return WaitFor("network-manager", () => FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include) != null, 30f,
            "MyNetworkManager exists in the startup scene");
        if (HasFailed()) yield break;

        MyNetworkManager manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
        ConfigureKcp(manager);
        manager.StartDevHost();
        yield return WaitFor("cluster-connect", () => NetworkServer.active && NetworkServer.connections.Count >= _expectedPlayers, 60f,
            $"KCP host admitted {_expectedPlayers} player processes");
        if (HasFailed()) yield break;

        float sceneStart = Time.realtimeSinceStartup;
        manager.ServerChangeSceneSynchronized(_scene);
        yield return WaitFor("scene-transition", () => ActiveSceneIs(_scene) && NetworkServer.connections.Values.All(c => c != null && c.isReady), 100f,
            $"all server connections loaded and became ready in {_scene}");
        _sceneLoadSeconds = Time.realtimeSinceStartup - sceneStart;
        if (HasFailed()) yield break;

        yield return ReadyLocalPlayer();
        yield return WaitFor("minigame-start", MatchIsRunning, 60f, "briefing completed and minigame timer started");
        if (HasFailed()) yield break;

        if (_exerciseReconnect && _expectedPlayers > 1)
        {
            yield return WaitFor("client-leave", () => NetworkServer.connections.Count < _expectedPlayers, 35f,
                "designated client disconnected during the active match");
            if (HasFailed()) yield break;
            yield return WaitFor("client-reconnect", () => NetworkServer.connections.Count >= _expectedPlayers, 45f,
                "designated client reconnected and was admitted again");
            if (HasFailed()) yield break;
        }
        else AddSkipped("client-reconnect", "Reconnect exercise disabled or no remote client exists.");

        yield return WaitFor("reconnected-player-ready", () => NetworkServer.connections.Values.All(c => c != null && c.identity != null), 30f,
            "every server connection owns a player identity");
        if (HasFailed()) yield break;

        VotingManager voting = VotingManager.Instance;
        if (voting == null)
        {
            AddFailure("voting", "VotingManager is missing in the representative minigame scene.");
            yield break;
        }
        // StartDevHost intentionally removes LobbyController. Reproduce only
        // its voting-state initialization so this KCP harness exercises the
        // same VotingManager contract without retaining Steam/lobby behavior.
        if (MinigameRotationState.Instance == null)
        {
            var rotationObject = new GameObject("ProjectValidationMinigameRotationState");
            rotationObject.AddComponent<MinigameRotationState>();
        }
        MinigameCatalog catalog = manager.MinigameCatalog;
        MinigameRotationState.Instance.SetCatalog(catalog);
        voting.SetCatalog(catalog);
        voting.SetVotingDuration(3f);
        bool votingStarted = voting.StartVotingRound();
        if (!votingStarted)
        {
            AddFailure("voting", "VotingManager rejected StartVotingRound; catalog/rotation may be invalid.");
            yield break;
        }
        foreach (PlayerData player in FindObjectsByType<PlayerData>())
            if (player != null && player.playerInfo.steamId != 0) voting.RegisterVote(player.playerInfo.steamId, 0, voting.RoundId);
        yield return new WaitForSecondsRealtime(1f);
        int votes = voting.GetVoteCounts().Sum();
        voting.EndVoting();
        AddResult("voting", votes > 0, $"Voting round replicated and accepted {votes} vote(s).");
        if (HasFailed()) yield break;

        PlayerScript target = FindObjectsByType<PlayerScript>()
            .FirstOrDefault(player => player != null && player.connectionToClient != NetworkServer.localConnection);
        if (target == null) target = FindAnyObjectByType<PlayerScript>();
        if (target == null)
        {
            AddFailure("death-respawn", "No spawned PlayerScript was available.");
            yield break;
        }
        Vector3 respawnPosition = target.transform.position;
        Quaternion respawnRotation = target.transform.rotation;
        target.ServerHandleContextualHit(DeathCause.Default, false);
        yield return WaitFor("death-state", () => target != null && target.IsDead, 10f, "server-authoritative death state became visible");
        if (HasFailed()) yield break;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.5f, target.ServerGetRemainingDeathPresentationTime()));
        target.ServerTeleport(respawnPosition, respawnRotation);
        target.RpcOnRespawn();
        yield return WaitFor("respawn-state", () => target != null && !target.IsDead, 10f, "death state cleared after server respawn");
        if (HasFailed()) yield break;

        PlayerData targetData = target.GetComponent<PlayerData>();
        target.ServerForceSpectate(DeathCause.Default);
        yield return WaitFor("spectate-state", () => target != null && target.IsDead && targetData != null && targetData.isSpectating,
            10f, "permanent death set the authoritative spectator state");
        if (HasFailed()) yield break;
        yield return new WaitForSecondsRealtime(Mathf.Max(2.1f, target.ServerGetRemainingDeathPresentationTime()));
        target.ServerTeleport(respawnPosition, respawnRotation);
        target.RpcOnRespawn();
        yield return WaitFor("spectate-exit", () => target != null && !target.IsDead && targetData != null && !targetData.isSpectating,
            10f, "server respawn cleared spectator and death state");
        if (HasFailed()) yield break;

        MatchManager firstMatch = MatchManager.singleton;
        manager.ServerChangeSceneSynchronized(_scene);
        yield return WaitFor("minigame-reload", () => ActiveSceneIs(_scene) && MatchManager.singleton != null && MatchManager.singleton != firstMatch &&
            NetworkServer.connections.Values.All(connection => connection != null && connection.isReady), 100f,
            "all players reloaded a fresh instance of the representative minigame");
        if (HasFailed()) yield break;
        yield return ReadyLocalPlayer();
        yield return WaitFor("minigame-restart", MatchIsRunning, 60f, "second briefing completed and a fresh minigame started");
        if (HasFailed()) yield break;

        // Do not end the match in the same server tick that first observes its
        // timer. Give the SyncVar at least a few network intervals to become
        // observable by remote clients before loading the results overlay.
        yield return new WaitForSecondsRealtime(2f);

        MatchManager match = MatchManager.singleton;
        if (match == null)
        {
            AddFailure("results", "MatchManager disappeared before results validation.");
            yield break;
        }
        match.InternalEndMatch();
        yield return WaitFor("results", () => _sawResults, 45f, "results overlay loaded and ResultsUI became available");
    }

    private IEnumerator RunClient()
    {
        yield return WaitFor("network-manager", () => FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include) != null, 30f,
            "MyNetworkManager exists in the startup scene");
        if (HasFailed()) yield break;

        MyNetworkManager manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
        ConfigureKcp(manager);
        manager.StartDevClient("127.0.0.1");
        yield return WaitFor("cluster-connect", () => NetworkClient.isConnected && NetworkClient.localPlayer != null, 60f,
            "client connected to KCP host and received a local player");
        if (HasFailed()) yield break;

        float sceneStart = Time.realtimeSinceStartup;
        yield return WaitFor("scene-transition", () => ActiveSceneIs(_scene) && NetworkClient.localPlayer != null, 100f,
            $"client loaded {_scene} and retained its local player");
        _sceneLoadSeconds = Time.realtimeSinceStartup - sceneStart;
        if (HasFailed()) yield break;

        yield return ReadyLocalPlayer();
        yield return WaitFor("minigame-start", MatchIsRunning, 60f, "client observed the active minigame timer");
        if (HasFailed()) yield break;

        if (_exerciseReconnect && _clientIndex == 1)
        {
            manager.StopClient();
            yield return WaitFor("client-leave", () => !NetworkClient.active, 15f, "client stopped its KCP connection");
            if (HasFailed()) yield break;
            yield return new WaitForSecondsRealtime(2f);
            yield return WaitFor("network-manager-after-leave",
                () => FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include) != null,
                20f, "a network manager is available after returning to the offline scene");
            if (HasFailed()) yield break;
            manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
            ConfigureKcp(manager);
            manager.StartDevClient("127.0.0.1");
            yield return WaitFor("client-reconnect", () => NetworkClient.isConnected && NetworkClient.localPlayer != null && ActiveSceneIs(_scene), 45f,
                "client reconnected, reloaded the active scene, and received a player");
            if (HasFailed()) yield break;
        }
        else AddSkipped("client-reconnect", "Only client index 1 performs the reconnect exercise.");

        yield return WaitFor("voting", () => _sawVoting, 45f, "client observed an active synchronized voting round");
        if (HasFailed()) yield break;
        yield return WaitFor("death-state", () => _sawDeath, 45f, "client observed a player death");
        if (HasFailed()) yield break;
        yield return WaitFor("respawn-state", () => _sawRespawnAfterDeath, 30f, "client observed death presentation restore after respawn");
        if (HasFailed()) yield break;
        yield return WaitFor("spectate-state", () => _sawSpectating, 45f, "client observed synchronized spectator state after permanent death");
        if (HasFailed()) yield break;
        yield return WaitFor("spectate-exit", () => _sawSpectateExit, 30f, "client observed spectator state clear after server respawn");
        if (HasFailed()) yield break;
        MatchManager firstMatch = MatchManager.singleton;
        yield return WaitFor("minigame-reload", () => ActiveSceneIs(_scene) && MatchManager.singleton != null && MatchManager.singleton != firstMatch,
            100f, "client loaded a fresh instance of the representative minigame");
        if (HasFailed()) yield break;
        yield return ReadyLocalPlayer();
        yield return WaitFor("minigame-restart", MatchIsRunning, 60f, "client observed the restarted minigame timer");
        if (HasFailed()) yield break;
        yield return WaitFor("results", () => _sawResults, 45f, "client loaded the results overlay and ResultsUI");
    }

    private IEnumerator ReadyLocalPlayer()
    {
        if (BriefingManager.singleton != null)
        {
            yield return WaitFor("briefing-ready-gate", () => BriefingManager.singleton != null && BriefingManager.singleton.ReadyInteractableClient,
                45f, "briefing enabled the normal Ready interaction");
            if (HasFailed()) yield break;
        }
        PlayerData data = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<PlayerData>() : null;
        if (data == null)
        {
            AddFailure("player-ready", "Local player has no PlayerData.");
            yield break;
        }
        data.ToggleReady();
        AddPassed("player-ready", "Ready was requested through PlayerData.ToggleReady().");
    }

    private void ConfigureKcp(MyNetworkManager manager)
    {
        KcpTransport transport = manager.GetComponent<KcpTransport>();
        if (transport == null) transport = manager.gameObject.AddComponent<KcpTransport>();
        transport.Port = _port;
    }

    private IEnumerator WaitFor(string name, Func<bool> condition, float timeout, string successDetail)
    {
        float started = Time.realtimeSinceStartup;
        float deadline = Mathf.Min(_globalDeadline, started + timeout);
        while (!condition() && Time.realtimeSinceStartup < deadline) yield return null;
        double duration = Time.realtimeSinceStartup - started;
        if (condition()) _checks.Add(new Check { name = name, status = "passed", durationSeconds = duration, detail = successDetail });
        else _checks.Add(new Check { name = name, status = "failed", durationSeconds = duration, detail = $"Timed out after {timeout:0.#} seconds: {successDetail}" });
    }

    private static bool ActiveSceneIs(string name) => string.Equals(SceneManager.GetActiveScene().name, name, StringComparison.OrdinalIgnoreCase);
    private static bool MatchIsRunning() => MatchManager.singleton != null && MatchManager.singleton.MatchTimer > 0f && !MatchManager.singleton.Freeze;
    private bool HasFailed() => _checks.Any(check => check.status == "failed");
    private void AddPassed(string name, string detail) => _checks.Add(new Check { name = name, status = "passed", detail = detail });
    private void AddFailure(string name, string detail) => _checks.Add(new Check { name = name, status = "failed", detail = detail });
    private void AddSkipped(string name, string detail) => _checks.Add(new Check { name = name, status = "skipped", detail = detail });
    private void AddResult(string name, bool passed, string detail) => _checks.Add(new Check { name = name, status = passed ? "passed" : "failed", detail = detail });

    private void OnMessageIn(NetworkDiagnostics.MessageInfo info)
    {
        _messagesIn++;
        _bytesIn += info.bytes;
        NetworkMessageTraffic traffic = GetMessageTraffic(info);
        traffic.messagesIn++;
        traffic.bytesIn += info.bytes;
    }

    private void OnMessageOut(NetworkDiagnostics.MessageInfo info)
    {
        long bytes = (long)info.bytes * info.count;
        _messagesOut += info.count;
        _bytesOut += bytes;
        NetworkMessageTraffic traffic = GetMessageTraffic(info);
        traffic.messagesOut += info.count;
        traffic.bytesOut += bytes;
    }

    private NetworkMessageTraffic GetMessageTraffic(NetworkDiagnostics.MessageInfo info)
    {
        string messageType = info.message.GetType().FullName ?? info.message.GetType().Name;
        if (_networkTraffic.TryGetValue(messageType, out NetworkMessageTraffic traffic)) return traffic;
        traffic = new NetworkMessageTraffic { messageType = messageType };
        _networkTraffic.Add(messageType, traffic);
        return traffic;
    }
    private void OnLogMessage(string condition, string stackTrace, LogType type)
    {
        if (type != LogType.Error && type != LogType.Exception && type != LogType.Assert) return;
        if (IsExpectedHeadlessGraphicsError(condition, SystemInfo.graphicsDeviceName))
        {
            _ignoredHeadlessGraphicsErrorCount++;
            if (_ignoredHeadlessGraphicsErrors.Count < 20)
                _ignoredHeadlessGraphicsErrors.Add($"[{type}] {condition}");
            return;
        }
        _runtimeErrorCount++;
        if (_runtimeErrors.Count < 20) _runtimeErrors.Add($"[{type}] {condition}\n{stackTrace}");
    }

    private static bool IsExpectedHeadlessGraphicsError(string condition, string graphicsDeviceName)
    {
        if (!string.Equals(graphicsDeviceName, "Null Device", StringComparison.Ordinal)) return false;
        return condition == "This custom render path shader needs to have at least 1 passes."
            || condition.StartsWith("Could not find material Hidden/VideoDecode", StringComparison.Ordinal)
            || condition.StartsWith("Could not find material Hidden/VideoComposite", StringComparison.Ordinal)
            || condition.StartsWith("Could not find video decode shader pass ", StringComparison.Ordinal)
            || condition.StartsWith("Video shaders not found.", StringComparison.Ordinal);
    }

    private void FailAndQuit(string name, string detail)
    {
        if (_finished) return;
        AddFailure(name, detail);
        CompleteAndQuit();
    }

    private void CompleteAndQuit()
    {
        if (_finished) return;
        _finished = true;
        NetworkDiagnostics.InMessageEvent -= OnMessageIn;
        NetworkDiagnostics.OutMessageEvent -= OnMessageOut;
        Application.logMessageReceived -= OnLogMessage;
        if (_runtimeErrorCount > 0 && !HasFailed())
            AddFailure("runtime-log-errors", $"Captured {_runtimeErrorCount} Error/Exception/Assert log message(s); see the first {_runtimeErrors.Count} in runtimeErrors and the complete Player.log.");
        double[] frames = _frameMilliseconds.Where(value => value > 0d).OrderBy(value => value).ToArray();
        double p95 = frames.Length == 0 ? 0d : frames[(int)Math.Min(frames.Length - 1, Math.Ceiling((frames.Length - 1) * 0.95d))];
        long managedMemoryEnd = GC.GetTotalMemory(false);
        long unityAllocatedMemoryEnd = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
        var summary = new RuntimeSummary
        {
            utc = DateTime.UtcNow.ToString("O"), role = _role, clientIndex = _clientIndex, expectedPlayers = _expectedPlayers,
            scene = _scene, unityVersion = Application.unityVersion, processor = SystemInfo.processorType,
            graphicsDevice = SystemInfo.graphicsDeviceName, succeeded = !HasFailed(), networkBytesIn = _bytesIn,
            networkBytesOut = _bytesOut, networkMessagesIn = _messagesIn, networkMessagesOut = _messagesOut,
            networkMessageTraffic = _networkTraffic.Values.OrderByDescending(traffic => traffic.bytesIn + traffic.bytesOut).ToArray(),
            peakManagedMemoryBytes = _peakManagedMemory, peakWorkingSetBytes = _peakWorkingSet,
            peakUnityAllocatedMemoryBytes = _peakUnityAllocatedMemory,
            managedMemoryStartBytes = _managedMemoryStart, managedMemoryEndBytes = managedMemoryEnd,
            managedMemoryGrowthBytes = managedMemoryEnd - _managedMemoryStart,
            unityAllocatedMemoryStartBytes = _unityAllocatedMemoryStart, unityAllocatedMemoryEndBytes = unityAllocatedMemoryEnd,
            unityAllocatedMemoryGrowthBytes = unityAllocatedMemoryEnd - _unityAllocatedMemoryStart,
            sceneLoadSeconds = _sceneLoadSeconds, frameP95Milliseconds = p95,
            runtimeErrorCount = _runtimeErrorCount, runtimeErrors = _runtimeErrors.ToArray(),
            ignoredHeadlessGraphicsErrorCount = _ignoredHeadlessGraphicsErrorCount,
            ignoredHeadlessGraphicsErrors = _ignoredHeadlessGraphicsErrors.ToArray(), checks = _checks.ToArray()
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "runtime-summary.json"), JsonUtility.ToJson(summary, true));
        File.WriteAllText(Path.Combine(_outputDirectory, "completed.txt"), DateTime.UtcNow.ToString("O"));
        Debug.Log($"[ProjectValidation] COMPLETE role={_role} success={summary.succeeded} output={_outputDirectory}");
        Application.Quit(summary.succeeded ? 0 : 2);
    }

    private void OnDestroy()
    {
        NetworkDiagnostics.InMessageEvent -= OnMessageIn;
        NetworkDiagnostics.OutMessageEvent -= OnMessageOut;
        Application.logMessageReceived -= OnLogMessage;
    }

    private static string GetRequiredArgument(string name)
    {
        if (TryGetArgument(name, out string value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException($"Missing required argument {name}");
    }
    private static string GetOptionalArgument(string name, string fallback) => TryGetArgument(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    private static int GetIntArgument(string name, int fallback, int minimum, int maximum) => TryGetArgument(name, out string value) && int.TryParse(value, out int parsed) ? Mathf.Clamp(parsed, minimum, maximum) : fallback;
    private static float GetFloatArgument(string name, float fallback, float minimum, float maximum) => TryGetArgument(name, out string value) && float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed) ? Mathf.Clamp(parsed, minimum, maximum) : fallback;
    private static bool GetBoolArgument(string name, bool fallback) => TryGetArgument(name, out string value) && bool.TryParse(value, out bool parsed) ? parsed : fallback;
    private static bool TryGetArgument(string name, out string value)
    {
        string prefix = name + "=";
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) { value = argument.Substring(prefix.Length).Trim('"'); return true; }
            if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase)) { value = "true"; return true; }
        }
        value = null;
        return false;
    }
}
#endif
