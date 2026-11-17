using NUnit.Framework;

public class QuestConditionExtendedTests
{
    [Test]
    public void EmptyFlagName_ReturnsTrue()
    {
        Assert.IsTrue(new QuestCondition { flagName = "", flagValue = false }.IsMet());
    }

    [Test]
    public void EmptyQuestID_ReturnsTrue()
    {
        Assert.IsTrue(new QuestCondition { questID = "", questCompleted = true }.IsMet());
    }

    [Test]
    public void BothFlagAndQuest_FlagHasPriority()
    {
        var c = new QuestCondition
        {
            flagName = "test",
            flagValue = true,
            questID = "quest",
            questCompleted = true
        };
        Assert.IsFalse(c.IsMet());
    }

    [Test]
    public void FlagExpectedFalse_NoManager_ReturnsTrue()
    {
        Assert.IsTrue(new QuestCondition { flagName = "test", flagValue = false }.IsMet());
    }
}