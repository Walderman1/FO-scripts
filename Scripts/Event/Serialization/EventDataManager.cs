using UnityEngine;
using System.IO;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EventDataManager : MonoBehaviour
{
    private static EventDataManager _instance;
    public static EventDataManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EventDataManager>();
                if (_instance == null)
                {
                    Logger.LogError(LogModule.Event, "EventDataManager не найден на сцене");
                    return null;
                }
            }
            return _instance;
        }
    }

    [Header("=== ОСНОВНЫЕ НАСТРОЙКИ ===")]
    [SerializeField] private string eventsFolder = "EventsData";

    [Header("=== НАСТРОЙКИ АВТОСОХРАНЕНИЯ ===")]
    [Tooltip("Включить автоматическое сохранение при входе в Play Mode")]
    [SerializeField] private bool autoSaveOnPlay = true;

    [Tooltip("Включить автоматическое сохранение при выходе из Play Mode")]
    [SerializeField] private bool autoSaveOnExitPlay = true;

    [Tooltip("Включить автоматическое сохранение при компиляции")]
    [SerializeField] private bool autoSaveOnCompile = true;

    [Tooltip("Включить автоматическое сохранение при сохранении сцены")]
    [SerializeField] private bool autoSaveOnSceneSave = true;

    [Tooltip("Включить автоматическое сохранение при закрытии Unity")]
    [SerializeField] private bool autoSaveOnQuit = true;

    [Tooltip("Интервал автосохранения в секундах (0 = отключено)")]
    [SerializeField] private float autoSaveInterval = 0f;

    [Tooltip("Создавать бэкапы при сохранении")]
    [SerializeField] private bool createBackups = true;

    [Tooltip("Максимальное количество бэкапов на событие")]
    [SerializeField] private int maxBackups = 5;

    [Header("=== ОТЛАДКА ===")]
    [SerializeField] private bool logAutoSave = true;

    private Dictionary<string, EventData> eventDataCache = new Dictionary<string, EventData>();
    private string dataPath;
    private string backupPath;
    private bool isInitialized = false;
    private float lastAutoSaveTime = 0f;
    private bool isSaving = false;

#if UNITY_EDITOR
    private bool isCompiling = false;
#endif

    public bool AutoSaveOnPlay
    {
        get => autoSaveOnPlay;
        set => autoSaveOnPlay = value;
    }

    public bool AutoSaveOnExitPlay
    {
        get => autoSaveOnExitPlay;
        set => autoSaveOnExitPlay = value;
    }

    public bool AutoSaveOnCompile
    {
        get => autoSaveOnCompile;
        set => autoSaveOnCompile = value;
    }

    public bool AutoSaveOnSceneSave
    {
        get => autoSaveOnSceneSave;
        set => autoSaveOnSceneSave = value;
    }

    public bool AutoSaveOnQuit
    {
        get => autoSaveOnQuit;
        set => autoSaveOnQuit = value;
    }

    public float AutoSaveInterval
    {
        get => autoSaveInterval;
        set => autoSaveInterval = value;
    }

    public bool CreateBackups
    {
        get => createBackups;
        set => createBackups = value;
    }

    public bool LogAutoSave
    {
        get => logAutoSave;
        set => logAutoSave = value;
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
        Logger.Log(LogModule.Event, "EventDataManager инициализирован");

        Initialize();

#if UNITY_EDITOR
        SetupAutoSaveListeners();
#endif
    }

    private void Update()
    {
        if (autoSaveInterval > 0f && !isSaving && !Application.isPlaying)
        {
            if (Time.unscaledTime - lastAutoSaveTime >= autoSaveInterval)
            {
                AutoSave("Interval");
                lastAutoSaveTime = Time.unscaledTime;
            }
        }
    }

