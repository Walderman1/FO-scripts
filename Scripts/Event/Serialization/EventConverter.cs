using UnityEngine;
using System.Collections.Generic;
using System;

public static class EventConverter
{
    public static EventData ToEventData(GameEvent gameEvent)
    {
        if (gameEvent == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка конвертации null GameEvent");
            return null;
        }

        EventData data = new EventData
        {
            eventID = gameEvent.eventID,
            eventName = gameEvent.eventName,
            description = gameEvent.description,
            triggerType = gameEvent.triggerType.ToString(),
            executionPolicy = gameEvent.executionPolicy.ToString(),
            maxExecutions = gameEvent.maxExecutions,
            resetOnSceneLoad = gameEvent.resetOnSceneLoad,
            dependsOnEventID = gameEvent.dependsOnEventID,
            mutuallyExclusiveWithEventID = gameEvent.mutuallyExclusiveWithEventID,
            executeOnce = gameEvent.executeOnce,
            executeOnAwake = gameEvent.executeOnAwake,
            delayBeforeExecute = gameEvent.delayBeforeExecute
        };

        if (gameEvent.triggerCondition != null)
        {
            data.triggerCondition = ConvertTriggerCondition(gameEvent.triggerCondition);
        }

        foreach (var action in gameEvent.actions)
        {
            if (action != null)
            {
                data.actions.Add(ConvertAction(action));
            }
        }

        foreach (var req in gameEvent.requirements)
        {
            if (req != null)
            {
                data.requirements.Add(ConvertRequirement(req));
            }
        }

        Logger.Log(LogModule.Event, $"Конвертация GameEvent в EventData: {gameEvent.eventID}");
        return data;
    }

    public static GameEvent ToGameEvent(EventData data)
    {
        if (data == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка конвертации null EventData");
            return null;
        }

        GameEvent gameEvent = ScriptableObject.CreateInstance<GameEvent>();
        RestoreGameEvent(gameEvent, data);
        Logger.Log(LogModule.Event, $"Конвертация EventData в GameEvent: {data.eventID}");
        return gameEvent;
    }

    public static void RestoreToExistingGameEvent(GameEvent gameEvent, EventData data)
    {
        if (gameEvent == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка восстановления в null GameEvent");
            return;
        }

        if (data == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка восстановления из null EventData");
            return;
        }

        RestoreGameEvent(gameEvent, data);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameEvent);
#endif

