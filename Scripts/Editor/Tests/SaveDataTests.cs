using NUnit.Framework;
using Newtonsoft.Json;

public class SaveDataTests
{
    [Test]
    public void SaveData_DefaultValues_AreInitialized()
    {
        var data = new SaveData();
        Assert.IsNotNull(data.inventory);
        Assert.IsNotNull(data.equipment);
        Assert.IsNotNull(data.location);
        Assert.IsNotNull(data.flags);
        Assert.IsNotNull(data.quests);
        Assert.IsNotNull(data.worldItems);
        Assert.IsNotNull(data.events);
        Assert.IsNotNull(data.dialogues);
        Assert.IsNotNull(data.crafting);
    }

    [Test]
    public void SaveData_JsonRoundTrip_PreservesData()
    {
        var data = new SaveData
        {
            saveDate = "2024-01-01",
            saveTime = "12:00",
            location = new LocationData { sceneName = "Game0" }
        };
        data.flags.entries.Add(new FlagEntry { name = "test", value = true });
        data.inventory.entries.Add(new InventoryEntry { itemId = "apple", count = 5 });
        data.quests.active.Add(new QuestSaveEntry { questId = "q1" });

        string json = JsonConvert.SerializeObject(data);
        var restored = JsonConvert.DeserializeObject<SaveData>(json);

        Assert.AreEqual("2024-01-01", restored.saveDate);
        Assert.AreEqual("Game0", restored.location.sceneName);
        Assert.AreEqual(1, restored.flags.entries.Count);
        Assert.AreEqual("test", restored.flags.entries[0].name);
        Assert.AreEqual("apple", restored.inventory.entries[0].itemId);
        Assert.AreEqual(5, restored.inventory.entries[0].count);
        Assert.AreEqual("q1", restored.quests.active[0].questId);
    }

    [Test]
    public void QuestsData_AllLists_Initialized()
    {
        var q = new QuestsData();
        Assert.IsNotNull(q.active);
        Assert.IsNotNull(q.completed);
        Assert.IsNotNull(q.canceled);
        Assert.AreEqual(0, q.active.Count);
    }

    [Test]
    public void WorldItemsData_Defaults()
    {
        var data = new WorldItemsData();
        Assert.IsNotNull(data.takenItemIds);
        Assert.IsNotNull(data.droppedItems);
        Assert.IsNotNull(data.movedItems);
    }

    [Test]
    public void MovedItemData_RoundTrip()
    {
        var data = new MovedItemData
        {
            uniqueId = "uid",
            locationName = "Game0",
            localPosX = 1.5f,
            localPosY = -2.3f
        };
        string json = JsonConvert.SerializeObject(data);
        var restored = JsonConvert.DeserializeObject<MovedItemData>(json);
        Assert.AreEqual("uid", restored.uniqueId);
        Assert.AreEqual("Game0", restored.locationName);
        Assert.AreEqual(1.5f, restored.localPosX, 0.001f);
        Assert.AreEqual(-2.3f, restored.localPosY, 0.001f);
    }

    [Test]
    public void ObjectiveSaveEntry_RoundTrip()
    {
        var entry = new ObjectiveSaveEntry { objectiveId = "obj", currentCount = 7 };
        string json = JsonConvert.SerializeObject(entry);
        var restored = JsonConvert.DeserializeObject<ObjectiveSaveEntry>(json);
        Assert.AreEqual("obj", restored.objectiveId);
        Assert.AreEqual(7, restored.currentCount);
    }
}