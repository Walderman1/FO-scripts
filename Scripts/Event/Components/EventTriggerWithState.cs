// EventTriggerWithState.cs
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
        }
        else
        {
            hasTriggered = EventStateManager.Instance?.IsExecuted($"Trigger_{triggerID}") ?? false;
        }
    }

    public void Trigger()
    {
        if (triggerOnlyOnce && hasTriggered)
        {
            Debug.Log($"⛔ Trigger '{triggerID}' already executed once");
            return;
        }

        foreach (var req in additionalRequirements)
        {
            if (!req.Check(new EventContext()))
            {
                Debug.Log($"⛔ Trigger '{triggerID}' requirements not met");
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

        Debug.Log($"✅ Trigger '{triggerID}' executed with {eventsToTrigger.Count} events");
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        EventStateManager.Instance?.ResetEvent($"Trigger_{triggerID}");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && triggerType == EventTriggerType.EnterLocation)
        {
            Trigger();
        }
    }

    private void OnMouseDown()
    {
        if (triggerType == EventTriggerType.Custom)
        {
            Trigger();
        }
    }

    public void TriggerCustom()
    {
        Trigger();
    }
}