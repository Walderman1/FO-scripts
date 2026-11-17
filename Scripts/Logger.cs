using UnityEngine;
using System.Collections.Generic;

public enum LogModule
{
    Core,
    DI,
    UI,
    Dialogue,
    Event,
    Inventory,
    Craft,
    Menu,
    Navigation,
    QuestSystem,
    RadialMenu,
    Abilities,
    Audio,
    Animation,
    Save,
    Editor,
    Tests,
    Debug
}

public static class Logger
{
    private static Dictionary<LogModule, bool> _moduleEnabled = new Dictionary<LogModule, bool>()
    {
        { LogModule.Core, true },
        { LogModule.DI, true },
        { LogModule.UI, true },
        { LogModule.Dialogue, true },
        { LogModule.Event, true },
        { LogModule.Inventory, true },
        { LogModule.Craft, true },
        { LogModule.Menu, true },
        { LogModule.Navigation, true },
        { LogModule.QuestSystem, true },
        { LogModule.RadialMenu, true },
        { LogModule.Abilities, true },
        { LogModule.Audio, true },
        { LogModule.Animation, true },
        { LogModule.Save, true },
        { LogModule.Editor, true },
        { LogModule.Tests, true },
        { LogModule.Debug, true },
    };

    private static string GetColor(LogModule module)
    {
        switch (module)
        {
            case LogModule.Core: return "white";
            case LogModule.DI: return "#8fb4ff";
            case LogModule.UI: return "cyan";
            case LogModule.Dialogue: return "yellow";
            case LogModule.Event: return "orange";
            case LogModule.Inventory: return "magenta";
            case LogModule.Craft: return "lime";
            case LogModule.Menu: return "white";
            case LogModule.Navigation: return "green";
            case LogModule.QuestSystem: return "purple";
            case LogModule.RadialMenu: return "red";
            case LogModule.Abilities: return "cyan";
            case LogModule.Audio: return "orange";
            case LogModule.Animation: return "silver";
            case LogModule.Save: return "magenta";
            case LogModule.Editor: return "grey";
            case LogModule.Tests: return "teal";
            case LogModule.Debug: return "purple";
            default: return "white";
        }
    }

    [HideInCallstack]
    public static void Log(LogModule module, string message, Object context = null)
    {
        if (!_moduleEnabled.ContainsKey(module) || !_moduleEnabled[module]) return;
        string coloredMessage = $"<color={GetColor(module)}>[{module}]</color> {message}";
        if (context != null) Debug.Log(coloredMessage, context);
        else Debug.Log(coloredMessage);
    }

    [HideInCallstack]
    public static void LogError(LogModule module, string message, Object context = null)
    {
        string coloredMessage = $"<color=red>[{module}]</color> {message}";
        if (context != null) Debug.LogError(coloredMessage, context);
        else Debug.LogError(coloredMessage);
    }

    [HideInCallstack]
    public static void LogWarning(LogModule module, string message, Object context = null)
    {
        string coloredMessage = $"<color=yellow>[{module}]</color> {message}";
        if (context != null) Debug.LogWarning(coloredMessage, context);
        else Debug.LogWarning(coloredMessage);
    }

    [HideInCallstack]
    public static void LogVerbose(LogModule module, string message, Object context = null)
    {
        if (!VerboseEnabled) return;
        if (!_moduleEnabled.ContainsKey(module) || !_moduleEnabled[module]) return;

        string coloredMessage = $"<color={GetColor(module)}>[{module}]</color> {message}";
        if (context != null) Debug.Log(coloredMessage, context);
        else Debug.Log(coloredMessage);
    }

    public static void SetModuleEnabled(LogModule module, bool isEnabled)
    {
        if (_moduleEnabled.ContainsKey(module))
            _moduleEnabled[module] = isEnabled;
        Debug.Log($"<color=grey>Logger: Module {module} is now {(isEnabled ? "ON" : "OFF")}</color>");
    }
    public static bool VerboseEnabled = false;

    public static void SetVerbose(bool enabled)
    {
        VerboseEnabled = enabled;
        Debug.Log($"<color=grey>Logger: Verbose mode {(enabled ? "ON" : "OFF")}</color>");
    }
}