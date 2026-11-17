using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogueSystem;
using VContainer;

[System.Serializable]
public abstract class EventAction
{
    public string actionName = "Action";
    public float delay = 0.5f;

    public void Execute(EventContext context)
    {
        if (delay > 0f)
        {
            var em = Resolve<EventManager>(context);
            if (em != null) em.RunCoroutine(DelayedExecute(context));
            else ExecuteImmediate(context);
        }
        else ExecuteImmediate(context);
    }

    private IEnumerator DelayedExecute(EventContext context)
    {
        yield return new WaitForSeconds(delay);
        ExecuteImmediate(context);
    }

    protected abstract void ExecuteImmediate(EventContext context);

    protected static T Resolve<T>(EventContext context) where T : class
    {
        var c = context?.Container;
        if (c != null)
        {
            try { var v = c.Resolve<T>(); if (v != null) return v; } catch { }
        }

        var root = SceneInjector.Instance?.RootScope ?? GameLifetimeScope.Current?.Container;
        if (root != null)
        {
            try { return root.Resolve<T>(); } catch { }
        }
        return null;
    }
}

[System.Serializable]
public class EventActionStartDialogue : EventAction
{
    public string dialogueFileName = "Prologue";
    public string dialogueID;

    protected override void ExecuteImmediate(EventContext context)
    {
        var tb = Resolve<TextBeginner>(context);
        if (tb != null) tb.StartDialogue(dialogueFileName);
        else Logger.LogError(LogModule.Event, "TextBeginner НЕ НАЙДЕН");
    }
}

[System.Serializable]
public class EventActionAddItem : EventAction
{
    [Newtonsoft.Json.JsonIgnore]
    public ItemSO itemSO;

    public string itemSOId;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    public int amount = 1;

    protected override void ExecuteImmediate(EventContext context)
    {
        var so = ResolveItemSO(context);
        if (so == null) { Logger.LogWarning(LogModule.Event, "EventActionAddItem: предмет не задан"); return; }

        var inv = Resolve<InventoryUIManager>(context);
        if (inv == null) { Logger.LogWarning(LogModule.Event, "InventoryUIManager не найден"); return; }

        int added = inv.AddItem(so, amount);
        Logger.Log(LogModule.Event, $"Добавлено {added}x {so.id} в инвентарь");
    }

    public ItemSO ResolveItemSO() => ResolveItemSO(null);

    public ItemSO ResolveItemSO(EventContext context)
    {
        if (itemSO != null) return itemSO;

        var db = Resolve<ItemDatabase>(context);
        if (db == null) return null;

        if (!string.IsNullOrEmpty(itemSOId))
        {
            itemSO = db.GetItem(itemSOId);
            if (itemSO != null) return itemSO;
        }

#pragma warning disable CS0618
        if (itemType != ItemType.None)
        {
            itemSO = db.GetItemByLegacyType(itemType);
            return itemSO;
        }
#pragma warning restore CS0618

        return null;
    }

    public void SyncItemSOId() => itemSOId = itemSO != null ? itemSO.id : "";
}

[System.Serializable]
public class EventActionRemoveItem : EventAction
{
    [Newtonsoft.Json.JsonIgnore]
    public ItemSO itemSO;

    public string itemSOId;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    public int amount = 1;

    protected override void ExecuteImmediate(EventContext context)
    {
        var so = ResolveItemSO(context);
        if (so == null) return;

        var inv = Resolve<InventoryUIManager>(context);
        if (inv == null) return;

        if (!inv.RemoveItem(so, amount))
            Logger.LogWarning(LogModule.Event, $"Не удалось удалить {amount}x {so.id}");
    }

    public ItemSO ResolveItemSO() => ResolveItemSO(null);

    public ItemSO ResolveItemSO(EventContext context)
    {
        if (itemSO != null) return itemSO;

        var db = Resolve<ItemDatabase>(context);
        if (db == null) return null;

        if (!string.IsNullOrEmpty(itemSOId))
        {
            itemSO = db.GetItem(itemSOId);
            if (itemSO != null) return itemSO;
        }

#pragma warning disable CS0618
        if (itemType != ItemType.None)
        {
            itemSO = db.GetItemByLegacyType(itemType);
            return itemSO;
        }
#pragma warning restore CS0618

        return null;
    }

    public void SyncItemSOId() => itemSOId = itemSO != null ? itemSO.id : "";
}

[System.Serializable]
public class EventActionSetFlag : EventAction
{
    public string flagName;
    public bool flagValue = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        var fm = Resolve<FlagManager>(context);
        fm?.SetFlag(flagName, flagValue);
    }
}

