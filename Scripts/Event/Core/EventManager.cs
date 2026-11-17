using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using VContainer;
#if UNITY_EDITOR
using UnityEditor;
#endif

[DefaultExecutionOrder(-7500)]
public class EventManager : MonoBehaviour
{
    [Inject] private EventDataManager _eventDataManager;
    [Inject] private EventStateManager _eventStateManager;
    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private IObjectResolver _container;

    [Header("Event Configuration")]
    [SerializeField] private bool autoLoadEvents = true;
    [SerializeField] private string eventsResourcesPath = "EventsData";
    public bool logEvents = true;

    public List<GameEvent> registeredEvents = new List<GameEvent>();

    private readonly HashSet<GameEvent> registeredSet = new HashSet<GameEvent>();
    private Dictionary<EventTriggerType, List<GameEvent>> eventsByTrigger = new Dictionary<EventTriggerType, List<GameEvent>>();
    private Dictionary<string, GameEvent> eventsById = new Dictionary<string, GameEvent>();

    private string cachedItemId = "";
    private string cachedItemIdSource = "";

    public Action<GameEvent, EventContext> OnEventExecuted;

    public bool AutoLoadEvents
    {
        get => autoLoadEvents;
        set => autoLoadEvents = value;
    }

    private IObjectResolver EffectiveContainer
    {
        get
        {
            var scene = SceneInjector.Instance?.CurrentSceneScope;
            if (scene != null) return scene;
            return _container;
        }
    }

