using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public class DebuggerWindow : EditorWindow
{
    // ========== НАСТРОЙКИ ЦВЕТОВ ==========
    private static class Colors
    {
        public static readonly Dictionary<LogModule, Color> Modules = new Dictionary<LogModule, Color>
        {
            { LogModule.Core, new Color32(180, 180, 180, 255) },
            { LogModule.UI, new Color32(100, 180, 255, 255) },
            { LogModule.Dialogue, new Color32(255, 220, 100, 255) },
            { LogModule.Event, new Color32(255, 160, 80, 255) },
            { LogModule.Inventory, new Color32(220, 120, 200, 255) },
            { LogModule.Menu, new Color32(180, 180, 180, 255) },
            { LogModule.Navigation, new Color32(80, 230, 150, 255) },
            { LogModule.QuestSystem, new Color32(180, 120, 255, 255) },
            { LogModule.RadialMenu, new Color32(255, 120, 140, 255) },
            { LogModule.Abilities, new Color32(80, 230, 230, 255) },
            { LogModule.Audio, new Color32(255, 180, 80, 255) },
            { LogModule.Animation, new Color32(150, 150, 220, 255) },
            { LogModule.Save, new Color32(200, 100, 200, 255) },
            { LogModule.Editor, new Color32(160, 160, 160, 255) },
            { LogModule.Debug, new Color32(180, 140, 255, 255) },
        };

        public static Color Error = new Color32(255, 80, 80, 255);
        public static Color Warning = new Color32(255, 220, 80, 255);
    }

    // ========== СОСТОЯНИЕ ==========
    private Dictionary<LogModule, bool> moduleFilters = new Dictionary<LogModule, bool>();
    private string searchFilter = "";
    private bool showErrorsOnly = false;
    private bool showWarningsOnly = false;
    private bool autoScroll = true;
    private bool showModuleDropdown = false;
    private bool showFilterDropdown = false;
    private string moduleSearchFilter = "";

    private List<LogEntry> logEntries = new List<LogEntry>();
    private Vector2 scrollPosition;
    private Vector2 moduleScrollPosition;
    private int maxLogs = 500;

    [System.Serializable]
    public class LogEntry
    {
        public string rawMessage;
        public string displayMessage;
        public LogModule module;
        public string stackTrace;
        public LogType type;
        public float time;

        public LogEntry(string msg, LogModule mod, string stack, LogType t)
        {
            rawMessage = msg;
            module = mod;
            stackTrace = stack;
            type = t;
            time = Time.realtimeSinceStartup;

            displayMessage = StripRichTextTags(msg);
        }

        private string StripRichTextTags(string text)
        {
            string result = Regex.Replace(text, @"<color=[^>]*>", "");
            result = Regex.Replace(result, @"</color>", "");
            return result;
        }
    }

    [MenuItem("Tools/Личное Окно Отладки")]
    public static void ShowWindow()
    {
        GetWindow<DebuggerWindow>("Отладчик");
    }

    private void OnEnable()
    {
        foreach (LogModule module in Enum.GetValues(typeof(LogModule)))
        {
            moduleFilters[module] = true;
        }
        Application.logMessageReceived += OnLogReceived;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= OnLogReceived;
    }

    private void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        LogModule module = LogModule.Core;
        string cleanMessage = condition;

        // Ищем ВСЕ совпадения [Something]
        var matches = Regex.Matches(condition, @"\[([A-Za-z0-9_]+)\]");

        if (matches.Count > 0)
        {
            // Идем с КОНЦА и ищем ПОСЛЕДНИЙ совпадающий с LogModule
            LogModule foundModule = LogModule.Core;
            Match foundMatch = null;

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                string moduleName = matches[i].Groups[1].Value;

                // Пропускаем время [0,52] - оно не является модулем
                if (moduleName.Contains(",") || moduleName.Contains("."))
                    continue;

                // Пытаемся спарсить в LogModule
                try
                {
                    LogModule parsed = (LogModule)Enum.Parse(typeof(LogModule), moduleName);
                    foundModule = parsed;
                    foundMatch = matches[i];
                    break;
                }
                catch
                {
                    continue;
                }
            }

            if (foundMatch != null)
            {
                module = foundModule;

                // Удаляем найденный модуль из текста
                int startIndex = foundMatch.Index;
                int endIndex = foundMatch.Index + foundMatch.Length;
                cleanMessage = condition.Remove(startIndex, endIndex - startIndex).Trim();

                // Чистим лишние пробелы
                cleanMessage = Regex.Replace(cleanMessage, @"\s+", " ");
                cleanMessage = cleanMessage.Trim();
            }
        }

        logEntries.Add(new LogEntry(cleanMessage, module, stackTrace, type));

        if (logEntries.Count > maxLogs)
            logEntries.RemoveAt(0);

        if (autoScroll)
            scrollPosition.y = float.MaxValue;

        Repaint();
    }

    private void OnGUI()
    {
        DrawToolbar();
        DrawModuleFilterDropdown();
        DrawFilterDropdown();
        DrawLogList();
        DrawStats();
    }

    private void DrawToolbar()
    {
        EditorGUILayout.BeginHorizontal("box");

        searchFilter = EditorGUILayout.TextField("Поиск:", searchFilter);

        int enabledCount = GetEnabledModulesCount();
        int totalCount = moduleFilters.Count;

        if (GUILayout.Button($"Модули ({enabledCount}/{totalCount}) ▼", GUILayout.Width(180)))
        {
            showModuleDropdown = !showModuleDropdown;
            if (showModuleDropdown) showFilterDropdown = false;
        }

        string filterLabel = "Фильтры";
        if (showErrorsOnly || showWarningsOnly)
        {
            string active = "";
            if (showErrorsOnly) active += "E";
            if (showWarningsOnly) active += "W";
            filterLabel = $"Фильтры ({active}) ▼";
        }
        else
        {
            filterLabel = "Фильтры ▼";
        }

        if (GUILayout.Button(filterLabel, GUILayout.Width(120)))
        {
            showFilterDropdown = !showFilterDropdown;
            if (showFilterDropdown) showModuleDropdown = false;
        }

        if (GUILayout.Button("Очистить", GUILayout.Width(80)))
        {
            logEntries.Clear();
            scrollPosition = Vector2.zero;
            Repaint();
        }
        if (GUILayout.Button("Экспорт", GUILayout.Width(80)))
        {
            ExportLogs();
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFilterDropdown()
    {
        if (!showFilterDropdown) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(250), GUILayout.Height(120));

        EditorGUILayout.LabelField("Дополнительные фильтры", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        bool newShowErrors = EditorGUILayout.Toggle("Только ошибки", showErrorsOnly);
        if (newShowErrors != showErrorsOnly)
        {
            showErrorsOnly = newShowErrors;
            if (showErrorsOnly) showWarningsOnly = false;
        }

        bool newShowWarnings = EditorGUILayout.Toggle("Только предупреждения", showWarningsOnly);
        if (newShowWarnings != showWarningsOnly)
        {
            showWarningsOnly = newShowWarnings;
            if (showWarningsOnly) showErrorsOnly = false;
        }

        bool newAutoScroll = EditorGUILayout.Toggle("Автоскролл", autoScroll);
        if (newAutoScroll != autoScroll)
        {
            autoScroll = newAutoScroll;
        }

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Закрыть", GUILayout.Width(80)))
        {
            showFilterDropdown = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawModuleFilterDropdown()
    {
        if (!showModuleDropdown) return;

        EditorGUILayout.BeginVertical("box", GUILayout.Width(400), GUILayout.Height(300));

        EditorGUILayout.BeginHorizontal();
        moduleSearchFilter = EditorGUILayout.TextField("Поиск модуля:", moduleSearchFilter);

        if (GUILayout.Button("x", GUILayout.Width(20)))
        {
            moduleSearchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Все", GUILayout.Width(50)))
        {
            List<LogModule> allModules = new List<LogModule>(moduleFilters.Keys);
            foreach (var module in allModules)
            {
                moduleFilters[module] = true;
            }
        }
        if (GUILayout.Button("Ничего", GUILayout.Width(60)))
        {
            List<LogModule> allModules = new List<LogModule>(moduleFilters.Keys);
            foreach (var module in allModules)
            {
                moduleFilters[module] = false;
            }
        }
        if (GUILayout.Button("Инвертировать", GUILayout.Width(90)))
        {
            List<LogModule> allModules = new List<LogModule>(moduleFilters.Keys);
            foreach (var module in allModules)
            {
                moduleFilters[module] = !moduleFilters[module];
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        moduleScrollPosition = EditorGUILayout.BeginScrollView(moduleScrollPosition, GUILayout.Height(200));

        List<LogModule> modules = new List<LogModule>(moduleFilters.Keys);
        if (!string.IsNullOrEmpty(moduleSearchFilter))
        {
            modules = modules.Where(m => m.ToString().ToLower().Contains(moduleSearchFilter.ToLower())).ToList();
        }

        if (!string.IsNullOrEmpty(moduleSearchFilter))
        {
            EditorGUILayout.LabelField($"Найдено модулей: {modules.Count}", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
        }

        foreach (var module in modules)
        {
            bool currentState = moduleFilters[module];

            EditorGUILayout.BeginHorizontal();

            Color moduleColor = Colors.Modules.ContainsKey(module) ? Colors.Modules[module] : Color.white;
            Rect colorRect = EditorGUILayout.GetControlRect(GUILayout.Width(16), GUILayout.Height(16));
            EditorGUI.DrawRect(colorRect, moduleColor);

            bool newState = EditorGUILayout.Toggle(module.ToString(), currentState);

            if (newState != currentState)
            {
                moduleFilters[module] = newState;
            }

            EditorGUILayout.EndHorizontal();
        }

        if (modules.Count == 0)
        {
            EditorGUILayout.LabelField("Модули не найдены", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"Показано: {modules.Count} из {moduleFilters.Count}", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Закрыть", GUILayout.Width(100)))
        {
            showModuleDropdown = false;
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private int GetEnabledModulesCount()
    {
        int count = 0;
        foreach (var kvp in moduleFilters)
        {
            if (kvp.Value) count++;
        }
        return count;
    }

    private void DrawLogList()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        List<LogEntry> entriesCopy = new List<LogEntry>(logEntries);

        string filterInfo = "";
        if (showErrorsOnly) filterInfo += " [Только ошибки]";
        if (showWarningsOnly) filterInfo += " [Только предупреждения]";
        if (!string.IsNullOrEmpty(searchFilter)) filterInfo += $" [Поиск: \"{searchFilter}\"]";

        if (!string.IsNullOrEmpty(filterInfo))
        {
            EditorGUILayout.LabelField($"Активные фильтры:{filterInfo}", EditorStyles.miniLabel);
            EditorGUILayout.Space(3);
        }

        int visibleCount = 0;
        foreach (var entry in entriesCopy)
        {
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module])
                continue;

            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception)
                continue;

            if (showWarningsOnly && entry.type != LogType.Warning)
                continue;

            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower()))
                continue;

            visibleCount++;
            DrawLogEntry(entry);
        }

        if (visibleCount == 0 && entriesCopy.Count > 0)
        {
            EditorGUILayout.LabelField("Нет логов, соответствующих фильтрам", EditorStyles.centeredGreyMiniLabel);
        }
        else if (entriesCopy.Count == 0)
        {
            EditorGUILayout.LabelField("Нет логов", EditorStyles.centeredGreyMiniLabel);
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawLogEntry(LogEntry entry)
    {
        EditorGUILayout.BeginHorizontal("box");

        // Время
        EditorGUILayout.LabelField(entry.time.ToString("F2"), GUILayout.Width(50));

        // Модуль с цветом
        Color originalColor = GUI.color;

        if (Colors.Modules.ContainsKey(entry.module))
        {
            GUI.color = Colors.Modules[entry.module];
        }

        EditorGUILayout.LabelField($"[{entry.module}]", GUILayout.Width(100));
        GUI.color = originalColor;

        // Сообщение
        if (entry.type == LogType.Error || entry.type == LogType.Exception)
        {
            GUI.color = Colors.Error;
            EditorGUILayout.LabelField(entry.displayMessage, GUILayout.MinWidth(100));
            GUI.color = originalColor;
        }
        else if (entry.type == LogType.Warning)
        {
            GUI.color = Colors.Warning;
            EditorGUILayout.LabelField(entry.displayMessage, GUILayout.MinWidth(100));
            GUI.color = originalColor;
        }
        else
        {
            EditorGUILayout.LabelField(entry.displayMessage, GUILayout.MinWidth(100));
        }

        // Кнопка стека для ошибок
        if (entry.type == LogType.Error || entry.type == LogType.Exception)
        {
            if (GUILayout.Button("Стек", GUILayout.Width(50)))
            {
                Debug.Log($"СТЕК ВЫЗОВОВ:\n{entry.stackTrace}");
            }
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawStats()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Всего логов: {logEntries.Count}");
        EditorGUILayout.LabelField($"Показано: {CountVisible()}");
        EditorGUILayout.EndHorizontal();
    }

    private int CountVisible()
    {
        int count = 0;
        List<LogEntry> entriesCopy = new List<LogEntry>(logEntries);

        foreach (var entry in entriesCopy)
        {
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module])
                continue;
            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception)
                continue;
            if (showWarningsOnly && entry.type != LogType.Warning)
                continue;
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower()))
                continue;
            count++;
        }
        return count;
    }

    private void ExportLogs()
    {
        // Собираем ТОЛЬКО видимые логи
        List<LogEntry> visibleEntries = new List<LogEntry>();

        foreach (var entry in logEntries)
        {
            // Проверяем фильтр модулей
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module])
                continue;

            // Проверяем "Только ошибки"
            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception)
                continue;

            // Проверяем "Только предупреждения"
            if (showWarningsOnly && entry.type != LogType.Warning)
                continue;

            // Проверяем текстовый поиск
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower()))
                continue;

            visibleEntries.Add(entry);
        }

        if (visibleEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("Экспорт логов", "Нет логов для экспорта (проверьте фильтры)", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Экспорт логов", "", "logs.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;

        using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
        {
            // Заголовок с информацией о фильтрах
            writer.WriteLine("========================================");
            writer.WriteLine($"Экспорт логов: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            writer.WriteLine($"Всего логов в файле: {visibleEntries.Count}");
            writer.WriteLine($"Всего логов в буфере: {logEntries.Count}");

            string filterInfo = "";
            if (showErrorsOnly) filterInfo += " [Только ошибки]";
            if (showWarningsOnly) filterInfo += " [Только предупреждения]";
            if (!string.IsNullOrEmpty(searchFilter)) filterInfo += $" [Поиск: \"{searchFilter}\"]";
            if (!string.IsNullOrEmpty(filterInfo))
            {
                writer.WriteLine($"Активные фильтры:{filterInfo}");
            }
            writer.WriteLine("========================================");
            writer.WriteLine();

            // Экспортируем логи
            foreach (var entry in visibleEntries)
            {
                writer.WriteLine($"[{entry.time:F2}] [{entry.module}] {entry.displayMessage}");
            }

            writer.WriteLine();
            writer.WriteLine("========================================");
            writer.WriteLine($"Экспортировано: {visibleEntries.Count} записей");
        }

        EditorUtility.DisplayDialog("Экспорт логов",
            $"Экспортировано {visibleEntries.Count} логов в файл:\n{path}",
            "OK");

        Debug.Log($"Логи экспортированы в: {path} (отфильтровано: {visibleEntries.Count} из {logEntries.Count})");
    }
}
