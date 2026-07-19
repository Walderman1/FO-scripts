// EventEditorHelper.cs - ДОБАВЛЕНЫ КНОПКИ УПРАВЛЕНИЯ АВТОСОХРАНЕНИЯМИ
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class EventEditorHelper
{
    // ============ ОСНОВНЫЕ ОПЕРАЦИИ ============

    [MenuItem("Tools/Event System/💾 Save ALL Events to JSON")]
    public static void SaveAllEventsToJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventManager not found!", "OK");
            return;
        }

        eventManager.SaveAllEventsFromProject();

        string path = EventDataManager.Instance.GetEventsFolderPath();
        int fileCount = Directory.GetFiles(path, "*.json").Length;
        int backupCount = 0;

        string backupPath = Path.Combine(path, "Backups");
        if (Directory.Exists(backupPath))
        {
            backupCount = Directory.GetFiles(backupPath, "*.json").Length;
        }

        EditorUtility.DisplayDialog("Event System",
            $"✅ Saved events to JSON!\n\n" +
            $"📁 Folder: {path}\n" +
            $"📄 JSON files: {fileCount}\n" +
            $"📦 Backups: {backupCount}\n\n" +
            "Click OK to open folder.", "OK");

        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Event System/💾 Save Loaded Events to JSON")]
    public static void SaveLoadedEventsToJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventManager not found!", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Confirm Save",
            "This will save loaded events to JSON files.\n\n" +
            "⚠️ WARNING: This will OVERWRITE existing JSON files!\n\n" +
            "Backups will be created automatically.\n\n" +
            "Continue?", "Yes, Save", "Cancel");

        if (!confirm) return;

        eventManager.SaveAllEventsToJson();

        string path = EventDataManager.Instance.GetEventsFolderPath();
        EditorUtility.DisplayDialog("Event System",
            $"✅ Saved {eventManager.registeredEvents.Count} loaded events to JSON!\n\n" +
            $"📁 Folder: {path}\n\n" +
            "Backups were created before overwriting.", "OK");
    }

    [MenuItem("Tools/Event System/📂 Load and Restore to SO")]
    public static void LoadAndRestoreToSO()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        bool confirm = EditorUtility.DisplayDialog("Confirm Restore",
            "This will restore events from JSON to SO files.\n\n" +
            "⚠️ WARNING: This will OVERWRITE existing SO files!\n\n" +
            "Make sure your JSON files are correct.\n\n" +
            "Continue?", "Yes, Restore", "Cancel");

        if (!confirm) return;

        int restored = EventDataManager.Instance.RestoreEventsToSO();

        EditorUtility.DisplayDialog("Event System",
            $"✅ Restored {restored} events from JSON to SO files!\n\n" +
            "Check Console for details.\n\n" +
            "Now open any GameEvent asset to see restored actions.", "OK");
    }

    [MenuItem("Tools/Event System/📂 Load All Events from JSON")]
    public static void LoadAllEventsFromJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var events = EventDataManager.Instance.LoadAllEvents();
        EditorUtility.DisplayDialog("Event System", $"Loaded {events.Count} events from JSON!", "OK");
    }

    [MenuItem("Tools/Event System/🔄 Reload Events")]
    public static void ReloadEvents()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager != null)
        {
            eventManager.ReloadEvents();
            EditorUtility.DisplayDialog("Event System", "Events reloaded!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "EventManager not found!", "OK");
        }
    }

    [MenuItem("Tools/Event System/📂 Open Events Folder")]
    public static void OpenEventsFolder()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        string path = EventDataManager.Instance.GetEventsFolderPath();
        if (Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }
    }

    [MenuItem("Tools/Event System/📂 Open Backups Folder")]
    public static void OpenBackupsFolder()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        string path = Path.Combine(EventDataManager.Instance.GetEventsFolderPath(), "Backups");
        if (Directory.Exists(path))
        {
            EditorUtility.RevealInFinder(path);
        }
        else
        {
            Directory.CreateDirectory(path);
            EditorUtility.RevealInFinder(path);
        }
    }

    [MenuItem("Tools/Event System/🔄 Restore from Backup")]
    public static void RestoreFromBackup()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager not found in scene!\n\n" +
                "Please add EventDataManager to EventSystem GameObject.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var backups = EventDataManager.Instance.GetBackupFiles();
        if (backups.Count == 0)
        {
            EditorUtility.DisplayDialog("Event System", "No backups found!", "OK");
            return;
        }

        string[] options = new string[backups.Count + 1];
        options[0] = "=== CANCEL ===";
        for (int i = 0; i < backups.Count; i++)
        {
            options[i + 1] = Path.GetFileName(backups[i]);
        }

        int selected = EditorUtility.DisplayDialogComplex(
            "Restore from Backup",
            $"Found {backups.Count} backup files.\n\nSelect backup to restore:",
            "Cancel",
            "Restore Latest",
            "Delete All Backups"
        );

        if (selected == 1)
        {
            string latest = backups[backups.Count - 1];
            string eventID = Path.GetFileNameWithoutExtension(latest);
            int lastUnderscore = eventID.LastIndexOf('_');
            if (lastUnderscore > 0)
            {
                eventID = eventID.Substring(0, lastUnderscore);
            }

            if (EventDataManager.Instance.RestoreFromBackup(eventID, Path.GetFileName(latest)))
            {
                EditorUtility.DisplayDialog("Event System",
                    $"✅ Restored {eventID} from backup!\n\n" +
                    "Click OK to reload events.", "OK");

                EventManager.Instance?.ReloadEvents();
            }
        }
        else if (selected == 2)
        {
            if (EditorUtility.DisplayDialog("Confirm",
                "Delete all backup files?", "Yes", "No"))
            {
                foreach (var backup in backups)
                {
                    File.Delete(backup);
                }
                EditorUtility.DisplayDialog("Event System", "All backups deleted!", "OK");
            }
        }
    }

    [MenuItem("Tools/Event System/🛠️ Setup Event System")]
    public static void SetupEventSystem()
    {
        var existing = Object.FindObjectOfType<EventManager>();
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Event System",
                "Event system already exists. Recreate?", "Yes", "No"))
            {
                return;
            }
        }

        GameObject go = new GameObject("EventSystem");
        var manager = go.AddComponent<EventManager>();
        var dataManager = go.AddComponent<EventDataManager>();

        manager.AutoLoadEvents = true;
        manager.logEvents = true;

        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog("Event System",
            "✅ Event system created successfully!\n\n" +
            "Components added:\n" +
            "• EventManager\n" +
            "• EventDataManager\n\n" +
            "Don't forget to save the scene (Ctrl+S)!\n\n" +
            "Now use 'Save ALL Events to JSON' to backup your events.", "OK");
    }

    // ============ УПРАВЛЕНИЕ АВТОСОХРАНЕНИЯМИ ============

    [MenuItem("Tools/Event System/🔴 DISABLE ALL Auto-Saves")]
    public static void DisableAllAutoSaves()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("🔴 DISABLE ALL AUTO-SAVES",
            "⚠️ ВЫ УВЕРЕНЫ?\n\n" +
            "Это отключит ВСЕ автосохранения:\n" +
            "• ❌ При входе в Play Mode\n" +
            "• ❌ При выходе из Play Mode\n" +
            "• ❌ При компиляции\n" +
            "• ❌ При сохранении сцены\n" +
            "• ❌ При закрытии Unity\n" +
            "• ❌ Интервал автосохранения\n\n" +
            "Также будет отключено логирование.\n\n" +
            "Включить обратно можно через инспектор EventDataManager.",
            "🔴 ОТКЛЮЧИТЬ ВСЁ", "Отмена"))
        {
            return;
        }

        EventDataManager.Instance.DisableAllAutoSaves();

        // Сохраняем изменения
        EditorUtility.SetDirty(EventDataManager.Instance);

        EditorUtility.DisplayDialog("Auto-Save Disabled",
            "✅ ВСЕ автосохранения ОТКЛЮЧЕНЫ!\n\n" +
            "Для включения:\n" +
            "1. Выберите EventSystem на сцене\n" +
            "2. В инспекторе найдите EventDataManager\n" +
            "3. Включите нужные опции в секции 'Настройки автосохранения'\n\n" +
            "Или используйте 'ENABLE ALL Auto-Saves' в меню Tools.", "OK");
    }

    [MenuItem("Tools/Event System/🟢 ENABLE ALL Auto-Saves")]
    public static void EnableAllAutoSaves()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("🟢 ENABLE ALL AUTO-SAVES",
            "Включить ВСЕ автосохранения?\n\n" +
            "Будут включены:\n" +
            "• ✅ При входе в Play Mode\n" +
            "• ✅ При выходе из Play Mode\n" +
            "• ✅ При компиляции\n" +
            "• ✅ При сохранении сцены\n" +
            "• ✅ При закрытии Unity\n" +
            "• ✅ Интервал автосохранения (60 секунд)\n\n" +
            "Также будет включено логирование.",
            "🟢 ВКЛЮЧИТЬ ВСЁ", "Отмена"))
        {
            return;
        }

        EventDataManager.Instance.EnableAllAutoSaves();

        EditorUtility.SetDirty(EventDataManager.Instance);

        EditorUtility.DisplayDialog("Auto-Save Enabled",
            "✅ ВСЕ автосохранения ВКЛЮЧЕНЫ!\n\n" +
            "Настройки можно изменить в инспекторе EventDataManager.", "OK");
    }

    [MenuItem("Tools/Event System/⚙️ Auto-Save Settings")]
    public static void OpenAutoSaveSettings()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        Selection.activeObject = EventDataManager.Instance;
        EditorGUIUtility.PingObject(EventDataManager.Instance);

        bool anyEnabled = EventDataManager.Instance.IsAnyAutoSaveEnabled();

        EditorUtility.DisplayDialog("Auto-Save Settings",
            $"📊 Текущий статус автосохранений:\n\n" +
            $"Состояние: {(anyEnabled ? "🟢 ВКЛЮЧЕНЫ" : "🔴 ОТКЛЮЧЕНЫ")}\n\n" +
            $"Настройки в инспекторе EventDataManager:\n" +
            $"• Auto Save On Play: {EventDataManager.Instance.AutoSaveOnPlay}\n" +
            $"• Auto Save On Exit Play: {EventDataManager.Instance.AutoSaveOnExitPlay}\n" +
            $"• Auto Save On Compile: {EventDataManager.Instance.AutoSaveOnCompile}\n" +
            $"• Auto Save On Scene Save: {EventDataManager.Instance.AutoSaveOnSceneSave}\n" +
            $"• Auto Save On Quit: {EventDataManager.Instance.AutoSaveOnQuit}\n" +
            $"• Auto Save Interval: {EventDataManager.Instance.AutoSaveInterval} сек.\n" +
            $"• Create Backups: {EventDataManager.Instance.CreateBackups}\n\n" +
            "Используйте кнопки меню для быстрого включения/отключения.", "OK");
    }

    [MenuItem("Tools/Event System/⚙️ Toggle Auto-Restore on Compile")]
    public static void ToggleAutoRestore()
    {
        bool isEnabled = EditorPrefs.GetBool("EventDataAutoRestore", true);
        EditorPrefs.SetBool("EventDataAutoRestore", !isEnabled);
        Debug.Log($"🔄 Auto-restore on compile: {(!isEnabled ? "ENABLED" : "DISABLED")}");

        EditorUtility.DisplayDialog("Event System",
            $"Auto-restore on compile is now: {(isEnabled ? "DISABLED" : "ENABLED")}\n\n" +
            "This setting is saved globally in Unity Editor preferences.\n\n" +
            "When enabled, events are automatically restored from JSON after compilation.\n" +
            "When disabled, you must restore manually using 'Load and Restore to SO'.", "OK");
    }

    [MenuItem("Tools/Event System/🗑️ Clear ALL JSON Files")]
    public static void ClearAllJsonFiles()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager not found!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("⚠️ Clear ALL JSON Files",
            "ВЫ УВЕРЕНЫ?\n\n" +
            "Это удалит ВСЕ JSON файлы с событиями.\n" +
            "Данные будут потеряны без возможности восстановления!\n\n" +
            "Рекомендуется сделать бэкап перед удалением.",
            "🗑️ УДАЛИТЬ ВСЁ", "Отмена"))
        {
            return;
        }

        string path = EventDataManager.Instance.GetEventsFolderPath();
        if (Directory.Exists(path))
        {
            string[] files = Directory.GetFiles(path, "*.json");
            int count = 0;

            foreach (string file in files)
            {
                if (!file.Contains("/Backups/") && !file.Contains("\\Backups\\"))
                {
                    File.Delete(file);
                    count++;
                }
            }

            Debug.Log($"🗑️ Deleted {count} JSON files from {path}");

            EditorUtility.DisplayDialog("Clear Complete",
                $"🗑️ Удалено {count} JSON файлов.\n\n" +
                $"Бэкапы в папке Backups сохранены.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Events folder not found!", "OK");
        }
    }
}
#endif