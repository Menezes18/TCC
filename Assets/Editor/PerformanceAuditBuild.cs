using System;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Debug = UnityEngine.Debug;

public static class PerformanceAuditBuild
{
    [Serializable]
    private sealed class BenchmarkBuildInfo
    {
        public string schema = "tcc-performance-package/v1";
        public string packageVersion;
        public string gitCommit;
        public bool workingTreeDirty;
        public string unityVersion;
        public string builtUtc;
        public string releaseBuildIdentifier;
        public string diagnosticBuildIdentifier;
    }

    private const string PackageVersion = "1.0.0";
    private const string BuildRelativePath = "Builds/PerformanceAuditPlayer/TCC.exe";
    private const string RunnerRelativePath = "Tools/PerformanceAudit/Run-PerformanceAudit.ps1";
    private const string PortableOutputRelativePath = "Builds/PerformanceAuditPortable/TCC-Performance-Benchmark.zip";
    private const string CompletePackageRelativePath = "Builds/PerformanceBenchmark/PerformanceBenchmark";
    private const string CompletePackageZipRelativePath = "Builds/PerformanceBenchmark/TCC-PerformanceBenchmark.zip";

    [MenuItem("Tools/Performance Audit/Build Development Player")]
    public static void BuildFromMenu()
    {
        string path = Path.GetFullPath(BuildRelativePath);
        Build(path);
        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Performance Audit/Run Core Scenes (raw captures)")]
    public static void RunCoreScenes()
    {
        BuildAndLaunch("MN_Queda,MN_Run,MN_new_Rua", true);
    }

    [MenuItem("Tools/Performance Audit/Run All 7 Scenes (summaries)")]
    public static void RunAllScenes()
    {
        BuildAndLaunch("RASCUNHO,MN_new_Rua,MN_Run,MN_Memoria,MN_BatataQ,MN_Queda,MN_Sumo", false);
    }

    [MenuItem("Tools/Performance Audit/Create Portable Benchmark ZIP")]
    public static void CreatePortableBenchmark()
    {
        string projectRoot = Path.GetFullPath(".");
        string staging = Path.Combine(projectRoot, "Temp", "PerformanceAuditPortable", "Staging");
        string playerPath = Path.Combine(staging, "Player", "TCC.exe");
        string outputPath = Path.Combine(projectRoot, PortableOutputRelativePath.Replace('/', Path.DirectorySeparatorChar));

        string controlledRoot = Path.Combine(projectRoot, "Temp", "PerformanceAuditPortable") + Path.DirectorySeparatorChar;
        if (!staging.StartsWith(controlledRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Portable staging path escaped the project Temp directory.");
        if (Directory.Exists(staging)) Directory.Delete(staging, true);
        Directory.CreateDirectory(staging);

        Build(playerPath);
        CopyDirectory(Path.Combine(projectRoot, "Tools", "PerformanceAudit"), Path.Combine(staging, "Tools", "PerformanceAudit"));
        CopyDirectory(Path.Combine(projectRoot, "Tools", "PerformanceAudit", "Portable"), staging);

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        if (File.Exists(outputPath)) File.Delete(outputPath);
        ZipFile.CreateFromDirectory(staging, outputPath, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"[PerfAudit] Portable benchmark created: {outputPath}");
        EditorUtility.RevealInFinder(outputPath);
    }

    [MenuItem("Tools/Performance Benchmark/Build Complete Portable Package")]
    public static void BuildCompletePortablePackage()
    {
        string packageRoot = Path.GetFullPath(CompletePackageRelativePath);
        string controlledRoot = Path.GetFullPath("Builds/PerformanceBenchmark") + Path.DirectorySeparatorChar;
        if (!packageRoot.StartsWith(controlledRoot, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Benchmark package path escaped Builds/PerformanceBenchmark.");

        if (Directory.Exists(packageRoot)) Directory.Delete(packageRoot, true);
        Directory.CreateDirectory(packageRoot);
        Directory.CreateDirectory(Path.Combine(packageRoot, "Results"));

        string releasePath = Path.Combine(packageRoot, "Builds", "Release", "TCC.exe");
        string diagnosticPath = Path.Combine(packageRoot, "Builds", "Diagnostic", "TCC.exe");
        Build(releasePath, false, out string releaseId);
        Build(diagnosticPath, true, out string diagnosticId);

        string scriptsSource = Path.GetFullPath("Tools/PerformanceBenchmark");
        CopyDirectory(scriptsSource, packageRoot);

        var info = new BenchmarkBuildInfo
        {
            packageVersion = PackageVersion,
            gitCommit = GetGitCommit(),
            workingTreeDirty = GetGitStatusDirty(),
            unityVersion = Application.unityVersion,
            builtUtc = DateTime.UtcNow.ToString("O"),
            releaseBuildIdentifier = releaseId,
            diagnosticBuildIdentifier = diagnosticId
        };
        File.WriteAllText(Path.Combine(packageRoot, "package-info.json"), JsonUtility.ToJson(info, true));

        string zipPath = Path.GetFullPath(CompletePackageZipRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(zipPath));
        if (File.Exists(zipPath)) File.Delete(zipPath);
        ZipFile.CreateFromDirectory(packageRoot, zipPath, System.IO.Compression.CompressionLevel.Optimal, false);
        Debug.Log($"[PerfBenchmark] Complete portable package: {packageRoot}");
        Debug.Log($"[PerfBenchmark] ZIP: {zipPath}");
        if (!Application.isBatchMode) EditorUtility.RevealInFinder(packageRoot);
    }

    public static void BuildCompletePortablePackageFromCommandLine()
    {
        BuildCompletePortablePackage();
    }

    public static void BuildFromCommandLine()
    {
        string output = GetCommandLineValue("-performanceAuditBuildPath");
        if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath(BuildRelativePath);
        Build(Path.GetFullPath(output));
    }

    public static string BuildDevelopmentPlayer(string outputPath)
    {
        return Build(outputPath, true);
    }

    private static string Build(string outputPath, bool development = true)
    {
        return Build(outputPath, development, out _);
    }

    private static string Build(string outputPath, bool development, out string buildIdentifier)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes exist in Build Settings.");

        string projectSettingsPath = Path.GetFullPath("ProjectSettings/ProjectSettings.asset");
        byte[] projectSettingsBefore = File.ReadAllBytes(projectSettingsPath);
        bool previousFrameTiming = PlayerSettings.enableFrameTimingStats;
        try
        {
            PlayerSettings.enableFrameTimingStats = true;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = development ? BuildOptions.Development : BuildOptions.None,
                extraScriptingDefines = new[] { "TCC_PERFORMANCE_BENCHMARK" }
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Performance Player build failed: {report.summary.result}");
            string lane = development ? "Diagnostic Development" : "Release-like";
            buildIdentifier = report.summary.guid.ToString();
            Debug.Log($"[PerfAudit] {lane} Player built at {outputPath} in {report.summary.totalTime}; id={buildIdentifier}.");
            return outputPath;
        }
        finally
        {
            PlayerSettings.enableFrameTimingStats = previousFrameTiming;
            // PlayerSettings changes are serialized immediately by some Editor
            // versions. Restore the exact pre-build file so validation does not
            // leave an unrelated ProjectSettings diff behind.
            if (!File.ReadAllBytes(projectSettingsPath).SequenceEqual(projectSettingsBefore))
                File.WriteAllBytes(projectSettingsPath, projectSettingsBefore);
        }
    }

    private static string GetGitCommit()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git.exe",
                Arguments = "rev-parse HEAD",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            string output = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit(5000);
            return process.ExitCode == 0 ? output : "unavailable";
        }
        catch
        {
            return "unavailable";
        }
    }

    private static bool GetGitStatusDirty()
    {
        try
        {
            var process = Process.Start(new ProcessStartInfo
            {
                FileName = "git.exe",
                Arguments = "status --porcelain",
                WorkingDirectory = Path.GetFullPath("."),
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true
            });
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit(5000);
            return process.ExitCode == 0 && !string.IsNullOrWhiteSpace(output);
        }
        catch
        {
            return false;
        }
    }

    private static void BuildAndLaunch(string scenes, bool rawCapture)
    {
        Build(Path.GetFullPath(BuildRelativePath));
        string runner = Path.GetFullPath(RunnerRelativePath);
        string arguments = $"-NoProfile -ExecutionPolicy Bypass -File \"{runner}\" -Scenes \"{scenes}\" -SkipBuild -CompareWithPrevious -PauseAtEnd";
        if (rawCapture) arguments += " -RawCapture";
        Process.Start(new ProcessStartInfo
        {
            FileName = "powershell.exe",
            Arguments = arguments,
            WorkingDirectory = Path.GetFullPath("."),
            UseShellExecute = true
        });
    }

    private static string GetCommandLineValue(string name)
    {
        string[] args = Environment.GetCommandLineArgs();
        for (int index = 0; index < args.Length - 1; index++)
            if (string.Equals(args[index], name, StringComparison.OrdinalIgnoreCase))
                return args[index + 1];
        return null;
    }

    private static void CopyDirectory(string source, string destination)
    {
        Directory.CreateDirectory(destination);
        foreach (string file in Directory.GetFiles(source))
            File.Copy(file, Path.Combine(destination, Path.GetFileName(file)), true);
        foreach (string directory in Directory.GetDirectories(source))
            CopyDirectory(directory, Path.Combine(destination, Path.GetFileName(directory)));
    }
}
