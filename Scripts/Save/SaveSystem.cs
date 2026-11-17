using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

[DefaultExecutionOrder(-5000)]
public class SaveSystem : MonoBehaviour
{
    public bool IsLoading { get; private set; }
    [SerializeField] private float autoSaveDelay = 1.5f;
    [SerializeField] private bool logSave = true;
    private bool _autoLoadedThisPlaySession;

    [Inject] private FlagManager _flagManager;
    [Inject] private QuestManager _questManager;
    [Inject] private WorldItemTracker _worldItemTracker;
    [Inject] private LoadingScreen _loadingScreen;
    [Inject] private EventStateManager _eventStateManager;
    [Inject] private DialogueTracker _dialogueTracker;
    [Inject] private CraftingProgress _craftingProgress;

    private Coroutine pendingAutoSave;
    private bool isSaving;

    private string SavePath => GamePaths.GameSaveFile;

    private static T SceneResolve<T>() where T : class
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }

    private InventoryUIManager Inv => SceneResolve<InventoryUIManager>();
    private EquipmentSystem Equip => SceneResolve<EquipmentSystem>();
    private SceneSlideTransition Slide => SceneResolve<SceneSlideTransition>();

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        StartCoroutine(DeferredStart());
    }

    private IEnumerator DeferredStart()
    {
        yield return null;
        yield return null;
        SubscribeEvents();
        TryLoadIfContinueRequested();
    }

    private void SubscribeEvents()
    {
        if (_questManager == null) return;

        _questManager.OnQuestStarted -= HandleQuestChanged;
        _questManager.OnQuestStarted += HandleQuestChanged;
        _questManager.OnQuestCompleted -= HandleQuestChanged;
        _questManager.OnQuestCompleted += HandleQuestChanged;
        _questManager.OnQuestFailed -= HandleQuestChanged;
        _questManager.OnQuestFailed += HandleQuestChanged;
        _questManager.OnObjectiveUpdated -= HandleQuestChanged;
        _questManager.OnObjectiveUpdated += HandleQuestChanged;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.buildIndex == 0) return;
        StartCoroutine(DeferredContinueLoad());
    }

    private void TryLoadIfContinueRequested()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0)
        {
            IsLoading = false;
            return;
        }
        StartCoroutine(DeferredContinueLoad());
    }

    private IEnumerator DeferredContinueLoad()
    {
        if (_autoLoadedThisPlaySession) yield break;
        _autoLoadedThisPlaySession = true;

        yield return null;

        if (PlayerPrefs.GetInt("Continue", 0) == 1)
        {
            PlayerPrefs.DeleteKey("Continue");
            PlayerPrefs.Save();

            string lastProfile = PlayerPrefs.GetString("LastProfile", "Finding_Oneself");
            GamePaths.CurrentProfile = lastProfile;

            LoadGame();
            yield break;
        }

#if UNITY_EDITOR
        if (Application.isEditor &&
            PlayerPrefs.GetInt("NewGameStart", 0) == 0 &&
            HasSave())
        {
            string lastProfile = PlayerPrefs.GetString("LastProfile", "Finding_Oneself");
            GamePaths.CurrentProfile = lastProfile;
            LoadGame();
        }
#endif
    }

    private void HandleQuestChanged(QuestInstance quest) => RequestAutoSave();

    public void RequestAutoSave()
    {
        if (pendingAutoSave != null) StopCoroutine(pendingAutoSave);
        pendingAutoSave = StartCoroutine(AutoSaveRoutine());
    }

    private IEnumerator AutoSaveRoutine()
    {
        yield return new WaitForSeconds(autoSaveDelay);
        pendingAutoSave = null;
        SaveGame();
    }

    public bool LoadGame()
    {
        if (pendingAutoSave != null)
        {
            StopCoroutine(pendingAutoSave);
            pendingAutoSave = null;
        }

        string json = SafeFile.Read(SavePath, out bool fromBackup);

        if (string.IsNullOrEmpty(json))
        {
            Logger.LogWarning(LogModule.Save, $"Файл сохранения не найден: {SavePath}");
            return false;
        }

        try
        {
            var data = JsonConvert.DeserializeObject<SaveData>(json);
            if (data == null) return false;

            ApplySaveData(data);

            if (logSave)
                Logger.Log(LogModule.Save, $"Игра загружена{(fromBackup ? " (из бэкапа)" : "")}");

            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка загрузки: {e.Message}");
            return false;
        }
    }

    public void LoadGameWithScreen() => StartCoroutine(LoadGameWithScreenRoutine());

    private IEnumerator LoadGameWithScreenRoutine()
    {
        if (_loadingScreen != null)
        {
            _loadingScreen.ShowInstant();
            Canvas.ForceUpdateCanvases();
            yield return new WaitForEndOfFrame();
            yield return new WaitForSecondsRealtime(0.2f);
        }

        bool ok = LoadGame();

        yield return new WaitForSecondsRealtime(1f);
        _loadingScreen?.Hide();

        if (!ok) Logger.LogWarning(LogModule.Save, "Загрузка не удалась");
    }

    public IEnumerator LoadGameAsync(System.Action<bool> onComplete = null)
    {
        IsLoading = true;
        bool ok = false;
        try
        {
            ok = LoadGame();
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.Save, $"LoadGameAsync: {e.Message}");
        }
        finally
        {
            IsLoading = false;
        }
        onComplete?.Invoke(ok);
        yield return null;
    }

    public bool HasSave() => File.Exists(SavePath);

    public void DeleteSave()
    {
        try
        {
            if (File.Exists(SavePath)) File.Delete(SavePath);
            if (File.Exists(SavePath + ".bak")) File.Delete(SavePath + ".bak");
            if (File.Exists(SavePath + ".tmp")) File.Delete(SavePath + ".tmp");
            _autoLoadedThisPlaySession = false;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка удаления: {e.Message}");
        }
    }

    private SaveData CollectSaveData(bool captureScreenshot)
    {
        return new SaveData
        {
            version = 2,
            saveDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            saveTime = DateTime.Now.ToString("dd.MM.yyyy HH:mm"),
            screenshotBase64 = captureScreenshot ? CaptureScreenshot() : ReadExistingScreenshot(),
            inventory = CollectInventory(),
            equipment = CollectEquipment(),
            location = CollectLocation(),
            flags = CollectFlags(),
            quests = CollectQuests(),
            worldItems = CollectWorldItems(),
            events = CollectEvents(),
            dialogues = CollectDialogues(),
            crafting = CollectCrafting()
        };
    }

    private string ReadExistingScreenshot()
    {
        try
        {
            if (!File.Exists(SavePath)) return "";
            string json = File.ReadAllText(SavePath);
            if (string.IsNullOrEmpty(json)) return "";

            var match = System.Text.RegularExpressions.Regex.Match(
                json, "\"screenshotBase64\"\\s*:\\s*\"([^\"]*)\"");
            return match.Success ? match.Groups[1].Value : "";
        }
        catch (Exception e)
        {
            Logger.LogVerbose(LogModule.Save, $"Не удалось прочитать существующий скриншот: {e.Message}");
            return "";
        }
    }

    public bool SaveGame() => SaveGame(true);

    public bool SaveGame(bool captureScreenshot)
    {
        if (isSaving) return false;
        isSaving = true;

        try
        {
            var data = CollectSaveData(captureScreenshot);
            if (data == null) return false;

            string json = JsonConvert.SerializeObject(data, Formatting.Indented);

            if (!SanityCheck(data))
            {
                Logger.LogWarning(LogModule.Save, "SanityCheck = false, запись отменена");
                SaveBackups.CreateSnapshot(SavePath, json, "broken");
                return false;
            }

            if (!captureScreenshot)
            {
                string quitBackup = SaveBackups.CreateSnapshot(SavePath, json, "quit");
                if (logSave)
                    Logger.Log(LogModule.Save,
                        $"Quit-сейв → Backups/{(quitBackup != null ? Path.GetFileName(quitBackup) : "FAIL")}. " +
                        $"Основной файл НЕ перезаписан.");
                return true;
            }

            SaveBackups.CreateBackup(SavePath, "pre_save");
            bool ok = SafeFile.Write(SavePath, json);

            if (ok)
            {
                Logger.Log(LogModule.Save,
                    $"Игра сохранена: {SavePath} | " +
                    $"quests(active={data.quests.active.Count}, " +
                    $"completed={data.quests.completed.Count}, " +
                    $"canceled={data.quests.canceled.Count}) | " +
                    $"inventory={data.inventory.entries.Count} | " +
                    $"flags={data.flags.entries.Count}");
            }

            return ok;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка сохранения: {e.Message}");
            return false;
        }
        finally
        {
            isSaving = false;
        }
    }

    private bool SanityCheck(SaveData data)
    {
        if (data == null) return false;
        if (data.version < 1) return false;

        var activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex == 0) return true;

        if (Inv == null)
            Logger.LogWarning(LogModule.Save,
                "SanityCheck: InventoryUIManager недоступен — записываем как есть");

        if (string.IsNullOrEmpty(data.location.sceneName))
            Logger.LogWarning(LogModule.Save,
                "SanityCheck: location.sceneName пустой — записываем как есть");

        return true;
    }

    public bool TryRecoverFromBak()
    {
        string bakPath = SavePath + ".bak";
        if (!File.Exists(bakPath))
        {
            Logger.LogWarning(LogModule.Save, "save.sv.bak не найден — восстанавливать нечего");
            return false;
        }

        try
        {
            File.Copy(bakPath, SavePath, true);
            Logger.Log(LogModule.Save, "Сохранение восстановлено из save.sv.bak");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка восстановления: {e.Message}");
            return false;
        }
    }

    private string CaptureScreenshot()
    {
        if (!Application.isPlaying) return "";

        if (_loadingScreen != null && _loadingScreen.IsVisible) return "";

        var slide = SceneInjector.Instance?.CurrentSceneScope?.Resolve<SceneSlideTransition>();
        if (slide != null && slide.IsTransitioning) return "";

        Texture2D screenTex = null;
        RenderTexture rt = null;
        Texture2D resized = null;

        try
        {
            screenTex = ScreenCapture.CaptureScreenshotAsTexture();
            if (screenTex == null) return "";

            int width = 256;
            int height = Mathf.RoundToInt(width * (float)screenTex.height / screenTex.width);
            rt = RenderTexture.GetTemporary(width, height);
            Graphics.Blit(screenTex, rt);

            resized = new Texture2D(width, height, TextureFormat.RGB24, false);
            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = rt;
            resized.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            resized.Apply();
            RenderTexture.active = previous;

            byte[] bytes = resized.EncodeToPNG();
            return Convert.ToBase64String(bytes);
        }
        catch (Exception e)
        {
            Logger.LogWarning(LogModule.Save, $"Ошибка создания скриншота: {e.Message}");
            return "";
        }
        finally
        {
            if (rt != null) RenderTexture.ReleaseTemporary(rt);
            if (resized != null) UnityEngine.Object.Destroy(resized);
            if (screenTex != null) UnityEngine.Object.Destroy(screenTex);
        }
    }

    private InventoryData CollectInventory()
    {
        var inv = Inv;
        return inv != null ? inv.ExportInventory() : new InventoryData();
    }

    private EquipmentData CollectEquipment()
    {
        var equip = Equip;
        return equip != null ? equip.ExportEquipment() : new EquipmentData();
    }

    private LocationData CollectLocation()
    {
        var data = new LocationData();
        var slide = Slide;
        if (slide == null) return data;

        var current = slide.GetCurrentScene();
        if (current != null) data.sceneName = current.name;

        return data;
    }

    private FlagsData CollectFlags()
    {
        var data = new FlagsData();
        if (_flagManager == null) return data;

        foreach (var kvp in _flagManager.GetAllFlags())
            data.entries.Add(new FlagEntry { name = kvp.Key, value = kvp.Value });

        return data;
    }

    private QuestsData CollectQuests()
    {
        var data = new QuestsData();
        if (_questManager == null)
        {
            Logger.LogWarning(LogModule.Save, "CollectQuests: QuestManager == null");
            return data;
        }

        foreach (var q in _questManager.GetActiveQuests()) data.active.Add(ToQuestSaveEntry(q));
        foreach (var q in _questManager.GetCompletedQuests()) data.completed.Add(ToQuestSaveEntry(q));
        foreach (var q in _questManager.GetCanceledQuests()) data.canceled.Add(ToQuestSaveEntry(q));

        Logger.LogVerbose(LogModule.Save,
            $"CollectQuests: active={data.active.Count}, " +
            $"completed={data.completed.Count}, canceled={data.canceled.Count}");

        return data;
    }

    private QuestSaveEntry ToQuestSaveEntry(QuestInstance quest)
    {
        var entry = new QuestSaveEntry { questId = quest.QuestID };

        foreach (var obj in quest.Objectives)
            entry.objectives.Add(new ObjectiveSaveEntry
            {
                objectiveId = obj.objectiveID,
                currentCount = obj.currentCount
            });

        return entry;
    }

    private WorldItemsData CollectWorldItems()
    {
        var data = new WorldItemsData();
        if (_worldItemTracker != null)
        {
            data.takenItemIds = _worldItemTracker.GetTakenItemIds();
            data.droppedItems = _worldItemTracker.GetDroppedItems();
            data.movedItems = _worldItemTracker.GetMovedItems();
        }
        return data;
    }

    private EventsData CollectEvents()
    {
        var data = new EventsData();
        if (_eventStateManager == null) return data;

        foreach (var kvp in _eventStateManager.ExportState())
        {
            data.entries.Add(new EventStateEntry
            {
                eventId = kvp.Key,
                isExecuted = kvp.Value.isExecuted,
                executionCount = kvp.Value.executionCount,
                lastExecuted = kvp.Value.lastExecuted
            });
        }

        return data;
    }

    private DialoguesData CollectDialogues()
    {
        var data = new DialoguesData();
        if (_dialogueTracker == null) return data;
        data.entries = _dialogueTracker.ExportState();
        return data;
    }

    private CraftingData CollectCrafting()
    {
        var data = new CraftingData();
        if (_craftingProgress == null) return data;
        data.knownRecipeIds = _craftingProgress.ExportState();
        return data;
    }

    private void ApplySaveData(SaveData data)
    {
        ApplyInventory(data.inventory);
        ApplyEquipment(data.equipment);
        ApplyFlags(data.flags);
        ApplyQuests(data.quests);
        ApplyWorldItems(data.worldItems);
        ApplyEvents(data.events);
        ApplyDialogues(data.dialogues);
        ApplyCrafting(data.crafting);
        ApplyLocation(data.location);
    }

    private void ApplyInventory(InventoryData data) => Inv?.ImportInventory(data);

    private void ApplyEquipment(EquipmentData data) => Equip?.ImportEquipment(data);

    private void ApplyFlags(FlagsData data)
    {
        if (_flagManager == null) return;
        _flagManager.ClearAllFlags();
        if (data == null) return;
        foreach (var entry in data.entries) _flagManager.SetFlag(entry.name, entry.value);
    }

    private void ApplyQuests(QuestsData data)
    {
        if (_questManager == null) return;

        _questManager.ClearAllQuests();
        if (data == null) return;

        int activeOk = 0, completedOk = 0, canceledOk = 0;

        foreach (var entry in data.active)
            if (_questManager.RestoreQuestState(entry.questId, false, false, ToProgressDict(entry)) != null) activeOk++;
        foreach (var entry in data.completed)
            if (_questManager.RestoreQuestState(entry.questId, true, false, ToProgressDict(entry)) != null) completedOk++;
        foreach (var entry in data.canceled)
            if (_questManager.RestoreQuestState(entry.questId, false, true, ToProgressDict(entry)) != null) canceledOk++;

        Logger.Log(LogModule.Save,
            $"ApplyQuests: " +
            $"active={activeOk}/{data.active.Count}, " +
            $"completed={completedOk}/{data.completed.Count}, " +
            $"canceled={canceledOk}/{data.canceled.Count}");

        _questManager.StartAutoQuests();
    }

    private Dictionary<string, int> ToProgressDict(QuestSaveEntry entry)
    {
        var dict = new Dictionary<string, int>();
        if (entry?.objectives == null) return dict;
        foreach (var obj in entry.objectives) dict[obj.objectiveId] = obj.currentCount;
        return dict;
    }

    private void ApplyWorldItems(WorldItemsData data)
    {
        _worldItemTracker?.SetData(
            data?.takenItemIds ?? new List<string>(),
            data?.droppedItems ?? new List<DroppedItemData>(),
            data?.movedItems ?? new List<MovedItemData>());
    }

    private void ApplyEvents(EventsData data)
    {
        if (_eventStateManager == null) return;

        var dict = new Dictionary<string, EventState>();
        if (data?.entries != null)
        {
            foreach (var e in data.entries)
            {
                if (e == null || string.IsNullOrEmpty(e.eventId)) continue;
                dict[e.eventId] = new EventState
                {
                    isExecuted = e.isExecuted,
                    executionCount = e.executionCount,
                    lastExecuted = e.lastExecuted ?? ""
                };
            }
        }

        _eventStateManager.ImportState(dict);
    }

    private void ApplyDialogues(DialoguesData data) => _dialogueTracker?.ImportState(data?.entries);

    private void ApplyCrafting(CraftingData data) => _craftingProgress?.ImportState(data?.knownRecipeIds);

    private void ApplyLocation(LocationData data)
    {
        if (data == null || string.IsNullOrEmpty(data.sceneName)) return;

        var slide = Slide;
        if (slide == null)
        {
            Logger.LogWarning(LogModule.Save, "SceneSlideTransition не найден — локация не восстановлена");
            return;
        }

        slide.SetCurrentSceneInstant(data.sceneName);
    }
}