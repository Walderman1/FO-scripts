using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-9200)]
public class EventDataManager : MonoBehaviour
{
    [Header("Основные настройки")]
    [SerializeField] private string eventsFolder = "EventsData";

    [Header("Настройки автосохранения")]
    [SerializeField] private bool autoSaveOnPlay = false;
    [SerializeField] private bool autoSaveOnExitPlay = false;
    [SerializeField] private bool autoSaveOnCompile = false;
    [SerializeField] private bool autoSaveOnSceneSave = false;
    [SerializeField] private bool autoSaveOnQuit = false;
    [SerializeField] private bool createBackups = true;
    [SerializeField] private int maxBackups = 5;

    [Header("Отладка")]
    [SerializeField] private bool logAutoSave = true;

    private Dictionary<string, EventData> eventDataCache = new Dictionary<string, EventData>();
    private string dataPath;
    private string backupPath;
    private bool isInitialized = false;
    private bool isSaving = false;

#if UNITY_EDITOR
    private bool listenersSubscribed = false;
#endif

    public bool AutoSaveOnPlay { get => autoSaveOnPlay; set => autoSaveOnPlay = value; }
    public bool AutoSaveOnExitPlay { get => autoSaveOnExitPlay; set => autoSaveOnExitPlay = value; }
    public bool AutoSaveOnCompile { get => autoSaveOnCompile; set => autoSaveOnCompile = value; }
    public bool AutoSaveOnSceneSave { get => autoSaveOnSceneSave; set => autoSaveOnSceneSave = value; }
    public bool AutoSaveOnQuit { get => autoSaveOnQuit; set => autoSaveOnQuit = value; }
    public bool CreateBackups { get => createBackups; set => createBackups = value; }
    public bool LogAutoSave { get => logAutoSave; set => logAutoSave = value; }

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        Initialize();

#if UNITY_EDITOR
        SetupAutoSaveListeners();
#endif
    }

    private void OnDestroy()
    {
#if UNITY_EDITOR
        UnsubscribeAutoSaveListeners();
#endif
    }

#if UNITY_EDITOR
    private void SetupAutoSaveListeners()
    {
        if (listenersSubscribed) return;

        if (autoSaveOnCompile)
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;

        if (autoSaveOnSceneSave)
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;

        if (autoSaveOnPlay || autoSaveOnExitPlay)
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        listenersSubscribed = true;
    }

    private void UnsubscribeAutoSaveListeners()
    {
        if (!listenersSubscribed) return;

        UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilationStarted;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        listenersSubscribed = false;
    }

    private void OnCompilationStarted(object obj)
    {
        if (autoSaveOnCompile && !isSaving && !Application.isPlaying)
            AutoSave("Compile");
    }

    private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (autoSaveOnSceneSave && !isSaving && !Application.isPlaying)
            AutoSave("SceneSave");
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (autoSaveOnPlay && !isSaving && !Application.isPlaying)
                AutoSave("PlayModeEnter");
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (autoSaveOnExitPlay && !isSaving)
                AutoSave("PlayModeExit");
        }
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit && !isSaving)
            AutoSave("Quit");
    }
#endif

    private void AutoSave(string reason)
    {
        if (isSaving) return;

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isCompiling)
        {
            var eventManager = FindAnyObjectByType<EventManager>(FindObjectsInactive.Include);
            if (eventManager != null && eventManager.registeredEvents.Count > 0)
            {
                isSaving = true;
                try { eventManager.SaveAllEventsFromProject(); }
                catch (Exception e) { Logger.LogError(LogModule.Event, $"Ошибка автосохранения ({reason}): {e.Message}"); }
                finally { isSaving = false; }
            }
        }
