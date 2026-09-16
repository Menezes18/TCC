#if DEVELOPMENT_BUILD || UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using kcp2k;
using Mirror;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.SceneManagement;

/// <summary>
/// Opt-in Development Player bootstrap for repeatable performance captures.
/// It is inert unless --performance-audit-scene is present on the command line.
/// </summary>
public sealed class PerformanceAuditBootstrap : MonoBehaviour
{
    [Serializable]
    private sealed class CaptureSummary
    {
        public string schema = "tcc-performance-audit/v1";
        public string scene;
        public string utc;
        public string unityVersion;
        public string operatingSystem;
        public string processor;
        public string graphicsDevice;
        public string graphicsApi;
        public int graphicsMemoryMB;
        public string quality;
        public int width;
        public int height;
        public bool fullscreen;
        public float renderScale;
        public int requestedFrames;
        public int sampledFrames;
        public float warmupSeconds;
        public bool rawProfilerCapture;
        public string rawProfilerPath;
        public string role;
        public int clientIndex;
        public int expectedPlayers;
        public bool measuredProcess;
        public long networkBytesIn;
        public long networkBytesOut;
        public long networkMessagesIn;
        public long networkMessagesOut;
        public NetworkMessageTraffic[] networkMessageTraffic;
        public string[] unavailableCounters;
        public MetricSummary[] metrics;
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

    [Serializable]
    private sealed class SoakSummary
    {
        public string schema = "tcc-performance-soak/v1";
        public string utc;
        public int transitionsRequested;
        public string[] rotationScenes;
        public MemorySnapshot[] snapshots;
    }

    [Serializable]
    private sealed class MemorySnapshot
    {
        public int transitionIndex;
        public string phase;
        public string scene;
        public double sceneLoadSeconds;
        public long managedMemoryBytes;
        public long unityAllocatedMemoryBytes;
        public long unityReservedMemoryBytes;
        public long processWorkingSetBytes;
        public long textureMemoryBytes;
        public long meshMemoryBytes;
        public long renderTextureMemoryBytes;
        public long materialMemoryBytes;
        public int textureCount;
        public int meshCount;
        public int renderTextureCount;
        public int materialCount;
    }

    [Serializable]
    private sealed class MetricSummary
    {
        public string name;
        public string source;
        public string unit;
        public int samples;
        public double median;
        public double p95;
        public double p99;
        public double max;
    }

    private sealed class Samples
    {
        public readonly string Name;
        public readonly string Unit;
        public string Source;
        private readonly List<double> _values;

        public Samples(string name, string unit, int capacity, string source = null)
        {
            Name = name;
            Unit = unit;
            Source = string.IsNullOrEmpty(source) ? name : source;
            _values = new List<double>(capacity);
        }

        public void Add(double value, bool requirePositive = false)
        {
            if (double.IsNaN(value) || double.IsInfinity(value)) return;
            if (requirePositive && value <= 0d) return;
            _values.Add(value);
        }

        public MetricSummary Summarize()
        {
            if (_values.Count == 0) return null;
            double[] sorted = _values.OrderBy(value => value).ToArray();
            return new MetricSummary
            {
                name = Name,
                source = Source,
                unit = Unit,
                samples = sorted.Length,
                median = Percentile(sorted, 0.50d),
                p95 = Percentile(sorted, 0.95d),
                p99 = Percentile(sorted, 0.99d),
                max = sorted[sorted.Length - 1]
            };
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 1) return sorted[0];
            double position = (sorted.Length - 1) * percentile;
            int lower = Mathf.FloorToInt((float)position);
            int upper = Mathf.CeilToInt((float)position);
            if (lower == upper) return sorted[lower];
            return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
        }
    }

    private sealed class Counter : IDisposable
    {
        public readonly Samples Samples;
        public readonly string CounterName;
        private ProfilerRecorder _recorder;

        public bool Available => _recorder.Valid;

        public Counter(string metricName, string unit, ProfilerCategory category, int capacity, params string[] names)
        {
            Samples = new Samples(metricName, unit, capacity);
            foreach (string name in names)
            {
                try
                {
                    ProfilerRecorder candidate = ProfilerRecorder.StartNew(category, name, 1);
                    if (candidate.Valid)
                    {
                        _recorder = candidate;
                        CounterName = name;
                        Samples.Source = name;
                        return;
                    }
                    candidate.Dispose();
                }
                catch (Exception)
                {
                    // Try the next version-specific alias.
                }
            }
            CounterName = string.Join(" | ", names);
        }

