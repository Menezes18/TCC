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
    private const string BuildRelativePath = "Builds/PerformanceAuditPlayer/TCC.exe";
    private const string RunnerRelativePath = "Tools/PerformanceAudit/Run-PerformanceAudit.ps1";
    private const string PortableOutputRelativePath = "Builds/PerformanceAuditPortable/TCC-Performance-Benchmark.zip";

    [MenuItem("Tools/Performance Audit/Build Development Player")]
    public static void BuildFromMenu()
    {
        string path = Build(Path.GetFullPath(BuildRelativePath));
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

    public static void BuildFromCommandLine()
    {
        string output = GetCommandLineValue("-performanceAuditBuildPath");
        if (string.IsNullOrWhiteSpace(output)) output = Path.GetFullPath(BuildRelativePath);
        Build(Path.GetFullPath(output));
    }

    private static string Build(string outputPath)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        string[] scenes = EditorBuildSettings.scenes.Where(scene => scene.enabled).Select(scene => scene.path).ToArray();
        if (scenes.Length == 0) throw new InvalidOperationException("No enabled scenes exist in Build Settings.");

        bool previousFrameTiming = PlayerSettings.enableFrameTimingStats;
        try
        {
            PlayerSettings.enableFrameTimingStats = true;
            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.Development
            };
            BuildReport report = BuildPipeline.BuildPlayer(options);
            if (report.summary.result != BuildResult.Succeeded)
                throw new InvalidOperationException($"Performance Player build failed: {report.summary.result}");
            Debug.Log($"[PerfAudit] Development Player built at {outputPath} in {report.summary.totalTime}.");
            return outputPath;
        }
        finally
        {
            PlayerSettings.enableFrameTimingStats = previousFrameTiming;
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