        Logger.Log(LogModule.Event, $"Восстановление GameEvent: {data.eventID} -> {gameEvent.eventID}");
    }

    private static void RestoreGameEvent(GameEvent gameEvent, EventData data)
    {
        gameEvent.eventID = data.eventID;
        gameEvent.eventName = data.eventName;
        gameEvent.description = data.description;

        if (Enum.TryParse<EventTriggerType>(data.triggerType, out var triggerType))
            gameEvent.triggerType = triggerType;

        if (Enum.TryParse<ExecutionPolicy>(data.executionPolicy, out var policy))
            gameEvent.executionPolicy = policy;

        gameEvent.maxExecutions = data.maxExecutions;
        gameEvent.resetOnSceneLoad = data.resetOnSceneLoad;
        gameEvent.dependsOnEventID = data.dependsOnEventID;
        gameEvent.mutuallyExclusiveWithEventID = data.mutuallyExclusiveWithEventID;
        gameEvent.executeOnce = data.executeOnce;
        gameEvent.executeOnAwake = data.executeOnAwake;
        gameEvent.delayBeforeExecute = data.delayBeforeExecute;

        if (data.triggerCondition != null)
        {
            gameEvent.triggerCondition = RestoreTriggerCondition(data.triggerCondition);
        }

        gameEvent.actions = new List<EventAction>();
        foreach (var actionData in data.actions)
        {
            if (actionData == null) continue;
            var action = RestoreAction(actionData);
            if (action != null)
            {
                gameEvent.actions.Add(action);
            }
        }

        gameEvent.requirements = new List<EventRequirement>();
        foreach (var reqData in data.requirements)
        {
            var req = RestoreRequirement(reqData);
            if (req != null)
                gameEvent.requirements.Add(req);
        }
    }

    public static TriggerConditionData ConvertTriggerCondition(TriggerCondition condition)
    {
        TriggerConditionData data = new TriggerConditionData();

        if (condition is TriggerConditionItem itemCond)
        {
            data.type = "Item";
            data.targetItem = itemCond.targetItem.ToString();
            data.requireNotEquipped = itemCond.requireNotEquipped;
        }
        else if (condition is TriggerConditionLocation locCond)
        {
            data.type = "Location";
            data.locationName = locCond.locationName;
            data.exactMatch = locCond.exactMatch;
        }
        else if (condition is TriggerConditionFlag flagCond)
        {
            data.type = "Flag";
            data.flagName = flagCond.flagName;
            data.flagValue = flagCond.flagValue;
        }
        else if (condition is TriggerConditionCustom customCond)
        {
            data.type = "Custom";
            data.customMethodName = customCond.customMethodName;
        }

        Logger.Log(LogModule.Event, $"Конвертация TriggerCondition: {data.type}");
        return data;
    }

    public static TriggerCondition RestoreTriggerCondition(TriggerConditionData data)
    {
        if (data == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка восстановления null TriggerConditionData");
            return null;
        }

        switch (data.type)
        {
            case "Item":
                var item = new TriggerConditionItem();
                if (Enum.TryParse<ItemType>(data.targetItem, out var itemType))
                    item.targetItem = itemType;
                item.requireNotEquipped = data.requireNotEquipped;
                Logger.Log(LogModule.Event, $"Восстановление TriggerCondition: Item");
                return item;

            case "Location":
                var loc = new TriggerConditionLocation();
                loc.locationName = data.locationName;
                loc.exactMatch = data.exactMatch;
                Logger.Log(LogModule.Event, $"Восстановление TriggerCondition: Location");
                return loc;

            case "Flag":
                var flag = new TriggerConditionFlag();
                flag.flagName = data.flagName;
                flag.flagValue = data.flagValue;
                Logger.Log(LogModule.Event, $"Восстановление TriggerCondition: Flag");
                return flag;

            case "Custom":
                var custom = new TriggerConditionCustom();
                custom.customMethodName = data.customMethodName;
                Logger.Log(LogModule.Event, $"Восстановление TriggerCondition: Custom");
                return custom;

            default:
                Logger.LogWarning(LogModule.Event, $"Неизвестный тип TriggerCondition: {data.type}");
                return null;
        }
    }

    public static EventActionData ConvertAction(EventAction action)
    {
        EventActionData data = new EventActionData();
        data.actionName = action.actionName;
        data.delay = action.delay;

        if (action is EventActionStartDialogue dialogue)
        {
            data.type = "StartDialogue";
            data.dialogueFileIndex = dialogue.dialogueFileIndex;
            data.dialogueID = dialogue.dialogueID;
        }
        else if (action is EventActionAddItem addItem)
        {
            data.type = "AddItem";
            data.itemType = addItem.itemType.ToString();
            data.amount = addItem.amount;
        }
        else if (action is EventActionRemoveItem removeItem)
        {
            data.type = "RemoveItem";
            data.itemType = removeItem.itemType.ToString();
            data.amount = removeItem.amount;
        }
        else if (action is EventActionSetFlag setFlag)
        {
            data.type = "SetFlag";
            data.flagName = setFlag.flagName;
            data.flagValue = setFlag.flagValue;
        }
        else if (action is EventActionTeleport teleport)
        {
            data.type = "Teleport";
            data.locationName = teleport.locationName;
            data.targetX = teleport.targetPosition.x;
            data.targetY = teleport.targetPosition.y;
            data.usePositionOverride = teleport.usePositionOverride;
        }
        else if (action is EventActionMultiple multiple)
        {
            data.type = "Multiple";
            foreach (var sub in multiple.subActions)
            {
                if (sub != null)
                    data.subActions.Add(ConvertAction(sub));
            }
        }
        else if (action is EventActionPlaySound sound)
        {
            data.type = "PlaySound";
            data.soundClipPath = sound.soundClip != null ? sound.soundClip.name : "";
            data.volume = sound.volume;
        }
        else if (action is EventActionSpawnObject spawn)
        {
            data.type = "SpawnObject";
            data.prefabPath = spawn.prefabToSpawn != null ? spawn.prefabToSpawn.name : "";
            data.spawnX = spawn.spawnPosition.x;
            data.spawnY = spawn.spawnPosition.y;
            data.spawnZ = spawn.spawnPosition.z;
            data.parentPath = spawn.parentTransform != null ? spawn.parentTransform.name : "";
        }
        else if (action is EventActionDestroyObject destroy)
        {
            data.type = "DestroyObject";
            data.targetObjectPath = destroy.targetObject != null ? destroy.targetObject.name : "";
            data.destroyDelay = destroy.destroyDelay;
        }
        else if (action is EventActionEnableObject enable)
        {
            data.type = "EnableObject";
            data.targetObjectPath = enable.targetObject != null ? enable.targetObject.name : "";
            data.enable = enable.enable;
        }
        else if (action is EventActionDisableObject disable)
        {
            data.type = "DisableObject";
            data.targetObjectPath = disable.targetObject != null ? disable.targetObject.name : "";
        }
        else if (action is EventActionEnableEvent enableEvent)
        {
            data.type = "EnableEvent";
            data.eventID = enableEvent.eventID;
            data.enable = enableEvent.enable;
        }
        else if (action is EventActionDisableEvent disableEvent)
        {
            data.type = "DisableEvent";
            data.eventID = disableEvent.eventID;
        }
        else if (action is EventActionCheckEvent checkEvent)
        {
            data.type = "CheckEvent";
            data.eventID = checkEvent.eventID;
            data.requiredState = checkEvent.requiredState;
            if (checkEvent.actionIfTrue != null)
                data.actionIfTrue = ConvertAction(checkEvent.actionIfTrue);
            if (checkEvent.actionIfFalse != null)
                data.actionIfFalse = ConvertAction(checkEvent.actionIfFalse);
        }
        else if (action is EventActionResetEvent resetEvent)
        {
            data.type = "ResetEvent";
            data.eventID = resetEvent.eventID;
        }
        else if (action is EventActionSaveState save)
        {
            data.type = "SaveState";
            data.customKey = save.customKey;
            data.value = save.value;
        }

        Logger.Log(LogModule.Event, $"Конвертация Action: {data.type}");
        return data;
    }

    public static EventAction RestoreAction(EventActionData data)
    {
        if (data == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка восстановления null EventActionData");
            return null;
        }

        switch (data.type)
        {
            case "StartDialogue":
                Logger.Log(LogModule.Event, $"Восстановление Action: StartDialogue");
                return new EventActionStartDialogue
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    dialogueFileIndex = data.dialogueFileIndex,
                    dialogueID = data.dialogueID
                };

            case "AddItem":
                var add = new EventActionAddItem();
                if (Enum.TryParse<ItemType>(data.itemType, out var itemType))
                    add.itemType = itemType;
                add.amount = data.amount;
                add.actionName = data.actionName;
                add.delay = data.delay;
                Logger.Log(LogModule.Event, $"Восстановление Action: AddItem");
                return add;

            case "RemoveItem":
                var remove = new EventActionRemoveItem();
                if (Enum.TryParse<ItemType>(data.itemType, out var rItemType))
                    remove.itemType = rItemType;
                remove.amount = data.amount;
                remove.actionName = data.actionName;
                remove.delay = data.delay;
                Logger.Log(LogModule.Event, $"Восстановление Action: RemoveItem");
                return remove;

            case "SetFlag":
                Logger.Log(LogModule.Event, $"Восстановление Action: SetFlag");
                return new EventActionSetFlag
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    flagName = data.flagName,
                    flagValue = data.flagValue
                };

            case "Teleport":
                Logger.Log(LogModule.Event, $"Восстановление Action: Teleport");
                return new EventActionTeleport
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    locationName = data.locationName,
                    targetPosition = new Vector2(data.targetX, data.targetY),
                    usePositionOverride = data.usePositionOverride
                };

            case "Multiple":
                var multiple = new EventActionMultiple();
                multiple.actionName = data.actionName;
                multiple.delay = data.delay;
                foreach (var sub in data.subActions)
                {
                    var restored = RestoreAction(sub);
                    if (restored != null)
                        multiple.subActions.Add(restored);
                }
                Logger.Log(LogModule.Event, $"Восстановление Action: Multiple с {multiple.subActions.Count} поддействиями");
                return multiple;

            case "PlaySound":
                Logger.Log(LogModule.Event, $"Восстановление Action: PlaySound");
                return new EventActionPlaySound
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    volume = data.volume
                };

            case "SpawnObject":
                Logger.Log(LogModule.Event, $"Восстановление Action: SpawnObject");
                return new EventActionSpawnObject
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    spawnPosition = new Vector3(data.spawnX, data.spawnY, data.spawnZ)
                };

            case "DestroyObject":
                Logger.Log(LogModule.Event, $"Восстановление Action: DestroyObject");
                return new EventActionDestroyObject
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    destroyDelay = data.destroyDelay
                };

            case "EnableObject":
                Logger.Log(LogModule.Event, $"Восстановление Action: EnableObject");
                return new EventActionEnableObject
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    enable = data.enable
                };

            case "DisableObject":
                Logger.Log(LogModule.Event, $"Восстановление Action: DisableObject");
                return new EventActionDisableObject
                {
                    actionName = data.actionName,
                    delay = data.delay
                };

            case "EnableEvent":
                Logger.Log(LogModule.Event, $"Восстановление Action: EnableEvent");
                return new EventActionEnableEvent
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    eventID = data.eventID,
                    enable = data.enable
                };

            case "DisableEvent":
                Logger.Log(LogModule.Event, $"Восстановление Action: DisableEvent");
                return new EventActionDisableEvent
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    eventID = data.eventID
                };

            case "CheckEvent":
                var checkEvent = new EventActionCheckEvent
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    eventID = data.eventID,
                    requiredState = data.requiredState
                };
                if (data.actionIfTrue != null)
                    checkEvent.actionIfTrue = RestoreAction(data.actionIfTrue);
                if (data.actionIfFalse != null)
                    checkEvent.actionIfFalse = RestoreAction(data.actionIfFalse);
                Logger.Log(LogModule.Event, $"Восстановление Action: CheckEvent");
                return checkEvent;

            case "ResetEvent":
                Logger.Log(LogModule.Event, $"Восстановление Action: ResetEvent");
                return new EventActionResetEvent
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    eventID = data.eventID
                };

            case "SaveState":
                Logger.Log(LogModule.Event, $"Восстановление Action: SaveState");
                return new EventActionSaveState
                {
                    actionName = data.actionName,
                    delay = data.delay,
                    customKey = data.customKey,
                    value = data.value
                };

            default:
                Logger.LogWarning(LogModule.Event, $"Неизвестный тип Action: {data.type}");
                return null;
        }
    }

    public static RequirementData ConvertRequirement(EventRequirement requirement)
    {
        RequirementData data = new RequirementData();

        if (requirement is RequirementHasItem hasItem)
        {
            data.type = "HasItem";
            data.itemType = hasItem.itemType.ToString();
            data.minCount = hasItem.minCount;
        }
        else if (requirement is RequirementFlag flag)
        {
            data.type = "Flag";
            data.flagName = flag.flagName;
            data.flagValue = flag.flagValue;
        }
        else if (requirement is RequirementLocation loc)
        {
            data.type = "Location";
            data.locationName = loc.locationName;
        }
        else if (requirement is RequirementEventExecuted evt)
        {
            data.type = "EventExecuted";
            data.eventID = evt.eventID;
            data.executed = evt.executed;
        }
        else if (requirement is RequirementEventExecutionCount count)
        {
            data.type = "EventExecutionCount";
            data.eventID = count.eventID;
            data.minCount = count.minCount;
            data.maxCount = count.maxCount;
        }
        else if (requirement is RequirementRandomChance chance)
        {
            data.type = "RandomChance";
            data.chance = chance.chance;
            data.useSeed = chance.useSeed;
            data.seed = chance.seed;
        }
        else if (requirement is RequirementCustomCondition custom)
        {
            data.type = "CustomCondition";
            data.methodName = custom.methodName;
            data.targetObjectPath = custom.targetObject != null ? custom.targetObject.name : "";
        }

        Logger.Log(LogModule.Event, $"Конвертация Requirement: {data.type}");
        return data;
    }

    public static EventRequirement RestoreRequirement(RequirementData data)
    {
        if (data == null)
        {
            Logger.LogWarning(LogModule.Event, "Попытка восстановления null RequirementData");
            return null;
        }

        switch (data.type)
        {
            case "HasItem":
                var hasItem = new RequirementHasItem();
                if (Enum.TryParse<ItemType>(data.itemType, out var itemType))
                    hasItem.itemType = itemType;
                hasItem.minCount = data.minCount;
                Logger.Log(LogModule.Event, $"Восстановление Requirement: HasItem");
                return hasItem;

            case "Flag":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: Flag");
                return new RequirementFlag
                {
                    flagName = data.flagName,
                    flagValue = data.flagValue
                };

            case "Location":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: Location");
                return new RequirementLocation
                {
                    locationName = data.locationName
                };

            case "EventExecuted":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: EventExecuted");
                return new RequirementEventExecuted
                {
                    eventID = data.eventID,
                    executed = data.executed
                };

            case "EventExecutionCount":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: EventExecutionCount");
                return new RequirementEventExecutionCount
                {
                    eventID = data.eventID,
                    minCount = data.minCount,
                    maxCount = data.maxCount
                };

            case "RandomChance":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: RandomChance");
                return new RequirementRandomChance
                {
                    chance = data.chance,
                    useSeed = data.useSeed,
                    seed = data.seed
                };

            case "CustomCondition":
                Logger.Log(LogModule.Event, $"Восстановление Requirement: CustomCondition");
                return new RequirementCustomCondition
                {
                    methodName = data.methodName
                };

            default:
                Logger.LogWarning(LogModule.Event, $"Неизвестный тип Requirement: {data.type}");
                return null;
        }
    }
}