#if UNITY_EDITOR
    private void SetupAutoSaveListeners()
    {
        if (autoSaveOnCompile)
        {
            UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;
            UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
        }

        if (autoSaveOnSceneSave)
        {
            UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
        }

        if (autoSaveOnPlay || autoSaveOnExitPlay)
        {
            UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }
    }

    private void OnCompilationStarted(object obj)
    {
        if (autoSaveOnCompile && !isSaving && !Application.isPlaying)
        {
            isCompiling = true;
            AutoSave("Compile");
        }
    }

    private void OnCompilationFinished(object obj)
    {
        isCompiling = false;
    }

    private void OnSceneSaving(UnityEngine.SceneManagement.Scene scene, string path)
    {
        if (autoSaveOnSceneSave && !isSaving && !Application.isPlaying)
        {
            AutoSave("SceneSave");
        }
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            if (autoSaveOnPlay && !isSaving && !Application.isPlaying)
            {
                AutoSave("PlayModeEnter");
            }
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            if (autoSaveOnExitPlay && !isSaving)
            {
                AutoSave("PlayModeExit");
            }
        }
    }

    private void OnApplicationQuit()
    {
        if (autoSaveOnQuit && !isSaving)
        {
            AutoSave("Quit");
        }

        UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilationStarted;
        UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
    }
