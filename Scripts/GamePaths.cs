using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GamePaths
{
    public const string SaveRootFolder = "Save";
    public const string SettingsSubFolder = "Settings";
    public const string DialogueDataSubFolder = "DialogueData";
    public const string EventDataSubFolder = "EventData";
    public const string CraftDataSubFolder = "CraftData";
    public const string GameSaveSubFolder = "Game";
    public const string SettingsFileName = "settings.json";
    public const string DialogueStatusesFileName = "dialogue_statuses.json";
    public const string EventStatusesFileName = "event_statuses.json";
    public const string CraftDataFileName = "CraftData.json";
    public const string GameSaveFileName = "save.sv";

    public static string CurrentProfile { get; set; } = "Finding_Oneself";

    public static string ProjectRoot => Path.GetFullPath(Path.Combine(Application.dataPath, ".."));

    public static string SaveRoot
    {
        get
        {
#if UNITY_EDITOR
            string basePath = ProjectRoot;
#else
            string basePath = Application.persistentDataPath;
            if (string.IsNullOrEmpty(basePath))
                basePath = Application.temporaryCachePath;
#endif
            return Path.Combine(basePath, SaveRootFolder);
        }
    }

    public static string ProfileRoot => Path.Combine(SaveRoot, CurrentProfile);

    public static string GetSaveFolder(string subFolder, bool ensureExists = true)
    {
        string root = ProfileRoot;
        if (string.IsNullOrEmpty(root))
        {
            Logger.LogError(LogModule.Save, "ProfileRoot пуст");
            return null;
        }

        string path = string.IsNullOrEmpty(subFolder) ? root : Path.Combine(root, subFolder);

        if (ensureExists && !Directory.Exists(path))
        {
            try { Directory.CreateDirectory(path); }
            catch (System.Exception e)
            {
                Logger.LogError(LogModule.Save, $"Не удалось создать папку '{path}': {e.Message}");
                return null;
            }
        }
        return path;
    }

    public static string GetSavePath(string subFolder, string fileName, bool ensureFolder = true)
    {
        string folder = GetSaveFolder(subFolder, ensureFolder);
        if (string.IsNullOrEmpty(folder)) return null;
        return Path.Combine(folder, fileName);
    }

    public static string SettingsFile => Path.Combine(SaveRoot, SettingsSubFolder, SettingsFileName);
    public static string DialogueStatusesFile => GetSavePath(DialogueDataSubFolder, DialogueStatusesFileName);
    public static string EventStatusesFile => GetSavePath(EventDataSubFolder, EventStatusesFileName);
    public static string CraftDataFile => GetSavePath(CraftDataSubFolder, CraftDataFileName);
    public static string GameSaveFile => GetSavePath(GameSaveSubFolder, GameSaveFileName);

    public static string EditorEventsFolder => Path.Combine(Application.dataPath, "EventsData");

    public static bool SaveFileExists(string subFolder, string fileName)
    {
        string path = GetSavePath(subFolder, fileName, ensureFolder: false);
        if (string.IsNullOrEmpty(path)) return false;
        return File.Exists(path);
    }

    public static List<string> GetAllProfiles(bool onlyWithSave = false)
    {
        var profiles = new List<string>();
        string saveRoot = SaveRoot;
        if (!Directory.Exists(saveRoot)) return profiles;

        foreach (var dir in Directory.GetDirectories(saveRoot))
        {
            string name = Path.GetFileName(dir);
            if (name == SettingsSubFolder) continue;

            if (onlyWithSave)
            {
                string savePath = Path.Combine(dir, GameSaveSubFolder, GameSaveFileName);
                if (!File.Exists(savePath)) continue;
            }
            profiles.Add(name);
        }
        return profiles;
    }

    public static string GetNextProfileName()
    {
        int i = 1;
        while (Directory.Exists(Path.Combine(SaveRoot, $"Save{i}")))
            i++;
        return $"Save{i}";
    }

    public static bool HasAnySave()
    {
        return GetAllProfiles(onlyWithSave: true).Count > 0;
    }
}