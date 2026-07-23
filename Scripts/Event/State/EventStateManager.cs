using UnityEngine;
using System.Collections.Generic;

public class EventStateManager : MonoBehaviour
{
    public static EventStateManager Instance;

    [Header("State Storage")]
    [SerializeField] private bool usePlayerPrefs = true;
    [SerializeField] private string saveKeyPrefix = "EventState_";
    [SerializeField] private bool logStateChanges = true;

    private Dictionary<string, bool> eventStates = new Dictionary<string, bool>();
    private Dictionary<string, int> eventExecutionCounts = new Dictionary<string, int>();
    private HashSet<string> executedInSession = new HashSet<string>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadStates();
            Logger.Log(LogModule.Event, "EventStateManager инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Event, "Уничтожение дублирующего EventStateManager");
            Destroy(gameObject);
        }
    }

    private void LoadStates()
    {
        if (!usePlayerPrefs) return;
        Logger.Log(LogModule.Event, "Загрузка состояний событий из PlayerPrefs");
    }

    private void SaveState(string eventID)
    {
        if (!usePlayerPrefs) return;

        string key = saveKeyPrefix + eventID;
        PlayerPrefs.SetInt(key, eventStates.TryGetValue(eventID, out bool value) && value ? 1 : 0);

        string countKey = saveKeyPrefix + "Count_" + eventID;
        int count = eventExecutionCounts.TryGetValue(eventID, out int c) ? c : 0;
        PlayerPrefs.SetInt(countKey, count);

        PlayerPrefs.Save();
    }

    public void MarkExecuted(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Logger.LogWarning(LogModule.Event, "Попытка отметить пустой eventID как выполненный");
            return;
        }

        if (!eventStates.ContainsKey(eventID))
            eventStates[eventID] = false;

        if (!eventStates[eventID])
        {
            eventStates[eventID] = true;
            executedInSession.Add(eventID);

            if (!eventExecutionCounts.ContainsKey(eventID))
                eventExecutionCounts[eventID] = 0;
            eventExecutionCounts[eventID]++;

            SaveState(eventID);

            if (logStateChanges)
                Logger.Log(LogModule.Event, $"Событие '{eventID}' отмечено как выполненное (счётчик: {eventExecutionCounts[eventID]})");
        }
        else
        {
            if (logStateChanges)
                Logger.LogWarning(LogModule.Event, $"Событие '{eventID}' уже выполнено, пропуск отметки");
        }
    }

    public bool IsExecuted(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        if (eventStates.TryGetValue(eventID, out bool value))
            return value;

        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + eventID;
            if (PlayerPrefs.HasKey(key))
            {
                bool savedState = PlayerPrefs.GetInt(key, 0) == 1;
                eventStates[eventID] = savedState;
                if (logStateChanges)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' загружено из PlayerPrefs: {savedState}");
                return savedState;
            }
        }

        return false;
    }

    public int GetExecutionCount(string eventID)
    {
        if (eventExecutionCounts.TryGetValue(eventID, out int count))
            return count;

        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + "Count_" + eventID;
            if (PlayerPrefs.HasKey(key))
            {
                count = PlayerPrefs.GetInt(key, 0);
                eventExecutionCounts[eventID] = count;
                if (logStateChanges)
                    Logger.Log(LogModule.Event, $"Счётчик события '{eventID}' загружен из PlayerPrefs: {count}");
                return count;
            }
        }

        return 0;
    }

    public bool WasExecutedInSession(string eventID)
    {
        return executedInSession.Contains(eventID);
    }

    public void ResetEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Logger.LogWarning(LogModule.Event, "Попытка сброса пустого eventID");
            return;
        }

        if (eventStates.ContainsKey(eventID))
            eventStates[eventID] = false;

        if (eventExecutionCounts.ContainsKey(eventID))
            eventExecutionCounts[eventID] = 0;

        executedInSession.Remove(eventID);

        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + eventID;
            PlayerPrefs.DeleteKey(key);

            string countKey = saveKeyPrefix + "Count_" + eventID;
            PlayerPrefs.DeleteKey(countKey);

            PlayerPrefs.Save();
        }

        if (logStateChanges)
            Logger.Log(LogModule.Event, $"Событие '{eventID}' сброшено");
    }

    public void ResetAllEvents()
    {
        eventStates.Clear();
        executedInSession.Clear();
        eventExecutionCounts.Clear();

        if (usePlayerPrefs)
        {
            var keys = new List<string>();
            var allKeys = PlayerPrefs.GetString(saveKeyPrefix + "_keys", "").Split(',');
            foreach (var key in allKeys)
            {
                if (!string.IsNullOrEmpty(key))
                    keys.Add(key);
            }

            foreach (var key in keys)
            {
                PlayerPrefs.DeleteKey(key);
            }

            PlayerPrefs.DeleteKey(saveKeyPrefix + "_keys");
            PlayerPrefs.Save();
        }

        Logger.Log(LogModule.Event, "Все события сброшены");
    }

    public void SetEventData(string eventID, string key, object value)
    {
        string dataKey = $"{eventID}_{key}";
        Logger.Log(LogModule.Event, $"Установлены данные события: {dataKey}");
    }

    public T GetEventData<T>(string eventID, string key, T defaultValue = default)
    {
        string dataKey = $"{eventID}_{key}";
        return defaultValue;
    }

    public bool HasEventData(string eventID, string key)
    {
        string dataKey = $"{eventID}_{key}";
        return false;
    }

    public bool CanExecuteAgain(string eventID, int maxExecutions = 1)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        int count = GetExecutionCount(eventID);
        bool canExecute = count < maxExecutions;
        if (logStateChanges)
            Logger.Log(LogModule.Event, $"Событие '{eventID}' может быть выполнено повторно: {canExecute} (счётчик: {count}, максимум: {maxExecutions})");
        return canExecute;
    }

    public bool IsFirstExecutionThisSession(string eventID)
    {
        bool isFirst = !executedInSession.Contains(eventID);
        if (logStateChanges)
            Logger.Log(LogModule.Event, $"Событие '{eventID}' выполняется впервые в этой сессии: {isFirst}");
        return isFirst;
    }

    public void ResetEventFromPrefs(string eventID)
    {
        if (string.IsNullOrEmpty(eventID))
        {
            Logger.LogWarning(LogModule.Event, "Попытка сброса пустого eventID из PlayerPrefs");
            return;
        }

        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + eventID;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Logger.Log(LogModule.Event, $"Удалён ключ: {key}");
            }

            string countKey = saveKeyPrefix + "Count_" + eventID;
            if (PlayerPrefs.HasKey(countKey))
            {
                PlayerPrefs.DeleteKey(countKey);
                PlayerPrefs.Save();
                Logger.Log(LogModule.Event, $"Удалён ключ: {countKey}");
            }
        }

        ResetEvent(eventID);
    }

#if UNITY_EDITOR
    [ContextMenu("Сбросить все события (Editor)")]
    public void ResetAllEventsEditor()
    {
        ResetAllEvents();
        Logger.Log(LogModule.Event, "Все события сброшены через инспектор");
    }

    [ContextMenu("Очистить все PlayerPrefs")]
    public void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        eventStates.Clear();
        executedInSession.Clear();
        eventExecutionCounts.Clear();
        Logger.Log(LogModule.Event, "Все PlayerPrefs очищены");
    }
#endif
}