#endif

    private void AutoSave(string reason)
    {
        if (isSaving) return;
        if (!logAutoSave) return;

#if UNITY_EDITOR
        if (!UnityEditor.EditorApplication.isPlaying && !UnityEditor.EditorApplication.isCompiling)
        {
            var eventManager = Object.FindObjectOfType<EventManager>();
            if (eventManager != null && eventManager.registeredEvents.Count > 0)
            {
                if (logAutoSave)
                    Logger.Log(LogModule.Event, $"Автосохранение событий ({reason})...");

                isSaving = true;
                try
                {
                    eventManager.SaveAllEventsFromProject();
                    if (logAutoSave)
                        Logger.Log(LogModule.Event, $"Автосохранение завершено ({reason})");
                }
                catch (System.Exception e)
                {
                    Logger.LogError(LogModule.Event, $"Ошибка автосохранения: {e.Message}");
                }
                finally
                {
                    isSaving = false;
                }
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
        autoSaveInterval = 0f;
        logAutoSave = false;

#if UNITY_EDITOR
        UnityEditor.Compilation.CompilationPipeline.compilationStarted -= OnCompilationStarted;
        UnityEditor.Compilation.CompilationPipeline.compilationFinished -= OnCompilationFinished;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving -= OnSceneSaving;
        UnityEditor.EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
#endif

        Logger.Log(LogModule.Event, "ВСЕ автосохранения ОТКЛЮЧЕНЫ");
    }

    public void EnableAllAutoSaves()
    {
        autoSaveOnPlay = true;
        autoSaveOnExitPlay = true;
        autoSaveOnCompile = true;
        autoSaveOnSceneSave = true;
        autoSaveOnQuit = true;
        autoSaveInterval = 60f;
        logAutoSave = true;

#if UNITY_EDITOR
        UnityEditor.Compilation.CompilationPipeline.compilationStarted += OnCompilationStarted;
        UnityEditor.Compilation.CompilationPipeline.compilationFinished += OnCompilationFinished;
        UnityEditor.SceneManagement.EditorSceneManager.sceneSaving += OnSceneSaving;
        UnityEditor.EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
#endif

        Logger.Log(LogModule.Event, "ВСЕ автосохранения ВКЛЮЧЕНЫ");
    }

    public bool IsAnyAutoSaveEnabled()
    {
        return autoSaveOnPlay || autoSaveOnExitPlay || autoSaveOnCompile ||
               autoSaveOnSceneSave || autoSaveOnQuit || autoSaveInterval > 0f;
    }

    public void Initialize()
    {
        if (isInitialized) return;

#if UNITY_EDITOR
        dataPath = Path.Combine(Application.dataPath, eventsFolder);
#else
        dataPath = Path.Combine(Application.persistentDataPath, eventsFolder);
#endif

        if (!Directory.Exists(dataPath))
        {
            Directory.CreateDirectory(dataPath);
            Logger.Log(LogModule.Event, $"Создана папка событий: {dataPath}");
        }
        else
        {
            Logger.Log(LogModule.Event, $"Папка событий существует: {dataPath}");
        }

        backupPath = Path.Combine(dataPath, "Backups");
        if (createBackups && !Directory.Exists(backupPath))
        {
            Directory.CreateDirectory(backupPath);
            Logger.Log(LogModule.Event, $"Создана папка резервных копий: {backupPath}");
        }

        isInitialized = true;
        lastAutoSaveTime = Time.unscaledTime;
    }

    public void SaveEvent(EventData eventData, bool forceSave = false)
    {
        if (!isInitialized) Initialize();

        if (string.IsNullOrEmpty(eventData.eventID))
        {
            Logger.LogWarning(LogModule.Event, "Невозможно сохранить событие без ID");
            return;
        }

        string filePath = Path.Combine(dataPath, $"{eventData.eventID}.json");

        if (File.Exists(filePath) && !forceSave)
        {
            if (eventDataCache.TryGetValue(eventData.eventID, out EventData existingData))
            {
                if (eventData.actions.Count == 0 && existingData.actions.Count > 0)
                {
                    Logger.LogWarning(LogModule.Event, $"Защита: Пропущено сохранение '{eventData.eventID}' - новое событие не содержит действий, а старое содержит {existingData.actions.Count} действий");
                    return;
                }
            }
            else
            {
                try
                {
                    string existingJson = File.ReadAllText(filePath);
                    EventData existingData2 = JsonUtility.FromJson<EventData>(existingJson);
                    if (existingData2 != null && existingData2.actions.Count > 0 && eventData.actions.Count == 0)
                    {
                        Logger.LogWarning(LogModule.Event, $"Защита: Пропущено сохранение '{eventData.eventID}' - новое событие не содержит действий, а существующее содержит {existingData2.actions.Count} действий");
                        return;
                    }
                }
                catch { }
            }
        }

        if (createBackups && File.Exists(filePath) && forceSave)
        {
            CreateBackup(eventData.eventID);
        }

        string json = JsonUtility.ToJson(eventData, true);
        File.WriteAllText(filePath, json);

        eventDataCache[eventData.eventID] = eventData;

        Logger.Log(LogModule.Event, $"Событие '{eventData.eventID}' сохранено в: {filePath}");
    }

    public void CreateBackup(string eventID)
    {
        if (!createBackups) return;

        string sourcePath = Path.Combine(dataPath, $"{eventID}.json");
        if (!File.Exists(sourcePath)) return;

        string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        string backupFile = Path.Combine(backupPath, $"{eventID}_{timestamp}.json");

        try
        {
            File.Copy(sourcePath, backupFile, true);
            Logger.Log(LogModule.Event, $"Создана резервная копия: {backupFile}");

            CleanupOldBackups(eventID);
        }
        catch (System.Exception e)
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

            System.Array.Sort(backupFiles, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));

            int toDelete = backupFiles.Length - maxBackups;
            for (int i = 0; i < toDelete; i++)
            {
                File.Delete(backupFiles[i]);
                Logger.Log(LogModule.Event, $"Удалена старая резервная копия: {Path.GetFileName(backupFiles[i])}");
            }
        }
        catch (System.Exception e)
        {
            Logger.LogWarning(LogModule.Event, $"Ошибка очистки резервных копий: {e.Message}");
        }
    }

    public bool RestoreFromBackup(string eventID, string backupFileName = null)
    {
        if (!createBackups) return false;

        string[] backupFiles;
        if (string.IsNullOrEmpty(backupFileName))
        {
            backupFiles = Directory.GetFiles(backupPath, $"{eventID}_*.json");
            if (backupFiles.Length == 0)
            {
                Logger.LogWarning(LogModule.Event, $"Нет резервных копий для {eventID}");
                return false;
            }

            System.Array.Sort(backupFiles, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
            backupFileName = backupFiles[0];
        }
        else
        {
            backupFileName = Path.Combine(backupPath, backupFileName);
            if (!File.Exists(backupFileName))
            {
                Logger.LogWarning(LogModule.Event, $"Файл резервной копии не найден: {backupFileName}");
                return false;
            }
        }

        try
        {
            string json = File.ReadAllText(backupFileName);
            EventData data = JsonUtility.FromJson<EventData>(json);
            if (data != null)
            {
                string targetPath = Path.Combine(dataPath, $"{eventID}.json");
                File.Copy(backupFileName, targetPath, true);
                eventDataCache[eventID] = data;
                Logger.Log(LogModule.Event, $"Восстановлен {eventID} из резервной копии: {Path.GetFileName(backupFileName)}");
                return true;
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.Event, $"Ошибка восстановления из резервной копии: {e.Message}");
        }

        return false;
    }

    public EventData LoadEvent(string eventID)
    {
        if (!isInitialized) Initialize();

        if (eventDataCache.TryGetValue(eventID, out EventData cached))
        {
            return cached;
        }

        string filePath = Path.Combine(dataPath, $"{eventID}.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            EventData data = JsonUtility.FromJson<EventData>(json);
            if (data != null)
            {
                eventDataCache[eventID] = data;
                return data;
            }
        }

        return null;
    }

    public List<EventData> LoadAllEvents()
    {
        if (!isInitialized) Initialize();

        eventDataCache.Clear();

        if (!Directory.Exists(dataPath))
        {
            Logger.LogWarning(LogModule.Event, $"Директория не найдена: {dataPath}");
            return new List<EventData>();
        }

        string[] files = Directory.GetFiles(dataPath, "*.json");
        List<EventData> events = new List<EventData>();

        Logger.Log(LogModule.Event, $"Найдено {files.Length} JSON файлов в {dataPath}");

        foreach (string file in files)
        {
            if (file.Contains("/Backups/") || file.Contains("\\Backups\\"))
                continue;

            try
            {
                string json = File.ReadAllText(file);
                EventData data = JsonUtility.FromJson<EventData>(json);
                if (data != null && !string.IsNullOrEmpty(data.eventID))
                {
                    eventDataCache[data.eventID] = data;
                    events.Add(data);
                    Logger.Log(LogModule.Event, $"Загружено: {data.eventID} с {data.actions.Count} действиями");
                }
                else
                {
                    Logger.LogWarning(LogModule.Event, $"Некорректные данные в: {Path.GetFileName(file)}");
                }
            }
            catch (System.Exception e)
            {
                Logger.LogError(LogModule.Event, $"Ошибка загрузки {file}: {e.Message}");
            }
        }

        Logger.Log(LogModule.Event, $"Загружено {events.Count} событий из JSON");
        return events;
    }

    public int RestoreEventsToSO()
    {
#if UNITY_EDITOR
        var events = LoadAllEvents();
        int restoredCount = 0;

        string[] guids = AssetDatabase.FindAssets("t:GameEvent");
        Dictionary<string, string> eventPaths = new Dictionary<string, string>();
        Dictionary<string, GameEvent> eventAssets = new Dictionary<string, GameEvent>();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameEvent evt = AssetDatabase.LoadAssetAtPath<GameEvent>(path);
            if (evt != null && !string.IsNullOrEmpty(evt.eventID))
            {
                eventPaths[evt.eventID] = path;
                eventAssets[evt.eventID] = evt;
            }
        }

        foreach (var data in events)
        {
            if (eventAssets.TryGetValue(data.eventID, out GameEvent evt))
            {
                EventConverter.RestoreToExistingGameEvent(evt, data);
                EditorUtility.SetDirty(evt);
                restoredCount++;
                Logger.Log(LogModule.Event, $"Восстановлено: {data.eventID} с {data.actions.Count} действиями");
            }
            else
            {
                GameEvent newEvent = EventConverter.ToGameEvent(data);
                if (newEvent != null)
                {
                    string newPath = $"Assets/Resources/Events/{data.eventID}.asset";
                    string dir = Path.GetDirectoryName(newPath);
                    if (!Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    AssetDatabase.CreateAsset(newEvent, newPath);
                    restoredCount++;
                    Logger.Log(LogModule.Event, $"Создано новое событие: {data.eventID}");
                }
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Logger.Log(LogModule.Event, $"Восстановлено {restoredCount} событий в SO файлы");
        return restoredCount;
#else
        Logger.LogWarning(LogModule.Event, "RestoreEventsToSO доступно только в редакторе");
        return 0;
#endif
    }

    public void DeleteEvent(string eventID)
    {
        if (!isInitialized) Initialize();

        eventDataCache.Remove(eventID);

        string filePath = Path.Combine(dataPath, $"{eventID}.json");
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            Logger.Log(LogModule.Event, $"Событие '{eventID}' удалено");
        }
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
        if (string.IsNullOrEmpty(eventID))
        {
            files = Directory.GetFiles(backupPath, "*.json");
        }
        else
        {
            files = Directory.GetFiles(backupPath, $"{eventID}_*.json");
        }

        return new List<string>(files);
    }
}
