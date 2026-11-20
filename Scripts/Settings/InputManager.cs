using DialogueSystem;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;

[DefaultExecutionOrder(-8000)]
public class InputManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private InputConfig config;

    [Header("Rules")]
    [SerializeField] private bool blockConflictingUI = true;

    [Inject] private SettingsManager _settingsManager;
    [Inject] private SaveSystem _saveSystem;
    [Inject] private GlobalControl _globalControl;
    [Inject] private QuestNotifications _questNotifications;

    private InputConfig runtimeConfig;
    private bool lastHoldSkipState = false;

    public InputConfig Config => runtimeConfig != null ? runtimeConfig : config;
    public bool BlockConflictingUI => blockConflictingUI;

    private const string CONFIG_RESOURCE_PATH = "Configs/InputConfig";

    private TextBeginner TB => ResolveScene<TextBeginner>();
    private InventoryUIManager Inv => ResolveScene<InventoryUIManager>();
    private QuestUI QUI => ResolveScene<QuestUI>();
    private RadialMenuController RMC => ResolveScene<RadialMenuController>();
    private SceneSlideTransition Slide => ResolveScene<SceneSlideTransition>();
    private TooltipManager Tooltip => ResolveScene<TooltipManager>();

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
    }

    private void Start()
    {
        EnsureConfigLoaded();

        if (_settingsManager != null)
        {
            _settingsManager.OnKeyBindingsChanged -= ApplyKeyBindings;
            _settingsManager.OnKeyBindingsChanged += ApplyKeyBindings;
        }
    }

    private void OnDestroy()
    {
        if (_settingsManager != null)
            _settingsManager.OnKeyBindingsChanged -= ApplyKeyBindings;
    }

    private void EnsureConfigLoaded()
    {
        InputConfig template = config;
        if (template == null) template = Resources.Load<InputConfig>(CONFIG_RESOURCE_PATH);
        if (template == null) template = Resources.Load<InputConfig>("InputConfig");

        if (template == null)
        {
            template = ScriptableObject.CreateInstance<InputConfig>();
            template.name = "InputConfig (Runtime Default)";
        }

        runtimeConfig = Instantiate(template);
        runtimeConfig.name = template.name + " (Runtime)";

        var keys = _settingsManager?.Settings?.keyBindings;
        if (keys != null) runtimeConfig.ApplyBindings(keys);
    }

    public void ApplyKeyBindings(KeyBindings keys)
    {
        if (runtimeConfig == null) EnsureConfigLoaded();
        if (runtimeConfig != null && keys != null) runtimeConfig.ApplyBindings(keys);
    }

    public void ReloadConfig()
    {
        runtimeConfig = null;
        EnsureConfigLoaded();
    }

    public void SetConfig(InputConfig newConfig)
    {
        if (newConfig == null) return;
        config = newConfig;
        runtimeConfig = Instantiate(newConfig);
        runtimeConfig.name = newConfig.name + " (Runtime)";

        var keys = _settingsManager?.Settings?.keyBindings;
        if (keys != null) runtimeConfig.ApplyBindings(keys);
    }

    private void Update()
    {
        // Пока открыто внутриигровое окно DevTools, игровые хоткеи не обрабатываем.
        if (InGameDevTools.BlocksGameplayInput) return;

        var cfg = Config;
        if (cfg == null) return;
        if (SceneManager.GetActiveScene().buildIndex == 0) return;
        if (KeyBindButton.IsRebinding) return;

        HandleDialogueSkip(cfg);
        HandleHotkeys(cfg);
        HandleSaveLoadHotkeys(cfg);
        HandleInventorySortHotkey(cfg);
        HandleEscapeKey(cfg);
        HandleNavigationInput(cfg);
    }

    private void HandleDialogueSkip(InputConfig cfg)
    {
        var tb = TB;
        if (tb == null || !tb.IsDialogueActive)
        {
            if (lastHoldSkipState)
            {
                tb?.SetHoldSkip(false);
                lastHoldSkipState = false;
            }
            return;
        }

        bool wantHold = cfg.SkipDialogueHeld() && !IsTypingInInputField();
        bool canSkip = !tb.IsChoosing;
        bool active = wantHold && canSkip;

        if (active != lastHoldSkipState)
        {
            tb.SetHoldSkip(active);
            lastHoldSkipState = active;
        }
    }

    private void HandleHotkeys(InputConfig cfg)
    {
        if (_globalControl == null) return;

        if (!IsTypingInInputField())
        {
            if (cfg.OpenInventoryPressed()) _globalControl.ToggleInventory();
            if (cfg.OpenEquipmentPressed()) _globalControl.ToggleEquipment();
            if (cfg.OpenQuestsPressed()) _globalControl.ToggleQuestUI();
        }

        if (!IsTypingInInputField() && (!blockConflictingUI || CanProcessGameplayInput()))
        {
            if (cfg.UseMagicLightPressed()) _globalControl.UseAbility("MagicLight");
            if (cfg.ClairvoyancePressed()) _globalControl.UseAbility("Clairvoyance");
            if (cfg.InteractClickPressed()) _globalControl.HandleObjectClick();
        }
    }

    private void HandleSaveLoadHotkeys(InputConfig cfg)
    {
        if (IsTypingInInputField()) return;
        if (_saveSystem == null) return;

        if (cfg.SaveGamePressed())
        {
            bool ok = _saveSystem.SaveGame();
            ShowSaveFeedback(ok ? "Сохранено" : "Ошибка сохранения", ok);
        }

        if (cfg.LoadGamePressed())
        {
            var tb = TB;
            if (tb != null && tb.IsDialogueActive) return;
            _saveSystem.LoadGameWithScreen();
            ShowSaveFeedback("Загружено", true);
        }
    }

    private void ShowSaveFeedback(string message, bool success)
    {
        Color color = success ? new Color(0.3f, 0.8f, 0.4f) : new Color(0.9f, 0.4f, 0.3f);

        if (_questNotifications != null)
        {
            _questNotifications.ShowCustom(message, color);
            return;
        }

        Logger.Log(LogModule.Save, message);
    }

    private void HandleInventorySortHotkey(InputConfig cfg)
    {
        if (IsTypingInInputField()) return;
        if (!cfg.SortInventoryPressed()) return;

        var inv = Inv;
        if (inv == null || !inv.IsInventoryOpen) return;

        inv.CycleSortMode();

        var tooltip = Tooltip;
        if (tooltip != null)
            tooltip.ShowTooltip($"Сортировка: {inv.GetSortModeName()}", Input.mousePosition);
    }

    private void HandleEscapeKey(InputConfig cfg)
    {
        if (!Input.GetKeyDown(cfg.closeAll)) return;

        var qui = QUI;
        if (qui != null && qui.IsOpen)
        {
            qui.CloseQuestPanel();
            return;
        }

        if (_globalControl == null) return;

        if (_globalControl.IsEquipmentOpen())
        {
            _globalControl.CloseEquipment();
            return;
        }

        var inv = Inv;
        if (inv != null && inv.IsInventoryOpen)
        {
            RMC?.CloseInventoryAndMenus();
            return;
        }

        if (_globalControl.isMagicLightActive)
        {
            _globalControl.Abilities?.DeactivateMagicLight();
            return;
        }

        RMC?.CloseAllMenus();
        _globalControl.ToggleSettingsMenu();
    }

    private void HandleNavigationInput(InputConfig cfg)
    {
        if (!CanProcessGameplayInput()) return;

        bool anyArrowPressed = cfg.NavigateLeftPressed() || cfg.NavigateRightPressed() || cfg.NavigateUpPressed() || cfg.NavigateDownPressed();
        if (!anyArrowPressed) return;

        var slide = Slide;
        if (slide == null) return;

        GameObject currentScene = slide.GetCurrentScene();
        if (currentScene == null) return;

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null) return;

        string targetSceneName = null;
        Vector2? direction = null;

        if (cfg.NavigateLeftPressed()) { targetSceneName = neighbors.LeftNeighbor; direction = Vector2.left; }
        else if (cfg.NavigateRightPressed()) { targetSceneName = neighbors.RightNeighbor; direction = Vector2.right; }
        else if (cfg.NavigateUpPressed()) { targetSceneName = neighbors.UpNeighbor; direction = Vector2.up; }
        else if (cfg.NavigateDownPressed()) { targetSceneName = neighbors.DownNeighbor; direction = Vector2.down; }

        if (string.IsNullOrEmpty(targetSceneName) || !direction.HasValue) return;

        slide.SwitchToScene(targetSceneName, direction.Value);
    }

    private bool CanProcessGameplayInput()
    {
        if (IsTypingInInputField()) return false;
        if (_globalControl == null) return false;
        if (_globalControl.isMenuOpen) return false;
        if (_globalControl.IsEquipmentOpen()) return false;
        if (_globalControl.IsInventoryOpen()) return false;

        var qui = QUI;
        if (qui != null && qui.IsOpen) return false;

        var tb = TB;
        if (tb != null && tb.IsDialogueActive) return false;

        return true;
    }

    private bool IsTypingInInputField()
    {
        if (EventSystem.current == null) return false;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null) return false;

        TMP_InputField field = selected.GetComponent<TMP_InputField>();
        if (field == null) field = selected.GetComponentInParent<TMP_InputField>();

        return field != null && field.isFocused;
    }
}