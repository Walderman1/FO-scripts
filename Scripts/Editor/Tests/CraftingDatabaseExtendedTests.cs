using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class CraftingDatabaseExtendedTests
{
    private GameObject _dbGO;
    private CraftingDatabase _db;
    private readonly List<CraftingRecipe> _recipes = new List<CraftingRecipe>();
    private readonly List<ItemSO> _items = new List<ItemSO>();

    [SetUp]
    public void SetUp()
    {
        _dbGO = new GameObject("TestDB");
        _dbGO.SetActive(false);
        _db = _dbGO.AddComponent<CraftingDatabase>();

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var t = typeof(CraftingDatabase);
        t.GetField("autoLoadFromResources", flags)?.SetValue(_db, false);
        t.GetField("validateOnLoad", flags)?.SetValue(_db, false);
        ((List<CraftingRecipe>)t.GetField("allRecipes", flags).GetValue(_db))?.Clear();

        _dbGO.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (_dbGO != null) Object.DestroyImmediate(_dbGO);
        foreach (var r in _recipes) if (r != null) Object.DestroyImmediate(r);
        _recipes.Clear();
        foreach (var i in _items) if (i != null) Object.DestroyImmediate(i);
        _items.Clear();
    }

    private ItemSO MakeItem(string id)
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = id;
        so.itemName = id;
        _items.Add(so);
        return so;
    }

    private CraftingRecipe MakeRecipe(string name, CraftMatchMode mode, params (string id, int count)[] ings)
    {
        var r = ScriptableObject.CreateInstance<CraftingRecipe>();
        r.name = name;
        r.matchMode = mode;
        r.resultItemSO = MakeItem("result_" + name);
        r.resultCount = 1;
        r.ingredients = new List<Ingredient>();
        foreach (var (id, count) in ings)
            r.ingredients.Add(new Ingredient { itemSO = MakeItem(id), count = count });
        _recipes.Add(r);
        return r;
    }

    private void AddToDB(CraftingRecipe r)
    {
        var list = (List<CraftingRecipe>)typeof(CraftingDatabase)
            .GetField("allRecipes", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(_db);
        list.Add(r);
    }

    [Test]
    public void AtLeast_ExactlyMinimum_Matches()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.AtLeastCount, ("apple", 2)));
        _db.LoadDatabase();
        Assert.IsNotNull(_db.FindRecipe(new Dictionary<string, int> { { "apple", 2 } }));
    }

    [Test]
    public void AtLeast_LessThanMinimum_NoMatch()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.AtLeastCount, ("apple", 2)));
        _db.LoadDatabase();
        Assert.IsNull(_db.FindRecipe(new Dictionary<string, int> { { "apple", 1 } }));
    }

    [Test]
    public void EmptyInput_ReturnsNull()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.ExactCount, ("apple", 1)));
        _db.LoadDatabase();
        Assert.IsNull(_db.FindRecipe(null));
        Assert.IsNull(_db.FindRecipe(new Dictionary<string, int>()));
    }

    [Test]
    public void ZeroValueInInput_Ignored()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.ExactCount, ("apple", 1)));
        _db.LoadDatabase();
        var r = _db.FindRecipe(new Dictionary<string, int> { { "apple", 1 }, { "potion", 0 } });
        Assert.IsNotNull(r);
    }

    [Test]
    public void TwoRecipesDifferentIngredients_EachFound()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.ExactCount, ("apple", 1)));
        AddToDB(MakeRecipe("r2", CraftMatchMode.ExactCount, ("potion", 1)));
        _db.LoadDatabase();

        Assert.AreEqual("r1", _db.FindRecipe(new Dictionary<string, int> { { "apple", 1 } }).name);
        Assert.AreEqual("r2", _db.FindRecipe(new Dictionary<string, int> { { "potion", 1 } }).name);
    }

    [Test]
    public void ThreeIngredientRecipe_Matches()
    {
        AddToDB(MakeRecipe("r1", CraftMatchMode.ExactCount, ("a", 1), ("b", 2), ("c", 3)));
        _db.LoadDatabase();
        var r = _db.FindRecipe(new Dictionary<string, int> { { "a", 1 }, { "b", 2 }, { "c", 3 } });
        Assert.IsNotNull(r);
    }
}