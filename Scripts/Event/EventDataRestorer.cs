#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

[InitializeOnLoad]
public static class EventDataRestorer
{
    private static bool isRestoring = false;
    private static bool hasRestoredAfterCompile = false;
    private static float checkDelay = 3f;
    private static float lastCheckTime = 0f;

    static EventDataRestorer()
    {
        EditorApplication.delayCall += OnEditorReady;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnEditorReady()
    {
        if (!isRestoring && !hasRestoredAfterCompile)
        {
            RestoreEvents();
            hasRestoredAfterCompile = true;
        }
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingPlayMode)
        {
            hasRestoredAfterCompile = false;
            RestoreEvents();
        }

        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            hasRestoredAfterCompile = true;
        }
    }

    private static void OnEditorUpdate()
    {
        if (EditorApplication.isCompiling)
        {
            hasRestoredAfterCompile = false;
            lastCheckTime = 0f;
            return;
        }

        if (!isRestoring && !hasRestoredAfterCompile)
        {
            if (lastCheckTime == 0f)
            {
                lastCheckTime = (float)EditorApplication.timeSinceStartup;
                return;
            }

            if ((float)EditorApplication.timeSinceStartup - lastCheckTime >= checkDelay)
            {
                RestoreEvents();
                hasRestoredAfterCompile = true;
                lastCheckTime = 0f;
            }
        }
    }

    private static void RestoreEvents()
    {
        if (isRestoring) return;

        if (!EditorPrefs.GetBool("EventDataAutoRestore", true))
        {
            return;
        }

        isRestoring = true;

        try
        {
            string eventsFolder = Path.Combine(Application.dataPath, "EventsData");
            if (!Directory.Exists(eventsFolder))
            {
                isRestoring = false;
                return;
            }

            string[] jsonFiles = Directory.GetFiles(eventsFolder, "*.json");
            if (jsonFiles.Length == 0)
            {
                isRestoring = false;
                return;
            }

            string backupPath = Path.Combine(eventsFolder, "Backups");
            bool hasBackups = Directory.Exists(backupPath) && Directory.GetFiles(backupPath, "*.json").Length > 0;

            var dataManager = Object.FindObjectOfType<EventDataManager>();
            if (dataManager == null)
            {
                GameObject tempGO = new GameObject("TempEventDataManager");
                dataManager = tempGO.AddComponent<EventDataManager>();
                dataManager.Initialize();

                int restored = dataManager.RestoreEventsToSO();
                Object.DestroyImmediate(tempGO);

                if (restored > 0)
                {
                    Logger.Log(LogModule.Event, $"Автоматически восстановлено {restored} событий из JSON");
                    if (hasBackups)
                    {
                        Logger.Log(LogModule.Event, $"Резервные копии доступны в: {backupPath}");
                    }
                }
            }
            else
            {
                int restored = dataManager.RestoreEventsToSO();
                if (restored > 0)
                {
                    Logger.Log(LogModule.Event, $"Автоматически восстановлено {restored} событий из JSON");
                    if (hasBackups)
                    {
                        Logger.Log(LogModule.Event, $"Резервные копии доступны в: {backupPath}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.Event, $"Ошибка восстановления событий: {e.Message}");
        }
        finally
        {
            isRestoring = false;
        }
    }

    [MenuItem("Tools/Event System/Восстановить события в SO (Авто)")]
    public static void RestoreEventsMenu()
    {
        isRestoring = false;
        hasRestoredAfterCompile = true;
        RestoreEvents();
    }

    [MenuItem("Tools/Event System/Восстановить события в SO (Принудительно)")]
    public static void ForceRestoreEvents()
    {
        isRestoring = false;
        hasRestoredAfterCompile = true;

        var dataManager = Object.FindObjectOfType<EventDataManager>();
        if (dataManager != null)
        {
            dataManager.Initialize();
            int restored = dataManager.RestoreEventsToSO();
            EditorUtility.DisplayDialog("Event System",
                $"Восстановлено {restored} событий в SO файлы\n\nПроверьте консоль для деталей.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
        }
    }

    [MenuItem("Tools/Event System/Переключить авто-восстановление при компиляции")]
    public static void ToggleAutoRestore()
    {
        bool isEnabled = EditorPrefs.GetBool("EventDataAutoRestore", true);
        EditorPrefs.SetBool("EventDataAutoRestore", !isEnabled);
        Logger.Log(LogModule.Event, $"Авто-восстановление при компиляции: {(isEnabled ? "ВЫКЛЮЧЕНО" : "ВКЛЮЧЕНО")}");

        EditorUtility.DisplayDialog("Event System",
            $"Авто-восстановление теперь: {(isEnabled ? "ВЫКЛЮЧЕНО" : "ВКЛЮЧЕНО")}\n\n" +
            "Эта настройка сохраняется глобально в настройках редактора Unity.", "OK");
    }
}
#endif
