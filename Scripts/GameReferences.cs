using DialogueSystem;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

[DefaultExecutionOrder(-10000)]
public class GameReferences : MonoBehaviour
{
    private static GameReferences _instance;
    public static GameReferences Instance => _instance;

    [Inject] private SettingsManager _settingsManager;
    [Inject] private InputManager _inputManager;
    [Inject] private DialogueTracker _dialogueTracker;
    [Inject] private EventManager _eventManager;
    [Inject] private EventDataManager _eventDataManager;
    [Inject] private EventStateManager _eventStateManager;
    [Inject] private FlagManager _flagManager;
    [Inject] private QuestManager _questManager;
    [Inject] private SessionManager _sessionManager;
    [Inject] private CraftingProgress _craftingProgress;
    [Inject] private SaveSystem _saveSystem;
    [Inject] private WorldItemTracker _worldItemTracker;
    [Inject] private LoadingScreen _loadingScreen;
    [Inject] private QuestNotifications _questNotifications;
    [Inject] private GlobalControl _globalControl;

    [Inject] private DialogueData _dialogueConfig;
    [Inject] private QuestConfigSO _questConfig;
    [Inject] private InputConfig _inputConfig;
    [Inject] private MenuUIConfig _menuConfig;

    public GlobalControl GlobalControl => _globalControl;
    public SettingsManager SettingsManager => _settingsManager;
    public InputManager InputManager => _inputManager;
    public DialogueTracker DialogueTracker => _dialogueTracker;
    public EventManager EventManager => _eventManager;
    public EventDataManager EventDataManager => _eventDataManager;
    public EventStateManager EventStateManager => _eventStateManager;
    public FlagManager FlagManager => _flagManager;
    public QuestManager QuestManager => _questManager;
    public SessionManager SessionManager => _sessionManager;
    public CraftingProgress CraftingProgress => _craftingProgress;
    public SaveSystem SaveSystem => _saveSystem;
    public WorldItemTracker WorldItemTracker => _worldItemTracker;
    public LoadingScreen LoadingScreen => _loadingScreen;
    public QuestNotifications QuestNotifications => _questNotifications;
    public DialogueData DialogueConfig => _dialogueConfig;
    public QuestConfigSO QuestConfig => _questConfig;
    public InputConfig InputConfig => _inputConfig;
    public MenuUIConfig MenuUIConfig => _menuConfig;

    public TextBeginner TextBeginner { get; private set; }
    public DialogueUI DialogueUI { get; private set; }
    public DialogueFileManager DialogueFileManager { get; private set; }
    public DialogueCharacterManager DialogueCharacterManager { get; private set; }
    public RadialMenu MainRadialMenu { get; private set; }
    public RadialMenu ChoiceRadialMenu { get; private set; }
    public RadialMenu ContextRadialMenu { get; private set; }
    public RadialMenuController RadialMenuController { get; private set; }
    public MenuUIManager MenuUIManager { get; private set; }
    public InventoryUIManager InventoryUIManager { get; private set; }
    public InventoryButtonPanel InventoryButtonPanel { get; private set; }
    public EquipmentSystem EquipmentSystem { get; private set; }
    public TooltipManager TooltipManager { get; private set; }
    public ItemViewPanel ItemViewPanel { get; private set; }
    public CraftingUI CraftingUI { get; private set; }
    public CraftingSystem CraftingSystem { get; private set; }
    public CraftingDatabase CraftingDatabase { get; private set; }
    public ItemDatabase ItemDatabase { get; private set; }
    public QuestUI QuestUI { get; private set; }
    public SceneSlideTransition SceneSlideTransition { get; private set; }
    public SceneTransition SceneTransition { get; private set; }
    public AbilityManager AbilityManager { get; private set; }
    public MagicLightAbility MagicLightAbility { get; private set; }
    public ClairvoyanceAbility ClairvoyanceAbility { get; private set; }
    public MagicSpellPanel MagicSpellPanel { get; private set; }

    public GameObject CharacterParticles { get; private set; }
    public GameObject FirstCharactersArea { get; private set; }
    public GameObject SecondCharactersArea { get; private set; }

