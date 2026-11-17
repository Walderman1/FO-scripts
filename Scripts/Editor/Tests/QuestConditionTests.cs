using NUnit.Framework;

public class QuestConditionTests
{
    [Test]
    public void IsMet_NoConditions_ReturnsTrue()
    {
        var c = new QuestCondition();
        Assert.IsTrue(c.IsMet());
    }

    [Test]
    public void IsMet_FlagExpectedTrue_NoFlagManager_ReturnsFalse()
    {
        var c = new QuestCondition { flagName = "TestFlag", flagValue = true };
        Assert.IsFalse(c.IsMet());
    }

    [Test]
    public void IsMet_FlagExpectedFalse_NoFlagManager_ReturnsTrue()
    {
        var c = new QuestCondition { flagName = "TestFlag", flagValue = false };
        Assert.IsTrue(c.IsMet());
    }

    [Test]
    public void IsMet_QuestExpectedCompleted_NoQuestManager_ReturnsFalse()
    {
        var c = new QuestCondition { questID = "some_quest", questCompleted = true };
        Assert.IsFalse(c.IsMet());
    }

    [Test]
    public void IsMet_QuestExpectedNotCompleted_NoQuestManager_ReturnsTrue()
    {
        var c = new QuestCondition { questID = "some_quest", questCompleted = false };
        Assert.IsTrue(c.IsMet());
    }
}