using UnityEngine;

public static class EventIntegration
{
    public static void OnItemPickup(ItemType itemType)
    {
        var context = new EventContext().WithItem(itemType);
        EventManager.Instance?.TriggerEvent(EventTriggerType.PickupItem, context);
        Logger.Log(LogModule.Event, $"Интеграция: поднят предмет {itemType}");
    }

    public static void OnLocationEnter(string locationName)
    {
        var context = new EventContext().WithLocation(locationName);
        EventManager.Instance?.TriggerEvent(EventTriggerType.EnterLocation, context);
        Logger.Log(LogModule.Event, $"Интеграция: вход в локацию {locationName}");
    }

    public static void OnItemEquip(ItemType itemType)
    {
        var context = new EventContext().WithItem(itemType);
        EventManager.Instance?.TriggerEvent(EventTriggerType.EquipItem, context);
        Logger.Log(LogModule.Event, $"Интеграция: экипирован предмет {itemType}");
    }

    public static void OnDialogueEnd(string dialogueID)
    {
        var context = new EventContext().WithDialogue(dialogueID);
        EventManager.Instance?.TriggerEvent(EventTriggerType.DialogueEnd, context);
        Logger.Log(LogModule.Event, $"Интеграция: завершён диалог {dialogueID}");
    }
}