    public Camera MainCamera { get; private set; }

    private FpsCounter _fpsCounter;
    public FpsCounter FpsCounter
    {
        get { if (_fpsCounter == null) _fpsCounter = FindFirst<FpsCounter>(); return _fpsCounter; }
        private set => _fpsCounter = value;
    }

    private ColorBlindFilter _colorBlindFilter;
    public ColorBlindFilter ColorBlindFilter
    {
        get { if (_colorBlindFilter == null) _colorBlindFilter = FindFirst<ColorBlindFilter>(); return _colorBlindFilter; }
        private set => _colorBlindFilter = value;
    }

    public System.Action OnSceneReferencesRefreshed;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.parent != null) transform.SetParent(null);
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        RefreshSceneReferences();
        SceneManager.sceneLoaded += OnSceneLoaded;
        OnSceneReferencesRefreshed?.Invoke();
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _instance = null;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneReferences();
        OnSceneReferencesRefreshed?.Invoke();
    }

    public void RefreshSceneReferences()
    {
        MainCamera = Camera.main;

        TextBeginner = FindFirst<TextBeginner>();
        DialogueUI = FindFirst<DialogueUI>();
        DialogueFileManager = FindFirst<DialogueFileManager>();
        DialogueCharacterManager = FindFirst<DialogueCharacterManager>();
        RadialMenuController = FindFirst<RadialMenuController>();
        MenuUIManager = FindFirst<MenuUIManager>();
        InventoryUIManager = FindFirst<InventoryUIManager>();
        InventoryButtonPanel = FindFirst<InventoryButtonPanel>();
        EquipmentSystem = FindFirst<EquipmentSystem>();
        TooltipManager = FindFirst<TooltipManager>();
        ItemViewPanel = FindFirst<ItemViewPanel>();
        CraftingUI = FindFirst<CraftingUI>();
        CraftingSystem = FindFirst<CraftingSystem>();
        CraftingDatabase = FindFirst<CraftingDatabase>();
        ItemDatabase = FindFirst<ItemDatabase>();
        QuestUI = FindFirst<QuestUI>();
        SceneSlideTransition = FindFirst<SceneSlideTransition>();
        SceneTransition = FindFirst<SceneTransition>();
        AbilityManager = FindFirst<AbilityManager>();
        MagicLightAbility = FindFirst<MagicLightAbility>();
        ClairvoyanceAbility = FindFirst<ClairvoyanceAbility>();
        MagicSpellPanel = FindFirst<MagicSpellPanel>();
        FpsCounter = FindFirst<FpsCounter>();
        ColorBlindFilter = FindFirst<ColorBlindFilter>();

        MainRadialMenu = FindByTag<RadialMenu>("RadialMenu");
        ChoiceRadialMenu = FindByTag<RadialMenu>("ChoicePanel");
        ContextRadialMenu = FindByTag<RadialMenu>("ContextMenu");

        CharacterParticles = FindByTagSafe("CharacterParticles");
        FirstCharactersArea = FindByTagSafe("FirstCharactersArea");
        SecondCharactersArea = FindByTagSafe("SecondCharactersArea");

        Logger.LogVerbose(LogModule.DI,
            $"Ссылки сцены: TextBeginner={TextBeginner != null}, " +
            $"RadialMenu: Main={MainRadialMenu != null}, Choice={ChoiceRadialMenu != null}, Context={ContextRadialMenu != null}, " +
            $"Inventory={InventoryUIManager != null}, QuestUI={QuestUI != null}");
    }

    private T FindFirst<T>() where T : Component => FindAnyObjectByType<T>(FindObjectsInactive.Include);

    private T FindByTag<T>(string tag) where T : Component
    {
        var sceneCount = SceneManager.sceneCount;
        for (int s = 0; s < sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                try
                {
                    if (roots[r].CompareTag(tag))
                    {
                        var comp = roots[r].GetComponent<T>();
                        if (comp != null) return comp;
                    }
                }
                catch (UnityException) { }

                var found = FindComponentByTagRecursive<T>(roots[r].transform, tag);
                if (found != null) return found;
            }
        }
        return null;
    }

    private T FindComponentByTagRecursive<T>(Transform parent, string tag) where T : Component
    {
        try
        {
            if (parent.CompareTag(tag))
            {
                var comp = parent.GetComponent<T>();
                if (comp != null) return comp;
            }
        }
        catch (UnityException) { }

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindComponentByTagRecursive<T>(parent.GetChild(i), tag);
            if (found != null) return found;
        }
        return null;
    }

    private GameObject FindByTagSafe(string tag)
    {
        try
        {
            var active = GameObject.FindGameObjectWithTag(tag);
            if (active != null) return active;
        }
        catch (UnityException) { return null; }

        var sceneCount = SceneManager.sceneCount;
        for (int s = 0; s < sceneCount; s++)
        {
            var scene = SceneManager.GetSceneAt(s);
            if (!scene.isLoaded) continue;

            var roots = scene.GetRootGameObjects();
            for (int r = 0; r < roots.Length; r++)
            {
                var found = FindByTagRecursive(roots[r].transform, tag);
                if (found != null) return found;
            }
        }
        return null;
    }

    private GameObject FindByTagRecursive(Transform parent, string tag)
    {
        try { if (parent.CompareTag(tag)) return parent.gameObject; }
        catch (UnityException) { }

        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindByTagRecursive(parent.GetChild(i), tag);
            if (found != null) return found;
        }
        return null;
    }

    public bool IsDialogueActive() => TextBeginner != null && TextBeginner.IsDialogueActive;
    public bool IsInventoryOpen() => InventoryUIManager != null && InventoryUIManager.IsInventoryOpen;
    public bool IsAnyMenuOpen() => RadialMenuController != null && RadialMenuController.IsAnyMenuOpen();
    public bool IsQuestUIOpen() => QuestUI != null && QuestUI.IsOpen;
    public bool IsCraftingOpen() => CraftingUI != null && CraftingUI.IsOpen;
    public bool IsEquipmentOpen() => GlobalControl != null && GlobalControl.IsEquipmentOpen();

    public bool IsPointerOverAnyOpenPanel(Vector2 screenPos)
    {
        if (InventoryUIManager != null && InventoryUIManager.IsPointerOverPanel(screenPos)) return true;
        if (GlobalControl != null && GlobalControl.Equipment != null && GlobalControl.Equipment.IsPointerOverPanel(screenPos)) return true;
        if (CraftingUI != null && CraftingUI.IsPointerOverPanel(screenPos)) return true;
        if (MagicSpellPanel != null && MagicSpellPanel.IsPointerOverPanel(screenPos)) return true;
        return false;
    }

    public bool IsAnyBlockingUIOpen()
    {
        return IsDialogueActive() || IsAnyMenuOpen() || IsInventoryOpen() || IsQuestUIOpen()
            || IsCraftingOpen() || IsEquipmentOpen() || IsSettingsOpen();
    }

    public bool IsSettingsOpen()
    {
        var gc = GlobalControl;
        return gc != null && gc.isMenuOpen;
    }

    [ContextMenu("Log All References")]
    public void LogAllReferences()
    {
        Logger.Log(LogModule.DI,
            "=== GAME REFERENCES ===\n" +
            $"Persistent: GC={GlobalControl != null}, Settings={SettingsManager != null}, Input={InputManager != null}\n" +
            $"Configs: Dialogue={DialogueConfig != null}, Quest={QuestConfig != null}, Input={InputConfig != null}\n" +
            $"Scene: TextBeginner={TextBeginner != null}, Inventory={InventoryUIManager != null}, RadialMenuController={RadialMenuController != null}\n" +
            $"RadialMenu: Main={MainRadialMenu != null}, Choice={ChoiceRadialMenu != null}, Context={ContextRadialMenu != null}\n" +
            $"Systems: FpsCounter={FpsCounter != null}, ColorBlindFilter={ColorBlindFilter != null}, MagicSpellPanel={MagicSpellPanel != null}\n" +
            $"Camera: {(MainCamera != null ? MainCamera.name : "null")}\n" +
            "=== END ===");
    }
}