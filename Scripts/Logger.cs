using UnityEngine;
using System.Collections.Generic;

public enum LogModule
{
    Core,
    UI,
    Dialogue,
    Event,
    Inventory,
    Menu,
    Navigation,
    QuestSystem,
    RadialMenu,
    Abilities,
    Audio,
    Animation,
    Save,
    Editor,
    Debug
}

public static class Logger
{
    private static Dictionary<LogModule, bool> _moduleEnabled = new Dictionary<LogModule, bool>()
    {
        { LogModule.Core, true },
        { LogModule.UI, true },
        { LogModule.Dialogue, true },
        { LogModule.Event, true },
        { LogModule.Inventory, true },
        { LogModule.Menu, true },
        { LogModule.Navigation, true },
        { LogModule.QuestSystem, true },
        { LogModule.RadialMenu, true },
        { LogModule.Abilities, true },
        { LogModule.Audio, true },
        { LogModule.Animation, true },
        { LogModule.Save, true },
        { LogModule.Editor, true },
        { LogModule.Debug, true },
    };

    private static string GetColor(LogModule module)
    {
        switch (module)
        {
            case LogModule.Core: return "white";
            case LogModule.UI: return "cyan";
            case LogModule.Dialogue: return "yellow";
            case LogModule.Event: return "orange";
            case LogModule.Inventory: return "magenta";
            case LogModule.Menu: return "white";
            case LogModule.Navigation: return "green";
            case LogModule.QuestSystem: return "purple";
            case LogModule.RadialMenu: return "red";
            case LogModule.Abilities: return "cyan";
            case LogModule.Audio: return "orange";
            case LogModule.Animation: return "silver";
            case LogModule.Save: return "magenta";
            case LogModule.Editor: return "grey";
            case LogModule.Debug: return "purple";
            default: return "white";
        }
    }

    public static void Log(LogModule module, string message, Object context = null)
    {
        if (!_moduleEnabled.ContainsKey(module) || !_moduleEnabled[module]) return;
        string coloredMessage = $"<color={GetColor(module)}>[{module.ToString()}]</color> {message}";
        if (context != null) Debug.Log(coloredMessage, context);
        else Debug.Log(coloredMessage);
    }

    public static void LogError(LogModule module, string message, Object context = null)
    {
        string coloredMessage = $"<color=red>[{module.ToString()}]</color> {message}";
        if (context != null) Debug.LogError(coloredMessage, context);
        else Debug.LogError(coloredMessage);
    }

    public static void LogWarning(LogModule module, string message, Object context = null)
    {
        string coloredMessage = $"<color=yellow>[{module.ToString()}]</color> {message}";
        if (context != null) Debug.LogWarning(coloredMessage, context);
        else Debug.LogWarning(coloredMessage);
    }

    public static void SetModuleEnabled(LogModule module, bool isEnabled)
    {
        if (_moduleEnabled.ContainsKey(module))
            _moduleEnabled[module] = isEnabled;
        Debug.Log($"<color=grey>Logger: Module {module} is now {(isEnabled ? "ON" : "OFF")}</color>");
    }
}
