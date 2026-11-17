using DialogueSystem;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;
using VContainer.Unity;

[DefaultExecutionOrder(-19000)]
public class SceneInjector : MonoBehaviour
{
    public static SceneInjector Instance { get; private set; }

    private static readonly Dictionary<Type, bool> hasInjectCache = new Dictionary<Type, bool>();

    [Inject] private IObjectResolver _resolver;
    [Inject] private GameLifetimeScope _gameScope;

    private IScopedObjectResolver currentSceneScope;
    private RadialMenuRegistry currentRegistry;
    private readonly HashSet<Type> registeredThisScope = new HashSet<Type>();

    public IObjectResolver CurrentSceneScope => currentSceneScope;
    public IObjectResolver RootScope => _resolver;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Logger.LogWarning(LogModule.Core,
                $"Дублирующий SceneInjector найден на {gameObject.name} — компонент отключён");
            enabled = false;
            return;
        }

        Instance = this;
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        DisposeSceneScope();
        DisposeRegistry();
        if (Instance == this) Instance = null;
    }

    private void DisposeSceneScope()
    {
        if (currentSceneScope == null) return;
        try { currentSceneScope.Dispose(); }
        catch (Exception e) { Logger.LogWarning(LogModule.Core, $"Dispose scope: {e.Message}"); }
        currentSceneScope = null;
    }

    private void DisposeRegistry()
    {
        if (currentRegistry == null) return;
        Destroy(currentRegistry.gameObject);
        currentRegistry = null;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (_resolver == null || _gameScope == null) return;

        DisposeSceneScope();
        DisposeRegistry();
        registeredThisScope.Clear();

        try
        {
            var registryGo = new GameObject("RadialMenuRegistry");
            registryGo.transform.SetParent(transform, false);
            currentRegistry = registryGo.AddComponent<RadialMenuRegistry>();
            currentRegistry.Refresh(scene);
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Core, $"registry creation failed: {e.Message}");
        }

        try
        {
            currentSceneScope = _resolver.CreateScope(builder =>
            {
                if (currentRegistry != null)
                    builder.RegisterComponent(currentRegistry).AsSelf();

                RegisterPersistent<FpsCounter>(builder);
                RegisterPersistent<ColorBlindFilter>(builder);
                RegisterPersistent<RadialMenuActionHandler>(builder);
                RegisterPersistent<EquipmentUIController>(builder);
                RegisterPersistent<PickupVisualsManager>(builder);

                if (scene.buildIndex == 0)
                {
                    RegisterFromScene<SceneTransition>(builder, scene);
                }
                else
                {
                    RegisterFromScene<TextBeginner>(builder, scene);
                    RegisterFromScene<DialogueUI>(builder, scene);
                    RegisterFromScene<DialogueFileManager>(builder, scene);
                    RegisterFromScene<DialogueCharacterManager>(builder, scene);

                    RegisterFromScene<InventoryUIManager>(builder, scene);
                    RegisterFromScene<InventoryButtonPanel>(builder, scene);
                    RegisterFromScene<EquipmentSystem>(builder, scene);
                    RegisterFromScene<TooltipManager>(builder, scene);
                    RegisterFromScene<ItemViewPanel>(builder, scene);
                    RegisterFromScene<CraftingUI>(builder, scene);
                    RegisterFromScene<QuestUI>(builder, scene);
                    RegisterFromScene<SceneSlideTransition>(builder, scene);
                    RegisterFromScene<AbilityManager>(builder, scene);
                    RegisterFromScene<MagicLightAbility>(builder, scene);
                    RegisterFromScene<ClairvoyanceAbility>(builder, scene);
                    RegisterFromScene<RadialMenuController>(builder, scene);
                    RegisterFromScene<MagicSpellPanel>(builder, scene);
                    RegisterFromScene<RadialMenuOpener>(builder, scene);
                    RegisterFromScene<MenuCloseHandler>(builder, scene);
                    RegisterFromScene<SceneEventController>(builder, scene);
                    RegisterFromScene<DialogueTrigger>(builder, scene);
                }
            });
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Core, $"CreateScope FAILED: {e.GetType().Name}: {e.Message}\n{e.StackTrace}");
            currentSceneScope = null;
        }

        if (currentSceneScope != null)
        {
            InjectSceneComponents(scene);
            InjectPersistentServices();
        }

        try
        {
            var gc = _resolver.Resolve<GlobalControl>();
            gc?.HandleSceneLoaded(scene);
        }
        catch (Exception e)
        {
            Logger.LogWarning(LogModule.Core, $"Resolve<GlobalControl> failed: {e.Message}");
        }
    }

    private void RegisterFromScene<T>(IContainerBuilder builder, Scene scene) where T : Component
    {
        try
        {
            var comp = FindInScene<T>(scene);
            if (comp != null)
            {
                builder.RegisterComponent(comp).AsSelf();
                registeredThisScope.Add(typeof(T));
            }
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Core, $"register {typeof(T).Name} THREW: {e.Message}");
        }
    }

    private void RegisterPersistent<T>(IContainerBuilder builder) where T : Component
    {
        if (registeredThisScope.Contains(typeof(T))) return;

        try
        {
            var comp = FindAnyObjectByType<T>(FindObjectsInactive.Include);

            if (comp == null)
            {
                var go = new GameObject(typeof(T).Name);
                go.transform.SetParent(null);
                comp = go.AddComponent<T>();
                DontDestroyOnLoad(go);
            }

            builder.RegisterComponent(comp).AsSelf();
            registeredThisScope.Add(typeof(T));
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Core, $"register persistent {typeof(T).Name} THREW: {e.Message}");
        }
    }

    private T FindInScene<T>(Scene scene) where T : Component
    {
        var roots = scene.GetRootGameObjects();
        int len = roots.Length;
        for (int i = 0; i < len; i++)
        {
            var root = roots[i];
            if (root == null) continue;
            var c = root.GetComponentInChildren<T>(true);
            if (c != null) return c;
        }

        return null;
    }

    private void InjectSceneComponents(Scene scene)
    {
        var roots = scene.GetRootGameObjects();
        int rootCount = roots.Length;

        for (int r = 0; r < rootCount; r++)
        {
            var root = roots[r];
            if (root == null) continue;

            var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
            int compCount = behaviours.Length;

            for (int i = 0; i < compCount; i++)
            {
                var comp = behaviours[i];
                if (comp == null) continue;
                if (!HasInjectMembers(comp.GetType())) continue;

                try { currentSceneScope.Inject(comp); }
                catch (Exception e)
                {
                    Logger.LogError(LogModule.Core, $"inject {comp.GetType().Name} ({comp.name}) FAILED: {e.Message}");
                }
            }
        }
    }

    private void InjectPersistentServices()
    {
        if (_gameScope == null) return;
        var services = _gameScope.InjectedServices;
        int count = services.Count;

        for (int i = 0; i < count; i++)
        {
            var svc = services[i];
            if (svc == null) continue;
            if (!HasInjectMembers(svc.GetType())) continue;

            try { currentSceneScope.Inject(svc); }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Core, $"reinject {svc.GetType().Name} ({svc.name}) FAILED: {e.Message}");
            }
        }
    }

    public static void InjectInto(GameObject target)
    {
        var injector = Instance;
        if (injector == null || injector.currentSceneScope == null || target == null) return;

        var behaviours = target.GetComponentsInChildren<MonoBehaviour>(true);
        int count = behaviours.Length;

        for (int i = 0; i < count; i++)
        {
            var comp = behaviours[i];
            if (comp == null) continue;
            if (!HasInjectMembers(comp.GetType())) continue;

            try { injector.currentSceneScope.Inject(comp); }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Core, $"InjectInto {comp.GetType().Name} ({comp.name}) FAILED: {e.Message}");
            }
        }
    }

    private static bool HasInjectMembers(Type type)
    {
        if (hasInjectCache.TryGetValue(type, out bool has)) return has;

        has = false;
        var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        int fLen = fields.Length;
        for (int i = 0; i < fLen; i++)
            if (fields[i].IsDefined(typeof(InjectAttribute), true)) { has = true; break; }

        if (!has)
        {
            var props = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int pLen = props.Length;
            for (int i = 0; i < pLen; i++)
                if (props[i].IsDefined(typeof(InjectAttribute), true)) { has = true; break; }
        }

        hasInjectCache[type] = has;
        return has;
    }
}