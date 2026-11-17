using UnityEngine;
using DialogueSystem;
using VContainer;

[System.Serializable]
public abstract class EventRequirement
{
    public abstract bool Check(EventContext context);

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
public class RequirementHasItem : EventRequirement
{
    [Newtonsoft.Json.JsonIgnore]
    public ItemSO itemSO;

    public string itemSOId;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    public int minCount = 1;

    public override bool Check(EventContext context)
    {
        var so = ResolveItemSO(context);
        if (so == null) return false;

        var inv = Resolve<InventoryUIManager>(context);
        if (inv == null) return false;

        return inv.GetItemCount(so) >= minCount;
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
public class RequirementFlag : EventRequirement
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        bool current = Resolve<FlagManager>(context)?.GetFlag(flagName) ?? false;
        return current == flagValue;
    }
}

[System.Serializable]
public class RequirementLocation : EventRequirement
{
    public string locationName;

    public override bool Check(EventContext context)
    {
        string current = GetCurrentLocationName(context);
        return !string.IsNullOrEmpty(current) && current == locationName;
    }

    private string GetCurrentLocationName(EventContext context)
    {
        var tb = Resolve<TextBeginner>(context);
        if (tb != null && tb.CurrentLocation != null) return tb.CurrentLocation.name;
        if (!string.IsNullOrEmpty(context?.LocationName)) return context.LocationName;
        return string.Empty;
    }
}

[System.Serializable]
public class RequirementEventExecuted : EventRequirement
{
    public string eventID;
    public bool executed = true;

    public override bool Check(EventContext context)
    {
        bool isExecuted = Resolve<EventStateManager>(context)?.IsExecuted(eventID) ?? false;
        return isExecuted == executed;
    }
}

[System.Serializable]
public class RequirementEventExecutionCount : EventRequirement
{
    public string eventID;
    public int minCount = 1;
    public int maxCount = int.MaxValue;

    public override bool Check(EventContext context)
    {
        int count = Resolve<EventStateManager>(context)?.GetExecutionCount(eventID) ?? 0;
        return count >= minCount && count <= maxCount;
    }
}

[System.Serializable]
public class RequirementRandomChance : EventRequirement
{
    [Range(0f, 1f)] public float chance = 0.5f;
    public bool useSeed = false;
    public int seed = 0;

    public override bool Check(EventContext context)
    {
        if (useSeed)
        {
            Random.InitState(seed);
            float value = Random.value;
            Random.InitState(System.Environment.TickCount);
            return value <= chance;
        }
        return Random.value <= chance;
    }
}

[System.Serializable]
public class RequirementCustomCondition : EventRequirement
{
    public string methodName;
    public GameObject targetObject;

    public override bool Check(EventContext context)
    {
        if (targetObject == null || string.IsNullOrEmpty(methodName)) return true;

        var method = targetObject.GetType().GetMethod(methodName);
        if (method != null)
        {
            try { return (bool)method.Invoke(targetObject, new object[] { context }); }
            catch { return false; }
        }
        return false;
    }
}