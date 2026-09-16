using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;
using Debug = UnityEngine.Debug;
using Object = UnityEngine.Object;

/// <summary>
/// Editor half of the project validation pipeline. A PowerShell runner writes an
/// opt-in request into Temp so the already-open Editor can compile, run tests,
/// validate scenes, and build without a second Unity instance touching Library.
/// </summary>
[InitializeOnLoad]
public static class ProjectValidationRunner
{
    private const string RequestPath = "Temp/project-validation-request.json";
    private const string MenuScript = "Tools/ProjectValidation/Run-ProjectValidation.ps1";
    private static double _nextRequestCheck;
    private static bool _running;

    [Serializable]
    private sealed class Request
    {
        public string resultDirectory;
        public string buildPath;
        public bool buildPlayer = true;
    }

    [Serializable]
    private sealed class Step
    {
        public string name;
        public string status;
        public double durationSeconds;
        public string detail;
        public string artifact;
    }

    [Serializable]
    private sealed class Summary
    {
        public string schema = "tcc-project-validation-editor/v1";
        public string utc;
        public string unityVersion;
        public bool succeeded;
        public Step[] steps;
    }

    static ProjectValidationRunner()
    {
        EditorApplication.update += CheckForRequest;
    }

    [MenuItem("Tools/TCC/Run Project Validation")]
    public static void RunFromMenu()
    {
        string script = Path.GetFullPath(MenuScript);
        if (!File.Exists(script))
        {
            Debug.LogError($"[ProjectValidation] Runner not found: {script}");
            return;
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{script}\" -Profile PreRelease -PauseAtEnd",
            WorkingDirectory = Path.GetFullPath("."),
            UseShellExecute = true
        });
    }

    private static void CheckForRequest()
    {
        // Batch mode invokes RunFromCommandLine explicitly. Consuming the
        // request during InitializeOnLoad would delete it before executeMethod.
        if (Application.isBatchMode) return;
        if (_running || EditorApplication.timeSinceStartup < _nextRequestCheck) return;
        _nextRequestCheck = EditorApplication.timeSinceStartup + 1d;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode) return;
        string requestPath = Path.GetFullPath(RequestPath);
        if (!File.Exists(requestPath)) return;

        // A request can arrive in the short window after a source file changes
        // on disk but before Unity starts its automatic import. Import first and
        // leave the request in place if that triggers a domain reload.
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        if (EditorApplication.isCompiling || EditorApplication.isUpdating) return;

        Request request;
        try
        {
            request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            return;
        }

        _running = true;
        try
        {
            Run(request);
            File.Delete(requestPath);
        }
        finally
        {
            _running = false;
        }
    }

    /// <summary>Batch-mode entry point used when the project is not open.</summary>
    public static void RunFromCommandLine()
    {
        string requestPath = GetCommandLineValue("-projectValidationRequest");
        if (string.IsNullOrWhiteSpace(requestPath) || !File.Exists(requestPath))
            throw new ArgumentException("Pass an existing -projectValidationRequest JSON file.");

        Request request = JsonUtility.FromJson<Request>(File.ReadAllText(requestPath));
        Run(request);
    }

    private static void Run(Request request)
    {
        if (request == null || string.IsNullOrWhiteSpace(request.resultDirectory))
            throw new ArgumentException("Project validation request has no resultDirectory.");

        string resultDirectory = Path.GetFullPath(request.resultDirectory);
        Directory.CreateDirectory(resultDirectory);
        var steps = new List<Step>();
        string[] enabledScenePaths = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        var sceneSnapshots = enabledScenePaths.ToDictionary(path => path, File.ReadAllBytes);

        try
        {
            AddStep(steps, "compile-and-weave", () =>
            {
                if (EditorUtility.scriptCompilationFailed)
                    throw new InvalidOperationException("Unity reports failed script compilation.");
                return "Editor assemblies loaded successfully; the Development Player stage separately exercises player compilation and Mirror weaving when enabled.";
            });

            AddStep(steps, "editmode-tests", () => RunEditModeTests(resultDirectory), Path.Combine(resultDirectory, "editmode-tests.xml"));
            AddStep(steps, "scene-validation", () => ValidateBuildScenes(resultDirectory), Path.Combine(resultDirectory, "scene-validation.json"));

            if (request.buildPlayer)
            {
                AddStep(steps, "windows-development-build", () =>
                {
                    if (string.IsNullOrWhiteSpace(request.buildPath))
                        throw new ArgumentException("Project validation request has no buildPath.");
                    string player = PerformanceAuditBuild.BuildDevelopmentPlayer(Path.GetFullPath(request.buildPath));
                    return $"Built {player}";
                }, request.buildPath);
            }
            else
            {
                steps.Add(new Step { name = "windows-development-build", status = "skipped", detail = "Disabled by validation profile." });
            }
        }
        finally
        {
            // Opening/building scenes may serialize new default fields even when
            // validation is intended to be read-only. Restore the exact on-disk
            // state captured before the synchronous run; the Editor is blocked
            // during these stages, so no concurrent scene edit can be lost.
            foreach (KeyValuePair<string, byte[]> snapshot in sceneSnapshots)
                if (!File.ReadAllBytes(snapshot.Key).SequenceEqual(snapshot.Value))
                    File.WriteAllBytes(snapshot.Key, snapshot.Value);
        }

        var summary = new Summary
        {
            utc = DateTime.UtcNow.ToString("O"),
            unityVersion = Application.unityVersion,
            succeeded = steps.All(step => step.status == "passed" || step.status == "skipped"),
            steps = steps.ToArray()
        };
        string summaryPath = Path.Combine(resultDirectory, "editor-summary.json");
        File.WriteAllText(summaryPath, JsonUtility.ToJson(summary, true));
        File.WriteAllText(Path.Combine(resultDirectory, "editor-completed.txt"), DateTime.UtcNow.ToString("O"));
        Debug.Log($"[ProjectValidation] Editor stages complete: {summaryPath}");
    }

    private static void AddStep(ICollection<Step> steps, string name, Func<string> action, string artifact = null)
    {
        var timer = Stopwatch.StartNew();
        var step = new Step { name = name, artifact = artifact ?? string.Empty };
        try
        {
            step.detail = action();
            step.status = "passed";
        }
        catch (Exception exception)
        {
            step.status = "failed";
            step.detail = exception.ToString();
            Debug.LogException(exception);
        }
        finally
        {
            timer.Stop();
            step.durationSeconds = timer.Elapsed.TotalSeconds;
            steps.Add(step);
        }
    }

    private static string RunEditModeTests(string resultDirectory)
    {
        string resultPath = Path.Combine(resultDirectory, "editmode-tests.xml");
        string[] generatedPerformanceFiles =
        {
            "Assets/Resources/PerformanceTestRunInfo.json",
            "Assets/Resources/PerformanceTestRunSettings.json"
        };
        var generatedSnapshots = generatedPerformanceFiles.ToDictionary(path => path, path => File.Exists(path) ? File.ReadAllBytes(path) : null);
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callback = new TestResultWriter(resultPath);
        api.RegisterCallbacks(callback);
        try
        {
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.EditMode }) { runSynchronously = true });
        }
        finally
        {
            api.UnregisterCallbacks(callback);
            Object.DestroyImmediate(api);
            foreach (KeyValuePair<string, byte[]> snapshot in generatedSnapshots)
            {
                if (snapshot.Value != null) File.WriteAllBytes(snapshot.Key, snapshot.Value);
                else
                {
                    if (File.Exists(snapshot.Key)) File.Delete(snapshot.Key);
                    if (File.Exists(snapshot.Key + ".meta")) File.Delete(snapshot.Key + ".meta");
                }
            }
        }

        if (!callback.Finished) throw new InvalidOperationException("EditMode test runner did not produce a final result.");
        if (callback.FailCount > 0) throw new InvalidOperationException($"{callback.FailCount} EditMode test(s) failed. See {resultPath}");
        return $"{callback.PassCount} passed, {callback.SkipCount} skipped, {callback.InconclusiveCount} inconclusive.";
    }

    private static string ValidateBuildScenes(string resultDirectory)
    {
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        var records = new List<SceneRecord>();
        var failures = new List<string>();
        foreach (string scenePath in scenes)
        {
            bool passed = SceneValidator.ValidateSceneAsset(scenePath, out List<string> errors);
            records.Add(new SceneRecord { scene = scenePath, passed = passed, errors = errors.ToArray() });
            if (!passed) failures.AddRange(errors);
        }

        var report = new SceneReport
        {
            schema = "tcc-scene-validation/v1",
            utc = DateTime.UtcNow.ToString("O"),
            scenes = records.ToArray(),
            passed = failures.Count == 0
        };
        string path = Path.Combine(resultDirectory, "scene-validation.json");
        File.WriteAllText(path, JsonUtility.ToJson(report, true));
        if (failures.Count > 0) throw new InvalidOperationException(string.Join("\n", failures));
        return $"{records.Count} enabled Build Settings scene(s) passed.";
    }

    [Serializable] private sealed class SceneRecord { public string scene; public bool passed; public string[] errors; }
    [Serializable] private sealed class SceneReport { public string schema; public string utc; public bool passed; public SceneRecord[] scenes; }

    private sealed class TestResultWriter : ICallbacks
    {
        private readonly string _path;
        public bool Finished { get; private set; }
        public int PassCount { get; private set; }
        public int FailCount { get; private set; }
        public int SkipCount { get; private set; }
        public int InconclusiveCount { get; private set; }

        public TestResultWriter(string path) => _path = path;
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path));
            TestRunnerApi.SaveResultToFile(result, _path);
            PassCount = result.PassCount;
            FailCount = result.FailCount;
            SkipCount = result.SkipCount;
            InconclusiveCount = result.InconclusiveCount;
            Finished = true;
        }
    }

    private static string GetCommandLineValue(string name)
    {
        string[] arguments = Environment.GetCommandLineArgs();
        for (int index = 0; index < arguments.Length - 1; index++)
            if (string.Equals(arguments[index], name, StringComparison.OrdinalIgnoreCase)) return arguments[index + 1].Trim('"');
        return null;
    }
}
