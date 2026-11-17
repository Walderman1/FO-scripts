#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Text.RegularExpressions;

public static class DebuggerCore
{
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

    private static Dictionary<LogModule, bool> moduleFilters = new Dictionary<LogModule, bool>();
    private static string searchFilter = "";
    private static bool showErrorsOnly = false;
    private static bool showWarningsOnly = false;
    private static bool autoScroll = true;
    private static bool showModuleDropdown = false;
    private static bool showFilterDropdown = false;
    private static string moduleSearchFilter = "";
    private static List<LogEntry> logEntries = new List<LogEntry>();
    private static Vector2 scrollPosition;
    private static Vector2 moduleScrollPosition;
    private static int maxLogs = 1000;

    private static readonly HashSet<EditorWindow> subscribedWindows = new HashSet<EditorWindow>();
    private static bool isInitialized = false;

    [InitializeOnLoadMethod]
    private static void Initialize()
    {
        if (isInitialized) return;
        isInitialized = true;

        foreach (LogModule module in Enum.GetValues(typeof(LogModule)))
            moduleFilters[module] = true;

        Application.logMessageReceived -= OnLogReceived;
        Application.logMessageReceived += OnLogReceived;
    }

    public static void Register(EditorWindow window)
    {
        if (window == null) return;
        Initialize();
        subscribedWindows.Add(window);
    }

    public static void Unregister(EditorWindow window)
    {
        if (window == null) return;
        subscribedWindows.Remove(window);
    }

    private static void RepaintSubscribers()
    {
        foreach (var w in subscribedWindows)
            if (w != null) w.Repaint();
    }

    public static int GetLogsCount() => logEntries.Count;

    public static int GetVisibleCount() => CountVisible();

    private static void OnLogReceived(string condition, string stackTrace, LogType type)
    {
        LogModule module = LogModule.Core;
        string cleanMessage = condition;

        var matches = Regex.Matches(condition, @"\[([A-Za-z0-9_]+)\]");

        if (matches.Count > 0)
        {
            LogModule foundModule = LogModule.Core;
            Match foundMatch = null;

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                string moduleName = matches[i].Groups[1].Value;
                if (moduleName.Contains(",") || moduleName.Contains(".")) continue;

                try
                {
                    LogModule parsed = (LogModule)Enum.Parse(typeof(LogModule), moduleName);
                    foundModule = parsed;
                    foundMatch = matches[i];
                    break;
                }
                catch { continue; }
            }

            if (foundMatch != null)
            {
                module = foundModule;
                int startIndex = foundMatch.Index;
                int endIndex = foundMatch.Index + foundMatch.Length;
                cleanMessage = condition.Remove(startIndex, endIndex - startIndex).Trim();
                cleanMessage = Regex.Replace(cleanMessage, @"\s+", " ").Trim();
            }
        }

        logEntries.Add(new LogEntry(cleanMessage, module, stackTrace, type));

        if (logEntries.Count > maxLogs)
            logEntries.RemoveAt(0);

        if (autoScroll)
            scrollPosition.y = float.MaxValue;

