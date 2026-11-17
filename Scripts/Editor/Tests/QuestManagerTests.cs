using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class QuestManagerTests
{
    private GameObject managerGO;
    private QuestManager qm;
    private readonly List<QuestSO> tempQuests = new List<QuestSO>();
    private readonly List<ItemSO> tempItems = new List<ItemSO>();

    [SetUp]
    public void SetUp()
    {
        managerGO = new GameObject("TestQuestManager");
        managerGO.SetActive(false);

        qm = managerGO.AddComponent<QuestManager>();

        var so = new SerializedObject(qm);
        so.FindProperty("autoLoad").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();

        managerGO.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (managerGO != null) Object.DestroyImmediate(managerGO);

        foreach (var q in tempQuests)
            if (q != null) Object.DestroyImmediate(q);
        tempQuests.Clear();

        foreach (var i in tempItems)
            if (i != null) Object.DestroyImmediate(i);
        tempItems.Clear();
    }

    private ItemSO MakeItemSO(string id)
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = id;
        so.itemName = id;
        tempItems.Add(so);
        return so;
    }

    private QuestSO CreateQuest(string id, params QuestObjective[] objectives)
    {
        var so = ScriptableObject.CreateInstance<QuestSO>();
        so.questID = id;
        so.questName = id;
        so.objectives = new List<QuestObjective>(objectives);
        tempQuests.Add(so);
        return so;
    }

    private QuestObjective Obj(string id, int required, int current = 0, bool optional = false,
        QuestObjectiveType type = QuestObjectiveType.Collect, string targetItemId = null)
    {
        return new QuestObjective
        {
            objectiveID = id,
            description = id,
            requiredCount = required,
            currentCount = current,
            isOptional = optional,
            type = type,
            targetItemSO = targetItemId != null ? MakeItemSO(targetItemId) : null
        };
    }

    private void RegisterQuest(QuestSO quest)
    {
        var field = typeof(QuestManager).GetField("allQuestData",
            BindingFlags.NonPublic | BindingFlags.Instance);
        var dict = (Dictionary<string, QuestSO>)field.GetValue(qm);
        dict[quest.questID] = quest;
    }

    [Test]
    public void StartQuest_AddsToActive_AndFiresOnQuestStarted()
    {
        var quest = CreateQuest("q1", Obj("obj1", 3));
        RegisterQuest(quest);

        int started = 0;
        qm.OnQuestStarted += _ => started++;

        qm.StartQuest("q1");

        Assert.AreEqual(1, started);
        Assert.AreEqual(1, qm.GetActiveQuests().Count);
        Assert.IsTrue(qm.IsQuestActive("q1"));
    }

    [Test]
    public void StartQuest_AlreadyActive_DoesNotDuplicate()
    {
        var quest = CreateQuest("q1", Obj("obj1", 3));
        RegisterQuest(quest);

        qm.StartQuest("q1");
        qm.StartQuest("q1");

        Assert.AreEqual(1, qm.GetActiveQuests().Count);
    }

    [Test]
    public void UpdateQuest_IncrementsObjective_AndCompletesQuest()
    {
        var quest = CreateQuest("q1", Obj("obj1", 3));
        RegisterQuest(quest);

        int completed = 0;
        qm.OnQuestCompleted += _ => completed++;

        qm.StartQuest("q1");
        qm.UpdateQuest("q1", "obj1", 3);

        Assert.AreEqual(1, completed);
        Assert.AreEqual(0, qm.GetActiveQuests().Count);
        Assert.AreEqual(1, qm.GetCompletedQuests().Count);
        Assert.IsTrue(qm.IsQuestCompleted("q1"));
    }

    [Test]
    public void UpdateQuest_ClampsToRequiredCount()
    {
        var quest = CreateQuest("q1", Obj("obj1", 3));
        RegisterQuest(quest);

        qm.StartQuest("q1");
        qm.UpdateQuest("q1", "obj1", 10);

        var instance = qm.GetActiveQuests().Find(q => q.QuestID == "q1");
        Assert.IsNull(instance);
        Assert.AreEqual(1, qm.GetCompletedQuests().Count);
    }

    [Test]
    public void UpdateQuestByItem_MatchesCollectObjective()
    {
        var quest = CreateQuest("q1", Obj("apple", 2,
            type: QuestObjectiveType.Collect, targetItemId: "apple"));
        RegisterQuest(quest);

        qm.StartQuest("q1");
        qm.UpdateQuestByItem("apple", 1);

        var instance = qm.GetActiveQuests().Find(q => q.QuestID == "q1");
        Assert.IsNotNull(instance);
        Assert.AreEqual(1, instance.Objectives[0].currentCount);
    }

    [Test]
    public void UpdateQuestByItem_WrongItem_DoesNotUpdate()
    {
        var quest = CreateQuest("q1", Obj("apple", 2,
            type: QuestObjectiveType.Collect, targetItemId: "apple"));
        RegisterQuest(quest);

        qm.StartQuest("q1");
        qm.UpdateQuestByItem("potion", 1);

        var instance = qm.GetActiveQuests().Find(q => q.QuestID == "q1");
        Assert.AreEqual(0, instance.Objectives[0].currentCount);
    }

    [Test]
    public void CancelQuest_MovesToCanceled_WithoutCompletingObjectives()
    {
        var quest = CreateQuest("q1", Obj("obj1", 3));
        RegisterQuest(quest);

        qm.StartQuest("q1");
        qm.UpdateQuest("q1", "obj1", 1);
        qm.CancelQuest("q1");

        Assert.AreEqual(0, qm.GetActiveQuests().Count);
        Assert.AreEqual(0, qm.GetCompletedQuests().Count);
        Assert.AreEqual(1, qm.GetCanceledQuests().Count);

        var canceled = qm.GetCanceledQuests()[0];
        Assert.IsTrue(canceled.IsCanceled);
        Assert.AreEqual(1, canceled.Objectives[0].currentCount);
        Assert.IsFalse(canceled.IsComplete());
    }

    [Test]
    public void Quest_AllOptionalObjectives_NeverAutoCompletes()
    {
        var quest = CreateQuest("q1",
            Obj("opt1", 1, optional: true),
            Obj("opt2", 5, optional: true));
        RegisterQuest(quest);

        qm.StartQuest("q1");

        Assert.AreEqual(1, qm.GetActiveQuests().Count);
        Assert.AreEqual(0, qm.GetCompletedQuests().Count);

        qm.UpdateQuest("q1", "opt1", 1);
        qm.UpdateQuest("q1", "opt2", 5);

        Assert.AreEqual(1, qm.GetActiveQuests().Count);
        Assert.AreEqual(0, qm.GetCompletedQuests().Count);
    }
}