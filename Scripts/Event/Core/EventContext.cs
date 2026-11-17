using UnityEngine;
using VContainer;

public class EventContext
{
    public GameObject SourceObject { get; set; }
    public GameObject TargetObject { get; set; }

    public ItemSO ItemSO { get; set; }
    public string ItemId { get; set; }

    [System.Obsolete("Используйте ItemSO / ItemId")]
    public ItemType ItemType { get; set; }

    public string LocationName { get; set; }
    public string DialogueID { get; set; }
    public int IntValue { get; set; }
    public float FloatValue { get; set; }
    public string StringValue { get; set; }
    public object CustomData { get; set; }
    public EquipmentType EquipmentType { get; set; }
    public string FlagName { get; set; }
    public bool FlagValue { get; set; }

    public IObjectResolver Container { get; set; }

    public EventContext() { }

    public EventContext(GameObject source, GameObject target = null)
    {
        SourceObject = source;
        TargetObject = target;
    }

    public EventContext WithContainer(IObjectResolver container)
    {
        Container = container;
        return this;
    }

    public EventContext WithItem(ItemSO item)
    {
        ItemSO = item;
        ItemId = item != null ? item.id : "";
        return this;
    }

    public EventContext WithItemId(string id)
    {
        ItemId = id;
        if (ItemSO == null && !string.IsNullOrEmpty(id))
            ItemSO = Container?.Resolve<ItemDatabase>()?.GetItem(id);
        return this;
    }

    [System.Obsolete("Используйте WithItem(ItemSO) или WithItemId(string)")]
    public EventContext WithItem(ItemType item)
    {
#pragma warning disable CS0618
        ItemType = item;
        if (item != ItemType.None)
        {
            ItemSO = Container?.Resolve<ItemDatabase>()?.GetItemByLegacyType(item);
            if (ItemSO != null) ItemId = ItemSO.id;
        }
#pragma warning restore CS0618
        return this;
    }

    public EventContext WithEquipmentType(EquipmentType equipmentType)
    {
        EquipmentType = equipmentType;
        return this;
    }

    public EventContext WithLocation(string location)
    {
        LocationName = location;
        return this;
    }

    public EventContext WithDialogue(string dialogueID)
    {
        DialogueID = dialogueID;
        return this;
    }

    public EventContext WithValue(int value)
    {
        IntValue = value;
        return this;
    }

    public EventContext WithValue(float value)
    {
        FloatValue = value;
        return this;
    }

    public EventContext WithValue(string value)
    {
        StringValue = value;
        return this;
    }

    public EventContext WithData(object data)
    {
        CustomData = data;
        return this;
    }

    public EventContext WithFlag(string name, bool value)
    {
        FlagName = name;
        FlagValue = value;
        return this;
    }
}