// EventManager.cs - ПОЛНАЯ ВЕРСИЯ С ЗАЩИТОЙ
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    [Header("Event Configuration")]
    [SerializeField] private bool autoLoadEvents = true;
    [SerializeField] private string eventsResourcesPath = "Events";
    public bool logEvents = true;

    public List<GameEvent> registeredEvents = new List<GameEvent>();

    private Dictionary<EventTriggerType, List<GameEvent>> eventsByTrigger = new Dictionary<EventTriggerType, List<GameEvent>>();
    private Dictionary<string, GameEvent> eventsById = new Dictionary<string, GameEvent>();

    public System.Action<GameEvent, EventContext> OnEventExecuted;

    public bool AutoLoadEvents
    {
        get => autoLoadEvents;
        set => autoLoadEvents = value;
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (autoLoadEvents)
            {
                bool loadedFromJson = LoadEventsFromJson();

                if (!loadedFromJson)
                {
                    LoadAllEventsFromResources();
                }
            }
            else
            {
                InitializeEventIndexes();
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        ExecuteAwakeEvents();
    }

    public void RunCoroutine(IEnumerator routine)
    {
        StartCoroutine(routine);
    }

    public void OnFlagChanged(string flagName, bool value)
    {
        TriggerEvent(EventTriggerType.GlobalFlag, new EventContext().WithValue(flagName).WithValue(value ? 1 : 0));
    }

    private bool LoadEventsFromJson()
    {
        if (EventDataManager.Instance == null)
        {
            Debug.Log("📭 EventDataManager not found, skipping JSON load");
            return false;
        }

        var eventDataList = EventDataManager.Instance.LoadAllEvents();

        if (eventDataList == null || eventDataList.Count == 0)
        {
            Debug.Log("📭 No events found in JSON");
            return false;
        }

        registeredEvents.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();

        int loadedCount = 0;
        foreach (var data in eventDataList)
        {
            GameEvent evt = EventConverter.ToGameEvent(data);
            if (evt != null)
            {
                RegisterEvent(evt);
                loadedCount++;
            }
        }

        Debug.Log($"📦 Loaded {loadedCount} events from JSON");
        return loadedCount > 0;
    }

    private void LoadAllEventsFromResources()
    {
        registeredEvents.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();

        GameEvent[] loadedEvents = Resources.LoadAll<GameEvent>(eventsResourcesPath);

        if (loadedEvents == null || loadedEvents.Length == 0)
        {
            if (logEvents)
                Debug.Log($"📭 Нет событий в папке Resources/{eventsResourcesPath}/");
            return;
        }

        if (logEvents)
            Debug.Log($"📦 Загружено {loadedEvents.Length} событий из Resources/{eventsResourcesPath}/");

        foreach (var evt in loadedEvents)
        {
            if (evt != null)
            {
                RegisterEvent(evt);
                if (logEvents)
                    Debug.Log($"  ✅ {evt.eventID} - {evt.eventName}");
            }
        }

        if (logEvents)
            Debug.Log($"📦 Всего зарегистрировано: {registeredEvents.Count} событий");
    }

    public void RegisterEvent(GameEvent evt)
    {
        if (evt == null) return;
        if (registeredEvents.Contains(evt)) return;

        registeredEvents.Add(evt);

        if (!string.IsNullOrEmpty(evt.eventID))
        {
            eventsById[evt.eventID] = evt;
        }

        if (!eventsByTrigger.ContainsKey(evt.triggerType))
        {
            eventsByTrigger[evt.triggerType] = new List<GameEvent>();
        }
        eventsByTrigger[evt.triggerType].Add(evt);
    }

    private void InitializeEventIndexes()
    {
        eventsByTrigger.Clear();
        eventsById.Clear();

        foreach (var evt in registeredEvents)
        {
            if (evt == null) continue;

            if (!string.IsNullOrEmpty(evt.eventID))
            {
                eventsById[evt.eventID] = evt;
            }

            if (!eventsByTrigger.ContainsKey(evt.triggerType))
            {
                eventsByTrigger[evt.triggerType] = new List<GameEvent>();
            }
            eventsByTrigger[evt.triggerType].Add(evt);
        }

        if (logEvents)
        {
            Debug.Log($"EventManager инициализирован с {registeredEvents.Count} событиями");
        }
    }

    private void ExecuteAwakeEvents()
    {
        foreach (var evt in registeredEvents)
        {
            if (evt != null && evt.executeOnAwake)
            {
                evt.Execute(new EventContext());
            }
        }
    }

    public void TriggerEvent(EventTriggerType triggerType, EventContext context = null)
    {
        if (context == null) context = new EventContext();

        if (logEvents)
        {
            Debug.Log($"🎯 Триггер: {triggerType}");
        }

        // 🔥 ПРЯМЫЕ ВЫЗОВЫ ДЛЯ КВЕСТОВ
        if (context != null)
        {
            // Подбор предмета
            if (triggerType == EventTriggerType.PickupItem && context.ItemType != ItemType.None)
            {
                QuestManager.Instance?.UpdateQuestByItem(context.ItemType, 1);
                if (logEvents)
                    Debug.Log($"✅ Прямой вызов (PickupItem): обновлён квест по предмету {context.ItemType}");
            }

            // Вход в локацию
            if (triggerType == EventTriggerType.EnterLocation && !string.IsNullOrEmpty(context.LocationName))
            {
                QuestManager.Instance?.UpdateQuestByLocation(context.LocationName);
                if (logEvents)
                    Debug.Log($"✅ Прямой вызов (EnterLocation): обновлён квест по локации {context.LocationName}");
            }

            // Диалоги
            if (triggerType == EventTriggerType.DialogueEnd && !string.IsNullOrEmpty(context.DialogueID))
            {
                QuestManager.Instance?.UpdateQuestByNPC(context.DialogueID);
                if (logEvents)
                    Debug.Log($"✅ Прямой вызов (DialogueEnd): обновлён квест по NPC {context.DialogueID}");
            }
        }

        // Выполняем все события по триггеру
        if (!eventsByTrigger.TryGetValue(triggerType, out var events))
        {
            return;
        }

        foreach (var evt in events)
        {
            if (evt != null)
            {
                evt.Execute(context);
            }
        }
    }

    public void TriggerEventById(string eventID, EventContext context = null)
    {
        if (context == null) context = new EventContext();

        if (eventsById.TryGetValue(eventID, out var evt))
        {
            evt?.Execute(context);
        }
        else
        {
            Debug.LogWarning($"Событие с ID {eventID} не найдено");
        }
    }

    public void ReloadEvents()
    {
        Debug.Log("🔄 Перезагрузка событий...");

        bool loadedFromJson = LoadEventsFromJson();

        if (!loadedFromJson)
        {
            LoadAllEventsFromResources();
        }
    }

    public List<GameEvent> GetEventsByTrigger(EventTriggerType triggerType)
    {
        if (eventsByTrigger.TryGetValue(triggerType, out var events))
            return events;
        return new List<GameEvent>();
    }

    public GameEvent GetEventById(string eventID)
    {
        if (eventsById.TryGetValue(eventID, out var evt))
            return evt;
        return null;
    }

    // ============ СОХРАНЕНИЕ В JSON С ЗАЩИТОЙ ============

    public void SaveEventToJson(GameEvent gameEvent, bool forceSave = false)
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot save to JSON");
            return;
        }

        var data = EventConverter.ToEventData(gameEvent);
        if (data != null)
        {
            EventDataManager.Instance.SaveEvent(data, forceSave);
        }
    }

    public void SaveAllEventsFromProject()
    {
#if UNITY_EDITOR
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot save to JSON");
            return;
        }

        // Находим все GameEvent в проекте
        string[] guids = AssetDatabase.FindAssets("t:GameEvent");

        if (guids.Length == 0)
        {
            Debug.Log("📭 No GameEvent assets found in project");
            return;
        }

        Debug.Log($"📦 Found {guids.Length} GameEvent assets in project");

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
                    // Проверяем, нужно ли сохранять
                    if (EventDataManager.Instance.EventExists(evt.eventID))
                    {
                        var existing = EventDataManager.Instance.LoadEvent(evt.eventID);
                        if (existing != null && existing.actions.Count > 0 && data.actions.Count == 0)
                        {
                            Debug.LogWarning($"🛡️ Skipped saving '{evt.eventID}' - would lose {existing.actions.Count} actions");
                            skippedCount++;
                            continue;
                        }
                    }

                    EventDataManager.Instance.SaveEvent(data, true);
                    savedCount++;
                    Debug.Log($"  ✅ Saved: {evt.eventID} ({evt.eventName}) with {data.actions.Count} actions");
                }
            }
            else
            {
                Debug.LogWarning($"  ⚠️ Skipped: {path} (no eventID)");
            }
        }

        Debug.Log($"💾 Saved {savedCount} events to JSON (skipped {skippedCount} to prevent data loss)");
