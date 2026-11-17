using NUnit.Framework;
using Newtonsoft.Json;
using System.Collections.Generic;

public class EventSerializationTests
{
    [Test]
    public void Serialize_StartDialogue_IncludesTypeName()
    {
        var data = new EventData
        {
            eventID = "test_event",
            actions = new List<EventAction>
            {
                new EventActionStartDialogue { dialogueFileName = "Prologue" }
            }
        };

        string json = JsonConvert.SerializeObject(data, EventSerialization.Settings);

        StringAssert.Contains("EventActionStartDialogue", json);
        StringAssert.Contains("Prologue", json);
    }

    [Test]
    public void Deserialize_StartDialogue_ReturnsCorrectType()
    {
        var original = new EventData
        {
            eventID = "test",
            actions = new List<EventAction>
            {
                new EventActionStartDialogue { dialogueFileName = "Prologue" }
            }
        };

        string json = JsonConvert.SerializeObject(original, EventSerialization.Settings);
        var restored = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);

        Assert.AreEqual(1, restored.actions.Count);
        Assert.IsInstanceOf<EventActionStartDialogue>(restored.actions[0]);
        Assert.AreEqual("Prologue", ((EventActionStartDialogue)restored.actions[0]).dialogueFileName);
    }

    [Test]
    public void Deserialize_MultipleActions_PreservesNested()
    {
        var multiple = new EventActionMultiple();
        multiple.subActions.Add(new EventActionSetFlag { flagName = "f1" });
        multiple.subActions.Add(new EventActionSetFlag { flagName = "f2" });

        var data = new EventData
        {
            eventID = "e",
            actions = new List<EventAction> { multiple }
        };

        string json = JsonConvert.SerializeObject(data, EventSerialization.Settings);
        var restored = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);

        var restoredMultiple = restored.actions[0] as EventActionMultiple;
        Assert.IsNotNull(restoredMultiple);
        Assert.AreEqual(2, restoredMultiple.subActions.Count);
        Assert.IsInstanceOf<EventActionSetFlag>(restoredMultiple.subActions[0]);
    }

    [Test]
    public void Deserialize_RequirementFlag_ReturnsCorrectType()
    {
        var data = new EventData
        {
            eventID = "e",
            requirements = new List<EventRequirement>
            {
                new RequirementFlag { flagName = "test", flagValue = true }
            }
        };

        string json = JsonConvert.SerializeObject(data, EventSerialization.Settings);
        var restored = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);

        Assert.IsInstanceOf<RequirementFlag>(restored.requirements[0]);
        var flag = (RequirementFlag)restored.requirements[0];
        Assert.AreEqual("test", flag.flagName);
        Assert.IsTrue(flag.flagValue);
    }

    [Test]
    public void Deserialize_TriggerConditionItem_ReturnsCorrectType()
    {
        var data = new EventData
        {
            eventID = "e",
            triggerCondition = new TriggerConditionItem { targetItemSOId = "sword" }
        };

        string json = JsonConvert.SerializeObject(data, EventSerialization.Settings);
        var restored = JsonConvert.DeserializeObject<EventData>(json, EventSerialization.Settings);

        Assert.IsInstanceOf<TriggerConditionItem>(restored.triggerCondition);
        Assert.AreEqual("sword", ((TriggerConditionItem)restored.triggerCondition).targetItemSOId);
    }

    [Test]
    public void Serialize_NullActions_DoesNotThrow()
    {
        var data = new EventData { eventID = "e", actions = null };
        Assert.DoesNotThrow(() => JsonConvert.SerializeObject(data, EventSerialization.Settings));
    }
}