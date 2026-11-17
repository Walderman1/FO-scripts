using UnityEngine;
using System.Collections.Generic;
using VContainer;

public class CraftingSystem : MonoBehaviour
{
    [Inject] private CraftingDatabase _craftingDatabase;
    [Inject] private CraftingProgress _craftingProgress;

    private CraftingDatabase Database
    {
        get
        {
            if (_craftingDatabase != null) return _craftingDatabase;
            return GameLifetimeScope.Current?.Container?.Resolve<CraftingDatabase>();
        }
    }

    private CraftingProgress Progress
    {
        get
        {
            if (_craftingProgress != null) return _craftingProgress;
            return GameLifetimeScope.Current?.Container?.Resolve<CraftingProgress>();
        }
    }

    public CraftingRecipe TryCraft(Dictionary<string, int> input, out string resultItemId, out int resultCount)
    {
        resultItemId = "";
        resultCount = 0;

        if (input == null || input.Count == 0) return null;

        var database = Database;
        if (database == null) return null;

        var recipe = database.FindRecipe(input);
        if (recipe == null) return null;

        resultItemId = recipe.ResolveResultItemId();
        resultCount = recipe.resultCount;

        if (string.IsNullOrEmpty(resultItemId)) return null;

        Progress?.LearnRecipe(recipe);

        return recipe;
    }

    public bool CanCraft(CraftingRecipe recipe, InventoryUIManager inventory)
    {
        if (recipe == null || inventory == null) return false;
        if (!recipe.IsValid()) return false;

        foreach (var ing in recipe.ingredients)
        {
            if (ing == null) return false;
            string id = ing.GetId();
            if (string.IsNullOrEmpty(id)) return false;
            if (inventory.GetItemCount(id) < ing.count) return false;
        }

        return true;
    }

    public bool ConsumeIngredients(CraftingRecipe recipe, InventoryUIManager inventory)
    {
        if (recipe == null || inventory == null) return false;

        foreach (var ing in recipe.ingredients)
        {
            if (ing == null) return false;
            string id = ing.GetId();
            if (string.IsNullOrEmpty(id)) return false;

            if (!inventory.RemoveItem(id, ing.count))
                return false;
        }

        return true;
    }
}