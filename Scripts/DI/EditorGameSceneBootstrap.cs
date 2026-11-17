using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class EditorGameSceneBootstrap
{
    private const string BootstrapPrefabPath = "Configs/GameBootstrap";
    private const int MenuSceneBuildIndex = 0;

    public enum StartMode { NewGame, Continue, Empty }

#if UNITY_EDITOR
    public static StartMode Mode
    {
        get => (StartMode)EditorPrefs.GetInt("EditorGameSceneBootstrap.Mode", (int)StartMode.Continue);
        set => EditorPrefs.SetInt("EditorGameSceneBootstrap.Mode", (int)value);
    }
#else
    public static StartMode Mode = StartMode.Continue;
#endif

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Init()
    {
        var active = SceneManager.GetActiveScene();
        if (active.buildIndex == MenuSceneBuildIndex) return;
        if (GameLifetimeScope.Current != null) return;

        ApplyStartMode();
        LoadBootstrap();
    }

    private static void ApplyStartMode()
    {
        PlayerPrefs.DeleteKey("NewGameStart");
        PlayerPrefs.DeleteKey("Continue");

        GamePaths.CurrentProfile = ResolveProfile();

        switch (Mode)
        {
            case StartMode.NewGame:
                PlayerPrefs.SetInt("NewGameStart", 1);
                break;
            case StartMode.Continue:
                PlayerPrefs.SetInt("Continue", 1);
                break;
            case StartMode.Empty:
                break;
        }
        PlayerPrefs.Save();

        Logger.Log(LogModule.Core,
            $"[Bootstrap] mode={Mode}, profile={GamePaths.CurrentProfile}, " +
            $"NewGameStart={PlayerPrefs.GetInt("NewGameStart", 0)}, " +
            $"Continue={PlayerPrefs.GetInt("Continue", 0)}");
    }

    private static string ResolveProfile()
    {
        string last = PlayerPrefs.GetString("LastProfile", "");
        if (!string.IsNullOrEmpty(last))
        {
            string path = Path.Combine(GamePaths.SaveRoot, last,
                GamePaths.GameSaveSubFolder, GamePaths.GameSaveFileName);
            if (File.Exists(path)) return last;
        }

        var profiles = GamePaths.GetAllProfiles(onlyWithSave: true);
        if (profiles != null && profiles.Count > 0) return profiles[0];

        return "Finding_Oneself";
    }

    private static void LoadBootstrap()
    {
        var prefab = Resources.Load<GameObject>(BootstrapPrefabPath);
        if (prefab == null)
        {
            Logger.LogError(LogModule.Core,
                $"Не найден Resources/{BootstrapPrefabPath}.prefab. " +
                $"Запусти через меню или создай bootstrap-префаб.");
            return;
        }

        var instance = Object.Instantiate(prefab);
        Object.DontDestroyOnLoad(instance);
    }
}