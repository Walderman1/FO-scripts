#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(CraftingRecipe))]
public class CraftingRecipeEditor : Editor
{
    private CraftingRecipe recipe;

    private SerializedProperty recipeIDProp;
    private SerializedProperty ingredientsProp;
    private SerializedProperty resultItemSOProp;
    private SerializedProperty resultCountProp;
    private SerializedProperty resultCategoryProp;
    private SerializedProperty matchModeProp;
    private SerializedProperty priorityProp;
    private SerializedProperty displayNameProp;
    private SerializedProperty descriptionProp;

    private bool showIngredients = true;
    private bool showFallbackCategory = false;
    private bool showSettings = true;

    private void OnEnable()
    {
        recipe = (CraftingRecipe)target;
        recipeIDProp = serializedObject.FindProperty("recipeID");
        ingredientsProp = serializedObject.FindProperty("ingredients");
        resultItemSOProp = serializedObject.FindProperty("resultItemSO");
        resultCountProp = serializedObject.FindProperty("resultCount");
        resultCategoryProp = serializedObject.FindProperty("resultCategory");
        matchModeProp = serializedObject.FindProperty("matchMode");
        priorityProp = serializedObject.FindProperty("priority");
        displayNameProp = serializedObject.FindProperty("displayName");
        descriptionProp = serializedObject.FindProperty("description");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawRecipeHeader();
        DrawMainInfo();
        DrawIngredients();
        DrawResult();
        DrawSettings();
        DrawActions();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawRecipeHeader()
    {
        EditorGUILayout.Space(8);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        string resultLabel = "";
        if (recipe.resultItemSO != null)
            resultLabel = $" → {recipe.resultItemSO.itemName}";
        else if (recipe.resultItemSO == null && HasLegacyResult())
            resultLabel = " [legacy результат]";
        else
            resultLabel = " [нет результата]";

        EditorGUILayout.LabelField($"⚗️ {recipe.name}{resultLabel}", style);
        EditorGUILayout.Space(6);
    }

    private bool HasLegacyResult()
    {
#pragma warning disable CS0618
        return recipe.resultItem != ItemType.None;
#pragma warning restore CS0618
    }

    private void DrawMainInfo()
    {
        EditorGUILayout.LabelField("ОСНОВНАЯ ИНФОРМАЦИЯ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        IdEditorHelper.DrawIdField(serializedObject, recipeIDProp, "ID рецепта", "recipe", "displayName");

        EditorGUILayout.PropertyField(displayNameProp, new GUIContent("Отображаемое название"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Описание"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawIngredients()
    {
        showIngredients = EditorGUILayout.Foldout(showIngredients,
            $"ИНГРЕДИЕНТЫ ({recipe.ingredients.Count})", true);

        if (!showIngredients) return;

        EditorGUILayout.BeginVertical("box");

        if (recipe.ingredients.Count == 0)
            EditorGUILayout.HelpBox("Нет ингредиентов. Рецепт невалиден.", MessageType.Warning);

        for (int i = 0; i < recipe.ingredients.Count; i++)
        {
            var ing = recipe.ingredients[i];
            if (ing == null) continue;

            EditorGUILayout.BeginVertical("helpbox");

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"Ингредиент {i + 1}", EditorStyles.boldLabel);

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Удалить", GUILayout.Width(70)))
            {
                recipe.ingredients.RemoveAt(i);
                EditorUtility.SetDirty(recipe);
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                i--;
                continue;
            }
            EditorGUILayout.EndHorizontal();

            DrawIngredientFields(ing, i);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(3);
        }

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Добавить ингредиент"))
        {
            recipe.ingredients.Add(new Ingredient());
            EditorUtility.SetDirty(recipe);
        }
        if (GUILayout.Button("Очистить"))
        {
            if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все ингредиенты?", "Да", "Нет"))
            {
                recipe.ingredients.Clear();
                EditorUtility.SetDirty(recipe);
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawIngredientFields(Ingredient ing, int index)
    {
        var ingProp = ingredientsProp.GetArrayElementAtIndex(index);
        var itemSOProp = ingProp.FindPropertyRelative("itemSO");
        var itemTypeProp = ingProp.FindPropertyRelative("itemType");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Предмет", GUILayout.Width(120));
        EditorGUILayout.PropertyField(itemSOProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        if (itemSOProp.objectReferenceValue == null && itemTypeProp != null)
        {
#pragma warning disable CS0618
            var legacyVal = (ItemType)itemTypeProp.enumValueIndex;
#pragma warning restore CS0618

            if (legacyVal != ItemType.None)
            {
                EditorGUILayout.HelpBox($"Legacy ItemType: {legacyVal}", MessageType.Warning);
                if (GUILayout.Button("Мигрировать в ItemSO"))
                {
                    var so = FindItemSOByLegacyType(legacyVal);
                    if (so != null)
                    {
                        itemSOProp.objectReferenceValue = so;
                        serializedObject.ApplyModifiedProperties();
                        EditorUtility.SetDirty(recipe);
                    }
                    else
                    {
                        EditorUtility.DisplayDialog("Ошибка",
                            $"ItemSO для {legacyVal} не найден.", "OK");
                    }
                }
            }
        }

        ing.count = EditorGUILayout.IntField("Количество", ing.count);

        if (ing.count <= 0)
            EditorGUILayout.HelpBox("Количество должно быть > 0", MessageType.Error);
    }

    private void DrawResult()
    {
        EditorGUILayout.LabelField("РЕЗУЛЬТАТ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Предмет-результат", GUILayout.Width(140));
        EditorGUILayout.PropertyField(resultItemSOProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        if (recipe.resultItemSO == null)
        {
            var resultItemProp = serializedObject.FindProperty("resultItem");
            if (resultItemProp != null)
            {
#pragma warning disable CS0618
                var legacyVal = (ItemType)resultItemProp.enumValueIndex;
#pragma warning restore CS0618

                if (legacyVal != ItemType.None)
                {
                    EditorGUILayout.HelpBox($"Legacy ItemType: {legacyVal}", MessageType.Warning);
                    if (GUILayout.Button("Мигрировать в ItemSO"))
                    {
                        var so = FindItemSOByLegacyType(legacyVal);
                        if (so != null)
                        {
                            resultItemSOProp.objectReferenceValue = so;
                            serializedObject.ApplyModifiedProperties();
                            EditorUtility.SetDirty(recipe);
                        }
                        else
                        {
                            EditorUtility.DisplayDialog("Ошибка",
                                $"ItemSO для {legacyVal} не найден.", "OK");
                        }
                    }
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "Результат не задан. Рецепт не будет работать.",
                        MessageType.Warning);
                }
            }
        }
        else
        {
            EditorGUILayout.LabelField(
                $"✓ Результат: {recipe.resultItemSO.itemName} (id: {recipe.resultItemSO.id})",
                EditorStyles.miniLabel);
        }

        EditorGUILayout.PropertyField(resultCountProp, new GUIContent("Количество"));

        if (recipe.resultCount <= 0)
            EditorGUILayout.HelpBox("Количество должно быть > 0", MessageType.Error);

        EditorGUILayout.Space(4);

        showFallbackCategory = EditorGUILayout.Foldout(showFallbackCategory,
            "Fallback по категории (редко используется)");

        if (showFallbackCategory)
        {
            EditorGUILayout.HelpBox(
                "Если resultItemSO не задан — берётся первый предмет из этой категории.\n" +
                "Не рекомендуется для обычного крафта.",
                MessageType.Info);

            EditorGUILayout.PropertyField(resultCategoryProp, new GUIContent("Категория"));
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawSettings()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "НАСТРОЙКИ", true);
        if (!showSettings) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Режим совпадения", GUILayout.Width(180));
        EditorGUILayout.PropertyField(matchModeProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.PropertyField(priorityProp, new GUIContent("Приоритет",
            "При конфликте двух рецептов с одинаковым набором побеждает больший приоритет"));

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();

        if (GUILayout.Button("Обновить"))
        {
            EditorUtility.SetDirty(recipe);
            AssetDatabase.SaveAssets();
        }

        if (GUILayout.Button("Копировать JSON"))
        {
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(recipe, true);
        }

        if (GUILayout.Button("Проверить"))
            ValidateRecipe();

        EditorGUILayout.EndHorizontal();
    }

    private void ValidateRecipe()
    {
        var problems = new List<string>();

        if (recipe.ingredients == null || recipe.ingredients.Count == 0)
            problems.Add("Нет ингредиентов");

        if (recipe.resultItemSO == null && !HasLegacyResult() &&
            recipe.resultCategory == ItemCategory.None)
            problems.Add("Не задан результат (ни ItemSO, ни категория)");

        if (recipe.resultCount <= 0)
            problems.Add("resultCount <= 0");

        var seen = new HashSet<ItemSO>();
        if (recipe.ingredients != null)
        {
            foreach (var ing in recipe.ingredients)
            {
                if (ing == null) continue;

                if (ing.itemSO == null)
                {
                    bool hasLegacy = false;
#pragma warning disable CS0618
                    hasLegacy = ing.itemType != ItemType.None;
#pragma warning restore CS0618
                    if (!hasLegacy) problems.Add("Ингредиент без предмета");
                }
                else if (!seen.Add(ing.itemSO))
                {
                    problems.Add($"Дубликат ингредиента: {ing.itemSO.id}");
                }

                if (ing.count <= 0)
                    problems.Add($"Ингредиент {ing.itemSO?.id ?? "?"}: count <= 0");
            }
        }

        if (problems.Count == 0)
        {
            EditorUtility.DisplayDialog("Проверка рецепта",
                "✓ Рецепт валиден.\n\n" +
                $"Ингредиентов: {recipe.ingredients.Count}\n" +
                $"Результат: {recipe.resultItemSO?.itemName ?? "?"} x{recipe.resultCount}",
                "OK");
        }
        else
        {
            EditorUtility.DisplayDialog("Проблемы в рецепте",
                "Найдены проблемы:\n\n• " + string.Join("\n• ", problems),
                "OK");
        }
    }

    private ItemSO FindItemSOByLegacyType(ItemType legacyType)
    {
        var db = Object.FindAnyObjectByType<ItemDatabase>(FindObjectsInactive.Include);
        if (db != null) return db.GetItemByLegacyType(legacyType);

        string[] guids = AssetDatabase.FindAssets("t:ItemSO");
        string targetName = legacyType.ToString();

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var so = AssetDatabase.LoadAssetAtPath<ItemSO>(path);
            if (so == null) continue;
            if (so.id == targetName.ToLowerInvariant() || so.itemName == targetName)
                return so;
        }
        return null;
    }
}
#endif