        public void Record()
        {
            if (!Available) return;
            double value = _recorder.LastValueAsDouble;
            if (_recorder.UnitType == ProfilerMarkerDataUnit.TimeNanoseconds)
                value /= 1_000_000d;
            Samples.Add(value);
        }

        public void Dispose()
        {
            if (_recorder.Valid) _recorder.Dispose();
        }
    }

    private static readonly WaitForEndOfFrame EndOfFrame = new WaitForEndOfFrame();
    private readonly FrameTiming[] _frameTimings = new FrameTiming[1];
    private string _scene;
    private string _outputDirectory;
    private int _frameCount;
    private float _warmupSeconds;
    private bool _rawCapture;
    private string _failure;
    private bool _driveMovement;
    private PlayerControlsSO _controls;
    private string _role;
    private int _clientIndex;
    private int _expectedPlayers;
    private ushort _port;
    private bool _measuredProcess;
    private long _networkBytesIn;
    private long _networkBytesOut;
    private long _networkMessagesIn;
    private long _networkMessagesOut;
    private readonly Dictionary<string, NetworkMessageTraffic> _networkTraffic = new();
    private int _transitionCount;
    private string[] _rotationScenes;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void InstallWhenRequested()
    {
        if (!TryGetArgument("--performance-audit-scene", out _)) return;
        if (TryGetArgument("--performance-audit-role", out _))
            SceneManager.sceneLoaded += ConfigureInitialNetworkManager;
        var root = new GameObject("PerformanceAuditBootstrap");
        DontDestroyOnLoad(root);
        root.AddComponent<PerformanceAuditBootstrap>();
    }

    private static void ConfigureInitialNetworkManager(Scene scene, LoadSceneMode mode)
    {
        MyNetworkManager manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
        if (manager == null) return;
        manager.headlessStartMode = HeadlessStartOptions.DoNothing;
        if (TryGetArgument("--performance-audit-port", out string value) && ushort.TryParse(value, out ushort port))
        {
            KcpTransport transport = manager.GetComponent<KcpTransport>();
            if (transport == null) transport = manager.gameObject.AddComponent<KcpTransport>();
            transport.Port = port;
        }
    }

    private IEnumerator Start()
    {
        _scene = GetRequiredArgument("--performance-audit-scene");
        _outputDirectory = Path.GetFullPath(GetRequiredArgument("--performance-audit-output"));
        _frameCount = GetIntArgument("--performance-audit-frames", 2000, 30, 10000);
        _warmupSeconds = GetFloatArgument("--performance-audit-warmup", 2f, 0f, 120f);
        _rawCapture = GetBoolArgument("--performance-audit-raw", false);
        _role = GetOptionalArgument("--performance-audit-role", "single").ToLowerInvariant();
        _clientIndex = GetIntArgument("--performance-audit-client-index", 0, 0, 16);
        _expectedPlayers = GetIntArgument("--performance-audit-player-count", 1, 1, 16);
        _port = (ushort)GetIntArgument("--performance-audit-port", 7777, 1024, 65535);
        _measuredProcess = GetBoolArgument("--performance-audit-measured", true);
        _transitionCount = GetIntArgument("--performance-audit-transitions", 0, 0, 100);
        _rotationScenes = GetOptionalArgument("--performance-audit-soak-scenes", _scene)
            .Split(',').Select(value => value.Trim()).Where(value => !string.IsNullOrEmpty(value)).ToArray();
        Directory.CreateDirectory(_outputDirectory);

        NetworkDiagnostics.InMessageEvent -= OnMessageIn;
        NetworkDiagnostics.OutMessageEvent -= OnMessageOut;
        NetworkDiagnostics.InMessageEvent += OnMessageIn;
        NetworkDiagnostics.OutMessageEvent += OnMessageOut;

        Application.runInBackground = true;
        ApplyRequestedTestSettings();
        yield return null;
        yield return null;
        Debug.Log($"[PerfAudit] START scene={_scene} frames={_frameCount} warmup={_warmupSeconds:0.###} raw={_rawCapture}");

        yield return WaitForCondition(
            () => FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include) != null,
            30f,
            "MyNetworkManager was not available in the startup scene");
        if (!string.IsNullOrEmpty(_failure)) yield break;

