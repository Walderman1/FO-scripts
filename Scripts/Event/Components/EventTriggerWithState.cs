using UnityEngine;
using System.Collections.Generic;

public class EventTriggerWithState : MonoBehaviour
{
    [Header("Trigger Configuration")]
    public EventTriggerType triggerType = EventTriggerType.Custom;
    public string customTriggerID;
    public List<GameEvent> eventsToTrigger = new List<GameEvent>();

    [Header("State Control")]
    public bool triggerOnlyOnce = false;
    public bool resetOnSceneLoad = false;
    public string triggerID;

    [Header("Conditions")]
    public List<EventRequirement> additionalRequirements = new List<EventRequirement>();

    private bool hasTriggered = false;

    private void Start()
    {
        if (resetOnSceneLoad)
        {
            hasTriggered = false;
            Logger.Log(LogModule.Event, $"Триггер '{triggerID}' сброшен при загрузке сцены");
        }
        else
        {
            hasTriggered = EventStateManager.Instance?.IsExecuted($"Trigger_{triggerID}") ?? false;
            if (hasTriggered)
            {
                Logger.Log(LogModule.Event, $"Триггер '{triggerID}' уже был выполнен ранее");
            }
        }
    }

    public void Trigger()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            Logger.Log(LogModule.Event, $"Триггер '{triggerID}' уже выполнен, пропуск");
            return;
        }

        foreach (var req in additionalRequirements)
        {
            if (!req.Check(new EventContext()))
            {
                Logger.Log(LogModule.Event, $"Триггер '{triggerID}' не соответствует требованиям");
                return;
            }
        }

        hasTriggered = true;
        EventStateManager.Instance?.MarkExecuted($"Trigger_{triggerID}");

        foreach (var evt in eventsToTrigger)
        {
            if (evt != null)
            {
                evt.Execute(new EventContext()
                    .WithLocation(gameObject.scene.name)
                    .WithData(this));
            }
        }

        Logger.Log(LogModule.Event, $"Триггер '{triggerID}' выполнен, запущено {eventsToTrigger.Count} событий");
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        EventStateManager.Instance?.ResetEvent($"Trigger_{triggerID}");
        Logger.Log(LogModule.Event, $"Триггер '{triggerID}' сброшен");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && triggerType == EventTriggerType.EnterLocation)
        {
            Logger.Log(LogModule.Event, $"Игрок вошёл в триггер '{triggerID}'");
            Trigger();
        }
    }

    private void OnMouseDown()
    {
        if (triggerType == EventTriggerType.Custom)
        {
            Logger.Log(LogModule.Event, $"Клик по триггеру '{triggerID}'");
            Trigger();
        }
    }

    public void TriggerCustom()
    {
        Logger.Log(LogModule.Event, $"Ручной вызов триггера '{triggerID}'");
        Trigger();
    }
}
