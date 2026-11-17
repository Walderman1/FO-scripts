using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class RadialMenuActionHandler : MonoBehaviour
{
    public static RadialMenuActionHandler Instance { get; private set; }

    [Inject] private GlobalControl _globalControl;     // ← DI вместо Instance
    [Inject] private IObjectResolver _resolver;

    private RadialMenu subscribedMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning(LogModule.RadialMenu, "duplicate, destroy this");
            Destroy(this);
            return;
        }
        Instance = this;
        Logger.LogVerbose(LogModule.RadialMenu, "Awake, Instance set");
    }

    private void OnDestroy()
    {
        Unsubscribe();
        if (Instance == this) Instance = null;
    }

    public void Initialize() { }

    public void Subscribe()
    {
        var menu = ResolveMainRadialMenu();
        if (menu == null) { Logger.LogWarning(LogModule.RadialMenu, "Subscribe: menu null, пропуск"); return; }
        if (subscribedMenu == menu) { Logger.LogVerbose(LogModule.RadialMenu, $"Subscribe: уже подписан на {menu.name}"); return; }

        if (subscribedMenu != null)
            subscribedMenu.onButtonClick -= OnRadialButtonClick;

        subscribedMenu = menu;
        subscribedMenu.onButtonClick -= OnRadialButtonClick;
        subscribedMenu.onButtonClick += OnRadialButtonClick;

        Logger.LogVerbose(LogModule.RadialMenu, $"Subscribe: OK menu={menu.name}");
    }

    public void Unsubscribe()
    {
        if (subscribedMenu == null) return;
        subscribedMenu.onButtonClick -= OnRadialButtonClick;
        subscribedMenu = null;
    }

    private void OnRadialButtonClick(RadialMenu.RadialAction action, string buttonName, int buttonIndex)
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"OnRadialButtonClick name={buttonName} action={action}");
        ProcessAction(action, buttonName, buttonIndex);
    }

    private GlobalControl GetGlobalControl()
    {
        if (_globalControl != null) return _globalControl;
        if (_resolver != null && _resolver.TryResolve<GlobalControl>(out var gc)) return gc;
        return null;
    }

    public void ProcessAction(RadialMenu.RadialAction action, string buttonName, int buttonIndex)
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"ProcessAction name={buttonName} action={action}");

        var gc = GetGlobalControl();
        Logger.LogVerbose(LogModule.RadialMenu, $"  globalControl={(gc != null ? gc.name : "NULL")}");

        switch (action)
        {
            case RadialMenu.RadialAction.Talk:
            case RadialMenu.RadialAction.Take:
            case RadialMenu.RadialAction.Use:
            case RadialMenu.RadialAction.Examine:
            case RadialMenu.RadialAction.Menu:
                Logger.LogVerbose(LogModule.RadialMenu, "  action=noop");
                break;
            case RadialMenu.RadialAction.UseMagic:
                Logger.LogVerbose(LogModule.RadialMenu, "  → UseAbility");
                if (gc != null) gc.UseAbility("MagicLight");
                break;
            case RadialMenu.RadialAction.OpenInventory:
                Logger.LogVerbose(LogModule.RadialMenu, "  → ToggleInventory");
                if (gc != null) gc.ToggleInventory();
                break;
            case RadialMenu.RadialAction.OpenEquipment:
                Logger.LogVerbose(LogModule.RadialMenu, "  → ToggleEquipment");
                if (gc != null) gc.ToggleEquipment();
                break;
            case RadialMenu.RadialAction.Cancel:
                Logger.LogVerbose(LogModule.RadialMenu, "  → LoadScene(0)");
                if (gc != null) gc.LoadScene(0);
                else SceneManager.LoadScene(0);
                break;
            case RadialMenu.RadialAction.Settings:
                Logger.LogVerbose(LogModule.RadialMenu, "  → ToggleSettingsMenu");
                if (gc != null) gc.ToggleSettingsMenu();
                break;
            case RadialMenu.RadialAction.Exit:
                Logger.LogVerbose(LogModule.RadialMenu, "  → QuitGame");
                if (gc != null) gc.QuitGame();
                else Application.Quit();
                break;
        }

        var controller = SceneInjector.Instance?.CurrentSceneScope?.Resolve<RadialMenuController>();
        Logger.LogVerbose(LogModule.RadialMenu, $"  controller={(controller != null ? controller.name : "NULL")}");
        controller?.CloseAllMenus();
    }

    private RadialMenu ResolveMainRadialMenu()
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope != null)
        {
            try
            {
                var registry = scope.Resolve<RadialMenuRegistry>();
                if (registry != null && registry.MainRadialMenu != null)
                    return registry.MainRadialMenu;
            }
            catch (System.Exception e)
            {
                Logger.LogWarning(LogModule.RadialMenu, $"ResolveMainRadialMenu scope.Resolve: {e.Message}");
            }
        }

        var all = FindObjectsByType<RadialMenu>(FindObjectsInactive.Include);
        foreach (var m in all)
        {
            if (m == null) continue;
            try { if (m.CompareTag("RadialMenu")) return m; } catch (UnityException) { }
        }

        return null;
    }
}