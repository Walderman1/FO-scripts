using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>Одна запись лога для внутриигрового окна DevTools.</summary>
public class RuntimeLogEntry
{
    public string message;
    public string module;
    public LogType type;
    public float time;
    public string stackTrace;
}

/// <summary>
/// Копит логи игры (в том числе из Logger) прямо в билде, чтобы их можно было
/// посмотреть во внутриигровом окне DevTools.
/// </summary>
public static class RuntimeLogBuffer
{
    private const int MaxEntries = 600;

    private static readonly object _lock = new object();
    private static readonly List<RuntimeLogEntry> _entries = new List<RuntimeLogEntry>(MaxEntries);
    private static readonly HashSet<string> _disabledModules =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    private static readonly Regex ModuleRegex = new Regex(@"\[([A-Za-z0-9_]+)\]", RegexOptions.Compiled);
    private static readonly Regex ColorTagRegex =
        new Regex(@"</?color[^>]*>", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly List<string> _modules = new List<string>();
    private static readonly Dictionary<string, Color> _colors =
        new Dictionary<string, Color>(StringComparer.OrdinalIgnoreCase);

    private static bool _hooked;
    private static int _version;

    /// <summary>Растёт при каждом новом логе — окно по нему понимает, что пора обновить список.</summary>
    public static int Version { get { return _version; } }

    public static IReadOnlyList<string> Modules { get { return _modules; } }

    public static void EnsureHooked()
    {
        if (_hooked) return;
        _hooked = true;

        BuildModuleList();

        Application.logMessageReceived -= HandleLog;
        Application.logMessageReceived += HandleLog;
    }

    private static void BuildModuleList()
    {
        if (_modules.Count > 0) return;

        foreach (string name in Enum.GetNames(typeof(LogModule)))
            _modules.Add(name);

        _colors["Core"] = new Color32(180, 180, 180, 255);
        _colors["DI"] = new Color32(143, 180, 255, 255);
        _colors["UI"] = new Color32(100, 180, 255, 255);
        _colors["Dialogue"] = new Color32(255, 220, 100, 255);
        _colors["Event"] = new Color32(255, 160, 80, 255);
        _colors["Inventory"] = new Color32(220, 120, 200, 255);
        _colors["Craft"] = new Color32(150, 230, 120, 255);
        _colors["Menu"] = new Color32(180, 180, 180, 255);
        _colors["Navigation"] = new Color32(80, 230, 150, 255);
        _colors["QuestSystem"] = new Color32(180, 120, 255, 255);
        _colors["RadialMenu"] = new Color32(255, 120, 140, 255);
        _colors["Abilities"] = new Color32(80, 230, 230, 255);
        _colors["Audio"] = new Color32(255, 180, 80, 255);
        _colors["Animation"] = new Color32(150, 150, 220, 255);
        _colors["Save"] = new Color32(200, 100, 200, 255);
        _colors["Editor"] = new Color32(160, 160, 160, 255);
        _colors["Tests"] = new Color32(120, 200, 200, 255);
        _colors["Debug"] = new Color32(180, 140, 255, 255);

        for (int i = 0; i < _modules.Count; i++)
            if (!_colors.ContainsKey(_modules[i])) _colors[_modules[i]] = Color.white;
    }

    public static bool IsModuleEnabled(string module)
    {
        if (string.IsNullOrEmpty(module)) return true;
        lock (_lock) return !_disabledModules.Contains(module);
    }

    public static void SetModuleEnabled(string module, bool enabled)
    {
        if (string.IsNullOrEmpty(module)) return;

        lock (_lock)
        {
            if (enabled) _disabledModules.Remove(module);
            else _disabledModules.Add(module);
        }

        _version++;
    }

    public static void SetAllModules(bool enabled)
    {
        lock (_lock)
        {
            _disabledModules.Clear();
            if (!enabled)
                for (int i = 0; i < _modules.Count; i++) _disabledModules.Add(_modules[i]);
        }

        _version++;
    }

    public static Color GetColor(string module, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception) return new Color32(255, 90, 90, 255);
        if (type == LogType.Warning) return new Color32(255, 220, 90, 255);

        if (!string.IsNullOrEmpty(module) && _colors.TryGetValue(module, out var color)) return color;
        return Color.white;
    }

    public static void Clear()
    {
        lock (_lock) _entries.Clear();
        _version++;
    }

    /// <summary>Копирует текущие записи в переданный список (потокобезопасно).</summary>
    public static void CopyTo(List<RuntimeLogEntry> target)
    {
        if (target == null) return;

        lock (_lock)
        {
            target.Clear();
            int start = _entries.Count > MaxEntries ? _entries.Count - MaxEntries : 0;
            for (int i = start; i < _entries.Count; i++) target.Add(_entries[i]);
        }
    }

    public static int Count
    {
        get { lock (_lock) return _entries.Count; }
    }

    private static void HandleLog(string condition, string stackTrace, LogType type)
    {
        EnsureHooked();

        string message = condition ?? "";
        string module = "Core";

        try
        {
            MatchCollection matches = ModuleRegex.Matches(message);

            for (int i = matches.Count - 1; i >= 0; i--)
            {
                string candidate = matches[i].Groups[1].Value;
                if (candidate.IndexOf(',') >= 0 || candidate.IndexOf('.') >= 0) continue;
                if (!Enum.TryParse(candidate, true, out LogModule parsed)) continue;
                if (!Enum.IsDefined(typeof(LogModule), parsed)) continue;

                module = parsed.ToString();
                message = message.Remove(matches[i].Index, matches[i].Length);
                break;
            }
        }
        catch
        {
            // Разбор модуля — не критичная часть, лог всё равно сохраняем.
        }

        message = ColorTagRegex.Replace(message, "").Trim();

        var entry = new RuntimeLogEntry
        {
            message = message,
            module = module,
            type = type,
            time = Time.realtimeSinceStartup,
            stackTrace = stackTrace
        };

        lock (_lock)
        {
            _entries.Add(entry);
            if (_entries.Count > MaxEntries) _entries.RemoveRange(0, _entries.Count - MaxEntries);
        }

        _version++;
    }
}