        RepaintSubscribers();
    }

    public static void Draw(float hostWidth = 800f)
    {
        Initialize();
        DrawToolbar(hostWidth);
        DrawModuleFilterDropdown(hostWidth);
        DrawFilterDropdown();
        DrawLogList(hostWidth);
        DrawStats();
    }

    private static void DrawToolbar(float hostWidth)
    {
        bool narrow = hostWidth < 650f;

        if (narrow)
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);
            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            int enabledCount = GetEnabledModulesCount();
            int totalCount = moduleFilters.Count;

            if (GUILayout.Button($"Модули ({enabledCount}/{totalCount})", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.MinWidth(80)))
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
                filterLabel = $"Фильтры ({active})";
            }

            if (GUILayout.Button(filterLabel, EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.MinWidth(60)))
            {
                showFilterDropdown = !showFilterDropdown;
                if (showFilterDropdown) showModuleDropdown = false;
            }

            if (GUILayout.Button("Очистить", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.MinWidth(50)))
            {
                logEntries.Clear();
                scrollPosition = Vector2.zero;
            }

            if (GUILayout.Button("Экспорт", EditorStyles.toolbarButton, GUILayout.ExpandWidth(true), GUILayout.MinWidth(50)))
                ExportLogs();

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            searchFilter = EditorGUILayout.TextField(searchFilter, EditorStyles.toolbarSearchField, GUILayout.ExpandWidth(true), GUILayout.MinWidth(120));

            int enabledCount = GetEnabledModulesCount();
            int totalCount = moduleFilters.Count;

            if (GUILayout.Button($"Модули ({enabledCount}/{totalCount}) ▼", EditorStyles.toolbarButton, GUILayout.MinWidth(150), GUILayout.ExpandWidth(false)))
            {
                showModuleDropdown = !showModuleDropdown;
                if (showModuleDropdown) showFilterDropdown = false;
            }

            string filterLabel = "Фильтры ▼";
            if (showErrorsOnly || showWarningsOnly)
            {
                string active = "";
                if (showErrorsOnly) active += "E";
                if (showWarningsOnly) active += "W";
                filterLabel = $"Фильтры ({active}) ▼";
            }

            if (GUILayout.Button(filterLabel, EditorStyles.toolbarButton, GUILayout.MinWidth(100), GUILayout.ExpandWidth(false)))
            {
                showFilterDropdown = !showFilterDropdown;
                if (showFilterDropdown) showModuleDropdown = false;
            }

            if (GUILayout.Button("Очистить", EditorStyles.toolbarButton, GUILayout.MinWidth(70), GUILayout.ExpandWidth(false)))
            {
                logEntries.Clear();
                scrollPosition = Vector2.zero;
            }

            if (GUILayout.Button("Экспорт", EditorStyles.toolbarButton, GUILayout.MinWidth(70), GUILayout.ExpandWidth(false)))
                ExportLogs();

            EditorGUILayout.EndHorizontal();
        }
    }

    private static void DrawFilterDropdown()
    {
        if (!showFilterDropdown) return;

        EditorGUILayout.BeginVertical("box");

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

        autoScroll = EditorGUILayout.Toggle("Автоскролл", autoScroll);

        bool newVerbose = EditorGUILayout.Toggle("Verbose логи", Logger.VerboseEnabled);
        if (newVerbose != Logger.VerboseEnabled) Logger.SetVerbose(newVerbose);

        EditorGUILayout.Space(3);
        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Закрыть", GUILayout.MinWidth(80))) showFilterDropdown = false;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private static void DrawModuleFilterDropdown(float hostWidth)
    {
        if (!showModuleDropdown) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        moduleSearchFilter = EditorGUILayout.TextField("Поиск модуля:", moduleSearchFilter, GUILayout.ExpandWidth(true));
        if (GUILayout.Button("x", GUILayout.Width(24)))
        {
            moduleSearchFilter = "";
            GUI.FocusControl(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Все", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
        {
            foreach (var m in new List<LogModule>(moduleFilters.Keys)) moduleFilters[m] = true;
        }
        if (GUILayout.Button("Ничего", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
        {
            foreach (var m in new List<LogModule>(moduleFilters.Keys)) moduleFilters[m] = false;
        }
        if (GUILayout.Button("Инвертировать", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
        {
            foreach (var m in new List<LogModule>(moduleFilters.Keys)) moduleFilters[m] = !moduleFilters[m];
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        float scrollHeight = Mathf.Clamp(hostWidth < 500 ? 140f : 200f, 100f, 250f);
        moduleScrollPosition = EditorGUILayout.BeginScrollView(moduleScrollPosition, GUILayout.Height(scrollHeight));

        List<LogModule> modules = new List<LogModule>(moduleFilters.Keys);
        if (!string.IsNullOrEmpty(moduleSearchFilter))
            modules = modules.Where(m => m.ToString().ToLower().Contains(moduleSearchFilter.ToLower())).ToList();

        foreach (var module in modules)
        {
            bool currentState = moduleFilters[module];

            EditorGUILayout.BeginHorizontal();
            Color moduleColor = Colors.Modules.ContainsKey(module) ? Colors.Modules[module] : Color.white;
            Rect colorRect = EditorGUILayout.GetControlRect(GUILayout.Width(16), GUILayout.Height(16));
            EditorGUI.DrawRect(colorRect, moduleColor);
            bool newState = EditorGUILayout.Toggle(module.ToString(), currentState);
            if (newState != currentState) moduleFilters[module] = newState;
            EditorGUILayout.EndHorizontal();
        }

        if (modules.Count == 0)
            EditorGUILayout.LabelField("Модули не найдены", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField($"Показано: {modules.Count} из {moduleFilters.Count}", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Закрыть", GUILayout.MinWidth(100))) showModuleDropdown = false;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private static int GetEnabledModulesCount()
    {
        int count = 0;
        foreach (var kvp in moduleFilters) if (kvp.Value) count++;
        return count;
    }

    private static void DrawLogList(float hostWidth)
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandHeight(true));

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

        bool narrow = hostWidth < 500f;
        int visibleCount = 0;

        foreach (var entry in entriesCopy)
        {
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module]) continue;
            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception) continue;
            if (showWarningsOnly && entry.type != LogType.Warning) continue;
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower())) continue;

            visibleCount++;
            DrawLogEntry(entry, narrow);
        }

        if (visibleCount == 0 && entriesCopy.Count > 0)
            EditorGUILayout.LabelField("Нет логов, соответствующих фильтрам", EditorStyles.centeredGreyMiniLabel);
        else if (entriesCopy.Count == 0)
            EditorGUILayout.LabelField("Нет логов", EditorStyles.centeredGreyMiniLabel);

        EditorGUILayout.EndScrollView();
    }

    private static void DrawLogEntry(LogEntry entry, bool narrow)
    {
        bool isError = entry.type == LogType.Error || entry.type == LogType.Exception;
        bool isWarning = entry.type == LogType.Warning;

        if (narrow)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(entry.time.ToString("F2"), GUILayout.Width(42));
            Color prev = GUI.color;
            if (Colors.Modules.ContainsKey(entry.module)) GUI.color = Colors.Modules[entry.module];
            EditorGUILayout.LabelField($"[{entry.module}]", GUILayout.ExpandWidth(true));
            GUI.color = prev;

            if (isError)
            {
                if (GUILayout.Button("Стек", GUILayout.Width(50)))
                    Logger.Log(LogModule.Editor, $"СТЕК ВЫЗОВОВ:\n{entry.stackTrace}");
            }
            EditorGUILayout.EndHorizontal();

            Color original = GUI.color;
            if (isError) GUI.color = Colors.Error;
            else if (isWarning) GUI.color = Colors.Warning;
            EditorGUILayout.LabelField(entry.displayMessage, EditorStyles.wordWrappedLabel);
            GUI.color = original;
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(entry.time.ToString("F2"), GUILayout.Width(45));

            Color original = GUI.color;
            if (Colors.Modules.ContainsKey(entry.module)) GUI.color = Colors.Modules[entry.module];
            EditorGUILayout.LabelField($"[{entry.module}]", GUILayout.MinWidth(90), GUILayout.ExpandWidth(false));
            GUI.color = original;

            if (isError) GUI.color = Colors.Error;
            else if (isWarning) GUI.color = Colors.Warning;
            EditorGUILayout.LabelField(entry.displayMessage, GUILayout.ExpandWidth(true));
            GUI.color = original;

            if (isError)
            {
                if (GUILayout.Button("Стек", GUILayout.Width(50)))
                    Logger.Log(LogModule.Editor, $"СТЕК ВЫЗОВОВ:\n{entry.stackTrace}");
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private static void DrawStats()
    {
        EditorGUILayout.BeginHorizontal("box");
        EditorGUILayout.LabelField($"Всего: {logEntries.Count}", GUILayout.ExpandWidth(true));
        EditorGUILayout.LabelField($"Показано: {CountVisible()}", GUILayout.ExpandWidth(true));
        EditorGUILayout.EndHorizontal();
    }

    private static int CountVisible()
    {
        int count = 0;
        foreach (var entry in logEntries)
        {
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module]) continue;
            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception) continue;
            if (showWarningsOnly && entry.type != LogType.Warning) continue;
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower())) continue;
            count++;
        }
        return count;
    }

    private static void ExportLogs()
    {
        List<LogEntry> visibleEntries = new List<LogEntry>();
        foreach (var entry in logEntries)
        {
            if (!moduleFilters.ContainsKey(entry.module) || !moduleFilters[entry.module]) continue;
            if (showErrorsOnly && entry.type != LogType.Error && entry.type != LogType.Exception) continue;
            if (showWarningsOnly && entry.type != LogType.Warning) continue;
            if (!string.IsNullOrEmpty(searchFilter) &&
                !entry.displayMessage.ToLower().Contains(searchFilter.ToLower())) continue;
            visibleEntries.Add(entry);
        }

        if (visibleEntries.Count == 0)
        {
            EditorUtility.DisplayDialog("Экспорт логов", "Нет логов для экспорта (проверьте фильтры)", "OK");
            return;
        }

        string path = EditorUtility.SaveFilePanel("Экспорт логов", "", "logs.txt", "txt");
        if (string.IsNullOrEmpty(path)) return;

        try
        {
            using (System.IO.StreamWriter writer = new System.IO.StreamWriter(path))
            {
                writer.WriteLine("========================================");
                writer.WriteLine($"Экспорт логов: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                writer.WriteLine($"Всего: {visibleEntries.Count} из {logEntries.Count}");
                writer.WriteLine("========================================");
                writer.WriteLine();

                foreach (var entry in visibleEntries)
                    writer.WriteLine($"[{entry.time:F2}] [{entry.module}] {entry.displayMessage}");
            }

            EditorUtility.DisplayDialog("Экспорт", $"Экспортировано {visibleEntries.Count} логов:\n{path}", "OK");
        }
        catch (System.Exception e)
        {
            EditorUtility.DisplayDialog("Ошибка экспорта", e.Message, "OK");
        }
    }
}
#endif