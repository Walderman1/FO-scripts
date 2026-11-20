#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

public static class TestRunnerGlobalBridge
{
    public struct ResultData
    {
        public TestStatus Status;
        public double Duration;
        public string Message;
    }

    private static TestRunnerApi _api;
    private static Handler _handler;
    private static bool _initialized;

    private static readonly Dictionary<string, ResultData> _results = new Dictionary<string, ResultData>();
    private static int _passed;
    private static int _failed;
    private static bool _running;

    public static IReadOnlyDictionary<string, ResultData> Results => _results;
    public static int Passed => _passed;
    public static int Failed => _failed;
    public static bool IsRunning => _running;

    public static event System.Action StateChanged;

    private static void EnsureInitialized()
    {
        if (_initialized) return;
        _initialized = true;

        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        CreateApi();
    }

    private static void CreateApi()
    {
        DestroyApi();

        _handler = new Handler();
        _api = ScriptableObject.CreateInstance<TestRunnerApi>();
        _api.hideFlags = HideFlags.DontUnloadUnusedAsset;
        _api.RegisterCallbacks(_handler);
    }

    private static void DestroyApi()
    {
        if (_api != null && _handler != null)
        {
            try { _api.UnregisterCallbacks(_handler); } catch { }
        }

        if (_api != null)
        {
            try { Object.DestroyImmediate(_api); } catch { }
        }

        _api = null;
        _handler = null;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change)
    {
        if (change == PlayModeStateChange.EnteredEditMode)
        {
            ResetAll();
        }
    }

    public static void ResetAll()
    {
        _running = false;
        _passed = 0;
        _failed = 0;
        _results.Clear();
        StateChanged?.Invoke();
    }

    public static bool Execute(ExecutionSettings settings)
    {
        EnsureInitialized();

        if (_running)
        {
            Debug.LogWarning("[TestRunnerGlobalBridge] Тесты уже выполняются.");
            return false;
        }

        _running = true;
        _passed = 0;
        _failed = 0;
        _results.Clear();
        StateChanged?.Invoke();

        try
        {
            _api.Execute(settings);
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[TestRunnerGlobalBridge] Execute: {e.Message}");
            _running = false;
            StateChanged?.Invoke();
            return false;
        }
    }

    public static void RetrieveTestList(TestMode mode, System.Action<ITestAdaptor> callback)
    {
        EnsureInitialized();
        var api = ScriptableObject.CreateInstance<TestRunnerApi>();
        api.hideFlags = HideFlags.DontUnloadUnusedAsset;
        api.RetrieveTestList(mode, callback);
    }

    private class Handler : ICallbacks
    {
        public void RunStarted(ITestAdaptor testsToRun)
        {
            _running = true;
            StateChanged?.Invoke();
        }

        public void RunFinished(ITestResultAdaptor result)
        {
            _running = false;
            StateChanged?.Invoke();
        }

        public void TestStarted(ITestAdaptor test) { }

        public void TestFinished(ITestResultAdaptor result)
        {
            if (result == null || result.HasChildren) return;

            _results[result.Test.FullName] = new ResultData
            {
                Status = result.TestStatus,
                Duration = result.Duration,
                Message = result.Message
            };

            if (result.TestStatus == TestStatus.Passed) _passed++;
            else if (result.TestStatus == TestStatus.Failed) _failed++;

            StateChanged?.Invoke();
        }
    }
}
#endif