#endif
    }

    public void DisableAllAutoSaves()
    {
        autoSaveOnPlay = false;
        autoSaveOnExitPlay = false;
        autoSaveOnCompile = false;
        autoSaveOnSceneSave = false;
        autoSaveOnQuit = false;
        logAutoSave = false;

#if UNITY_EDITOR
        UnsubscribeAutoSaveListeners();
#endif
    }

    public void EnableAllAutoSaves()
    {
        autoSaveOnPlay = true;
        autoSaveOnExitPlay = true;
        autoSaveOnCompile = true;
        autoSaveOnSceneSave = true;
        autoSaveOnQuit = true;
        logAutoSave = true;

#if UNITY_EDITOR
        SetupAutoSaveListeners();
#endif
    }

    public bool IsAnyAutoSaveEnabled()
    {
        return autoSaveOnPlay || autoSaveOnExitPlay || autoSaveOnCompile ||
               autoSaveOnSceneSave || autoSaveOnQuit;
    }

    public void Initialize()
    {
        if (isInitialized) return;

#if UNITY_EDITOR
        dataPath = GamePaths.EditorEventsFolder;
#else
        dataPath = Path.Combine(Application.persistentDataPath, eventsFolder);
#endif

        if (!Directory.Exists(dataPath))
        {
            try { Directory.CreateDirectory(dataPath); }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Event, $"Не удалось создать папку событий '{dataPath}': {e.Message}");
                return;
            }
        }

        backupPath = Path.Combine(dataPath, "Backups");
        if (createBackups && !Directory.Exists(backupPath))
        {
            try { Directory.CreateDirectory(backupPath); }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Event, $"Не удалось создать папку резервных копий '{backupPath}': {e.Message}");
                return;
            }
        }

        isInitialized = true;
    }

    public void SaveEvent(EventData eventData, bool forceSave = false)
    {
        if (!isInitialized) Initialize();
        if (eventData == null || string.IsNullOrEmpty(eventData.eventID)) return;

        string filePath = Path.Combine(dataPath, $"{eventData.eventID}.json");

        if (File.Exists(filePath) && !forceSave)
        {
            if (eventDataCache.TryGetValue(eventData.eventID, out EventData existingData))
            {
                if (eventData.actions != null && existingData.actions != null
                    && eventData.actions.Count == 0 && existingData.actions.Count > 0) return;
            }
            else
            {
                try
                {
                    string existingJson = File.ReadAllText(filePath);
                    EventData existingData2 = JsonConvert.DeserializeObject<EventData>(existingJson, EventSerialization.Settings);
                    if (existingData2 != null && existingData2.actions != null
                        && existingData2.actions.Count > 0
                        && (eventData.actions == null || eventData.actions.Count == 0)) return;
                }
                catch { }
            }
        }

        if (createBackups && File.Exists(filePath) && forceSave)
            CreateBackup(eventData.eventID);

        string json = JsonConvert.SerializeObject(eventData, EventSerialization.Settings);

        if (SafeFile.Write(filePath, json))
        {
            eventDataCache[eventData.eventID] = eventData;
            Logger.Log(LogModule.Event, $"Событие '{eventData.eventID}' сохранено в: {filePath}");
        }
    }

    public void CreateBackup(string eventID)
    {
        if (!createBackups) return;

        string sourcePath = Path.Combine(dataPath, $"{eventID}.json");
        if (!File.Exists(sourcePath)) return;

        if (!Directory.Exists(backupPath))
        {
            try { Directory.CreateDirectory(backupPath); }
            catch { return; }
        }

        string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string backupFile = Path.Combine(backupPath, $"{eventID}_{timestamp}.json");

        try
        {
            File.Copy(sourcePath, backupFile, true);
            CleanupOldBackups(eventID);
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Event, $"Ошибка создания резервной копии: {e.Message}");
        }
    }

    private void CleanupOldBackups(string eventID)
    {
        try
        {
            string[] backupFiles = Directory.GetFiles(backupPath, $"{eventID}_*.json");
            if (backupFiles.Length <= maxBackups) return;

            Array.Sort(backupFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

            int toDelete = backupFiles.Length - maxBackups;
            for (int i = 0; i < toDelete; i++)
                File.Delete(backupFiles[i]);
        }
        catch { }
    }

    public bool RestoreFromBackup(string eventID, string backupFileName = null)
    {
        if (!createBackups) return false;

        string[] backupFiles;
        if (string.IsNullOrEmpty(backupFileName))
        {
            if (!Directory.Exists(backupPath)) return false;
            backupFiles = Directory.GetFiles(backupPath, $"{eventID}_*.json");
            if (backupFiles.Length == 0) return false;

            Array.Sort(backupFiles, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            backupFileName = backupFiles[0];
        }
        else
        {
            backupFileName = Path.Combine(backupPath, backupFileName);
            if (!File.Exists(backupFileName)) return false;
        }

        try
        {
            string json = File.ReadAllText(backupFileName);
            EventData data = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);
            if (data != null)
            {
                string targetPath = Path.Combine(dataPath, $"{eventID}.json");
                File.Copy(backupFileName, targetPath, true);
                eventDataCache[eventID] = data;
                return true;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Event, $"Ошибка восстановления из резервной копии: {e.Message}");
        }

        return false;
    }

    public EventData LoadEvent(string eventID)
    {
        if (!isInitialized) Initialize();

        if (eventDataCache.TryGetValue(eventID, out EventData cached)) return cached;

        string filePath = Path.Combine(dataPath, $"{eventID}.json");
        string json = SafeFile.Read(filePath, out _);

        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            EventData data = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);
            if (data != null)
            {
                eventDataCache[eventID] = data;
                return data;
            }
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Event, $"Ошибка разбора JSON события '{eventID}': {e.Message}");
        }

        return null;
    }

    public List<EventData> LoadAllEvents()
    {
        if (!isInitialized) Initialize();

        eventDataCache.Clear();
        List<EventData> events = new List<EventData>();

        TextAsset[] jsonAssets = Resources.LoadAll<TextAsset>(eventsFolder);
        if (jsonAssets.Length > 0)
        {
            foreach (TextAsset asset in jsonAssets)
            {
                try
                {
                    EventData data = JsonConvert.DeserializeObject<EventData>(asset.text, EventSerialization.Settings);
                    if (data != null && !string.IsNullOrEmpty(data.eventID))
                    {
                        eventDataCache[data.eventID] = data;
                        events.Add(data);
                    }
                }
                catch { }
            }

            if (events.Count > 0) return events;
        }

        if (!Directory.Exists(dataPath)) return events;

        string[] files = Directory.GetFiles(dataPath, "*.json");

        foreach (string file in files)
        {
            if (file.Contains("/Backups/") || file.Contains("\\Backups\\")) continue;

            try
            {
                string json = File.ReadAllText(file);
                EventData data = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);
                if (data != null && !string.IsNullOrEmpty(data.eventID))
                {
                    eventDataCache[data.eventID] = data;
                    events.Add(data);
                }
            }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Event, $"Ошибка загрузки {file}: {e.Message}");
            }
        }

        return events;
    }

    public int RestoreEventsToSO()
    {
#if UNITY_EDITOR
        var events = LoadAllEvents();
        int restoredCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:GameEvent");
        Dictionary<string, GameEvent> eventAssets = new Dictionary<string, GameEvent>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameEvent evt = AssetDatabase.LoadAssetAtPath<GameEvent>(path);
            if (evt != null && !string.IsNullOrEmpty(evt.eventID))
                eventAssets[evt.eventID] = evt;
        }

        foreach (var data in events)
        {
            if (eventAssets.TryGetValue(data.eventID, out GameEvent evt))
            {
                EventConverter.RestoreToExistingGameEvent(evt, data);
                EditorUtility.SetDirty(evt);
                restoredCount++;
            }
            else
            {
                GameEvent newEvent = EventConverter.ToGameEvent(data);
                if (newEvent != null)
                {
                    string newPath = $"Assets/Resources/Events/{data.eventID}.asset";
                    string dir = Path.GetDirectoryName(newPath);
                    if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                    AssetDatabase.CreateAsset(newEvent, newPath);
                    restoredCount++;
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        return restoredCount;
#else
        return 0;
#endif
    }

    public void DeleteEvent(string eventID)
    {
        if (!isInitialized) Initialize();

        eventDataCache.Remove(eventID);

        string filePath = Path.Combine(dataPath, $"{eventID}.json");
        if (File.Exists(filePath)) File.Delete(filePath);
    }

    public List<EventData> GetAllEvents()
    {
        if (!isInitialized) Initialize();
        return new List<EventData>(eventDataCache.Values);
    }

    public string GetEventsFolderPath()
    {
        if (!isInitialized) Initialize();
        return dataPath;
    }

    public bool EventExists(string eventID)
    {
        if (!isInitialized) Initialize();
        return eventDataCache.ContainsKey(eventID) || File.Exists(Path.Combine(dataPath, $"{eventID}.json"));
    }

    public List<string> GetBackupFiles(string eventID = null)
    {
        if (!isInitialized) Initialize();
        if (!Directory.Exists(backupPath)) return new List<string>();

        string[] files;
        if (string.IsNullOrEmpty(eventID)) files = Directory.GetFiles(backupPath, "*.json");
        else files = Directory.GetFiles(backupPath, $"{eventID}_*.json");

        return new List<string>(files);
    }
}