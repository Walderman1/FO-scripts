using NUnit.Framework;

public class IdGeneratorTests
{
    [Test]
    public void Slugify_EnglishText_LowercasesAndReplacesSpaces()
    {
        Assert.AreEqual("hello_world", IdGenerator.Slugify("Hello World"));
    }

    [Test]
    public void Slugify_Cyrillic_Transliterates()
    {
        Assert.AreEqual("privet_mir", IdGenerator.Slugify("Привет Мир"));
    }

    [Test]
    public void Slugify_SpecialChars_RemovesThem()
    {
        Assert.AreEqual("abc123", IdGenerator.Slugify("abc!@#123"));
    }

    [Test]
    public void Slugify_ConsecutiveSeparators_CollapsesToOne()
    {
        Assert.AreEqual("a_b", IdGenerator.Slugify("a   _   -b"));
    }

    [Test]
    public void Slugify_EmptyOrNull_ReturnsEmpty()
    {
        Assert.AreEqual("", IdGenerator.Slugify(""));
        Assert.AreEqual("", IdGenerator.Slugify(null));
        Assert.AreEqual("", IdGenerator.Slugify("   "));
    }

    [Test]
    public void Slugify_LeadingTrailingSeparators_Trimmed()
    {
        Assert.AreEqual("abc", IdGenerator.Slugify("___abc___"));
    }

    [Test]
    public void Combine_PrefixAndParts_JoinsWithUnderscore()
    {
        Assert.AreEqual("quest_my_quest", IdGenerator.Combine("quest", "my quest"));
    }

    [Test]
    public void Combine_NullPrefix_OnlyParts()
    {
        Assert.AreEqual("a_b_c", IdGenerator.Combine(null, "a", "b", "c"));
    }

    [Test]
    public void Combine_EmptyParts_Skipped()
    {
        Assert.AreEqual("a_c", IdGenerator.Combine(null, "a", "", "c"));
    }

    [Test]
    public void FromAssetName_UsesAssetName()
    {
        var so = UnityEngine.ScriptableObject.CreateInstance<ItemSO>();
        so.name = "Apple";
        Assert.AreEqual("item_apple", IdGenerator.FromAssetName(so, "item"));
        UnityEngine.Object.DestroyImmediate(so);
    }

    [Test]
    public void FromAssetName_Null_ReturnsEmpty()
    {
        Assert.AreEqual("", IdGenerator.FromAssetName(null, "item"));
    }

    [Test]
    public void ContainsCyrillic_TrueForRussian()
    {
        Assert.IsTrue(IdGenerator.ContainsCyrillic("привет"));
        Assert.IsTrue(IdGenerator.ContainsCyrillic("Hello Мир"));
    }

    [Test]
    public void ContainsCyrillic_FalseForEnglish()
    {
        Assert.IsFalse(IdGenerator.ContainsCyrillic("hello"));
        Assert.IsFalse(IdGenerator.ContainsCyrillic(""));
        Assert.IsFalse(IdGenerator.ContainsCyrillic(null));
    }
}