        MyNetworkManager manager = FindAnyObjectByType<MyNetworkManager>(FindObjectsInactive.Include);
        yield return EstablishSession(manager);
        if (!string.IsNullOrEmpty(_failure)) yield break;

        yield return ReadyForCurrentScene();
        if (!string.IsNullOrEmpty(_failure)) yield break;

        ResolveMovementControls();
        _driveMovement = _controls != null;
        Debug.Log($"[PerfAudit] READY scene={_scene} host={NetworkServer.active} player={NetworkClient.localPlayer != null} " +
                  $"scriptedMovement={_driveMovement} quality={QualitySettings.names[QualitySettings.GetQualityLevel()].Trim()} " +
                  $"resolution={Screen.width}x{Screen.height} fullscreen={Screen.fullScreen} renderScale={GetRenderScale():0.00}");

        float warmupDeadline = Time.realtimeSinceStartup + _warmupSeconds;
        while (Time.realtimeSinceStartup < warmupDeadline)
            yield return null;

        if (!_measuredProcess)
        {
            File.WriteAllText(Path.Combine(_outputDirectory, "peer-ready.txt"), DateTime.UtcNow.ToString("O"));
            while (true) yield return new WaitForSecondsRealtime(1f);
        }

        if (_transitionCount > 0)
        {
            yield return RunTransitionSoak(manager);
            yield break;
        }

