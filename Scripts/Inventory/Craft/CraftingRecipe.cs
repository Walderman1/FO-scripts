using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Рецепт", menuName = "|Крафт|/Рецепт")]
public class CraftingRecipe : ScriptableObject
{
    [Header("=== ИДЕНТИФИКАЦИЯ ===")]
    public string recipeID;

    [Header("=== ИНГРЕДИЕНТЫ ===")]
    public List<Ingredient> ingredients = new();

    [Header("=== РЕЗУЛЬТАТ ===")]
    public ItemSO resultItemSO;

    [System.Obsolete("Используйте resultItemSO")]
    public ItemType resultItem = ItemType.None;

    public ItemCategory resultCategory = ItemCategory.None;
    public int resultCount = 1;

    [Header("=== НАСТРОЙКИ ===")]
    public CraftMatchMode matchMode = CraftMatchMode.ExactCount;
    public int priority = 0;

    [Header("=== ОТОБРАЖЕНИЕ ===")]
    public string displayName = "";
    [TextArea(2, 4)] public string description = "";

    public bool IsValid()
    {
        if (ingredients == null || ingredients.Count == 0) return false;

        if (resultItemSO == null && resultCategory == ItemCategory.None)
        {
#pragma warning disable CS0618
            if (resultItem == ItemType.None) return false;
#pragma warning restore CS0618
        }

        if (resultCount <= 0) return false;

        foreach (var ing in ingredients)
        {
            if (ing == null) return false;
            if (!ing.IsValid()) return false;
        }

        return true;
    }

    public ItemSO ResolveResultItemSO()
    {
        if (resultItemSO != null) return resultItemSO;

        // ★ ScriptableObject → GameReferences.Instance оправдан
        var db = GameReferences.Instance?.ItemDatabase;

        if (resultCategory != ItemCategory.None && db != null)
        {
            var items = db.GetItemsByCategory(resultCategory);
            if (items != null && items.Count > 0)
            {
                resultItemSO = items[0];
                return resultItemSO;
            }
        }

#pragma warning disable CS0618
        if (resultItem != ItemType.None && db != null)
        {
            resultItemSO = db.GetItemByLegacyType(resultItem);
            return resultItemSO;
        }
#pragma warning restore CS0618

        return null;
    }

    public string ResolveResultItemId()
    {
        var so = ResolveResultItemSO();
        return so != null ? so.id : "";
    }

    public string GetDisplayName()
    {
        if (!string.IsNullOrEmpty(displayName)) return displayName;

        var so = ResolveResultItemSO();
        if (so != null) return so.itemName;

        if (resultCategory != ItemCategory.None) return $"Любой {resultCategory}";
        return name;
    }

    private void OnValidate()
    {
        if (ingredients == null || ingredients.Count == 0)
            Logger.LogWarning(LogModule.Craft, $"Рецепт '{name}': нет ингредиентов");

        if (resultItemSO == null && resultCategory == ItemCategory.None)
        {
#pragma warning disable CS0618
            if (resultItem == ItemType.None)
                Logger.LogWarning(LogModule.Craft, $"Рецепт '{name}': не указан результат");
#pragma warning restore CS0618
        }
    }
}

public enum CraftMatchMode
{
    [InspectorName("Точное количество")] ExactCount,
    [InspectorName("Минимум")] AtLeastCount
}