using UnityEngine;
using System.Collections.Generic;
using System;
using Newtonsoft.Json;

[Serializable]
public class EventState
{
    public bool isExecuted;
    public int executionCount;
    public string lastExecuted = "";
}

[DefaultExecutionOrder(-9300)]
public class EventStateManager : MonoBehaviour
{
    [Header("State Storage")]
    [SerializeField] private bool logStateChanges = true;

    private Dictionary<string, bool> eventStates = new Dictionary<string, bool>();
    private Dictionary<string, int> eventExecutionCounts = new Dictionary<string, int>();
    private Dictionary<string, string> eventLastExecuted = new Dictionary<string, string>();
    private HashSet<string> executedInSession = new HashSet<string>();

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    public void MarkExecuted(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return;

        eventStates[eventID] = true;
        executedInSession.Add(eventID);
        eventLastExecuted[eventID] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        if (!eventExecutionCounts.ContainsKey(eventID)) eventExecutionCounts[eventID] = 0;
        eventExecutionCounts[eventID]++;

        if (logStateChanges)
            Logger.LogVerbose(LogModule.Event, $"Событие '{eventID}' отмечено (счётчик: {eventExecutionCounts[eventID]})");
    }

    public bool IsExecuted(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return eventStates.TryGetValue(eventID, out bool value) && value;
    }

    public int GetExecutionCount(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return 0;
        return eventExecutionCounts.TryGetValue(eventID, out int count) ? count : 0;
    }

    public bool WasExecutedInSession(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return executedInSession.Contains(eventID);
    }

    public void ResetEvent(string eventID)
    {
        if (string.IsNullOrEmpty(eventID)) return;

        eventStates[eventID] = false;
        eventExecutionCounts[eventID] = 0;
        eventLastExecuted.Remove(eventID);
        executedInSession.Remove(eventID);

        if (logStateChanges)
            Logger.Log(LogModule.Event, $"Событие '{eventID}' сброшено");
    }

    public void ResetAllEvents()
    {
        eventStates.Clear();
        executedInSession.Clear();
        eventExecutionCounts.Clear();
        eventLastExecuted.Clear();
    }

    public bool CanExecuteAgain(string eventID, int maxExecutions = 1)
    {
        if (string.IsNullOrEmpty(eventID)) return false;
        return GetExecutionCount(eventID) < maxExecutions;
    }

    public bool IsFirstExecutionThisSession(string eventID) => !executedInSession.Contains(eventID);

    public void ClearAllData() => ResetAllEvents();

    public Dictionary<string, EventState> ExportState()
    {
        var result = new Dictionary<string, EventState>();
        foreach (var kvp in eventStates)
        {
            result[kvp.Key] = new EventState
            {
                isExecuted = kvp.Value,
                executionCount = eventExecutionCounts.TryGetValue(kvp.Key, out int c) ? c : 0,
                lastExecuted = eventLastExecuted.TryGetValue(kvp.Key, out string l) ? (l ?? "") : ""
            };
        }
        return result;
    }

    public void ImportState(Dictionary<string, EventState> data)
    {
        eventStates.Clear();
        eventExecutionCounts.Clear();
        eventLastExecuted.Clear();
        executedInSession.Clear();

        if (data == null) return;

        foreach (var kvp in data)
        {
            if (string.IsNullOrEmpty(kvp.Key) || kvp.Value == null) continue;
            eventStates[kvp.Key] = kvp.Value.isExecuted;
            eventExecutionCounts[kvp.Key] = kvp.Value.executionCount;
            eventLastExecuted[kvp.Key] = kvp.Value.lastExecuted ?? "";
        }
    }
}