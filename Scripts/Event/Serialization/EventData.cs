// EventData.cs
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

    // Диалог
    public int dialogueFileIndex;
    public string dialogueID;

    // Предметы
    public string itemType;
    public int amount;

    // Флаги
    public string flagName;
    public bool flagValue;

    // Телепорт
    public string locationName;
    public float targetX;
    public float targetY;
    public bool usePositionOverride;

    // Звук
    public string soundClipPath;
    public float volume;

    // Объекты
    public string prefabPath;
    public float spawnX;
    public float spawnY;
    public float spawnZ;
    public string parentPath;
    public string targetObjectPath;
    public float destroyDelay;
    public bool enable;

    // События
    public string eventID;
    public bool requiredState;
    public List<EventActionData> subActions = new List<EventActionData>();

    // Для CheckEvent - вложенные действия
    public EventActionData actionIfTrue;
    public EventActionData actionIfFalse;

    // Сохранение
    public string customKey;
    public string value;
}

[Serializable]
public class RequirementData
{
    public string type;

    // Для HasItem
    public string itemType;
    public int minCount;

    // Для Flag
    public string flagName;
    public bool flagValue;

    // Для Location
    public string locationName;

    // Для EventExecuted
    public string eventID;
    public bool executed;

    // Для EventExecutionCount
    public int maxCount;

    // Для RandomChance
    public float chance;
    public bool useSeed;
    public int seed;

    // Для CustomCondition
    public string methodName;
    public string targetObjectPath;
}