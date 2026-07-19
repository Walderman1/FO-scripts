// EventContext.cs
using UnityEngine;

public class EventContext
{
    public GameObject SourceObject { get; set; }
    public GameObject TargetObject { get; set; }
    public ItemType ItemType { get; set; }
    public string LocationName { get; set; }
    public string DialogueID { get; set; }
    public int IntValue { get; set; }
    public float FloatValue { get; set; }
    public string StringValue { get; set; }
    public object CustomData { get; set; }
    public EquipmentType EquipmentType { get; set; }

    public EventContext()
    {
    }

    public EventContext(GameObject source, GameObject target = null)
    {
        SourceObject = source;
        TargetObject = target;
    }

    public EventContext WithItem(ItemType item)
    {
        ItemType = item;
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
}