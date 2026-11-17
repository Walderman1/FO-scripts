using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using VContainer;
using VContainer.Unity;
using DialogueSystem;

[DefaultExecutionOrder(-20000)]
public class GameLifetimeScope : LifetimeScope
{
    public static GameLifetimeScope Current { get; private set; }

    [SerializeField] private GlobalControl globalControl;

    private readonly List<Component> injectedServices = new List<Component>();
    private SceneInjector sceneInjector;

    public IReadOnlyList<Component> InjectedServices => injectedServices;

    protected override void Awake()
    {
        if (Current != null && Current != this)
        {
            Logger.LogWarning(LogModule.DI,
                $"Дублирующий GameLifetimeScope на {gameObject.name} — уничтожен");
            Destroy(gameObject);
            return;
        }

        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        base.Awake();
        Current = this;

        Logger.LogVerbose(LogModule.DI,
            $"GameLifetimeScope.Awake id={RuntimeHelpers.GetHashCode(this)}");
    }

    protected override void OnDestroy()
    {
        if (Current == this) Current = null;
        base.OnDestroy();
    }

    protected override void Configure(IContainerBuilder builder)
    {
        injectedServices.Clear();

        var dialogueConfig = Resources.Load<DialogueData>("Configs/DialogueConfig");
        var questConfig = Resources.Load<QuestConfigSO>("Configs/QuestConfig");
        var inputConfig = Resources.Load<InputConfig>("Configs/InputConfig");
        var menuConfig = Resources.Load<MenuUIConfig>("Configs/MenuUIConfig");

        builder.RegisterInstance(dialogueConfig);
        builder.RegisterInstance(questConfig);
        builder.RegisterInstance(inputConfig);
        builder.RegisterInstance(menuConfig);

        RegisterOrCreate<SettingsManager>(builder);
        RegisterOrCreate<InputManager>(builder);
        RegisterOrCreate<FlagManager>(builder);
        RegisterOrCreate<EventStateManager>(builder);
        RegisterOrCreate<EventDataManager>(builder);
        RegisterOrCreate<DialogueTracker>(builder);
        RegisterOrCreate<CraftingProgress>(builder);
        RegisterOrCreate<ItemDatabase>(builder);
        RegisterOrCreate<CraftingDatabase>(builder);
        RegisterOrCreate<CraftingSystem>(builder);
        RegisterOrCreate<EventManager>(builder);
        RegisterOrCreate<QuestManager>(builder);
        RegisterOrCreate<SaveSystem>(builder);
        RegisterOrCreate<WorldItemTracker>(builder);
        RegisterOrCreate<SessionManager>(builder);
        RegisterOrCreate<LoadingScreen>(builder);
        RegisterOrCreate<QuestNotifications>(builder);
        RegisterOrCreate<FpsCounter>(builder);
        RegisterOrCreate<ColorBlindFilter>(builder);
        RegisterOrCreate<GameReferences>(builder);

        SceneInjector existingInjector = GetComponentInChildren<SceneInjector>(true);
        if (existingInjector == null && transform.parent != null)
            existingInjector = transform.parent.GetComponentInChildren<SceneInjector>(true);

        if (existingInjector != null)
        {
            sceneInjector = existingInjector;
            Logger.LogVerbose(LogModule.DI, "Переиспользуем существующий SceneInjector");
        }
        else
        {
            var injectorGo = new GameObject("SceneInjector");
            injectorGo.transform.SetParent(transform, false);
            sceneInjector = injectorGo.AddComponent<SceneInjector>();
        }

        builder.RegisterComponent(sceneInjector).AsSelf();
        injectedServices.Add(sceneInjector);

        if (globalControl == null)
            globalControl = FindAnyObjectByType<GlobalControl>(FindObjectsInactive.Include);

        if (globalControl != null)
        {
            builder.RegisterComponent(globalControl).AsSelf();
            injectedServices.Add(globalControl);
            Logger.LogVerbose(LogModule.DI,
                $"GlobalControl зарегистрирован: {globalControl.gameObject.name}");
        }
        else
        {
            Logger.LogError(LogModule.DI, "GlobalControl не найден в сцене");
        }

        builder.RegisterBuildCallback(container =>
        {
            foreach (var service in injectedServices)
            {
                if (service == null) continue;
                try { container.Inject(service); }
                catch (Exception e)
                {
                    Logger.LogVerbose(LogModule.DI,
                        $"Первичная инъекция {service.GetType().Name}: {e.Message}");
                }
            }
        });
    }

    private void RegisterOrCreate<T>(IContainerBuilder builder) where T : Component
    {
        var existing = FindAnyObjectByType<T>(FindObjectsInactive.Include);
        if (existing != null)
        {
            builder.RegisterComponent(existing).AsSelf();
            injectedServices.Add(existing);
            return;
        }

        var go = new GameObject(typeof(T).Name);
        go.transform.SetParent(transform, false);
        var comp = go.AddComponent<T>();
        builder.RegisterComponent(comp).AsSelf();
        injectedServices.Add(comp);
    }
}