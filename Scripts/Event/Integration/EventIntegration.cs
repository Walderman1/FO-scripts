using UnityEngine;
using VContainer;

public static class EventIntegration
{
    private static EventManager GetManager() =>
        SceneInjector.Instance?.RootScope?.Resolve<EventManager>();

    private static VContainer.IObjectResolver GetContainer() =>
        SceneInjector.Instance?.CurrentSceneScope
        ?? SceneInjector.Instance?.RootScope
        ?? GameLifetimeScope.Current?.Container;

    public static void OnItemPickup(string itemId)
    {
        var em = GetManager();
        if (em == null) return;

        var ctx = new EventContext().WithItemId(itemId);
        ctx.Container = GetContainer();
        em.TriggerEvent(EventTriggerType.PickupItem, ctx);
    }

    public static void OnItemPickup(ItemSO item) => OnItemPickup(item != null ? item.id : "");

    [System.Obsolete("Используйте OnItemPickup(string) или OnItemPickup(ItemSO)")]
    public static void OnItemPickup(ItemType itemType)
    {
#pragma warning disable CS0618
        var so = SceneInjector.Instance?.RootScope?.Resolve<ItemDatabase>()?.GetItemByLegacyType(itemType);
        OnItemPickup(so);
#pragma warning restore CS0618
    }

    public static void OnLocationEnter(string locationName)
    {
        var em = GetManager();
        if (em == null) return;

        var ctx = new EventContext().WithLocation(locationName);
        ctx.Container = GetContainer();
        em.TriggerEvent(EventTriggerType.EnterLocation, ctx);
    }

    public static void OnItemEquip(string itemId)
    {
        var em = GetManager();
        if (em == null) return;

        var ctx = new EventContext().WithItemId(itemId);
        ctx.Container = GetContainer();
        em.TriggerEvent(EventTriggerType.EquipItem, ctx);
    }

    public static void OnItemEquip(ItemSO item) => OnItemEquip(item != null ? item.id : "");

    [System.Obsolete("Используйте OnItemEquip(string) или OnItemEquip(ItemSO)")]
    public static void OnItemEquip(ItemType itemType)
    {
#pragma warning disable CS0618
        var so = SceneInjector.Instance?.RootScope?.Resolve<ItemDatabase>()?.GetItemByLegacyType(itemType);
        OnItemEquip(so);
#pragma warning restore CS0618
    }

    public static void OnDialogueEnd(string dialogueID)
    {
        var em = GetManager();
        if (em == null) return;

        var ctx = new EventContext().WithDialogue(dialogueID);
        ctx.Container = GetContainer();
        em.TriggerEvent(EventTriggerType.DialogueEnd, ctx);
    }
}