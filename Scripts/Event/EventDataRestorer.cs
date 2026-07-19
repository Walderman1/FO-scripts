// EventDataRestorer.cs - С УЧЕТОМ НАСТРОЕК АВТОСОХРАНЕНИЯ
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

        // ✅ ПРОВЕРЯЕМ, ВКЛЮЧЕНО ЛИ АВТО-ВОССТАНОВЛЕНИЕ
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
                    Debug.Log($"🔄 Auto-restored {restored} events from JSON");
                    if (hasBackups)
                    {
                        Debug.Log($"📦 Backups available in: {backupPath}");
                    }
                }
            }
            else
            {
                int restored = dataManager.RestoreEventsToSO();
                if (restored > 0)
                {
                    Debug.Log($"🔄 Auto-restored {restored} events from JSON");
                    if (hasBackups)
                    {
                        Debug.Log($"📦 Backups available in: {backupPath}");
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ Error restoring events: {e.Message}");
        }
        finally
        {
            isRestoring = false;
        }
    }

    // ============ МЕНЮ ============

    [MenuItem("Tools/Event System/🔄 Restore Events to SO (Auto)")]
    public static void RestoreEventsMenu()
    {
        isRestoring = false;
        hasRestoredAfterCompile = true;
        RestoreEvents();
    }

    [MenuItem("Tools/Event System/🔄 Restore Events to SO (Force)")]
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
                $"✅ Restored {restored} events to SO files!\n\nCheck Console for details.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
        }
    }

    [MenuItem("Tools/Event System/⚙️ Toggle Auto-Restore on Compile")]
    public static void ToggleAutoRestore()
    {
        bool isEnabled = EditorPrefs.GetBool("EventDataAutoRestore", true);
        EditorPrefs.SetBool("EventDataAutoRestore", !isEnabled);
        Debug.Log($"🔄 Auto-restore on compile: {(!isEnabled ? "ENABLED" : "DISABLED")}");

        EditorUtility.DisplayDialog("Event System",
            $"Auto-restore is now: {(isEnabled ? "DISABLED" : "ENABLED")}\n\n" +
            "This setting is saved globally in Unity Editor preferences.", "OK");
    }
}
#endif