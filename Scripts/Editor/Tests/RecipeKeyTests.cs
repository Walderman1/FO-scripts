using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

public class RecipeKeyTests
{
    [Test]
    public void Equals_SameIngredients_ReturnsTrue()
    {
        var a = new RecipeKey(new Dictionary<string, int>
        {
            { "apple", 2 },
            { "potion", 1 }
        });
        var b = new RecipeKey(new Dictionary<string, int>
        {
            { "potion", 1 },
            { "apple", 2 }
        });

        Assert.IsTrue(a.Equals(b));
        Assert.AreEqual(a.GetHashCode(), b.GetHashCode());
    }

    [Test]
    public void Equals_DifferentCount_ReturnsFalse()
    {
        var a = new RecipeKey(new Dictionary<string, int> { { "apple", 2 } });
        var b = new RecipeKey(new Dictionary<string, int> { { "apple", 3 } });

        Assert.IsFalse(a.Equals(b));
    }

    [Test]
    public void Equals_DifferentItems_ReturnsFalse()
    {
        var a = new RecipeKey(new Dictionary<string, int> { { "apple", 1 } });
        var b = new RecipeKey(new Dictionary<string, int> { { "potion", 1 } });

        Assert.IsFalse(a.Equals(b));
    }

    [Test]
    public void Equals_ZeroOrNoneEntries_AreIgnored()
    {
        var a = new RecipeKey(new Dictionary<string, int>
        {
            { "apple", 1 },
            { "", 5 },
            { "potion", 0 }
        });
        var b = new RecipeKey(new Dictionary<string, int> { { "apple", 1 } });

        Assert.IsTrue(a.Equals(b));
    }

    [Test]
    public void Constructor_FromIngredients_AggregatesDuplicates()
    {
        var appleSO = ScriptableObject.CreateInstance<ItemSO>();
        appleSO.id = "apple";

        var ingredients = new List<Ingredient>
        {
            new Ingredient { itemSO = appleSO, count = 1 },
            new Ingredient { itemSO = appleSO, count = 2 }
        };
        var key = new RecipeKey(ingredients);
        var expected = new RecipeKey(new Dictionary<string, int> { { "apple", 3 } });

        Assert.IsTrue(key.Equals(expected));

        UnityEngine.Object.DestroyImmediate(appleSO);
    }
}