[System.Serializable]
public class EventActionTeleport : EventAction
{
    public string locationName;
    public Vector2 targetPosition;
    public bool usePositionOverride = false;

    protected override void ExecuteImmediate(EventContext context)
    {
        Resolve<SceneSlideTransition>(context)?.SwitchToScene(locationName);
    }
}

[System.Serializable]
public class EventActionMultiple : EventAction
{
    public List<EventAction> subActions = new List<EventAction>();

    protected override void ExecuteImmediate(EventContext context)
    {
        foreach (var action in subActions)
            if (action != null) action.Execute(context);
    }
}

[System.Serializable]
public class EventActionPlaySound : EventAction
{
    public AudioClip soundClip;
    public float volume = 1f;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (soundClip != null) AudioSource.PlayClipAtPoint(soundClip, Vector3.zero, volume);
    }
}

[System.Serializable]
public class EventActionSpawnObject : EventAction
{
    public GameObject prefabToSpawn;
    public Vector3 spawnPosition;
    public Transform parentTransform;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (prefabToSpawn == null) return;

        Vector3 position = spawnPosition;
        if (context != null && context.TargetObject != null && parentTransform == null)
            position = context.TargetObject.transform.position;

        GameObject obj = Object.Instantiate(prefabToSpawn, position, Quaternion.identity);
        if (parentTransform != null) obj.transform.SetParent(parentTransform);
    }
}

[System.Serializable]
public class EventActionDestroyObject : EventAction
{
    public GameObject targetObject;
    public float destroyDelay = 0f;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null) Object.Destroy(targetObject, destroyDelay);
    }
}

[System.Serializable]
public class EventActionEnableObject : EventAction
{
    public GameObject targetObject;
    public bool enable = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null) targetObject.SetActive(enable);
    }
}

[System.Serializable]
public class EventActionDisableObject : EventAction
{
    public GameObject targetObject;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (targetObject != null) targetObject.SetActive(false);
    }
}

[System.Serializable]
public class EventActionEnableEvent : EventAction
{
    public string eventID;
    public bool enable = true;

    protected override void ExecuteImmediate(EventContext context)
    {
        var sm = Resolve<EventStateManager>(context);
        if (enable) sm?.ResetEvent(eventID);
        else sm?.MarkExecuted(eventID);
    }
}

[System.Serializable]
public class EventActionDisableEvent : EventAction
{
    public string eventID;

    protected override void ExecuteImmediate(EventContext context)
    {
        Resolve<EventStateManager>(context)?.MarkExecuted(eventID);
    }
}

[System.Serializable]
public class EventActionCheckEvent : EventAction
{
    public string eventID;
    public bool requiredState = true;
    public EventAction actionIfTrue;
    public EventAction actionIfFalse;

    protected override void ExecuteImmediate(EventContext context)
    {
        bool state = Resolve<EventStateManager>(context)?.IsExecuted(eventID) ?? false;

        if (state == requiredState) actionIfTrue?.Execute(context);
        else actionIfFalse?.Execute(context);
    }
}

[System.Serializable]
public class EventActionResetEvent : EventAction
{
    public string eventID;

    protected override void ExecuteImmediate(EventContext context)
    {
        Resolve<EventStateManager>(context)?.ResetEvent(eventID);
    }
}

[System.Serializable]
public class EventActionSaveState : EventAction
{
    public string customKey;
    public string value;

    protected override void ExecuteImmediate(EventContext context)
    {
        if (string.IsNullOrEmpty(customKey)) return;
        PlayerPrefs.SetString(customKey, value);
        PlayerPrefs.Save();
    }
}
[System.Serializable]
public class EventActionStartQuest : EventAction
{
    public string questID;

    protected override void ExecuteImmediate(EventContext context)
    {
        var qm = Resolve<QuestManager>(context);
        if (qm == null)
        {
            Logger.LogWarning(LogModule.Event, "EventActionStartQuest: QuestManager недоступен");
            return;
        }

        qm.StartQuest(questID);
        Logger.Log(LogModule.Event, $"StartQuest: {questID}");
    }
}

[System.Serializable]
public class EventActionCompleteQuest : EventAction
{
    public string questID;

    protected override void ExecuteImmediate(EventContext context)
    {
        var qm = Resolve<QuestManager>(context);
        if (qm == null)
        {
            Logger.LogWarning(LogModule.Event, "EventActionCompleteQuest: QuestManager недоступен");
            return;
        }

        qm.CompleteQuest(questID);
        Logger.Log(LogModule.Event, $"CompleteQuest: {questID}");
    }
}