using UnityEngine;
using System.Collections.Generic;
using VContainer;

[CreateAssetMenu(fileName = "Событие", menuName = "|Система событий|/Событие")]
public class GameEvent : ScriptableObject
{
    [Header("Event Identification")]
    public string eventID;
    public string eventName;
    [TextArea(2, 4)]
    public string description;

    [Header("Trigger Conditions")]
    public EventTriggerType triggerType = EventTriggerType.None;
    [SerializeReference] public TriggerCondition triggerCondition;

    [Header("Execution Control")]
    public ExecutionPolicy executionPolicy = ExecutionPolicy.ExecuteOnce;
    public int maxExecutions = 1;
    public bool resetOnSceneLoad = false;
    public string dependsOnEventID;
    public string mutuallyExclusiveWithEventID;

    [Header("Actions")]
    public bool executeOnce = false;
    public bool executeOnAwake = false;
    public float delayBeforeExecute = 0.5f;
    [SerializeReference] public List<EventAction> actions = new List<EventAction>();

    [Header("Requirements (AND)")]
    [SerializeReference] public List<EventRequirement> requirements = new List<EventRequirement>();

    [System.NonSerialized]
    private bool hasBeenExecuted = false;

    public bool HasBeenExecuted => hasBeenExecuted;

    private bool ShouldLog
    {
        get
        {
            var em = EventManagerRef;
            return em != null && em.logEvents;
        }
    }

    private EventManager EventManagerRef => GameLifetimeScope.Current?.Container?.Resolve<EventManager>();

    public bool CanExecute(EventContext context)
    {
        var container = context?.Container;
        var stateManager = container?.Resolve<EventStateManager>();

        if (executionPolicy == ExecutionPolicy.ExecuteOnce)
        {
            if (stateManager?.IsExecuted(eventID) ?? hasBeenExecuted)
            {
                if (ShouldLog)
                    Logger.LogVerbose(LogModule.Event, $"Событие '{eventID}' уже было выполнено один раз");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecuteMultiple)
        {
            int count = stateManager?.GetExecutionCount(eventID) ?? 0;
            if (count >= maxExecutions)
            {
                if (ShouldLog)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' достигло максимума выполнений ({maxExecutions})");
                return false;
            }
        }
        else if (executionPolicy == ExecutionPolicy.ExecutePerSession)
        {
            if (stateManager?.WasExecutedInSession(eventID) ?? false)
            {
                if (ShouldLog)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' уже было выполнено в этой сессии");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(dependsOnEventID))
        {
            bool dependsExecuted = stateManager?.IsExecuted(dependsOnEventID) ?? false;
            if (!dependsExecuted)
            {
                if (ShouldLog)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' зависит от '{dependsOnEventID}', которое не выполнено");
                return false;
            }
        }

        if (!string.IsNullOrEmpty(mutuallyExclusiveWithEventID))
        {
            bool exclusiveExecuted = stateManager?.IsExecuted(mutuallyExclusiveWithEventID) ?? false;
            if (exclusiveExecuted)
            {
                if (ShouldLog)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' взаимоисключающее с '{mutuallyExclusiveWithEventID}', которое уже выполнено");
                return false;
            }
        }

        foreach (var req in requirements)
        {
            if (req != null && !req.Check(context))
            {
                if (ShouldLog)
                    Logger.Log(LogModule.Event, $"Событие '{eventID}' не соответствует требованиям");
                return false;
            }
        }

        if (triggerCondition != null && !triggerCondition.Check(context))
        {
            if (ShouldLog)
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

        Logger.Log(LogModule.Event, $"Выполняется событие: {eventID} - {eventName}");

        bool anyError = false;
        foreach (var action in actions)
        {
            if (action == null) continue;
            try { action.Execute(context); }
            catch (System.Exception e)
            {
                anyError = true;
                Logger.LogError(LogModule.Event, $"Ошибка выполнения действия '{action.actionName}' в событии '{eventID}': {e.Message}");
            }
        }

        if (!anyError)
        {
            var stateManager = context?.Container?.Resolve<EventStateManager>();
            if (stateManager != null) stateManager.MarkExecuted(eventID);
            else Logger.LogWarning(LogModule.Event, $"EventStateManager недоступен! Состояние события '{eventID}' не сохранено");
        }

        EventManagerRef?.OnEventExecuted?.Invoke(this, context);
    }

    public void ResetExecution()
    {
        hasBeenExecuted = false;
        GameLifetimeScope.Current?.Container?.Resolve<EventStateManager>()?.ResetEvent(eventID);
        Logger.Log(LogModule.Event, $"Событие '{eventID}' сброшено");
    }

    public bool IsExecuted()
    {
        return hasBeenExecuted ||
               (GameLifetimeScope.Current?.Container?.Resolve<EventStateManager>()?.IsExecuted(eventID) ?? false);
    }
}