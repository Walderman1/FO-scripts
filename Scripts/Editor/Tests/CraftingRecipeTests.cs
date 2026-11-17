using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class CraftingRecipeTests
{
    private readonly List<ItemSO> _created = new List<ItemSO>();
    private readonly List<CraftingRecipe> _recipes = new List<CraftingRecipe>();

    [TearDown]
    public void TearDown()
    {
        foreach (var r in _recipes) if (r != null) Object.DestroyImmediate(r);
        _recipes.Clear();
        foreach (var so in _created) if (so != null) Object.DestroyImmediate(so);
        _created.Clear();
    }

    private ItemSO MakeItem(string id, string name = null)
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = id;
        so.itemName = name ?? id;
        _created.Add(so);
        return so;
    }

    private CraftingRecipe MakeRecipe()
    {
        var r = ScriptableObject.CreateInstance<CraftingRecipe>();
        r.ingredients = new List<Ingredient>();
        _recipes.Add(r);
        return r;
    }

    [Test]
    public void IsValid_NoIngredients_ReturnsFalse()
    {
        var r = MakeRecipe();
        r.resultItemSO = MakeItem("result");
        r.resultCount = 1;
        Assert.IsFalse(r.IsValid());
    }

    [Test]
    public void IsValid_NoResult_ReturnsFalse()
    {
        var r = MakeRecipe();
        r.ingredients.Add(new Ingredient { itemSO = MakeItem("a"), count = 1 });
        r.resultCount = 1;
        Assert.IsFalse(r.IsValid());
    }

    [Test]
    public void IsValid_ZeroResultCount_ReturnsFalse()
    {
        var r = MakeRecipe();
        r.ingredients.Add(new Ingredient { itemSO = MakeItem("a"), count = 1 });
        r.resultItemSO = MakeItem("result");
        r.resultCount = 0;
        Assert.IsFalse(r.IsValid());
    }

    [Test]
    public void IsValid_Complete_ReturnsTrue()
    {
        var r = MakeRecipe();
        r.ingredients.Add(new Ingredient { itemSO = MakeItem("a"), count = 1 });
        r.resultItemSO = MakeItem("result");
        r.resultCount = 1;
        Assert.IsTrue(r.IsValid());
    }

    [Test]
    public void ResolveResultItemSO_DirectReference_ReturnsIt()
    {
        var r = MakeRecipe();
        var result = MakeItem("result");
        r.resultItemSO = result;
        Assert.AreSame(result, r.ResolveResultItemSO());
    }

    [Test]
    public void GetDisplayName_WithDisplayName_ReturnsIt()
    {
        var r = MakeRecipe();
        r.displayName = "Custom Name";
        Assert.AreEqual("Custom Name", r.GetDisplayName());
    }

    [Test]
    public void GetDisplayName_FromResultSO_ReturnsItemName()
    {
        var r = MakeRecipe();
        r.resultItemSO = MakeItem("id", "Result Item");
        Assert.AreEqual("Result Item", r.GetDisplayName());
    }

    [Test]
    public void ResolveResultItemId_DirectReference_ReturnsId()
    {
        var r = MakeRecipe();
        r.resultItemSO = MakeItem("result_id");
        Assert.AreEqual("result_id", r.ResolveResultItemId());
    }
}