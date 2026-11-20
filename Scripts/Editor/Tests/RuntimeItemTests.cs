using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

/// <summary>
/// Тесты системы предметов из картинок (папка ItemsInbox).
/// Проверяются только «чистые» части, без чтения файлов и без запуска игры.
/// </summary>
public class RuntimeItemTests
{
    private readonly List<Object> _created = new List<Object>();

    [TearDown]
    public void TearDown()
    {
        for (int i = 0; i < _created.Count; i++)
            if (_created[i] != null) Object.DestroyImmediate(_created[i]);

        _created.Clear();
    }

    private T Track<T>(T obj) where T : Object
    {
        _created.Add(obj);
        return obj;
    }

    #region Slugify

    [Test]
    public void Slugify_Cyrillic_Transliterates()
    {
        Assert.AreEqual("krasnyy_tsvetok", RuntimeItemNaming.Slugify("Красный цветок"));
    }

    [Test]
    public void Slugify_English_KeepsLetters()
    {
        Assert.AreEqual("red_flower", RuntimeItemNaming.Slugify("Red Flower"));
    }

    [Test]
    public void Slugify_StartsFromDigit_PrefixesItem()
    {
        Assert.AreEqual("item_3_swords", RuntimeItemNaming.Slugify("3 swords"));
    }

    [Test]
    public void Slugify_SpecialChars_AreDropped()
    {
        Assert.AreEqual("abc123", RuntimeItemNaming.Slugify("abc!@#123"));
    }

    [Test]
    public void Slugify_Empty_ReturnsEmpty()
    {
        Assert.AreEqual("", RuntimeItemNaming.Slugify(""));
        Assert.AreEqual("", RuntimeItemNaming.Slugify(null));
        Assert.AreEqual("", RuntimeItemNaming.Slugify("   "));
    }

    #endregion

    #region Имена файлов

    [Test]
    public void DisplayNameFromFile_UnderscoresBecomeSpaces()
    {
        Assert.AreEqual("Красный цветок", RuntimeItemNaming.DisplayNameFromFile("Красный_цветок"));
    }

    [Test]
    public void DisplayNameFromFile_Empty_ReturnsFallback()
    {
        Assert.AreEqual("Предмет", RuntimeItemNaming.DisplayNameFromFile(""));
        Assert.AreEqual("Предмет", RuntimeItemNaming.DisplayNameFromFile(null));
    }

    [Test]
    public void IsImageFile_RecognisesSupportedExtensions()
    {
        Assert.IsTrue(RuntimeItemNaming.IsImageFile("flower.png"));
        Assert.IsTrue(RuntimeItemNaming.IsImageFile("flower.JPG"));
        Assert.IsTrue(RuntimeItemNaming.IsImageFile("flower.jpeg"));
        Assert.IsFalse(RuntimeItemNaming.IsImageFile("flower.txt"));
        Assert.IsFalse(RuntimeItemNaming.IsImageFile("flower"));
        Assert.IsFalse(RuntimeItemNaming.IsImageFile(null));
    }

    [Test]
    public void MakeUnique_AddsSuffix()
    {
        var used = new HashSet<string> { "flower", "flower_2" };

        Assert.AreEqual("flower_3", RuntimeItemNaming.MakeUnique("flower", used));
        Assert.AreEqual("rose", RuntimeItemNaming.MakeUnique("rose", used));
        Assert.AreEqual("flower", RuntimeItemNaming.MakeUnique("flower", new HashSet<string>()));
    }

    #endregion

    #region JSON-настройки

    [Test]
    public void HasKey_FindsExistingKey()
    {
        const string json = "{ \"name\": \"Цветок\", \"maxStack\": 5 }";

        Assert.IsTrue(RuntimeItemDefinition.HasKey(json, "name"));
        Assert.IsTrue(RuntimeItemDefinition.HasKey(json, "maxStack"));
        Assert.IsFalse(RuntimeItemDefinition.HasKey(json, "description"));
    }

    [Test]
    public void HasKey_EmptyJson_ReturnsFalse()
    {
        Assert.IsFalse(RuntimeItemDefinition.HasKey("", "name"));
        Assert.IsFalse(RuntimeItemDefinition.HasKey(null, "name"));
    }

    [Test]
    public void Parse_ValidJson_FillsFields()
    {
        const string json = "{ \"name\": \"Цветок\", \"description\": \"Пахнет\", \"maxStack\": 7 }";

        var definition = RuntimeItemDefinition.Parse(json, "test.json");

        Assert.IsNotNull(definition);
        Assert.AreEqual("Цветок", definition.name);
        Assert.AreEqual("Пахнет", definition.description);
        Assert.AreEqual(7, definition.maxStack);
    }

    [Test]
    public void Parse_EmptyJson_ReturnsNull()
    {
        Assert.IsNull(RuntimeItemDefinition.Parse("", "test.json"));
        Assert.IsNull(RuntimeItemDefinition.Parse(null, "test.json"));
        Assert.IsNull(RuntimeItemDefinition.Parse("   ", "test.json"));
    }

    #endregion

    #region Регистрация в ItemDatabase

    [Test]
    public void RegisterRuntimeItem_AddsItemToDatabase()
    {
        var go = Track(new GameObject("TestItemDatabase"));
        var database = Track(go.AddComponent<ItemDatabase>());

        var so = Track(ScriptableObject.CreateInstance<ItemSO>());
        so.id = "runtime_test_item";
        so.itemName = "Тестовый предмет";

        Assert.IsTrue(database.RegisterRuntimeItem(so));
        Assert.IsTrue(database.HasItem("runtime_test_item"));
        Assert.AreSame(so, database.GetItem("runtime_test_item"));
        Assert.Contains(so, database.GetAllItems());
    }

    [Test]
    public void RegisterRuntimeItem_SameItemTwice_IsIdempotent()
    {
        var go = Track(new GameObject("TestItemDatabase"));
        var database = Track(go.AddComponent<ItemDatabase>());

        var so = Track(ScriptableObject.CreateInstance<ItemSO>());
        so.id = "runtime_test_item_2";
        so.itemName = "Тестовый предмет 2";

        Assert.IsTrue(database.RegisterRuntimeItem(so));
        Assert.IsTrue(database.RegisterRuntimeItem(so));

        int count = 0;
        foreach (var item in database.GetAllItems())
            if (item == so) count++;

        Assert.AreEqual(1, count);
    }

    [Test]
    public void RegisterRuntimeItem_EmptyId_ReturnsFalse()
    {
        var go = Track(new GameObject("TestItemDatabase"));
        var database = Track(go.AddComponent<ItemDatabase>());

        var so = Track(ScriptableObject.CreateInstance<ItemSO>());
        so.id = "";

        Assert.IsFalse(database.RegisterRuntimeItem(so));
        Assert.IsFalse(database.RegisterRuntimeItem(null));
    }

    [Test]
    public void RegisterRuntimeItem_IdConflict_ReturnsFalse()
    {
        var go = Track(new GameObject("TestItemDatabase"));
        var database = Track(go.AddComponent<ItemDatabase>());

        var first = Track(ScriptableObject.CreateInstance<ItemSO>());
        first.id = "same_id";
        first.itemName = "Первый";

        var second = Track(ScriptableObject.CreateInstance<ItemSO>());
        second.id = "same_id";
        second.itemName = "Второй";

        Assert.IsTrue(database.RegisterRuntimeItem(first));
        Assert.IsFalse(database.RegisterRuntimeItem(second));
        Assert.AreSame(first, database.GetItem("same_id"));
    }

    #endregion
}
