using NUnit.Framework;
using System.IO;
using UnityEngine.TestTools;
using System.Text.RegularExpressions;

public class SafeFileTests
{
    private string _tempDir;
    private string _testFile;

    [SetUp]
    public void SetUp()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "SafeFileTests_" + System.Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
        _testFile = Path.Combine(_tempDir, "test.txt");
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDir))
        {
            try { Directory.Delete(_tempDir, true); }
            catch { }
        }
    }

    [Test]
    public void Write_NewFile_CreatesIt()
    {
        bool ok = SafeFile.Write(_testFile, "hello");
        Assert.IsTrue(ok);
        Assert.IsTrue(File.Exists(_testFile));
        Assert.AreEqual("hello", File.ReadAllText(_testFile));
    }

    [Test]
    public void Read_ExistingFile_ReturnsContent()
    {
        File.WriteAllText(_testFile, "hello");
        var content = SafeFile.Read(_testFile, out bool fromBackup);
        Assert.AreEqual("hello", content);
        Assert.IsFalse(fromBackup);
    }

    [Test]
    public void Read_MissingFile_ReturnsNull()
    {
        var content = SafeFile.Read(_testFile, out bool fromBackup);
        Assert.IsNull(content);
        Assert.IsFalse(fromBackup);
    }

    [Test]
    public void Write_ExistingFile_CreatesBackup()
    {
        File.WriteAllText(_testFile, "old");
        SafeFile.Write(_testFile, "new");

        Assert.AreEqual("new", File.ReadAllText(_testFile));
        Assert.IsTrue(File.Exists(_testFile + ".bak"));
        Assert.AreEqual("old", File.ReadAllText(_testFile + ".bak"));
    }

    [Test]
    public void Read_CorruptMain_UsesBackup()
    {
        File.WriteAllText(_testFile + ".bak", "backup content");
        File.WriteAllText(_testFile, "");

        var content = SafeFile.Read(_testFile, out bool fromBackup);

        Assert.AreEqual("backup content", content);
        Assert.IsTrue(fromBackup);
    }

    [Test]
    public void Write_EmptyContent_ReturnsTrue()
    {
        Assert.IsTrue(SafeFile.Write(_testFile, ""));
    }

    [Test]
    public void Write_NullPath_ReturnsFalse()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, new Regex("SafeFile.Write: path is null or empty"));
        Assert.IsFalse(SafeFile.Write(null, "content"));
    }

    [Test]
    public void Read_NullPath_ReturnsNull()
    {
        LogAssert.Expect(UnityEngine.LogType.Error, new Regex("SafeFile.Read: path is null or empty"));
        Assert.IsNull(SafeFile.Read(null, out _));
    }
}