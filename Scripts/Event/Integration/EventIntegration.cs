// EventIntegration.cs
using UnityEngine;

public static class EventIntegration
{
    // Интеграция с PickupItem
    public static void OnItemPickup(ItemType itemType)
    {
        var context = new EventContext().WithItem(itemType);
        EventManager.Instance?.TriggerEvent(EventTriggerType.PickupItem, context);
    }

    // Интеграция с LocationSystem
    public static void OnLocationEnter(string locationName)
    {
        var context = new EventContext().WithLocation(locationName);
        EventManager.Instance?.TriggerEvent(EventTriggerType.EnterLocation, context);
    }

    // Интеграция с EquipmentSystem
    public static void OnItemEquip(ItemType itemType)
    {
        var context = new EventContext().WithItem(itemType);
        EventManager.Instance?.TriggerEvent(EventTriggerType.EquipItem, context);
    }

    // Интеграция с DialogueSystem
    public static void OnDialogueEnd(string dialogueID)
    {
        var context = new EventContext().WithDialogue(dialogueID);
        EventManager.Instance?.TriggerEvent(EventTriggerType.DialogueEnd, context);
    }
}