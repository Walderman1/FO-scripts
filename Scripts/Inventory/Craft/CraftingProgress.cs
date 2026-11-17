using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-9000)]
public class CraftingProgress : MonoBehaviour
{
    private HashSet<string> knownRecipes = new HashSet<string>();

    public System.Action<string> OnRecipeLearned;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private string GetRecipeId(CraftingRecipe recipe)
    {
        if (recipe == null) return string.Empty;
        if (!string.IsNullOrEmpty(recipe.recipeID)) return recipe.recipeID;
        return recipe.name;
    }

    public bool IsKnown(CraftingRecipe recipe) => recipe != null && knownRecipes.Contains(GetRecipeId(recipe));
    public bool IsKnown(string recipeId) => knownRecipes.Contains(recipeId);

    public void LearnRecipe(CraftingRecipe recipe)
    {
        if (recipe == null) return;
        string id = GetRecipeId(recipe);
        if (knownRecipes.Add(id))
        {
            Logger.Log(LogModule.Craft, $"Изучен новый рецепт: {id}. Всего: {knownRecipes.Count}");
            OnRecipeLearned?.Invoke(id);
        }
    }

    public IEnumerable<string> GetAllKnownIds() => knownRecipes;

    public void Save() { }
    public void Load() { }

    public void ResetProgress()
    {
        knownRecipes.Clear();
    }

    public List<string> ExportState() => new List<string>(knownRecipes);

    public void ImportState(List<string> ids)
    {
        knownRecipes.Clear();
        if (ids == null) return;
        foreach (var id in ids)
        {
            if (!string.IsNullOrEmpty(id)) knownRecipes.Add(id);
        }
    }
}