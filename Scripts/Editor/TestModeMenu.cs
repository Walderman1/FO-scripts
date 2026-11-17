#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class TestModeMenu
{
    [MenuItem("Tools/Test/Mode/Continue (загрузить сейв)")]
    static void SetContinue() => SetMode(EditorGameSceneBootstrap.StartMode.Continue);

    [MenuItem("Tools/Test/Mode/New Game")]
    static void SetNewGame() => SetMode(EditorGameSceneBootstrap.StartMode.NewGame);

    [MenuItem("Tools/Test/Mode/Empty (ничего)")]
    static void SetEmpty() => SetMode(EditorGameSceneBootstrap.StartMode.Empty);

    static void SetMode(EditorGameSceneBootstrap.StartMode mode)
    {
        EditorGameSceneBootstrap.Mode = mode;
        Logger.Log(LogModule.Editor, $"Режим запуска игровой сцены: {mode}");
    }
}
#endif