#else
        Debug.LogWarning("SaveAllEventsFromProject is only available in Editor");
#endif
    }

    public void SaveAllEventsToJson()
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot save to JSON");
            return;
        }

        int savedCount = 0;
        int skippedCount = 0;

        foreach (var evt in registeredEvents)
        {
            if (evt != null && !string.IsNullOrEmpty(evt.eventID))
            {
                var data = EventConverter.ToEventData(evt);
                if (data != null)
                {
                    // Проверяем, нужно ли сохранять
                    if (EventDataManager.Instance.EventExists(evt.eventID))
                    {
                        var existing = EventDataManager.Instance.LoadEvent(evt.eventID);
                        if (existing != null && existing.actions.Count > 0 && data.actions.Count == 0)
                        {
                            Debug.LogWarning($"🛡️ Skipped saving '{evt.eventID}' - would lose {existing.actions.Count} actions");
                            skippedCount++;
                            continue;
                        }
                    }

                    EventDataManager.Instance.SaveEvent(data, true);
                    savedCount++;
                }
            }
        }

        Debug.Log($"💾 Saved {savedCount} loaded events to JSON (skipped {skippedCount} to prevent data loss)");
    }

    // ============ ВОССТАНОВЛЕНИЕ ИЗ JSON ============

    public int RestoreEventsFromJsonToSO()
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot restore");
            return 0;
        }

        return EventDataManager.Instance.RestoreEventsToSO();
    }

    // ============ БЭКАПЫ ============

    public void CreateBackup(string eventID)
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot create backup");
            return;
        }

        EventDataManager.Instance.CreateBackup(eventID);
    }

    public bool RestoreFromBackup(string eventID, string backupFileName = null)
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found, cannot restore from backup");
            return false;
        }

        return EventDataManager.Instance.RestoreFromBackup(eventID, backupFileName);
    }

    public List<string> GetBackupFiles(string eventID = null)
    {
        if (EventDataManager.Instance == null)
        {
            Debug.LogWarning("EventDataManager not found");
            return new List<string>();
        }

        return EventDataManager.Instance.GetBackupFiles(eventID);
    }

    // ============ ОЧИСТКА ============

    public void ClearAllEvents()
    {
        registeredEvents.Clear();
        eventsByTrigger.Clear();
        eventsById.Clear();
        Debug.Log("🗑️ All events cleared from memory");
    }

    public void ResetAllEventStates()
    {
        EventStateManager.Instance?.ResetAllEvents();
        Debug.Log("🔄 All event states reset");
    }

    // ============ ОТЛАДКА ============

    [ContextMenu("Log All Events")]
    public void LogAllEvents()
    {
        Debug.Log($"=== ALL EVENTS ({registeredEvents.Count}) ===");
        foreach (var evt in registeredEvents)
        {
            if (evt != null)
            {
                Debug.Log($"  {evt.eventID} - {evt.eventName} (Trigger: {evt.triggerType}, Actions: {evt.actions.Count})");
            }
        }
    }

    [ContextMenu("Log Events By Trigger")]
    public void LogEventsByTrigger()
    {
        Debug.Log("=== EVENTS BY TRIGGER ===");
        foreach (var kvp in eventsByTrigger)
        {
            Debug.Log($"  {kvp.Key}: {kvp.Value.Count} events");
            foreach (var evt in kvp.Value)
            {
                if (evt != null)
                {
                    Debug.Log($"    - {evt.eventID} ({evt.eventName})");
                }
            }
        }
    }

    [ContextMenu("Clear All Event States")]
    public void ClearAllEventStates()
    {
        ResetAllEventStates();
    }
}