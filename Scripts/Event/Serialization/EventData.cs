#pragma warning disable UAC1005, UAC1006

using System;
using System.Collections.Generic;

[Serializable]
public class EventData
{
    public string eventID;
    public string eventName;
    public string description;
    public string triggerType;
    public TriggerCondition triggerCondition;
    public string executionPolicy;
    public int maxExecutions = 1;
    public bool resetOnSceneLoad = false;
    public string dependsOnEventID;
    public string mutuallyExclusiveWithEventID;
    public bool executeOnce = false;
    public bool executeOnAwake = false;
    public float delayBeforeExecute = 0f;
    public List<EventAction> actions = new List<EventAction>();
    public List<EventRequirement> requirements = new List<EventRequirement>();
}

#pragma warning restore UAC1005, UAC1006