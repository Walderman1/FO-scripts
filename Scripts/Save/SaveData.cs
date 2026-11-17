using System;
using System.Collections.Generic;

[Serializable]
public class SaveData
{
    public int version = 2;
    public string saveDate;
    public string saveTime;
    public string screenshotBase64;
    public InventoryData inventory = new InventoryData();
    public EquipmentData equipment = new EquipmentData();
    public LocationData location = new LocationData();
    public FlagsData flags = new FlagsData();
    public QuestsData quests = new QuestsData();
    public WorldItemsData worldItems = new WorldItemsData();
    public EventsData events = new EventsData();
    public DialoguesData dialogues = new DialoguesData();
    public CraftingData crafting = new CraftingData();
}

[Serializable]
public class EventsData
{
    public List<EventStateEntry> entries = new List<EventStateEntry>();
}

[Serializable]
public class EventStateEntry
{
    public string eventId;
    public bool isExecuted;
    public int executionCount;
    public string lastExecuted;
}

[Serializable]
public class DialoguesData
{
    public List<DialogueStatusEntry> entries = new List<DialogueStatusEntry>();
}

[Serializable]
public class DialogueStatusEntry
{
    public string dialogueId;
    public bool isCompleted;
    public int timesPlayed;
    public string lastPlayed;
    public string nextDialogueId;
    public bool isLocked;
}

[Serializable]
public class CraftingData
{
    public List<string> knownRecipeIds = new List<string>();
}

[Serializable]
public class WorldItemsData
{
    public List<string> takenItemIds = new List<string>();
    public List<DroppedItemData> droppedItems = new List<DroppedItemData>();
    public List<MovedItemData> movedItems = new List<MovedItemData>();
}

[Serializable]
public class MovedItemData
{
    public string uniqueId;
    public string locationName;
    public float localPosX;
    public float localPosY;
}

[Serializable]
public class DroppedItemData
{
    public string uniqueId;
    public string itemSOId;
    public int count;
    public string locationName;
    public float localPosX;
    public float localPosY;
}

[Serializable]
public class InventoryData
{
    public List<InventoryEntry> entries = new List<InventoryEntry>();
}

[Serializable]
public class InventoryEntry
{
    public string itemId;
    public int count;
}

[Serializable]
public class EquipmentData
{
    public List<EquipmentEntry> entries = new List<EquipmentEntry>();
}

[Serializable]
public class EquipmentEntry
{
    public int slotType;
    public string itemId;
    public int count;
}

[Serializable]
public class LocationData
{
    public string sceneName;
}

[Serializable]
public class FlagsData
{
    public List<FlagEntry> entries = new List<FlagEntry>();
}

[Serializable]
public class FlagEntry
{
    public string name;
    public bool value;
}

[Serializable]
public class QuestsData
{
    public List<QuestSaveEntry> active = new List<QuestSaveEntry>();
    public List<QuestSaveEntry> completed = new List<QuestSaveEntry>();
    public List<QuestSaveEntry> canceled = new List<QuestSaveEntry>();
}

[Serializable]
public class QuestSaveEntry
{
    public string questId;
    public List<ObjectiveSaveEntry> objectives = new List<ObjectiveSaveEntry>();
}

[Serializable]
public class ObjectiveSaveEntry
{
    public string objectiveId;
    public int currentCount;
}