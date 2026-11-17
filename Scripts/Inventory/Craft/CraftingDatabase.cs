using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CraftingDatabase : MonoBehaviour
{
    [Header("=== РЕЦЕПТЫ ===")]
    [SerializeField] private List<CraftingRecipe> allRecipes = new();
    [SerializeField] private bool autoLoadFromResources = true;
    [SerializeField] private bool validateOnLoad = true;

    private Dictionary<RecipeKey, CraftingRecipe> recipeLookup = new();

    public System.Action OnDatabaseLoaded;

    private void Awake()
    {
        LoadDatabase();
        Logger.LogVerbose(LogModule.Craft, "CraftingDatabase инициализирован");
    }

    public void LoadDatabase()
    {
        recipeLookup.Clear();

        if (autoLoadFromResources && (allRecipes == null || allRecipes.Count == 0))
        {
            allRecipes = Resources.LoadAll<CraftingRecipe>("Items/Crafting").ToList();
            Logger.Log(LogModule.Craft, $"Автозагрузка: {allRecipes.Count} рецептов");
        }

        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;

            var key = new RecipeKey(recipe.ingredients);

            if (recipeLookup.TryGetValue(key, out var existing))
            {
                if (recipe.priority > existing.priority)
                {
                    Logger.LogWarning(LogModule.Craft,
                        $"Конфликт рецептов: '{existing.name}' vs '{recipe.name}', берём '{recipe.name}'");
                    recipeLookup[key] = recipe;
                }
                continue;
            }

            recipeLookup[key] = recipe;
        }

        if (validateOnLoad) Validate();
        OnDatabaseLoaded?.Invoke();
    }

    private void Validate()
    {
        foreach (var recipe in allRecipes)
        {
            if (recipe == null) continue;

            if (!recipe.IsValid())
            {
                Logger.LogWarning(LogModule.Craft, $"Рецепт '{recipe.name}': невалидный");
                continue;
            }

            if (recipe.ResolveResultItemSO() == null)
                Logger.LogWarning(LogModule.Craft, $"Рецепт '{recipe.name}': результат не резолвится в ItemSO");

            var seen = new HashSet<string>();
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null) continue;
                string id = ing.GetId();
                if (string.IsNullOrEmpty(id))
                    Logger.LogWarning(LogModule.Craft, $"Рецепт '{recipe.name}': ингредиент без id");
                if (ing.count <= 0)
                    Logger.LogWarning(LogModule.Craft, $"Рецепт '{recipe.name}': {id} count <= 0");
                if (!seen.Add(id))
                    Logger.LogWarning(LogModule.Craft, $"Рецепт '{recipe.name}': дубликат {id}");
            }
        }
    }

    public CraftingRecipe FindRecipe(Dictionary<string, int> input)
    {
        if (input == null || input.Count == 0) return null;

        var cleaned = input
            .Where(kv => kv.Value > 0 && !string.IsNullOrEmpty(kv.Key))
            .ToDictionary(kv => kv.Key, kv => kv.Value);

        if (cleaned.Count == 0) return null;

        var key = new RecipeKey(cleaned);
        if (recipeLookup.TryGetValue(key, out var exact))
            return exact;

        foreach (var recipe in allRecipes)
        {
            if (recipe == null || !recipe.IsValid()) continue;
            if (RecipeMatches(recipe, cleaned))
                return recipe;
        }

        return null;
    }

    private bool RecipeMatches(CraftingRecipe recipe, Dictionary<string, int> input)
    {
        var remaining = new Dictionary<string, int>(input);

        foreach (var ing in recipe.ingredients)
        {
            if (ing == null) return false;
            string id = ing.GetId();
            if (string.IsNullOrEmpty(id)) return false;

            if (!remaining.TryGetValue(id, out int have) || have < ing.count)
                return false;

            remaining[id] = have - ing.count;
        }

        int totalLeft = remaining.Values.Sum();

        if (recipe.matchMode == CraftMatchMode.ExactCount && totalLeft > 0)
            return false;

        return true;
    }

    public List<CraftingRecipe> GetAllRecipes() => allRecipes;

    public List<CraftingRecipe> SearchByName(string query, IEnumerable<CraftingRecipe> pool = null)
    {
        if (string.IsNullOrWhiteSpace(query)) return new List<CraftingRecipe>();

        var source = pool ?? allRecipes;
        string q = query.Trim().ToLowerInvariant();

        return source
            .Where(r => r != null)
            .Where(r => !string.IsNullOrEmpty(r.GetDisplayName()))
            .Where(r => r.GetDisplayName().ToLowerInvariant().Contains(q))
            .ToList();
    }

#if UNITY_EDITOR
    [ContextMenu("Обновить из Resources")]
    public void RefreshFromResources()
    {
        allRecipes = Resources.LoadAll<CraftingRecipe>("Items/Crafting").ToList();
        UnityEditor.EditorUtility.SetDirty(this);
        LoadDatabase();
    }
#endif
}