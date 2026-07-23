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
        if (executionPolicy == ExecutionPolicy.ExecuteOnce)
        {
            if (EventStateManager.Instance?.IsExecuted(eventID) ?? hasBeenExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' уже было выполнено один раз");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecuteMultiple)
        {
            int count = EventStateManager.Instance?.GetExecutionCount(eventID) ?? 0;
            if (count >= maxExecutions)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' достигло максимума выполнений ({maxExecutions})");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecutePerSession)
        {
            if (EventStateManager.Instance?.WasExecutedInSession(eventID) ?? false)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' уже было выполнено в этой сессии");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(dependsOnEventID))
        {
            bool dependsExecuted = EventStateManager.Instance?.IsExecuted(dependsOnEventID) ?? false;
            if (!dependsExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' зависит от '{dependsOnEventID}', которое не выполнено");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(mutuallyExclusiveWithEventID))
        {
            bool exclusiveExecuted = EventStateManager.Instance?.IsExecuted(mutuallyExclusiveWithEventID) ?? false;
            if (exclusiveExecuted)
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' взаимоисключающее с '{mutuallyExclusiveWithEventID}', которое уже выполнено");
                return false;
            }
        }

        foreach (var req in requirements)
        {
            if (req != null && !req.Check(context))
            {
                if (EventManager.Instance != null && EventManager.Instance.logEvents)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' не соответствует требованиям");
                return false;
            }
        }

        if (triggerCondition != null && !triggerCondition.Check(context))
        {
            if (EventManager.Instance != null && EventManager.Instance.logEvents)
                Logger.Log(LogModule.Event, $"Событие '{eventID}' не соответствует условию триггера");
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

        hasBeenExecuted = true;

        if (EventStateManager.Instance != null)
        {
            EventStateManager.Instance.MarkExecuted(eventID);
        }
        else
        {
            Logger.LogWarning(LogModule.Event, $"EventStateManager.Instance отсутствует! Состояние события '{eventID}' не сохранено");
        }

        Logger.Log(LogModule.Event, $"Выполняется событие: {eventID} - {eventName}");

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
                    Logger.LogError(LogModule.Event, $"Ошибка выполнения действия '{action.actionName}' в событии '{eventID}': {e.Message}");
                }
            }
        }

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted?.Invoke(this, context);
        }
    }

    public void ResetExecution()
    {
        hasBeenExecuted = false;
        EventStateManager.Instance?.ResetEvent(eventID);
        Logger.Log(LogModule.Event, $"Событие '{eventID}' сброшено");
    }

    public bool IsExecuted()
    {
        return hasBeenExecuted || (EventStateManager.Instance?.IsExecuted(eventID) ?? false);
    }
}
