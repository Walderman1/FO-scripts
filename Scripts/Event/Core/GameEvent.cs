// GameEvent.cs
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewGameEvent", menuName = "Game/Event System/Game Event")]
public class GameEvent : ScriptableObject
{
    [Header("Event Identification")]
    public string eventID;
    public string eventName;
    [TextArea(2, 4)]
    public string description;

    [Header("Trigger Conditions")]
    public EventTriggerType triggerType = EventTriggerType.None;
    public TriggerCondition triggerCondition;

    [Header("Execution Control")]
    public ExecutionPolicy executionPolicy = ExecutionPolicy.ExecuteOnce;
    public int maxExecutions = 1;
    public bool resetOnSceneLoad = false;
    public string dependsOnEventID;
    public string mutuallyExclusiveWithEventID;

    [Header("Actions")]
    public bool executeOnce = false;
    public bool executeOnAwake = false;
    public float delayBeforeExecute = 0f;
    public List<EventAction> actions = new List<EventAction>();

    [Header("Requirements (AND)")]
    public List<EventRequirement> requirements = new List<EventRequirement>();

    [System.NonSerialized]
    private bool hasBeenExecuted = false;

    public bool HasBeenExecuted => hasBeenExecuted;

    public bool CanExecute(EventContext context)
    {
        // Проверка политики выполнения
        if (executionPolicy == ExecutionPolicy.ExecuteOnce)
        {
            if (EventStateManager.Instance?.IsExecuted(eventID) ?? hasBeenExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' already executed once");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecuteMultiple)
        {
            int count = EventStateManager.Instance?.GetExecutionCount(eventID) ?? 0;
            if (count >= maxExecutions)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' reached max executions ({maxExecutions})");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecutePerSession)
        {
            if (EventStateManager.Instance?.WasExecutedInSession(eventID) ?? false)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' already executed in this session");
                return false;
            }
        }

        // Проверка зависимости от другого события
        if (!string.IsNullOrEmpty(dependsOnEventID))
        {
            bool dependsExecuted = EventStateManager.Instance?.IsExecuted(dependsOnEventID) ?? false;
            if (!dependsExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' depends on '{dependsOnEventID}' which is not executed");
                return false;
            }
        }

        // Проверка взаимоисключения
        if (!string.IsNullOrEmpty(mutuallyExclusiveWithEventID))
        {
            bool exclusiveExecuted = EventStateManager.Instance?.IsExecuted(mutuallyExclusiveWithEventID) ?? false;
            if (exclusiveExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' is mutually exclusive with '{mutuallyExclusiveWithEventID}' which is already executed");
                return false;
            }
        }

        // Проверка требований
        foreach (var req in requirements)
        {
            if (req != null && !req.Check(context))
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Debug.Log($"⛔ Event '{eventID}' requirement not met");
                return false;
            }
        }

        // Проверка триггера
        if (triggerCondition != null && !triggerCondition.Check(context))
        {
            if (EventManager.Instance != null && EventManager.Instance.logEvents)
                Debug.Log($"⛔ Event '{eventID}' trigger condition not met");
            return false;
        }

        return true;
    }

    public bool CheckRequirements(EventContext context)
    {
        if (requirements.Count == 0) return true;

        foreach (var req in requirements)
        {
            if (req == null) continue;
            if (!req.Check(context))
                return false;
        }
        return true;
    }

    public bool CheckTrigger(EventContext context)
    {
        if (triggerCondition == null) return true;
        return triggerCondition.Check(context);
    }

    public void Execute(EventContext context)
    {
        if (!CanExecute(context))
            return;

        // Помечаем как выполненное
        hasBeenExecuted = true;

        if (EventStateManager.Instance != null)
        {
            EventStateManager.Instance.MarkExecuted(eventID);
        }
        else
        {
            Debug.LogWarning($"EventStateManager.Instance is null! Event '{eventID}' state not saved.");
        }

        Debug.Log($"✅ Executing event: {eventID} - {eventName}");

        // Выполняем все действия
        foreach (var action in actions)
        {
            if (action != null)
            {
                try
                {
                    action.Execute(context);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error executing action '{action.actionName}' in event '{eventID}': {e.Message}");
                }
            }
        }

        // Оповещаем систему
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted?.Invoke(this, context);
        }
    }

    public void ResetExecution()
    {
        hasBeenExecuted = false;
        EventStateManager.Instance?.ResetEvent(eventID);
        Debug.Log($"🔄 Event '{eventID}' reset");
    }

    public bool IsExecuted()
    {
        return hasBeenExecuted || (EventStateManager.Instance?.IsExecuted(eventID) ?? false);
    }
}