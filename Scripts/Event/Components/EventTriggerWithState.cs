using UnityEngine;
using System.Collections.Generic;
using VContainer;

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

    [Inject] private EventStateManager _eventStateManager;
    [Inject] private IObjectResolver _container;

    private bool hasTriggered = false;

    private void Start()
    {
        if (resetOnSceneLoad)
        {
            hasTriggered = false;
        }
        else
        {
            hasTriggered = _eventStateManager?.IsExecuted($"Trigger_{triggerID}") ?? false;
        }
    }

    public void Trigger()
    {
        if (triggerOnlyOnce && hasTriggered) return;

        foreach (var req in additionalRequirements)
        {
            var ctx = new EventContext().WithContainer(_container);
            if (!req.Check(ctx)) return;
        }

        hasTriggered = true;
        _eventStateManager?.MarkExecuted($"Trigger_{triggerID}");

        foreach (var evt in eventsToTrigger)
        {
            if (evt != null)
                evt.Execute(new EventContext()
                    .WithContainer(_container)
                    .WithLocation(gameObject.scene.name)
                    .WithData(this));
        }
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        _eventStateManager?.ResetEvent($"Trigger_{triggerID}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && triggerType == EventTriggerType.EnterLocation)
            Trigger();
    }

    private void OnMouseDown()
    {
        if (triggerType == EventTriggerType.Custom)
            Trigger();
    }

    public void TriggerCustom() => Trigger();
}