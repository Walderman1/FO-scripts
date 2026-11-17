using NUnit.Framework;
using UnityEngine;

public class QuestObjectiveAndRewardTests
{
    private readonly System.Collections.Generic.List<ItemSO> _created = new System.Collections.Generic.List<ItemSO>();

    [TearDown]
    public void TearDown()
    {
        foreach (var so in _created) if (so != null) Object.DestroyImmediate(so);
        _created.Clear();
    }

    private ItemSO MakeItem(string id)
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = id;
        _created.Add(so);
        return so;
    }

    [Test]
    public void Objective_IsComplete_EqualCounts_True()
    {
        Assert.IsTrue(new QuestObjective { currentCount = 5, requiredCount = 5 }.IsComplete());
    }

    [Test]
    public void Objective_IsComplete_Exceeded_True()
    {
        Assert.IsTrue(new QuestObjective { currentCount = 10, requiredCount = 5 }.IsComplete());
    }

    [Test]
    public void Objective_IsComplete_Less_NotComplete()
    {
        Assert.IsFalse(new QuestObjective { currentCount = 3, requiredCount = 5 }.IsComplete());
    }

    [Test]
    public void Objective_GetTargetItemId_WithSO_ReturnsId()
    {
        var obj = new QuestObjective { targetItemSO = MakeItem("apple") };
        Assert.AreEqual("apple", obj.GetTargetItemId());
    }

    [Test]
    public void Objective_GetTargetItemId_NoSO_ReturnsEmpty()
    {
        Assert.AreEqual("", new QuestObjective().GetTargetItemId());
    }

    [Test]
    public void Reward_GetItemId_WithSO_ReturnsId()
    {
        var reward = new QuestReward { itemSO = MakeItem("gold"), amount = 10 };
        Assert.AreEqual("gold", reward.GetItemId());
    }

    [Test]
    public void Reward_GetItemId_NoItem_ReturnsEmpty()
    {
        Assert.AreEqual("", new QuestReward().GetItemId());
    }

    [Test]
    public void Reward_Defaults_AmountOne()
    {
        var reward = new QuestReward();
        Assert.AreEqual(1, reward.amount);
        Assert.AreEqual(0, reward.experience);
    }
}