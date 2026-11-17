using NUnit.Framework;
using UnityEngine.TestTools;

public class LoggerTests
{
    [TearDown]
    public void TearDown()
    {
        LogAssert.ignoreFailingMessages = false;
        Logger.SetVerbose(false);
        foreach (LogModule m in System.Enum.GetValues(typeof(LogModule)))
            Logger.SetModuleEnabled(m, true);
    }

    [Test]
    public void SetVerbose_TogglesState()
    {
        Logger.SetVerbose(true);
        Assert.IsTrue(Logger.VerboseEnabled);

        Logger.SetVerbose(false);
        Assert.IsFalse(Logger.VerboseEnabled);
    }

    [Test]
    public void SetModuleEnabled_DoesNotThrow()
    {
        Assert.DoesNotThrow(() => Logger.SetModuleEnabled(LogModule.Core, false));
        Assert.DoesNotThrow(() => Logger.SetModuleEnabled(LogModule.Core, true));
    }

    [Test]
    public void AllLogMethods_DoNotThrow()
    {
        LogAssert.ignoreFailingMessages = true;

        Assert.DoesNotThrow(() => Logger.Log(LogModule.Core, "test"));
        Assert.DoesNotThrow(() => Logger.LogWarning(LogModule.Core, "test"));
        Assert.DoesNotThrow(() => Logger.LogError(LogModule.Core, "test"));
        Assert.DoesNotThrow(() => Logger.LogVerbose(LogModule.Core, "test"));

        LogAssert.ignoreFailingMessages = false;
    }

    [Test]
    public void LogVerbose_WhenVerboseDisabled_DoesNotThrow()
    {
        Logger.SetVerbose(false);
        Assert.DoesNotThrow(() => Logger.LogVerbose(LogModule.Core, "silent"));
    }
}