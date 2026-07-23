#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class EventEditorHelper
{
    [MenuItem("Tools/Event System/Сохранить ВСЕ события в JSON")]
    public static void SaveAllEventsToJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventManager не найден!", "OK");
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
            $"Сохранено событий в JSON\n\n" +
            $"Папка: {path}\n" +
            $"JSON файлов: {fileCount}\n" +
            $"Резервных копий: {backupCount}\n\n" +
            "Нажмите OK для открытия папки.", "OK");

        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Event System/Сохранить загруженные события в JSON")]
    public static void SaveLoadedEventsToJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventManager не найден!", "OK");
            return;
        }

        bool confirm = EditorUtility.DisplayDialog("Подтверждение сохранения",
            "Это сохранит загруженные события в JSON файлы.\n\n" +
            "ВНИМАНИЕ: Это ПЕРЕЗАПИШЕТ существующие JSON файлы!\n\n" +
            "Резервные копии будут созданы автоматически.\n\n" +
            "Продолжить?", "Да, сохранить", "Отмена");

        if (!confirm) return;

        eventManager.SaveAllEventsToJson();

        string path = EventDataManager.Instance.GetEventsFolderPath();
        EditorUtility.DisplayDialog("Event System",
            $"Сохранено {eventManager.registeredEvents.Count} загруженных событий в JSON\n\n" +
            $"Папка: {path}\n\n" +
            "Резервные копии созданы перед перезаписью.", "OK");
    }

    [MenuItem("Tools/Event System/Загрузить и восстановить в SO")]
    public static void LoadAndRestoreToSO()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        bool confirm = EditorUtility.DisplayDialog("Подтверждение восстановления",
            "Это восстановит события из JSON в SO файлы.\n\n" +
            "ВНИМАНИЕ: Это ПЕРЕЗАПИШЕТ существующие SO файлы!\n\n" +
            "Убедитесь, что ваши JSON файлы корректны.\n\n" +
            "Продолжить?", "Да, восстановить", "Отмена");

        if (!confirm) return;

        int restored = EventDataManager.Instance.RestoreEventsToSO();

        EditorUtility.DisplayDialog("Event System",
            $"Восстановлено {restored} событий из JSON в SO файлы\n\n" +
            "Проверьте консоль для деталей.\n\n" +
            "Теперь откройте любой GameEvent ассет чтобы увидеть восстановленные действия.", "OK");
    }

    [MenuItem("Tools/Event System/Загрузить все события из JSON")]
    public static void LoadAllEventsFromJson()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var events = EventDataManager.Instance.LoadAllEvents();
        EditorUtility.DisplayDialog("Event System", $"Загружено {events.Count} событий из JSON!", "OK");
    }

    [MenuItem("Tools/Event System/Перезагрузить события")]
    public static void ReloadEvents()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        var eventManager = Object.FindObjectOfType<EventManager>();
        if (eventManager != null)
        {
            eventManager.ReloadEvents();
            EditorUtility.DisplayDialog("Event System", "События перезагружены!", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "EventManager не найден!", "OK");
        }
    }

    [MenuItem("Tools/Event System/Открыть папку событий")]
    public static void OpenEventsFolder()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
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

    [MenuItem("Tools/Event System/Открыть папку резервных копий")]
    public static void OpenBackupsFolder()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
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

    [MenuItem("Tools/Event System/Восстановить из резервной копии")]
    public static void RestoreFromBackup()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        EventDataManager.Instance.Initialize();

        var backups = EventDataManager.Instance.GetBackupFiles();
        if (backups.Count == 0)
        {
            EditorUtility.DisplayDialog("Event System", "Резервные копии не найдены!", "OK");
            return;
        }

        string[] options = new string[backups.Count + 1];
        options[0] = "=== ОТМЕНА ===";
        for (int i = 0; i < backups.Count; i++)
        {
            options[i + 1] = Path.GetFileName(backups[i]);
        }

        int selected = EditorUtility.DisplayDialogComplex(
            "Восстановление из резервной копии",
            $"Найдено {backups.Count} резервных файлов.\n\nВыберите файл для восстановления:",
            "Отмена",
            "Восстановить последний",
            "Удалить все резервные копии"
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
                    $"Восстановлено {eventID} из резервной копии\n\n" +
                    "Нажмите OK для перезагрузки событий.", "OK");

                EventManager.Instance?.ReloadEvents();
            }
        }
        else if (selected == 2)
        {
            if (EditorUtility.DisplayDialog("Подтверждение",
                "Удалить все резервные файлы?", "Да", "Нет"))
            {
                foreach (var backup in backups)
                {
                    File.Delete(backup);
                }
                EditorUtility.DisplayDialog("Event System", "Все резервные копии удалены!", "OK");
            }
        }
    }

    [MenuItem("Tools/Event System/Настроить систему событий")]
    public static void SetupEventSystem()
    {
        var existing = Object.FindObjectOfType<EventManager>();
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog("Event System",
                "Система событий уже существует. Пересоздать?", "Да", "Нет"))
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
            "Система событий успешно создана\n\n" +
            "Добавлены компоненты:\n" +
            "• EventManager\n" +
            "• EventDataManager\n\n" +
            "Не забудьте сохранить сцену (Ctrl+S)\n\n" +
            "Теперь используйте 'Сохранить все события в JSON' для резервного копирования.", "OK");
    }

    [MenuItem("Tools/Event System/ОТКЛЮЧИТЬ все автосохранения")]
    public static void DisableAllAutoSaves()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("ОТКЛЮЧИТЬ ВСЕ АВТОСОХРАНЕНИЯ",
            "ВЫ УВЕРЕНЫ?\n\n" +
            "Это отключит ВСЕ автосохранения:\n" +
            "• При входе в Play Mode\n" +
            "• При выходе из Play Mode\n" +
            "• При компиляции\n" +
            "• При сохранении сцены\n" +
            "• При закрытии Unity\n" +
            "• Интервал автосохранения\n\n" +
            "Также будет отключено логирование.\n\n" +
            "Включить обратно можно через инспектор EventDataManager.",
            "ОТКЛЮЧИТЬ ВСЁ", "Отмена"))
        {
            return;
        }

        EventDataManager.Instance.DisableAllAutoSaves();

        EditorUtility.SetDirty(EventDataManager.Instance);

        EditorUtility.DisplayDialog("Auto-Save Disabled",
            "ВСЕ автосохранения ОТКЛЮЧЕНЫ\n\n" +
            "Для включения:\n" +
            "1. Выберите EventSystem на сцене\n" +
            "2. В инспекторе найдите EventDataManager\n" +
            "3. Включите нужные опции в секции 'Настройки автосохранения'\n\n" +
            "Или используйте 'ВКЛЮЧИТЬ все автосохранения' в меню Tools.", "OK");
    }

    [MenuItem("Tools/Event System/ВКЛЮЧИТЬ все автосохранения")]
    public static void EnableAllAutoSaves()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("ВКЛЮЧИТЬ ВСЕ АВТОСОХРАНЕНИЯ",
            "Включить ВСЕ автосохранения?\n\n" +
            "Будут включены:\n" +
            "• При входе в Play Mode\n" +
            "• При выходе из Play Mode\n" +
            "• При компиляции\n" +
            "• При сохранении сцены\n" +
            "• При закрытии Unity\n" +
            "• Интервал автосохранения (60 секунд)\n\n" +
            "Также будет включено логирование.",
            "ВКЛЮЧИТЬ ВСЁ", "Отмена"))
        {
            return;
        }

        EventDataManager.Instance.EnableAllAutoSaves();

        EditorUtility.SetDirty(EventDataManager.Instance);

        EditorUtility.DisplayDialog("Auto-Save Enabled",
            "ВСЕ автосохранения ВКЛЮЧЕНЫ\n\n" +
            "Настройки можно изменить в инспекторе EventDataManager.", "OK");
    }

    [MenuItem("Tools/Event System/Настройки автосохранения")]
    public static void OpenAutoSaveSettings()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        Selection.activeObject = EventDataManager.Instance;
        EditorGUIUtility.PingObject(EventDataManager.Instance);

        bool anyEnabled = EventDataManager.Instance.IsAnyAutoSaveEnabled();

        EditorUtility.DisplayDialog("Настройки автосохранения",
            $"Текущий статус автосохранений:\n\n" +
            $"Состояние: {(anyEnabled ? "ВКЛЮЧЕНЫ" : "ОТКЛЮЧЕНЫ")}\n\n" +
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

    [MenuItem("Tools/Event System/Переключить авто-восстановление при компиляции")]
    public static void ToggleAutoRestore()
    {
        bool isEnabled = EditorPrefs.GetBool("EventDataAutoRestore", true);
        EditorPrefs.SetBool("EventDataAutoRestore", !isEnabled);
        Logger.Log(LogModule.Event, $"Авто-восстановление при компиляции: {(isEnabled ? "ВЫКЛЮЧЕНО" : "ВКЛЮЧЕНО")}");

        EditorUtility.DisplayDialog("Event System",
            $"Авто-восстановление при компиляции теперь: {(isEnabled ? "ВЫКЛЮЧЕНО" : "ВКЛЮЧЕНО")}\n\n" +
            "Эта настройка сохраняется глобально в настройках редактора Unity.\n\n" +
            "Когда включено, события автоматически восстанавливаются из JSON после компиляции.\n" +
            "Когда выключено, вы должны восстанавливать вручную через 'Загрузить и восстановить в SO'.", "OK");
    }

    [MenuItem("Tools/Event System/Удалить ВСЕ JSON файлы")]
    public static void ClearAllJsonFiles()
    {
        if (EventDataManager.Instance == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        if (!EditorUtility.DisplayDialog("Удалить ВСЕ JSON файлы",
            "ВЫ УВЕРЕНЫ?\n\n" +
            "Это удалит ВСЕ JSON файлы с событиями.\n" +
            "Данные будут потеряны без возможности восстановления!\n\n" +
            "Рекомендуется сделать бэкап перед удалением.",
            "УДАЛИТЬ ВСЁ", "Отмена"))
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

            Logger.Log(LogModule.Event, $"Удалено {count} JSON файлов из {path}");

            EditorUtility.DisplayDialog("Очистка завершена",
                $"Удалено {count} JSON файлов\n\n" +
                $"Резервные копии в папке Backups сохранены.", "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Error", "Папка с событиями не найдена!", "OK");
        }
    }
}
#endif
