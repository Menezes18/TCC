using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

[InitializeOnLoad]
public static class RefactorRegressionRunner
{
    private const string RequestPath = "Temp/run-refactor-regressions";
    public const string ResultPath = "Temp/refactor-regressions.xml";
    private static double nextRequestCheck;

    static RefactorRegressionRunner() => EditorApplication.update += CheckRequest;

    private static void CheckRequest()
    {
        if (EditorApplication.timeSinceStartup < nextRequestCheck) return;
        nextRequestCheck = EditorApplication.timeSinceStartup + 1;
        if (EditorApplication.isCompiling || EditorApplication.isUpdating || EditorApplication.isPlayingOrWillChangePlaymode)
            return;
        if (!File.Exists(RequestPath)) return;
        File.Delete(RequestPath);
        Run();
    }

    [MenuItem("Tools/TCC/Run Refactor Regression Tests")]
    public static void Run()
    {
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        var callbacks = new ResultWriter();
        api.RegisterCallbacks(callbacks);
        try
        {
            api.Execute(new ExecutionSettings(new Filter
            {
                testMode = TestMode.EditMode,
                groupNames = new[] { "^RefactorRegressionTests\\." }
            }) { runSynchronously = true });
        }
        finally
        {
            api.UnregisterCallbacks(callbacks);
            Object.DestroyImmediate(api);
        }
    }

    private sealed class ResultWriter : ICallbacks
    {
        public void RunStarted(ITestAdaptor tests) { }
        public void TestStarted(ITestAdaptor test) { }
        public void TestFinished(ITestResultAdaptor result) { }
        public void RunFinished(ITestResultAdaptor result)
        {
            TestRunnerApi.SaveResultToFile(result, ResultPath);
            Debug.Log($"[Refactor tests] Results saved to {ResultPath}");
        }
    }
}
