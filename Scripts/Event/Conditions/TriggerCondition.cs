using UnityEngine;
using DialogueSystem;
using VContainer;

[System.Serializable]
public abstract class TriggerCondition
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
public class TriggerConditionItem : TriggerCondition
{
    [Newtonsoft.Json.JsonIgnore]
    public ItemSO targetItemSO;

    public string targetItemSOId;

    [System.Obsolete("Используйте targetItemSO")]
    public ItemType targetItem = ItemType.None;

    public bool requireNotEquipped = false;

    public override bool Check(EventContext context)
    {
        string requiredId = GetRequiredItemId(context);
        if (string.IsNullOrEmpty(requiredId)) return false;

        string contextId = context.ItemId;
        if (string.IsNullOrEmpty(contextId))
        {
#pragma warning disable CS0618
            if (context.ItemType != ItemType.None)
                contextId = Resolve<ItemDatabase>(context)?.GetItemByLegacyType(context.ItemType)?.id;
#pragma warning restore CS0618
        }

        if (contextId != requiredId) return false;

        if (requireNotEquipped)
        {
            var so = ResolveItemSO(context);
            if (so == null) return false;
            bool isEquipped = Resolve<EquipmentSystem>(context)?.IsEquipped(so.equipmentType) ?? false;
            return !isEquipped;
        }
        return true;
    }

    public ItemSO ResolveItemSO() => ResolveItemSO(null);

    public ItemSO ResolveItemSO(EventContext context)
    {
        if (targetItemSO != null) return targetItemSO;

        var db = Resolve<ItemDatabase>(context);
        if (db == null) return null;

        if (!string.IsNullOrEmpty(targetItemSOId))
        {
            targetItemSO = db.GetItem(targetItemSOId);
            if (targetItemSO != null) return targetItemSO;
        }

#pragma warning disable CS0618
        if (targetItem != ItemType.None)
        {
            targetItemSO = db.GetItemByLegacyType(targetItem);
            return targetItemSO;
        }
#pragma warning restore CS0618

        return null;
    }

    public string GetRequiredItemId(EventContext context)
    {
        if (targetItemSO != null) return targetItemSO.id;
        if (!string.IsNullOrEmpty(targetItemSOId)) return targetItemSOId;
        var so = ResolveItemSO(context);
        return so != null ? so.id : "";
    }

    public void SyncItemSOId() => targetItemSOId = targetItemSO != null ? targetItemSO.id : "";
}

[System.Serializable]
public class TriggerConditionLocation : TriggerCondition
{
    public string locationName;
    public bool exactMatch = true;

    public override bool Check(EventContext context)
    {
        if (string.IsNullOrEmpty(locationName)) return true;

        string current = GetCurrentLocationName(context);

        if (exactMatch) return current == locationName;
        return !string.IsNullOrEmpty(current) && current.Contains(locationName);
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
public class TriggerConditionFlag : TriggerCondition
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        if (context != null && !string.IsNullOrEmpty(context.FlagName))
            return context.FlagName == flagName && context.FlagValue == flagValue;

        bool current = Resolve<FlagManager>(context)?.GetFlag(flagName) ?? false;
        return current == flagValue;
    }
}

[System.Serializable]
public class TriggerConditionCustom : TriggerCondition
{
    public string customMethodName;

    public override bool Check(EventContext context) => true;
}