using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class QuestInstanceTests
{
    private readonly List<QuestSO> created = new List<QuestSO>();

    private QuestSO CreateQuest(params QuestObjective[] objectives)
    {
        var so = ScriptableObject.CreateInstance<QuestSO>();
        so.questID = "test";
        so.questName = "Test Quest";
        so.objectives = new List<QuestObjective>(objectives);
        created.Add(so);
        return so;
    }

    private QuestObjective Obj(string id, int required, int current = 0, bool optional = false)
    {
        return new QuestObjective
        {
            objectiveID = id,
            description = id,
            requiredCount = required,
            currentCount = current,
            isOptional = optional
        };
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var so in created)
            if (so != null) Object.DestroyImmediate(so);
        created.Clear();
    }

    [Test]
    public void Progress_WithNoObjectives_ReturnsZero()
    {
        var instance = new QuestInstance(CreateQuest());
        Assert.AreEqual(0f, instance.Progress);
    }

    [Test]
    public void Progress_HalfCompleted_ReturnsHalf()
    {
        var instance = new QuestInstance(CreateQuest(Obj("a", 4, 2)));
        Assert.AreEqual(0.5f, instance.Progress, 0.001f);
    }

    [Test]
    public void Progress_IgnoresOptionalObjectives()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("a", 2, 1),
            Obj("opt", 10, 10, optional: true)
        ));
        Assert.AreEqual(0.5f, instance.Progress, 0.001f);
    }

    [Test]
    public void IsComplete_AllRequiredDone_ReturnsTrue()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("a", 2, 2),
            Obj("b", 1, 1)
        ));
        Assert.IsTrue(instance.IsComplete());
    }

    [Test]
    public void IsComplete_OptionalIncomplete_StillReturnsTrue()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("a", 1, 1),
            Obj("opt", 5, 0, optional: true)
        ));
        Assert.IsTrue(instance.IsComplete());
    }

    [Test]
    public void IsComplete_RequiredIncomplete_ReturnsFalse()
    {
        var instance = new QuestInstance(CreateQuest(Obj("a", 3, 1)));
        Assert.IsFalse(instance.IsComplete());
    }

    [Test]
    public void IsComplete_AllOptional_ReturnsFalse()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("opt", 1, 1, optional: true)
        ));
        Assert.IsFalse(instance.IsComplete());
    }

    [Test]
    public void IsComplete_NoObjectives_ReturnsFalse()
    {
        var instance = new QuestInstance(CreateQuest());
        Assert.IsFalse(instance.IsComplete());
    }

    [Test]
    public void GetProgressText_FormatsCorrectly()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("a", 4, 2),
            Obj("b", 3, 1)
        ));
        Assert.AreEqual("3/7", instance.GetProgressText());
    }

    [Test]
    public void GetObjective_ReturnsCorrectObjective()
    {
        var instance = new QuestInstance(CreateQuest(
            Obj("first", 1),
            Obj("second", 1)
        ));
        Assert.AreEqual("second", instance.GetObjective("second").objectiveID);
    }

    [Test]
    public void GetObjective_UnknownId_ReturnsNull()
    {
        var instance = new QuestInstance(CreateQuest(Obj("a", 1)));
        Assert.IsNull(instance.GetObjective("missing"));
    }
}