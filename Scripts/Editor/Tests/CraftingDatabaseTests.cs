using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

public class CraftingDatabaseTests
{
    private GameObject dbGO;
    private CraftingDatabase db;
    private readonly List<CraftingRecipe> tempRecipes = new List<CraftingRecipe>();
    private readonly List<ItemSO> tempItems = new List<ItemSO>();

    [SetUp]
    public void SetUp()
    {
        dbGO = new GameObject("TestCraftingDB");
        dbGO.SetActive(false);

        db = dbGO.AddComponent<CraftingDatabase>();

        var flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var t = typeof(CraftingDatabase);

        t.GetField("autoLoadFromResources", flags)?.SetValue(db, false);
        t.GetField("validateOnLoad", flags)?.SetValue(db, false);

        var list = (List<CraftingRecipe>)t.GetField("allRecipes", flags).GetValue(db);
        list?.Clear();

        dbGO.SetActive(true);
    }

    [TearDown]
    public void TearDown()
    {
        if (dbGO != null) Object.DestroyImmediate(dbGO);

        foreach (var r in tempRecipes)
            if (r != null) Object.DestroyImmediate(r);
        tempRecipes.Clear();

        foreach (var i in tempItems)
            if (i != null) Object.DestroyImmediate(i);
        tempItems.Clear();
    }

    private ItemSO MakeItemSO(string id)
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = id;
        so.itemName = id;
        tempItems.Add(so);
        return so;
    }

    private CraftingRecipe MakeRecipe(string name, CraftMatchMode mode, params (string id, int count)[] ingredients)
    {
        var r = ScriptableObject.CreateInstance<CraftingRecipe>();
        r.name = name;
        r.matchMode = mode;
        r.resultItemSO = MakeItemSO("result_" + name);
        r.resultCount = 1;
        r.ingredients = new List<Ingredient>();

        foreach (var (id, count) in ingredients)
        {
            r.ingredients.Add(new Ingredient
            {
                itemSO = MakeItemSO(id),
                count = count
            });
        }

        tempRecipes.Add(r);
        return r;
    }

    private void AddRecipeToDb(CraftingRecipe r)
    {
        var list = (List<CraftingRecipe>)typeof(CraftingDatabase)
            .GetField("allRecipes", BindingFlags.NonPublic | BindingFlags.Instance)
            .GetValue(db);
        list.Add(r);
    }

    [Test]
    public void FindRecipe_ExactMatch_ReturnsRecipe()
    {
        var r = MakeRecipe("r1", CraftMatchMode.ExactCount,
            ("apple", 2), ("potion", 1));
        AddRecipeToDb(r);
        db.LoadDatabase();

        var result = db.FindRecipe(new Dictionary<string, int>
        {
            { "apple", 2 },
            { "potion", 1 }
        });

        Assert.AreSame(r, result);
    }

    [Test]
    public void FindRecipe_ExactCount_ExtraInput_ReturnsNull()
    {
        var r = MakeRecipe("r1", CraftMatchMode.ExactCount, ("apple", 2));
        AddRecipeToDb(r);
        db.LoadDatabase();

        var result = db.FindRecipe(new Dictionary<string, int>
        {
            { "apple", 3 }
        });

        Assert.IsNull(result);
    }

    [Test]
    public void FindRecipe_AtLeastCount_ExtraInput_ReturnsRecipe()
    {
        var r = MakeRecipe("r1", CraftMatchMode.AtLeastCount, ("apple", 2));
        AddRecipeToDb(r);
        db.LoadDatabase();

        var result = db.FindRecipe(new Dictionary<string, int>
        {
            { "apple", 5 }
        });

        Assert.AreSame(r, result);
    }

    [Test]
    public void FindRecipe_NoMatch_ReturnsNull()
    {
        var r = MakeRecipe("r1", CraftMatchMode.ExactCount, ("apple", 2));
        AddRecipeToDb(r);
        db.LoadDatabase();

        var result = db.FindRecipe(new Dictionary<string, int>
        {
            { "potion", 2 }
        });

        Assert.IsNull(result);
    }

    [Test]
    public void FindRecipe_EmptyInput_ReturnsNull()
    {
        db.LoadDatabase();

        Assert.IsNull(db.FindRecipe(new Dictionary<string, int>()));
        Assert.IsNull(db.FindRecipe(null));
    }

    [Test]
    public void FindRecipe_HigherPriorityWins_OnConflict()
    {
        var low = MakeRecipe("low", CraftMatchMode.ExactCount, ("apple", 1));
        low.priority = 0;
        var high = MakeRecipe("high", CraftMatchMode.ExactCount, ("apple", 1));
        high.priority = 10;

        AddRecipeToDb(low);
        AddRecipeToDb(high);
        db.LoadDatabase();

        var result = db.FindRecipe(new Dictionary<string, int>
        {
            { "apple", 1 }
        });

        Assert.AreSame(high, result);
    }
}