    private QuestManager ResolveQuestManager()
    {
        var scene = SceneInjector.Instance?.CurrentSceneScope;
        if (scene != null)
        {
            try { var qm = scene.Resolve<QuestManager>(); if (qm != null) return qm; } catch { }
        }

        var root = SceneInjector.Instance?.RootScope;
        if (root != null)
        {
            try { return root.Resolve<QuestManager>(); } catch { }
        }
        return null;
    }

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (autoLoadEvents)
        {
            bool loadedFromJson = LoadEventsFromJson();
            if (!loadedFromJson) LoadEventsFromResourcesDirectly();
            if (!loadedFromJson && registeredEvents.Count == 0) LoadAllEventsFromResources();
        }
        else InitializeEventIndexes();
    }

    private void OnDestroy()
    {
        registeredSet.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();
        cachedItemId = "";
        cachedItemIdSource = "";
    }

    private void LoadEventsFromResourcesDirectly()
    {
        TextAsset[] jsonAssets = Resources.LoadAll<TextAsset>(eventsResourcesPath);
        if (jsonAssets.Length == 0) return;

        registeredEvents.Clear();
        registeredSet.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();

        int loadedCount = 0;
        foreach (TextAsset asset in jsonAssets)
        {
            try
            {
                EventData data = JsonUtility.FromJson<EventData>(asset.text);
                if (data != null && !string.IsNullOrEmpty(data.eventID))
                {
                    GameEvent evt = EventConverter.ToGameEvent(data);
                    if (evt != null) { RegisterEvent(evt); loadedCount++; }
                }
            }
            catch (Exception e)
            {
                Logger.LogError(LogModule.Event, $"Error loading {asset.name}: {e.Message}");
            }
        }

        if (logEvents)
            Logger.Log(LogModule.Event, $"Loaded {loadedCount} events from Resources/{eventsResourcesPath}");
    }

    public void RunCoroutine(IEnumerator routine) => StartCoroutine(routine);

    public void OnFlagChanged(string flagName, bool value)
    {
        TriggerEvent(EventTriggerType.GlobalFlag, new EventContext().WithFlag(flagName, value));
    }

    private bool LoadEventsFromJson()
    {
        if (_eventDataManager == null) return false;

        var eventDataList = _eventDataManager.LoadAllEvents();
        if (eventDataList == null || eventDataList.Count == 0) return false;

        registeredEvents.Clear();
        registeredSet.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();

        int loadedCount = 0;
        foreach (var data in eventDataList)
        {
            GameEvent evt = EventConverter.ToGameEvent(data);
            if (evt != null) { RegisterEvent(evt); loadedCount++; }
        }
        return loadedCount > 0;
    }

    private void LoadAllEventsFromResources()
    {
        registeredEvents.Clear();
        registeredSet.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();

        GameEvent[] loadedEvents = Resources.LoadAll<GameEvent>(eventsResourcesPath);
        if (loadedEvents == null || loadedEvents.Length == 0) return;

        foreach (var evt in loadedEvents)
            if (evt != null) RegisterEvent(evt);
    }

    public void RegisterEvent(GameEvent evt)
    {
        if (evt == null) return;
        if (!registeredSet.Add(evt)) return;

        registeredEvents.Add(evt);

        if (!string.IsNullOrEmpty(evt.eventID))
            eventsById[evt.eventID] = evt;

        if (!eventsByTrigger.TryGetValue(evt.triggerType, out var list))
        {
            list = new List<GameEvent>();
            eventsByTrigger[evt.triggerType] = list;
        }
        list.Add(evt);
    }

    private void InitializeEventIndexes()
    {
        eventsByTrigger.Clear();
        eventsById.Clear();
        registeredSet.Clear();

        foreach (var evt in registeredEvents)
        {
            if (evt == null) continue;
            registeredSet.Add(evt);

            if (!string.IsNullOrEmpty(evt.eventID))
                eventsById[evt.eventID] = evt;

            if (!eventsByTrigger.TryGetValue(evt.triggerType, out var list))
            {
                list = new List<GameEvent>();
                eventsByTrigger[evt.triggerType] = list;
            }
            list.Add(evt);
        }
    }

    public void ExecuteAwakeEvents()
    {
        var container = EffectiveContainer;
        int count = registeredEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var evt = registeredEvents[i];
            if (evt != null && evt.executeOnAwake)
            {
                var ctx = new EventContext().WithContainer(container);
                evt.Execute(ctx);
            }
        }
    }

    public void TriggerEvent(EventTriggerType triggerType, EventContext context = null)
    {
        if (context == null) context = new EventContext();
        if (context.Container == null) context.Container = EffectiveContainer;

        if (logEvents)
            Logger.Log(LogModule.Event, $"Триггер: {triggerType}");

        var questManager = ResolveQuestManager();
        string resolvedItemId = ResolveItemIdFromContext(context);

        if (!string.IsNullOrEmpty(resolvedItemId))
        {
            if (triggerType == EventTriggerType.PickupItem ||
                triggerType == EventTriggerType.Custom ||
                triggerType == EventTriggerType.UseItem)
            {
                questManager?.UpdateQuestByItem(resolvedItemId, 1);
            }

            if (triggerType == EventTriggerType.EquipItem)
            {
                questManager?.UpdateQuestByEquip(resolvedItemId, 1);
            }
        }

        if (triggerType == EventTriggerType.EnterLocation && !string.IsNullOrEmpty(context.LocationName))
            questManager?.UpdateQuestByLocation(context.LocationName);

        if (triggerType == EventTriggerType.DialogueEnd && !string.IsNullOrEmpty(context.DialogueID))
            questManager?.UpdateQuestByNPC(context.DialogueID);

        if (triggerType == EventTriggerType.Custom)
        {
            if (!string.IsNullOrEmpty(context.LocationName))
                questManager?.UpdateQuestByLocation(context.LocationName);
            if (!string.IsNullOrEmpty(context.DialogueID))
                questManager?.UpdateQuestByNPC(context.DialogueID);
        }

        if (!eventsByTrigger.TryGetValue(triggerType, out var events)) return;

        int count = events.Count;
        for (int i = 0; i < count; i++)
        {
            var evt = events[i];
            if (evt != null) evt.Execute(context);
        }
    }

    private string ResolveItemIdFromContext(EventContext context)
    {
        if (context == null) return "";

        if (!string.IsNullOrEmpty(context.ItemId))
        {
            if (cachedItemIdSource == context.ItemId) return cachedItemId;
            cachedItemIdSource = context.ItemId;
            cachedItemId = context.ItemId;
            return cachedItemId;
        }

        if (context.ItemSO != null)
        {
            if (cachedItemIdSource == context.ItemSO.id) return cachedItemId;
            cachedItemIdSource = context.ItemSO.id;
            cachedItemId = context.ItemSO.id;
            return cachedItemId;
        }

#pragma warning disable CS0618
        if (context.ItemType != ItemType.None)
        {
            var so = _itemDatabase?.GetItemByLegacyType(context.ItemType);
            string id = so != null ? so.id : "";
            if (cachedItemIdSource == id) return cachedItemId;
            cachedItemIdSource = id;
            cachedItemId = id;
            return cachedItemId;
        }
#pragma warning restore CS0618

        cachedItemIdSource = "";
        cachedItemId = "";
        return "";
    }

    public void TriggerEventById(string eventID, EventContext context = null)
    {
        if (context == null) context = new EventContext();
        if (context.Container == null) context.Container = EffectiveContainer;

        if (eventsById.TryGetValue(eventID, out var evt))
            evt?.Execute(context);
    }

    public void ReloadEvents()
    {
        bool loadedFromJson = LoadEventsFromJson();
        if (!loadedFromJson) LoadAllEventsFromResources();
    }

    public List<GameEvent> GetEventsByTrigger(EventTriggerType triggerType)
    {
        if (eventsByTrigger.TryGetValue(triggerType, out var events)) return events;
        return new List<GameEvent>();
    }

    public GameEvent GetEventById(string eventID)
    {
        if (eventsById.TryGetValue(eventID, out var evt)) return evt;
        return null;
    }

    public void SaveEventToJson(GameEvent gameEvent, bool forceSave = false)
    {
        if (_eventDataManager == null) return;
        var data = EventConverter.ToEventData(gameEvent);
        if (data != null) _eventDataManager.SaveEvent(data, forceSave);
    }

    public void SaveAllEventsFromProject()
    {
#if UNITY_EDITOR
        if (_eventDataManager == null) return;

        string[] guids = AssetDatabase.FindAssets("t:GameEvent");
        if (guids.Length == 0) return;

        int savedCount = 0;
        int skippedCount = 0;

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            GameEvent evt = AssetDatabase.LoadAssetAtPath<GameEvent>(path);

            if (evt != null && !string.IsNullOrEmpty(evt.eventID))
            {
                var data = EventConverter.ToEventData(evt);
                if (data != null)
                {
                    if (_eventDataManager.EventExists(evt.eventID))
                    {
                        var existing = _eventDataManager.LoadEvent(evt.eventID);
                        if (existing != null && existing.actions.Count > 0 && data.actions.Count == 0)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    _eventDataManager.SaveEvent(data, true);
                    savedCount++;
                }
            }
        }

        Logger.Log(LogModule.Event, $"Saved {savedCount} events to JSON (skipped {skippedCount})");
#endif
    }

    public void SaveAllEventsToJson()
    {
        if (_eventDataManager == null) return;

        int savedCount = 0;
        int skippedCount = 0;
        int total = registeredEvents.Count;

        for (int i = 0; i < total; i++)
        {
            var evt = registeredEvents[i];
            if (evt != null && !string.IsNullOrEmpty(evt.eventID))
            {
                var data = EventConverter.ToEventData(evt);
                if (data != null)
                {
                    if (_eventDataManager.EventExists(evt.eventID))
                    {
                        var existing = _eventDataManager.LoadEvent(evt.eventID);
                        if (existing != null && existing.actions.Count > 0 && data.actions.Count == 0)
                        {
                            skippedCount++;
                            continue;
                        }
                    }

                    _eventDataManager.SaveEvent(data, true);
                    savedCount++;
                }
            }
        }
    }

    public int RestoreEventsFromJsonToSO()
    {
        if (_eventDataManager == null) return 0;
        return _eventDataManager.RestoreEventsToSO();
    }

    public void CreateBackup(string eventID) => _eventDataManager?.CreateBackup(eventID);

    public bool RestoreFromBackup(string eventID, string backupFileName = null)
    {
        if (_eventDataManager == null) return false;
        return _eventDataManager.RestoreFromBackup(eventID, backupFileName);
    }

    public List<string> GetBackupFiles(string eventID = null)
    {
        if (_eventDataManager == null) return new List<string>();
        return _eventDataManager.GetBackupFiles(eventID);
    }

    public void ClearAllEvents()
    {
        registeredEvents.Clear();
        registeredSet.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();
        cachedItemId = "";
        cachedItemIdSource = "";
    }

    public void ResetAllExecutions()
    {
        int count = registeredEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var evt = registeredEvents[i];
            if (evt != null) evt.ResetExecution();
        }
    }

    public void ResetAllEventStates() => _eventStateManager?.ResetAllEvents();

    [ContextMenu("Log All Events")]
    public void LogAllEvents()
    {
        int count = registeredEvents.Count;
        for (int i = 0; i < count; i++)
        {
            var evt = registeredEvents[i];
            if (evt != null)
                Logger.Log(LogModule.Event, $"  {evt.eventID} - {evt.eventName} (Trigger: {evt.triggerType}, Actions: {evt.actions.Count})");
        }
    }

    [ContextMenu("Log Events By Trigger")]
    public void LogEventsByTrigger()
    {
        foreach (var kvp in eventsByTrigger)
        {
            Logger.Log(LogModule.Event, $"  {kvp.Key}: {kvp.Value.Count} events");
            var list = kvp.Value;
            int count = list.Count;
            for (int i = 0; i < count; i++)
            {
                var evt = list[i];
                if (evt != null)
                    Logger.Log(LogModule.Event, $"    - {evt.eventID} ({evt.eventName})");
            }
        }
    }

    [ContextMenu("Clear All Event States")]
    public void ClearAllEventStates() => ResetAllEventStates();
}