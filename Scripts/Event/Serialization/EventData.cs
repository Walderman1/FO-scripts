using System;
using System.Collections.Generic;

[Serializable]
public class EventData
{
    public string eventID;
    public string eventName;
    public string description;
    public string triggerType;
    public TriggerConditionData triggerCondition;
    public string executionPolicy;
    public int maxExecutions = 1;
    public bool resetOnSceneLoad = false;
    public string dependsOnEventID;
    public string mutuallyExclusiveWithEventID;
    public bool executeOnce = false;
    public bool executeOnAwake = false;
    public float delayBeforeExecute = 0f;
    public List<EventActionData> actions = new List<EventActionData>();
    public List<RequirementData> requirements = new List<RequirementData>();
}

[Serializable]
public class TriggerConditionData
{
    public string type;
    public string targetItem;
    public bool requireNotEquipped;
    public string locationName;
    public bool exactMatch;
    public string flagName;
    public bool flagValue;
    public string customMethodName;
}

[Serializable]
public class EventActionData
{
    public string type;
    public float delay;
    public string actionName;

    public int dialogueFileIndex;
    public string dialogueID;

    public string itemType;
    public int amount;

    public string flagName;
    public bool flagValue;

    public string locationName;
    public float targetX;
    public float targetY;
    public bool usePositionOverride;

    public string soundClipPath;
    public float volume;

    public string prefabPath;
    public float spawnX;
    public float spawnY;
    public float spawnZ;
    public string parentPath;
    public string targetObjectPath;
    public float destroyDelay;
    public bool enable;

    public string eventID;
    public bool requiredState;
    public List<EventActionData> subActions = new List<EventActionData>();

    public EventActionData actionIfTrue;
    public EventActionData actionIfFalse;

    public string customKey;
    public string value;
}

[Serializable]
public class RequirementData
{
    public string type;

    public string itemType;
    public int minCount;

    public string flagName;
    public bool flagValue;

    public string locationName;

    public string eventID;
    public bool executed;

    public int maxCount;

    public float chance;
    public bool useSeed;
    public int seed;

    public string methodName;
    public string targetObjectPath;
}