        yield return Capture();
    }

    private IEnumerator ReadyForCurrentScene()
    {
        if (string.Equals(_scene, "RASCUNHO", StringComparison.OrdinalIgnoreCase)) yield break;

        float briefingDeadline = Time.realtimeSinceStartup + 15f;
        while (BriefingManager.singleton == null && Time.realtimeSinceStartup < briefingDeadline)
            yield return null;

        if (BriefingManager.singleton != null)
        {
            yield return WaitForCondition(
                () => BriefingManager.singleton != null && BriefingManager.singleton.ReadyInteractableClient,
                45f, "briefing acknowledgement did not enable Ready");
            if (!string.IsNullOrEmpty(_failure)) yield break;

            PlayerData playerData = NetworkClient.localPlayer != null ? NetworkClient.localPlayer.GetComponent<PlayerData>() : null;
            if (playerData == null)
            {
                Fail("the local player has no PlayerData component");
                yield break;
            }
            playerData.ToggleReady();
            yield return WaitForCondition(
                () => BriefingManager.singleton != null && BriefingManager.singleton.HasFinishedBriefing,
                45f, "the briefing did not finish after Ready");
            if (!string.IsNullOrEmpty(_failure)) yield break;
        }

        yield return WaitForCondition(
            () => MatchManager.singleton != null && MatchManager.singleton.MatchTimer > 0f && !MatchManager.singleton.Freeze,
            45f, "the active match timer did not start");
    }

    private IEnumerator EstablishSession(MyNetworkManager manager)
    {
        KcpTransport transport = manager.GetComponent<KcpTransport>();
        if (transport == null) transport = manager.gameObject.AddComponent<KcpTransport>();
        transport.Port = _port;

        if (_role == "single")
        {
            manager.StartDevHost();
            yield return WaitForCondition(() => NetworkServer.active && NetworkClient.localPlayer != null, 45f,
                "the local KCP host/player did not become ready");
        }
        else if (_role == "host")
        {
            manager.StartDevHost();
            yield return WaitForCondition(() => NetworkServer.active && NetworkClient.localPlayer != null, 45f,
                "the local KCP host/player did not become ready");
            if (!string.IsNullOrEmpty(_failure)) yield break;
            File.WriteAllText(Path.Combine(_outputDirectory, "network-ready.txt"), DateTime.UtcNow.ToString("O"));
            yield return WaitForCondition(
                () => NetworkServer.connections.Count >= _expectedPlayers,
                60f, $"the KCP host did not admit {_expectedPlayers} player processes");
        }
        else if (_role == "client")
        {
            manager.StartDevClient("127.0.0.1");
            yield return WaitForCondition(() => NetworkClient.isConnected && NetworkClient.localPlayer != null, 60f,
                "the KCP client did not connect and receive a local player");
        }
        else
        {
            Fail($"unknown performance audit role '{_role}'");
        }
        if (!string.IsNullOrEmpty(_failure)) yield break;

        if (string.Equals(_scene, "RASCUNHO", StringComparison.OrdinalIgnoreCase)) yield break;
        if (_role == "single" || _role == "host") manager.ServerChangeSceneSynchronized(_scene);
        yield return WaitForCondition(
            () => string.Equals(SceneManager.GetActiveScene().name, _scene, StringComparison.OrdinalIgnoreCase) &&
                  NetworkClient.localPlayer != null,
            90f, $"scene '{_scene}' did not finish loading for role '{_role}'");
    }

    private void Update()
    {
        if (!_driveMovement || _controls == null) return;
        int phase = Mathf.FloorToInt(Time.unscaledTime * 0.5f) & 3;
        Vector2 direction = phase switch
        {
            0 => Vector2.up,
            1 => Vector2.right,
            2 => Vector2.down,
            _ => Vector2.left
        };
        _controls.Move(direction, direction);
    }

    private IEnumerator RunTransitionSoak(MyNetworkManager manager)
    {
        if (_role != "single" || _expectedPlayers != 1)
        {
            Fail("transition soak currently requires the single graphical host scenario");
            yield break;
        }
        if (_rotationScenes.Length == 0)
        {
            Fail("transition soak requires at least one rotation scene");
            yield break;
        }

        var snapshots = new List<MemorySnapshot>(_transitionCount + 3)
        {
            CaptureMemorySnapshot(0, "initial-settled", _scene, 0d)
        };
        WriteSoakSummary(snapshots);

        for (int index = 1; index <= _transitionCount; index++)
        {
            string nextScene = _rotationScenes[index % _rotationScenes.Length];
            float started = Time.realtimeSinceStartup;
            manager.ServerChangeSceneSynchronized(nextScene);
            yield return WaitForCondition(
                () => string.Equals(SceneManager.GetActiveScene().name, nextScene, StringComparison.OrdinalIgnoreCase) &&
                      NetworkClient.localPlayer != null,
                90f, $"soak transition {index} did not load scene '{nextScene}'");
            if (!string.IsNullOrEmpty(_failure)) yield break;

            _scene = nextScene;
            yield return ReadyForCurrentScene();
            if (!string.IsNullOrEmpty(_failure)) yield break;
            yield return new WaitForSecondsRealtime(_warmupSeconds);
            snapshots.Add(CaptureMemorySnapshot(index, "scene-settled", _scene,
                Time.realtimeSinceStartup - started));
            WriteSoakSummary(snapshots);
        }

        float lobbyStarted = Time.realtimeSinceStartup;
        manager.ServerChangeSceneSynchronized("RASCUNHO");
        yield return WaitForCondition(
            () => string.Equals(SceneManager.GetActiveScene().name, "RASCUNHO", StringComparison.OrdinalIgnoreCase) &&
                  NetworkClient.localPlayer != null,
            90f, "soak did not return to RASCUNHO");
        if (!string.IsNullOrEmpty(_failure)) yield break;
        yield return new WaitForSecondsRealtime(_warmupSeconds);
        snapshots.Add(CaptureMemorySnapshot(_transitionCount + 1, "lobby-settled", "RASCUNHO",
            Time.realtimeSinceStartup - lobbyStarted));
        WriteSoakSummary(snapshots);

        AsyncOperation unload = Resources.UnloadUnusedAssets();
        while (!unload.isDone) yield return null;
        GC.Collect();
        GC.WaitForPendingFinalizers();
        snapshots.Add(CaptureMemorySnapshot(_transitionCount + 2, "lobby-after-explicit-unload", "RASCUNHO", 0d));
        WriteSoakSummary(snapshots);
        string summaryPath = Path.Combine(_outputDirectory, "soak-summary.json");
        File.WriteAllText(Path.Combine(_outputDirectory, "completed.txt"), DateTime.UtcNow.ToString("O"));
        Debug.Log($"[PerfAudit] SOAK COMPLETE transitions={_transitionCount} summary={summaryPath}");
        _driveMovement = false;
        Application.Quit(0);
    }

    private void WriteSoakSummary(List<MemorySnapshot> snapshots)
    {
        var result = new SoakSummary
        {
            utc = DateTime.UtcNow.ToString("O"),
            transitionsRequested = _transitionCount,
            rotationScenes = _rotationScenes,
            snapshots = snapshots.ToArray()
        };
        File.WriteAllText(Path.Combine(_outputDirectory, "soak-summary.json"), JsonUtility.ToJson(result, true));
    }

    private static MemorySnapshot CaptureMemorySnapshot(int transitionIndex, string phase, string scene, double loadSeconds)
    {
        Texture[] textures = Resources.FindObjectsOfTypeAll<Texture>();
        Mesh[] meshes = Resources.FindObjectsOfTypeAll<Mesh>();
        RenderTexture[] renderTextures = Resources.FindObjectsOfTypeAll<RenderTexture>();
        Material[] materials = Resources.FindObjectsOfTypeAll<Material>();
        long processWorkingSet = 0;
        try { processWorkingSet = System.Diagnostics.Process.GetCurrentProcess().WorkingSet64; }
        catch { /* best-effort process telemetry */ }

        return new MemorySnapshot
        {
            transitionIndex = transitionIndex,
            phase = phase,
            scene = scene,
            sceneLoadSeconds = loadSeconds,
            managedMemoryBytes = GC.GetTotalMemory(false),
            unityAllocatedMemoryBytes = Profiler.GetTotalAllocatedMemoryLong(),
            unityReservedMemoryBytes = Profiler.GetTotalReservedMemoryLong(),
            processWorkingSetBytes = processWorkingSet,
            textureMemoryBytes = SumRuntimeMemory(textures.Where(texture => texture is not RenderTexture)),
            meshMemoryBytes = SumRuntimeMemory(meshes),
            renderTextureMemoryBytes = SumRuntimeMemory(renderTextures),
            materialMemoryBytes = SumRuntimeMemory(materials),
            textureCount = textures.Count(texture => texture is not RenderTexture),
            meshCount = meshes.Length,
            renderTextureCount = renderTextures.Length,
            materialCount = materials.Length
        };
    }

    private static long SumRuntimeMemory<T>(IEnumerable<T> objects) where T : UnityEngine.Object
    {
        long total = 0;
        foreach (T value in objects)
            if (value != null) total += Profiler.GetRuntimeMemorySizeLong(value);
        return total;
    }

    private IEnumerator Capture()
    {
        _networkBytesIn = 0;
        _networkBytesOut = 0;
        _networkMessagesIn = 0;
        _networkMessagesOut = 0;
        _networkTraffic.Clear();

        var metrics = new List<Samples>
        {
            new Samples("frame", "ms", _frameCount, "Time.unscaledDeltaTime"),
            new Samples("cpuFrame", "ms", _frameCount, "FrameTiming.cpuFrameTime"),
            new Samples("mainThread", "ms", _frameCount, "FrameTiming.cpuMainThreadFrameTime"),
            new Samples("renderThread", "ms", _frameCount, "FrameTiming.cpuRenderThreadFrameTime"),
            new Samples("gpuFrame", "ms", _frameCount, "FrameTiming.gpuFrameTime")
        };

        var counters = new List<Counter>
        {
            new Counter("scripts", "ms", ProfilerCategory.Scripts, _frameCount, "Scripts", "BehaviourUpdate"),
            new Counter("physics", "ms", ProfilerCategory.Physics, _frameCount, "Physics", "Physics.Simulate"),
            new Counter("gcAlloc", "bytes", ProfilerCategory.Memory, _frameCount, "GC Allocated In Frame"),
            new Counter("setPass", "count", ProfilerCategory.Render, _frameCount, "SetPass Calls Count"),
            new Counter("drawCalls", "count", ProfilerCategory.Render, _frameCount, "Draw Calls Count"),
            new Counter("batches", "count", ProfilerCategory.Render, _frameCount, "Batches Count"),
            new Counter("triangles", "count", ProfilerCategory.Render, _frameCount, "Triangles Count"),
            new Counter("vertices", "count", ProfilerCategory.Render, _frameCount, "Vertices Count"),
            new Counter("totalUsedMemory", "bytes", ProfilerCategory.Memory, _frameCount, "Total Used Memory"),
            new Counter("textureMemory", "bytes", ProfilerCategory.Memory, _frameCount, "Texture Memory"),
            new Counter("meshMemory", "bytes", ProfilerCategory.Memory, _frameCount, "Mesh Memory"),
            new Counter("renderTextureMemory", "bytes", ProfilerCategory.Memory, _frameCount, "Render Texture Memory"),
            new Counter("gfxUsedMemory", "bytes", ProfilerCategory.Memory, _frameCount, "Gfx Used Memory")
        };

        string rawPath = Path.Combine(_outputDirectory, $"{_scene}.raw");
        if (_rawCapture)
        {
            Profiler.enableAllocationCallstacks = true;
            Profiler.logFile = rawPath;
            Profiler.enableBinaryLog = true;
            Profiler.enabled = true;
        }

        int timingSamples = 0;
        for (int frame = 0; frame < _frameCount; frame++)
        {
            FrameTimingManager.CaptureFrameTimings();
            yield return EndOfFrame;

            metrics[0].Add(Time.unscaledDeltaTime * 1000d, true);
            if (FrameTimingManager.GetLatestTimings(1, _frameTimings) > 0)
            {
                FrameTiming timing = _frameTimings[0];
                metrics[1].Add(timing.cpuFrameTime, true);
                metrics[2].Add(timing.cpuMainThreadFrameTime, true);
                metrics[3].Add(timing.cpuRenderThreadFrameTime, true);
                metrics[4].Add(timing.gpuFrameTime, true);
                timingSamples++;
            }
            foreach (Counter counter in counters) counter.Record();
        }

        if (_rawCapture)
        {
            Profiler.enabled = false;
            Profiler.enableBinaryLog = false;
            Profiler.enableAllocationCallstacks = false;
        }

        var unavailable = counters.Where(counter => !counter.Available).Select(counter => counter.CounterName).ToArray();
        foreach (Counter counter in counters)
        {
            if (counter.Available) metrics.Add(counter.Samples);
            counter.Dispose();
        }

        MetricSummary[] summaries = metrics.Select(metric => metric.Summarize()).Where(summary => summary != null).ToArray();
        var result = new CaptureSummary
        {
            scene = _scene,
            utc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            operatingSystem = SystemInfo.operatingSystem,
            processor = SystemInfo.processorType,
            graphicsDevice = SystemInfo.graphicsDeviceName,
            graphicsApi = SystemInfo.graphicsDeviceVersion,
            graphicsMemoryMB = SystemInfo.graphicsMemorySize,
            quality = QualitySettings.names[QualitySettings.GetQualityLevel()].Trim(),
            width = Screen.width,
            height = Screen.height,
            fullscreen = Screen.fullScreen,
            renderScale = GetRenderScale(),
            requestedFrames = _frameCount,
            sampledFrames = timingSamples,
            warmupSeconds = _warmupSeconds,
            rawProfilerCapture = _rawCapture,
            rawProfilerPath = _rawCapture ? rawPath : string.Empty,
            role = _role,
            clientIndex = _clientIndex,
            expectedPlayers = _expectedPlayers,
            measuredProcess = _measuredProcess,
            networkBytesIn = _networkBytesIn,
            networkBytesOut = _networkBytesOut,
            networkMessagesIn = _networkMessagesIn,
            networkMessagesOut = _networkMessagesOut,
            networkMessageTraffic = _networkTraffic.Values
                .OrderByDescending(traffic => traffic.bytesIn + traffic.bytesOut).ToArray(),
            unavailableCounters = unavailable,
            metrics = summaries
        };

        string summaryPath = Path.Combine(_outputDirectory, "summary.json");
        File.WriteAllText(summaryPath, JsonUtility.ToJson(result, true));
        File.WriteAllText(Path.Combine(_outputDirectory, "completed.txt"), DateTime.UtcNow.ToString("O"));
        Debug.Log($"[PerfAudit] COMPLETE scene={_scene} frames={_frameCount} summary={summaryPath}");
        _driveMovement = false;
        Application.Quit(0);
    }

    private void OnMessageIn(NetworkDiagnostics.MessageInfo info)
    {
        _networkMessagesIn++;
        _networkBytesIn += info.bytes;
        NetworkMessageTraffic traffic = GetMessageTraffic(info);
        traffic.messagesIn++;
        traffic.bytesIn += info.bytes;
    }

    private void OnMessageOut(NetworkDiagnostics.MessageInfo info)
    {
        long bytes = (long)info.bytes * info.count;
        _networkMessagesOut += info.count;
        _networkBytesOut += bytes;
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

    private void OnDestroy()
    {
        NetworkDiagnostics.InMessageEvent -= OnMessageIn;
        NetworkDiagnostics.OutMessageEvent -= OnMessageOut;
        SceneManager.sceneLoaded -= ConfigureInitialNetworkManager;
    }

    private void ResolveMovementControls()
    {
        if (NetworkClient.localPlayer == null) return;
        PlayerScript player = NetworkClient.localPlayer.GetComponent<PlayerScript>();
        if (player == null) return;
        FieldInfo field = typeof(PlayerScript).GetField("PlayerControlsSO", BindingFlags.Instance | BindingFlags.NonPublic);
        _controls = field?.GetValue(player) as PlayerControlsSO;
    }

    private IEnumerator WaitForCondition(Func<bool> condition, float timeoutSeconds, string error)
    {
        float deadline = Time.realtimeSinceStartup + timeoutSeconds;
        while (!condition() && Time.realtimeSinceStartup < deadline)
            yield return null;
        if (!condition()) Fail(error);
    }

    private void Fail(string message)
    {
        if (!string.IsNullOrEmpty(_failure)) return;
        _failure = message;
        Debug.LogError($"[PerfAudit] FAILED scene={_scene}: {message}");
        try
        {
            Directory.CreateDirectory(_outputDirectory);
            File.WriteAllText(Path.Combine(_outputDirectory, "failed.txt"), message);
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        Application.Quit(2);
    }

    private static float GetRenderScale()
    {
        var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        return asset != null ? asset.renderScale : 1f;
    }

    private static void ApplyRequestedTestSettings()
    {
        string qualityName = GetOptionalArgument("--performance-audit-quality", "Ultra");
        int qualityIndex = Array.FindIndex(QualitySettings.names,
            name => string.Equals(name.Trim(), qualityName.Trim(), StringComparison.OrdinalIgnoreCase));
        if (qualityIndex < 0)
            throw new ArgumentException($"Unknown quality level '{qualityName}'. Available: {string.Join(", ", QualitySettings.names)}");
        QualitySettings.SetQualityLevel(qualityIndex, true);

        int width = GetIntArgument("--performance-audit-width", 1920, 640, 7680);
        int height = GetIntArgument("--performance-audit-height", 1080, 360, 4320);
        bool fullscreen = GetBoolArgument("--performance-audit-fullscreen", true);
        Screen.SetResolution(width, height, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);

        float renderScale = GetFloatArgument("--performance-audit-render-scale", 1f, 0.1f, 2f);
        var asset = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline as UnityEngine.Rendering.Universal.UniversalRenderPipelineAsset;
        if (asset != null) asset.renderScale = renderScale;
    }

    private static string GetRequiredArgument(string name)
    {
        if (TryGetArgument(name, out string value) && !string.IsNullOrWhiteSpace(value)) return value;
        throw new ArgumentException($"Missing required argument {name}");
    }

    private static string GetOptionalArgument(string name, string fallback)
    {
        return TryGetArgument(name, out string value) && !string.IsNullOrWhiteSpace(value) ? value : fallback;
    }

    private static int GetIntArgument(string name, int fallback, int minimum, int maximum)
    {
        return TryGetArgument(name, out string value) && int.TryParse(value, out int parsed)
            ? Mathf.Clamp(parsed, minimum, maximum)
            : fallback;
    }

    private static float GetFloatArgument(string name, float fallback, float minimum, float maximum)
    {
        return TryGetArgument(name, out string value) && float.TryParse(value, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out float parsed)
            ? Mathf.Clamp(parsed, minimum, maximum)
            : fallback;
    }

    private static bool GetBoolArgument(string name, bool fallback)
    {
        return TryGetArgument(name, out string value) && bool.TryParse(value, out bool parsed) ? parsed : fallback;
    }

    private static bool TryGetArgument(string name, out string value)
    {
        string prefix = name + "=";
        foreach (string argument in Environment.GetCommandLineArgs())
        {
            if (argument.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
            {
                value = argument.Substring(prefix.Length).Trim('"');
                return true;
            }
            if (string.Equals(argument, name, StringComparison.OrdinalIgnoreCase))
            {
                value = "true";
                return true;
            }
        }
        value = null;
        return false;
    }
}
#endif
