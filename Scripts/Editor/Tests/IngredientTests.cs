using NUnit.Framework;
using UnityEngine;

public class IngredientTests
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
    public void GetId_WithSO_ReturnsId()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple"), count = 1 };
        Assert.AreEqual("apple", ing.GetId());
    }

    [Test]
    public void GetId_NullSO_ReturnsEmpty()
    {
        var ing = new Ingredient();
        Assert.AreEqual("", ing.GetId());
    }

    [Test]
    public void IsValid_ValidItem_ReturnsTrue()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple"), count = 1 };
        Assert.IsTrue(ing.IsValid());
    }

    [Test]
    public void IsValid_ZeroCount_ReturnsFalse()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple"), count = 0 };
        Assert.IsFalse(ing.IsValid());
    }

    [Test]
    public void IsValid_NegativeCount_ReturnsFalse()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple"), count = -1 };
        Assert.IsFalse(ing.IsValid());
    }

    [Test]
    public void IsValid_NoItem_ReturnsFalse()
    {
        var ing = new Ingredient { count = 1 };
        Assert.IsFalse(ing.IsValid());
    }

    [Test]
    public void Matches_SameId_ReturnsTrue()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple") };
        Assert.IsTrue(ing.Matches("apple"));
    }

    [Test]
    public void Matches_DifferentId_ReturnsFalse()
    {
        var ing = new Ingredient { itemSO = MakeItem("apple") };
        Assert.IsFalse(ing.Matches("potion"));
    }

    [Test]
    public void Matches_EmptyId_ReturnsFalse()
    {
        var ing = new Ingredient();
        Assert.IsFalse(ing.Matches(""));
        Assert.IsFalse(ing.Matches(null));
    }

    [Test]
    public void GetDisplayName_WithSO_ReturnsItemName()
    {
        var so = MakeItem("apple");
        so.itemName = "Яблоко";
        var ing = new Ingredient { itemSO = so };
        Assert.AreEqual("Яблоко", ing.GetDisplayName());
    }

    [Test]
    public void GetDisplayName_NoItem_ReturnsQuestionMarks()
    {
        var ing = new Ingredient();
        Assert.AreEqual("???", ing.GetDisplayName());
    }
}