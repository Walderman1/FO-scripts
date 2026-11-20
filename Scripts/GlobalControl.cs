using DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VContainer;

[DefaultExecutionOrder(-5000)]
public class GlobalControl : MonoBehaviour
{
    [Header("Audio Settings")]
    public AudioSource audioSource;
    public AudioClip detectionClip;

    [Header("UI Elements")]
    public Button continueButton;

    [Header("Visual Effects")]
    public Animator transitionAnimator;

    [Header("Game Objects")]
    [SerializeField] private LayerMask targetLayer;

    [Inject] private SettingsManager _settingsManager;
    [Inject] private EventManager _eventManager;
    [Inject] private EventStateManager _eventStateManager;
    [Inject] private DialogueTracker _dialogueTracker;
    [Inject] private FlagManager _flagManager;
    [Inject] private QuestManager _questManager;
    [Inject] private CraftingProgress _craftingProgress;
    [Inject] private WorldItemTracker _worldItemTracker;
    [Inject] private SaveSystem _saveSystem;
    [Inject] private LoadingScreen _loadingScreen;
    [Inject] private FpsCounter _fpsCounter;
    [Inject] private ColorBlindFilter _colorBlindFilter;
    [Inject] private IObjectResolver _resolver;

    private MenuUIConfig menuConfig;
    private TMP_FontAsset defaultFont;

    private bool isSceneInitialized = false;

    public SettingsUI GameSettings { get; private set; }
    public EquipmentUIController Equipment { get; private set; }
    public ObjectInteractionController Interaction { get; private set; }
    public AbilityController Abilities { get; private set; }
    public RadialMenuActionHandler RadialActions { get; private set; }

    public bool isMenuOpen => GameSettings != null && GameSettings.IsOpen;
    public bool isMagicLightActive => Abilities != null && Abilities.IsMagicLightActive;

    private InventoryUIManager Inv => ResolveScene<InventoryUIManager>();
    private QuestUI QUI => ResolveScene<QuestUI>();

    private static T ResolveScene<T>() where T : class
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        EnsureEventSystem();

        menuConfig = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig");
        if (menuConfig == null) menuConfig = ScriptableObject.CreateInstance<MenuUIConfig>();

        defaultFont = Resources.Load<TMP_FontAsset>(menuConfig.defaultFontPath);
        if (defaultFont == null) defaultFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");

        CreateSubControllers();

