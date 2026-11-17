using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public static class EventConverter
{
    public static EventData ToEventData(GameEvent gameEvent)
    {
        if (gameEvent == null) return null;

        SyncAssetIds(gameEvent);

        return new EventData
        {
            eventID = gameEvent.eventID,
            eventName = gameEvent.eventName,
            description = gameEvent.description,
            triggerType = gameEvent.triggerType.ToString(),
            triggerCondition = gameEvent.triggerCondition,
            executionPolicy = gameEvent.executionPolicy.ToString(),
            maxExecutions = gameEvent.maxExecutions,
            resetOnSceneLoad = gameEvent.resetOnSceneLoad,
            dependsOnEventID = gameEvent.dependsOnEventID,
            mutuallyExclusiveWithEventID = gameEvent.mutuallyExclusiveWithEventID,
            executeOnce = gameEvent.executeOnce,
            executeOnAwake = gameEvent.executeOnAwake,
            delayBeforeExecute = gameEvent.delayBeforeExecute,
            actions = new List<EventAction>(gameEvent.actions),
            requirements = new List<EventRequirement>(gameEvent.requirements)
        };
    }

    private static void SyncAssetIds(GameEvent gameEvent)
    {
        if (gameEvent == null) return;

        SyncActionIds(gameEvent.actions);
        SyncRequirementIds(gameEvent.requirements);
        SyncTriggerConditionId(gameEvent.triggerCondition);
    }

    private static void SyncActionIds(List<EventAction> actions)
    {
        if (actions == null) return;

        foreach (var action in actions)
        {
            if (action == null) continue;

            if (action is EventActionAddItem add) add.SyncItemSOId();
            else if (action is EventActionRemoveItem rem) rem.SyncItemSOId();
            else if (action is EventActionMultiple multi) SyncActionIds(multi.subActions);
            else if (action is EventActionCheckEvent check)
            {
                SyncActionIds(new List<EventAction> { check.actionIfTrue, check.actionIfFalse });
            }
        }
    }

    private static void SyncRequirementIds(List<EventRequirement> requirements)
    {
        if (requirements == null) return;

        foreach (var req in requirements)
        {
            if (req is RequirementHasItem hasItem) hasItem.SyncItemSOId();
        }
    }

    private static void SyncTriggerConditionId(TriggerCondition condition)
    {
        if (condition is TriggerConditionItem condItem) condItem.SyncItemSOId();
    }

    private static void ResolveAssetReferences(GameEvent gameEvent)
    {
        var db = ResolveItemDatabase();
        ResolveActionRefs(gameEvent.actions, db);
        ResolveRequirementRefs(gameEvent.requirements, db);
        ResolveTriggerConditionRefs(gameEvent.triggerCondition, db);
    }

    private static ItemDatabase ResolveItemDatabase()
    {
        var refs = GameReferences.Instance;
        if (refs != null && refs.ItemDatabase != null) return refs.ItemDatabase;

        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope != null)
        {
            try { var db = scope.Resolve<ItemDatabase>(); if (db != null) return db; } catch { }
        }

        var root = SceneInjector.Instance?.RootScope;
        if (root != null)
        {
            try { return root.Resolve<ItemDatabase>(); } catch { }
        }

        return null;
    }

    private static void ResolveActionRefs(List<EventAction> actions, ItemDatabase db)
    {
        if (actions == null) return;

        foreach (var action in actions)
        {
            if (action == null) continue;

            if (action is EventActionAddItem add) add.ResolveItemSO();
            else if (action is EventActionRemoveItem rem) rem.ResolveItemSO();
            else if (action is EventActionMultiple multi) ResolveActionRefs(multi.subActions, db);
            else if (action is EventActionCheckEvent check)
            {
                ResolveActionRefs(new List<EventAction> { check.actionIfTrue, check.actionIfFalse }, db);
            }
        }
    }

    private static void ResolveRequirementRefs(List<EventRequirement> requirements, ItemDatabase db)
    {
        if (requirements == null) return;

        foreach (var req in requirements)
        {
            if (req is RequirementHasItem hasItem) hasItem.ResolveItemSO();
        }
    }

    private static void ResolveTriggerConditionRefs(TriggerCondition condition, ItemDatabase db)
    {
        if (condition is TriggerConditionItem condItem) condItem.ResolveItemSO();
    }

    public static GameEvent ToGameEvent(EventData data)
    {
        if (data == null) return null;

        var gameEvent = ScriptableObject.CreateInstance<GameEvent>();
        RestoreGameEvent(gameEvent, data);
        return gameEvent;
    }

    public static void RestoreToExistingGameEvent(GameEvent gameEvent, EventData data)
    {
        if (gameEvent == null || data == null) return;

        RestoreGameEvent(gameEvent, data);

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(gameEvent);
#endif
    }

    private static void RestoreGameEvent(GameEvent gameEvent, EventData data)
    {
        gameEvent.eventID = data.eventID;
        gameEvent.eventName = data.eventName;
        gameEvent.description = data.description;

        if (System.Enum.TryParse<EventTriggerType>(data.triggerType, out var triggerType))
            gameEvent.triggerType = triggerType;

        if (System.Enum.TryParse<ExecutionPolicy>(data.executionPolicy, out var policy))
            gameEvent.executionPolicy = policy;

        gameEvent.maxExecutions = data.maxExecutions;
        gameEvent.resetOnSceneLoad = data.resetOnSceneLoad;
        gameEvent.dependsOnEventID = data.dependsOnEventID;
        gameEvent.mutuallyExclusiveWithEventID = data.mutuallyExclusiveWithEventID;
        gameEvent.executeOnce = data.executeOnce;
        gameEvent.executeOnAwake = data.executeOnAwake;
        gameEvent.delayBeforeExecute = data.delayBeforeExecute;
        gameEvent.triggerCondition = data.triggerCondition;

        gameEvent.actions = data.actions != null
            ? new List<EventAction>(data.actions)
            : new List<EventAction>();

        gameEvent.requirements = data.requirements != null
            ? new List<EventRequirement>(data.requirements)
            : new List<EventRequirement>();

        ResolveAssetReferences(gameEvent);
    }
}