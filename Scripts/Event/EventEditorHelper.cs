#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.IO;

public static class EventEditorHelper
{
    private static EventManager ResolveEventManager()
    {
        return Object.FindAnyObjectByType<EventManager>(FindObjectsInactive.Include);
    }

    private static EventDataManager ResolveEventDataManager()
    {
        return Object.FindAnyObjectByType<EventDataManager>(FindObjectsInactive.Include);
    }

    [MenuItem("Tools/Система событий/Сохранить ВСЕ события в JSON")]
    public static void SaveAllEventsToJson()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        dataManager.Initialize();

        var eventManager = ResolveEventManager();
        if (eventManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventManager не найден!", "OK");
            return;
        }

        eventManager.SaveAllEventsFromProject();

        string path = dataManager.GetEventsFolderPath();
        int fileCount = Directory.GetFiles(path, "*.json").Length;
        int backupCount = 0;

        string backupPath = Path.Combine(path, "Backups");
        if (Directory.Exists(backupPath))
            backupCount = Directory.GetFiles(backupPath, "*.json").Length;

        EditorUtility.DisplayDialog("Event System",
            $"Сохранено событий в JSON\n\n" +
            $"Папка: {path}\n" +
            $"JSON файлов: {fileCount}\n" +
            $"Резервных копий: {backupCount}\n\n" +
            "Нажмите OK для открытия папки.", "OK");

        EditorUtility.RevealInFinder(path);
    }

    [MenuItem("Tools/Система событий/Загрузить JSON в кэш (без применения к SO)")]
    public static void LoadAllEventsFromJson()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        dataManager.Initialize();

        var events = dataManager.LoadAllEvents();
        EditorUtility.DisplayDialog("Event System",
            $"Загружено {events.Count} событий в кэш (JSON прочитан).\n\n" +
            "SO-ассеты GameEvent НЕ изменены.\n\n" +
            "Чтобы применить JSON к SO, используйте:\n" +
            "Tools → Event System → Загрузить и восстановить в SO",
            "OK");
    }

    [MenuItem("Tools/Система событий/Загрузить и восстановить в SO")]
    public static void LoadAndRestoreToSO()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
        {
            EditorUtility.DisplayDialog("Error",
                "EventDataManager не найден на сцене\n\n" +
                "Добавьте EventDataManager на GameObject EventSystem.", "OK");
            return;
        }

        dataManager.Initialize();

        bool confirm = EditorUtility.DisplayDialog("Подтверждение восстановления",
            "Это восстановит события из JSON в SO файлы (Assets/Resources/Events).\n\n" +
            "ВНИМАНИЕ: Существующие SO файлы с совпадающим eventID будут ПЕРЕЗАПИСАНЫ.\n\n" +
            "Убедитесь, что JSON файлы корректны и в новом формате.\n\n" +
            "Продолжить?", "Да, восстановить", "Отмена");

        if (!confirm) return;

        int restored = dataManager.RestoreEventsToSO();

        EditorUtility.DisplayDialog("Event System",
            $"Восстановлено {restored} событий из JSON в SO файлы\n\n" +
            "Проверьте консоль для деталей.\n\n" +
            "Теперь откройте любой GameEvent ассет чтобы увидеть восстановленные действия.",
            "OK");
    }

    [MenuItem("Tools/Система событий/Перезагрузить события")]
    public static void ReloadEvents()
    {
        var eventManager = ResolveEventManager();
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

    [MenuItem("Tools/Система событий/Открыть папку событий")]
    public static void OpenEventsFolder()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        dataManager.Initialize();

        string path = dataManager.GetEventsFolderPath();
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

    [MenuItem("Tools/Система событий/ОТКЛЮЧИТЬ все автосохранения")]
    public static void DisableAllAutoSaves()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
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
            "• При закрытии Unity\n\n" +
            "Также будет отключено логирование.\n\n" +
            "Включить обратно можно через инспектор EventDataManager.",
            "ОТКЛЮЧИТЬ ВСЁ", "Отмена"))
            return;

        dataManager.DisableAllAutoSaves();
        EditorUtility.SetDirty(dataManager);

        EditorUtility.DisplayDialog("Auto-Save Disabled",
            "ВСЕ автосохранения ОТКЛЮЧЕНЫ\n\n" +
            "Для включения:\n" +
            "1. Выберите EventSystem на сцене\n" +
            "2. В инспекторе найдите EventDataManager\n" +
            "3. Включите нужные опции в секции 'Настройки автосохранения'\n\n" +
            "Или используйте 'ВКЛЮЧИТЬ все автосохранения' в меню Tools.", "OK");
    }

    [MenuItem("Tools/Система событий/ВКЛЮЧИТЬ все автосохранения")]
    public static void EnableAllAutoSaves()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
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
            "• При закрытии Unity\n\n" +
            "Также будет включено логирование.",
            "ВКЛЮЧИТЬ ВСЁ", "Отмена"))
            return;

        dataManager.EnableAllAutoSaves();
        EditorUtility.SetDirty(dataManager);

        EditorUtility.DisplayDialog("Auto-Save Enabled",
            "ВСЕ автосохранения ВКЛЮЧЕНЫ\n\n" +
            "Настройки можно изменить в инспекторе EventDataManager.", "OK");
    }

    [MenuItem("Tools/Система событий/Настройки автосохранения")]
    public static void OpenAutoSaveSettings()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
        {
            EditorUtility.DisplayDialog("Error", "EventDataManager не найден!", "OK");
            return;
        }

        Selection.activeObject = dataManager;
        EditorGUIUtility.PingObject(dataManager);

        bool anyEnabled = dataManager.IsAnyAutoSaveEnabled();

        EditorUtility.DisplayDialog("Настройки автосохранения",
            $"Текущий статус автосохранений:\n\n" +
            $"Состояние: {(anyEnabled ? "ВКЛЮЧЕНЫ" : "ОТКЛЮЧЕНЫ")}\n\n" +
            $"Настройки в инспекторе EventDataManager:\n" +
            $"• Auto Save On Play: {dataManager.AutoSaveOnPlay}\n" +
            $"• Auto Save On Exit Play: {dataManager.AutoSaveOnExitPlay}\n" +
            $"• Auto Save On Compile: {dataManager.AutoSaveOnCompile}\n" +
            $"• Auto Save On Scene Save: {dataManager.AutoSaveOnSceneSave}\n" +
            $"• Auto Save On Quit: {dataManager.AutoSaveOnQuit}\n" +
            $"• Create Backups: {dataManager.CreateBackups}\n\n" +
            "Используйте кнопки меню для быстрого включения/отключения.", "OK");
    }

    [MenuItem("Tools/Система событий/Удалить ВСЕ JSON файлы")]
    public static void ClearAllJsonFiles()
    {
        var dataManager = ResolveEventDataManager();
        if (dataManager == null)
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
            return;

        string path = dataManager.GetEventsFolderPath();
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