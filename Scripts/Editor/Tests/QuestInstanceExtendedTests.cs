using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class QuestInstanceExtendedTests
{
    private readonly List<QuestSO> _created = new List<QuestSO>();

    [TearDown]
    public void TearDown()
    {
        foreach (var so in _created) if (so != null) Object.DestroyImmediate(so);
        _created.Clear();
    }

    private QuestSO CreateQuest(params QuestObjective[] objectives)
    {
        var so = ScriptableObject.CreateInstance<QuestSO>();
        so.questID = "test";
        so.questName = "Test";
        so.objectives = new List<QuestObjective>(objectives);
        _created.Add(so);
        return so;
    }

    private QuestObjective Obj(string id, int req, int cur = 0, bool opt = false)
    {
        return new QuestObjective
        {
            objectiveID = id,
            description = id,
            requiredCount = req,
            currentCount = cur,
            isOptional = opt
        };
    }

    [Test]
    public void Progress_Mixed_IgnoresOptional()
    {
        var q = new QuestInstance(CreateQuest(Obj("a", 10, 5), Obj("b", 10, 5, true)));
        Assert.AreEqual(0.5f, q.Progress, 0.001f);
    }

    [Test]
    public void Progress_AllOptional_ReturnsZero()
    {
        var q = new QuestInstance(CreateQuest(Obj("opt", 10, 10, true)));
        Assert.AreEqual(0f, q.Progress, 0.001f);
    }

    [Test]
    public void Progress_AllComplete_ReturnsOne()
    {
        var q = new QuestInstance(CreateQuest(Obj("a", 5, 5), Obj("b", 3, 3)));
        Assert.AreEqual(1f, q.Progress, 0.001f);
    }

    [Test]
    public void GetProgressText_SumsRequired()
    {
        var q = new QuestInstance(CreateQuest(
            Obj("a", 5, 2),
            Obj("b", 3, 1),
            Obj("opt", 10, 10, true)
        ));
        Assert.AreEqual("3/8", q.GetProgressText());
    }

    [Test]
    public void QuestID_FromSO()
    {
        var so = CreateQuest(Obj("a", 1));
        so.questID = "custom";
        Assert.AreEqual("custom", new QuestInstance(so).QuestID);
    }

    [Test]
    public void Objectives_AreCloned_NotSharedBetweenInstances()
    {
        var so = CreateQuest(Obj("a", 5));
        var q1 = new QuestInstance(so);
        var q2 = new QuestInstance(so);

        q1.Objectives[0].currentCount = 3;

        Assert.AreEqual(3, q1.Objectives[0].currentCount);
        Assert.AreEqual(0, q2.Objectives[0].currentCount);
    }
}