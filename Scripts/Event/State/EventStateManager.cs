// EventStateManager.cs
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
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void LoadStates()
    {
        if (!usePlayerPrefs) return;

        // Загружаем состояния из PlayerPrefs
        // Формат: EventState_[eventID] = 1 (выполнено) или 0 (не выполнено)
        // EventCount_[eventID] = количество выполнений
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
        if (string.IsNullOrEmpty(eventID)) return;

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
                Debug.Log($"✅ Event '{eventID}' marked as executed (count: {eventExecutionCounts[eventID]})");
        }
        else
        {
            if (logStateChanges)
                Debug.Log($"⚠️ Event '{eventID}' already executed, skipping mark");
        }
    }

    public bool IsExecuted(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        // Сначала проверяем состояние в памяти
        if (eventStates.TryGetValue(eventID, out bool value))
            return value;

        // Если нет в памяти, проверяем PlayerPrefs
        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + eventID;
            if (PlayerPrefs.HasKey(key))
            {
                bool savedState = PlayerPrefs.GetInt(key, 0) == 1;
                eventStates[eventID] = savedState;
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
        if (string.IsNullOrEmpty(eventID)) return;

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
            Debug.Log($"🔄 Event '{eventID}' reset");
    }

    public void ResetAllEvents()
    {
        eventStates.Clear();
        executedInSession.Clear();
        eventExecutionCounts.Clear();

        if (usePlayerPrefs)
        {
            // Удаляем все ключи с префиксом
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

        Debug.Log("🔄 Все события сброшены!");
    }

    // Установка пользовательских данных для события
    public void SetEventData(string eventID, string key, object value)
    {
        string dataKey = $"{eventID}_{key}";
        // Можно использовать отдельный словарь для данных
        // или сохранять в PlayerPrefs
    }

    public T GetEventData<T>(string eventID, string key, T defaultValue = default)
    {
        string dataKey = $"{eventID}_{key}";
        // Возвращаем значение или defaultValue
        return defaultValue;
    }

    public bool HasEventData(string eventID, string key)
    {
        string dataKey = $"{eventID}_{key}";
        // Проверяем наличие данных
        return false;
    }

    // Проверка: можно ли выполнить событие повторно
    public bool CanExecuteAgain(string eventID, int maxExecutions = 1)
    {
        if (string.IsNullOrEmpty(eventID)) return false;

        int count = GetExecutionCount(eventID);
        return count < maxExecutions;
    }

    // Проверка: выполнено ли событие в этой сессии
    public bool IsFirstExecutionThisSession(string eventID)
    {
        return !executedInSession.Contains(eventID);
    }

    // ✅ НОВЫЙ МЕТОД: Сброс конкретного события из PlayerPrefs
    public void ResetEventFromPrefs(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return;

        if (usePlayerPrefs)
        {
            string key = saveKeyPrefix + eventID;
            if (PlayerPrefs.HasKey(key))
            {
                PlayerPrefs.DeleteKey(key);
                PlayerPrefs.Save();
                Debug.Log($"🗑️ Удалён ключ: {key}");
            }

            string countKey = saveKeyPrefix + "Count_" + eventID;
            if (PlayerPrefs.HasKey(countKey))
            {
                PlayerPrefs.DeleteKey(countKey);
                PlayerPrefs.Save();
                Debug.Log($"🗑️ Удалён ключ: {countKey}");
            }
        }

        ResetEvent(eventID);
    }

#if UNITY_EDITOR
    [ContextMenu("🔄 Сбросить все события (Editor)")]
    public void ResetAllEventsEditor()
    {
        ResetAllEvents();
        Debug.Log("🔄 Все события сброшены через инспектор!");
    }

    [ContextMenu("🗑️ Очистить все PlayerPrefs")]
    public void ClearAllPlayerPrefs()
    {
        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        eventStates.Clear();
        executedInSession.Clear();
        eventExecutionCounts.Clear();
        Debug.Log("🗑️ Все PlayerPrefs очищены!");
    }
#endif
}