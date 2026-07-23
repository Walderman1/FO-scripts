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
        Logger.Log(LogModule.Event, "Создан новый контекст события");
    }

    public EventContext(GameObject source, GameObject target = null)
    {
        SourceObject = source;
        TargetObject = target;
        Logger.Log(LogModule.Event, $"Создан контекст события с источником: {source?.name}, целью: {target?.name}");
    }

    public EventContext WithItem(ItemType item)
    {
        ItemType = item;
        Logger.Log(LogModule.Event, $"Контекст: установлен предмет {item}");
        return this;
    }

    public EventContext WithEquipmentType(EquipmentType equipmentType)
    {
        EquipmentType = equipmentType;
        Logger.Log(LogModule.Event, $"Контекст: установлен тип экипировки {equipmentType}");
        return this;
    }

    public EventContext WithLocation(string location)
    {
        LocationName = location;
        Logger.Log(LogModule.Event, $"Контекст: установлена локация {location}");
        return this;
    }

    public EventContext WithDialogue(string dialogueID)
    {
        DialogueID = dialogueID;
        Logger.Log(LogModule.Event, $"Контекст: установлен ID диалога {dialogueID}");
        return this;
    }

    public EventContext WithValue(int value)
    {
        IntValue = value;
        Logger.Log(LogModule.Event, $"Контекст: установлено целое значение {value}");
        return this;
    }

    public EventContext WithValue(float value)
    {
        FloatValue = value;
        Logger.Log(LogModule.Event, $"Контекст: установлено дробное значение {value}");
        return this;
    }

    public EventContext WithValue(string value)
    {
        StringValue = value;
        Logger.Log(LogModule.Event, $"Контекст: установлена строка {value}");
        return this;
    }

    public EventContext WithData(object data)
    {
        CustomData = data;
        Logger.Log(LogModule.Event, $"Контекст: установлены пользовательские данные {data?.GetType().Name}");
        return this;
    }
}