        if (SceneManager.sceneCount > 0 && SceneManager.GetActiveScene().buildIndex == 0)
            EnsureMenuUIManager();
    }

    private void Start()
    {
        InjectSubComponents();
        _settingsManager?.ApplySettings();

        if (_fpsCounter != null && _settingsManager?.Settings != null)
            _fpsCounter.SetVisible(_settingsManager.Settings.showFps);

        if (_colorBlindFilter != null && _settingsManager?.Settings != null)
            _colorBlindFilter.SetMode(_settingsManager.Settings.colorBlindMode);
    }

    private void InjectSubComponents()
    {
        var container = GameLifetimeScope.Current?.Container;
        if (container == null) return;

        var menu = GetComponent<MenuUIManager>();
        if (menu != null)
        {
            try { container.Inject(menu); } catch { }
        }

        if (GameSettings != null) container.Inject(GameSettings);
        if (Equipment != null) container.Inject(Equipment);
        if (Interaction != null) container.Inject(Interaction);
        if (Abilities != null) container.Inject(Abilities);

        if (RadialActions != null)
        {
            try { container.Inject(RadialActions); } catch { }
        }
    }

    private void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null) return;

        var esGO = new GameObject("EventSystem");
        esGO.AddComponent<EventSystem>();
        esGO.AddComponent<StandaloneInputModule>();
        DontDestroyOnLoad(esGO);
    }

    private void CreateSubControllers()
    {
        GameSettings = gameObject.AddComponent<SettingsUI>();
        Equipment = gameObject.AddComponent<EquipmentUIController>();
        Equipment.Initialize();
        Interaction = gameObject.AddComponent<ObjectInteractionController>();
        Abilities = gameObject.AddComponent<AbilityController>();
        Abilities.Initialize();
        RadialActions = gameObject.AddComponent<RadialMenuActionHandler>();
        RadialActions.Initialize();
    }

    private void EnsureMenuUIManager()
    {
        var menu = GetComponent<MenuUIManager>();

        if (menu == null)
        {
            menu = gameObject.AddComponent<MenuUIManager>();
            var container = GameLifetimeScope.Current?.Container;
            if (container != null)
            {
                try { container.Inject(menu); } catch { }
            }
        }

        menu.enabled = true;
    }

    public void HandleSceneLoaded(Scene scene)
    {
        isSceneInitialized = false;
        StartCoroutine(CleanupEventSystemsRoutine());

        if (scene.buildIndex == 0)
        {
            CleanupGameComponents();

            if (GameSettings != null)
            {
                GameSettings.OnClose = null;
                GameSettings.HidePanel();
            }

            EnsureMenuUIManager();
            var menu = GetComponent<MenuUIManager>();
            menu?.ShowMenuUI();
        }
        else
        {
            var menu = GetComponent<MenuUIManager>();
            menu?.HideMenuUI();

            CleanupComponents();

            if (GameSettings == null) CreateSubControllers();
            else
            {
                GameSettings.OnClose = null;
                GameSettings.EnsureCreated();
            }

            InitializeGameScene();
        }
    }

    private IEnumerator HideLoadingScreenDelayed()
    {
        yield return new WaitForSecondsRealtime(1f);
        _loadingScreen?.Hide();
    }

    private IEnumerator CleanupEventSystemsRoutine()
    {
        var systems = FindObjectsByType<EventSystem>(FindObjectsInactive.Include);
        EventSystem keeper = null;

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            if (systems[i].gameObject.scene.name == "DontDestroyOnLoad") { keeper = systems[i]; break; }
        }

        if (keeper == null && systems.Length > 0) keeper = systems[0];

        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            if (systems[i] == keeper) continue;
            Destroy(systems[i].gameObject);
        }

        if (EventSystem.current == null && keeper != null) EventSystem.current = keeper;
        yield return null;
    }

    private void CleanupComponents()
    {
        audioSource = null;
        detectionClip = null;
        continueButton = null;
        transitionAnimator = null;
        isSceneInitialized = false;
    }

    private void CleanupGameComponents()
    {
        Equipment?.ResetState();
        Abilities?.ResetState();
        RadialActions?.Unsubscribe();
    }

    private void InitializeGameScene()
    {
        if (isSceneInitialized) return;

        if (continueButton == null) continueButton = FindAnyObjectByType<Button>();
        if (transitionAnimator == null) transitionAnimator = FindAnyObjectByType<Animator>();

        if (PlayerPrefs.GetInt("NewGameStart", 0) == 1)
        {
            PlayerPrefs.DeleteKey("NewGameStart");
            _eventManager?.ResetAllExecutions();
            _eventStateManager?.ClearAllData();
            _dialogueTracker?.ClearAllData();
            _flagManager?.ResetFlags();
            _questManager?.ClearAllQuests();
            _craftingProgress?.ResetProgress();
            _worldItemTracker?.SetData(
                new List<string>(),
                new List<DroppedItemData>(),
                new List<MovedItemData>());

            _questManager?.StartAutoQuests();
        }

        Equipment?.FindPanel();
        Interaction?.Initialize(transitionAnimator, audioSource, detectionClip, targetLayer);
        RadialActions?.Subscribe();

        CheckForSaveFile();
        isSceneInitialized = true;

        StartCoroutine(ExecuteAwakeEventsDelayed());
    }

    private IEnumerator ExecuteAwakeEventsDelayed()
    {
        yield return null;
        yield return null;

        while (_saveSystem != null && _saveSystem.IsLoading)
            yield return null;

        _eventManager?.ExecuteAwakeEvents();
    }

    public void ToggleEquipment() => Equipment?.Toggle();
    public void OpenEquipment() => Equipment?.Open();
    public void CloseEquipment() => Equipment?.Close();
    public bool IsEquipmentOpen() => Equipment != null && Equipment.IsOpen;

    public void ToggleInventory() => Inv?.ToggleInventory();
    public bool IsInventoryOpen() => Inv != null && Inv.IsInventoryOpen;

    public void ToggleQuestUI()
    {
        var qui = QUI;
        if (qui == null) qui = FindAnyObjectByType<QuestUI>(FindObjectsInactive.Include);
        if (qui == null) return;
        qui.ToggleQuestPanel();
    }

    public void UseAbility(string abilityName) => Abilities?.UseAbility(abilityName);
    public void HandleObjectClick() => Interaction?.HandleObjectClick();
    public void SetNextObject(GameObject targetObject) => Interaction?.SetNextObject(targetObject);
    public void InteractWithObject(GameObject targetObject) => Interaction?.Interact(targetObject);
    public void DetectObject(GameObject targetObject) => Interaction?.Detect(targetObject, isMagicLightActive);

    public void StartNewGame()
    {
        _saveSystem?.DeleteSave();
        PlayerPrefs.SetInt("NewGameStart", 1);
        StartCoroutine(StartSceneWithLoading(1));
    }

    public void ContinueGame()
    {
        string lastProfile = PlayerPrefs.GetString("LastProfile", "");
        string savePath = Path.Combine(GamePaths.SaveRoot, lastProfile, GamePaths.GameSaveSubFolder, GamePaths.GameSaveFileName);

        if (!string.IsNullOrEmpty(lastProfile) && File.Exists(savePath))
        {
            GamePaths.CurrentProfile = lastProfile;
        }
        else
        {
            var profiles = GamePaths.GetAllProfiles(onlyWithSave: true);
            if (profiles.Count > 0)
            {
                GamePaths.CurrentProfile = profiles[0];
                PlayerPrefs.SetString("LastProfile", profiles[0]);
                PlayerPrefs.Save();
            }
        }

        PlayerPrefs.SetInt("Continue", 1);
        PlayerPrefs.Save();
        StartCoroutine(StartSceneWithLoading(1));
    }

    public void LoadGameWithScreen(string profileName)
    {
        GamePaths.CurrentProfile = profileName;
        PlayerPrefs.SetInt("Continue", 1);
        PlayerPrefs.Save();
        StartCoroutine(StartSceneWithLoading(1));
    }

    private IEnumerator StartSceneWithLoading(int sceneIndex)
    {
        // Новую игру надо запомнить сразу: InitializeGameScene() удалит этот ключ,
        // когда сцена уже загрузится.
        bool isNewGame = PlayerPrefs.GetInt("NewGameStart", 0) == 1;

        _loadingScreen?.Show("Загрузка мира...");
        yield return null;

        var op = SceneManager.LoadSceneAsync(sceneIndex);
        op.allowSceneActivation = false;

        while (op.progress < 0.9f)
        {
            _loadingScreen?.SetProgress(op.progress * 0.5f, "Загрузка сцены...");
            yield return null;
        }

        op.allowSceneActivation = true;
        while (!op.isDone)
        {
            _loadingScreen?.SetProgress(0.5f, "Финализация...");
            yield return null;
        }

        _loadingScreen?.SetProgress(0.55f, "Инициализация...");
        yield return null;
        yield return null;

        bool needLoad = PlayerPrefs.GetInt("Continue", 0) == 1;
        if (needLoad && _saveSystem != null)
        {
            PlayerPrefs.DeleteKey("Continue");
            PlayerPrefs.Save();

            _loadingScreen?.SetProgress(0.6f, "Применение сохранения...");
            bool ok = false;
            yield return _saveSystem.LoadGameAsync(r => ok = r);
        }

        _loadingScreen?.SetProgress(0.65f, "Расстановка локаций...");
        yield return null;

        var tb = ResolveSceneSafe<TextBeginner>();
        var dcm = ResolveSceneSafe<DialogueCharacterManager>();
        if (tb != null && dcm != null)
        {
            _loadingScreen?.SetProgress(0.7f, "Появление персонажа...");
            yield return SpawnCharacterAsync(tb, dcm, needLoad, isNewGame);
        }

        _loadingScreen?.SetProgress(0.75f, "События...");
        yield return null;
        yield return null;
        _eventManager?.ExecuteAwakeEvents();
        yield return null;

        // Страховка: если событие «Новая игра» почему-то не выполнилось,
        // персонаж и пролог всё равно должны появиться.
        if (isNewGame) EnsureNewGameStarted(tb);
        yield return null;

        float animDuration = Mathf.Max(LoadingScreen.GetArtificialDelay(), 0.5f);
        float startProgress = 0.75f;
        float elapsed = 0f;

        while (elapsed < animDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / animDuration);
            float p = Mathf.Lerp(startProgress, 1f, t);
            int pct = Mathf.RoundToInt(p * 100f);
            _loadingScreen?.SetProgress(p, $"Завершение... {pct}%");
            yield return null;
        }

        _loadingScreen?.SetProgress(1f, "Готово!");
        yield return new WaitForSecondsRealtime(0.1f);

        if (_loadingScreen != null)
            yield return _loadingScreen.HideAsync();
    }

    private const string NewGameEventId = "event_novaya_igra";

    private IEnumerator SpawnCharacterAsync(TextBeginner tb, DialogueCharacterManager dcm,
                                           bool fromSave, bool isNewGame)
    {
        if (dcm == null || tb == null) yield break;
        if (dcm.CurrentCharacter != null) yield break;
        if (tb.IsDialogueActive) yield break;

        string defaultCharacter = null;
        try { defaultCharacter = _resolver?.Resolve<DialogueData>()?.DefaultCharacterName; } catch { }
        if (string.IsNullOrEmpty(defaultCharacter)) yield break;

        // Новая игра больше не зависит от того, успело ли отметиться событие:
        // персонаж появляется всегда, а пролог запускает само событие (или EnsureNewGameStarted).
        bool shouldSpawn = fromSave || isNewGame ||
                           (_eventStateManager != null && _eventStateManager.IsExecuted(NewGameEventId));

        if (!shouldSpawn) yield break;

        dcm.ShowCharacter(defaultCharacter);
        yield return null;
        dcm.MoveCharacterToCurrentLocationReference();
        yield return null;
    }

    /// <summary>Гарантирует старт «Новой игры», даже если событие не выполнилось само.</summary>
    private void EnsureNewGameStarted(TextBeginner tb)
    {
        if (_eventStateManager != null && _eventStateManager.IsExecuted(NewGameEventId)) return;

        var evt = _eventManager?.GetEventById(NewGameEventId);
        if (evt != null)
        {
            Logger.LogWarning(LogModule.Event,
                $"Событие '{NewGameEventId}' не выполнилось на старте — запускаю вручную");

            var container = SceneInjector.Instance?.CurrentSceneScope;
            evt.Execute(new EventContext().WithContainer(container));

            if (_eventStateManager != null && _eventStateManager.IsExecuted(NewGameEventId)) return;
        }
        else
        {
            Logger.LogWarning(LogModule.Event,
                $"Событие '{NewGameEventId}' не найдено — пролог запускается напрямую");
        }

        if (tb == null || tb.IsDialogueActive) return;

        string dialogueFile = GetNewGameDialogueFileName();
        if (string.IsNullOrEmpty(dialogueFile)) return;

        Logger.LogWarning(LogModule.Event, $"Прямой запуск пролога: {dialogueFile}");
        tb.StartDialogue(dialogueFile);
    }

    private string GetNewGameDialogueFileName()
    {
        var evt = _eventManager?.GetEventById(NewGameEventId);
        if (evt?.actions != null)
        {
            foreach (var action in evt.actions)
                if (action is EventActionStartDialogue start && !string.IsNullOrEmpty(start.dialogueFileName))
                    return start.dialogueFileName;
        }
        return "Prologue";
    }

    private T ResolveSceneSafe<T>() where T : class
    {
        try { return _resolver?.Resolve<T>(); } catch { return null; }
    }

    public void ReturnToMenu() => SceneManager.LoadScene(0);
    public void LoadScene(int sceneIndex) => SceneManager.LoadScene(sceneIndex);
    public void ToggleSettingsMenu() => GameSettings?.Toggle();
    public void CloseMenu() => GameSettings?.Close();

    private void CheckForSaveFile()
    {
        if (SceneManager.GetActiveScene().buildIndex != 0) return;
        if (continueButton != null)
            continueButton.interactable = File.Exists(GamePaths.GameSaveFile);
    }

    public void QuitGame() => Application.Quit();
}