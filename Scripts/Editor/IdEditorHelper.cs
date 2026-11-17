#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;

public static class IdEditorHelper
{
    public static void DrawIdField(
        SerializedObject so,
        SerializedProperty idProp,
        string label,
        string prefix,
        params string[] sourceFields)
    {
        if (idProp == null) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.PropertyField(idProp, new GUIContent(label));

        if (GUILayout.Button(new GUIContent("📝", "Сгенерировать из имени"), GUILayout.Width(26), GUILayout.Height(18)))
        {
            string generated = GenerateFromFields(so, prefix, sourceFields);
            if (string.IsNullOrEmpty(generated))
                generated = IdGenerator.FromAssetName(so.targetObject, prefix);

            if (!string.IsNullOrEmpty(generated))
            {
                idProp.stringValue = generated;
                so.ApplyModifiedProperties();
            }
        }

        if (so.targetObject != null && EditorUtility.IsPersistent(so.targetObject))
        {
            if (GUILayout.Button(new GUIContent("📁", "Сгенерировать из имени ассета"), GUILayout.Width(26), GUILayout.Height(18)))
            {
                string generated = IdGenerator.FromAssetName(so.targetObject, prefix);
                if (!string.IsNullOrEmpty(generated))
                {
                    idProp.stringValue = generated;
                    so.ApplyModifiedProperties();
                }
            }
        }

        EditorGUILayout.EndHorizontal();

        if (string.IsNullOrEmpty(idProp.stringValue))
            EditorGUILayout.HelpBox("ID пустой.", MessageType.Warning);
        else if (IdGenerator.ContainsCyrillic(idProp.stringValue))
            EditorGUILayout.HelpBox("ID содержит кириллицу — не рекомендуется.", MessageType.Warning);
    }

    public static void DrawInline(
        string label,
        string currentValue,
        Action<string> setter,
        string prefix,
        string[] sources,
        int fallbackIndex)
    {
        EditorGUILayout.BeginHorizontal();

        string newValue = EditorGUILayout.TextField(label, currentValue);
        if (newValue != currentValue) setter?.Invoke(newValue);

        if (GUILayout.Button(new GUIContent("📝", "Сгенерировать"), GUILayout.Width(26), GUILayout.Height(18)))
        {
            string generated = GenerateInline(prefix, sources, fallbackIndex);
            if (!string.IsNullOrEmpty(generated)) setter?.Invoke(generated);
        }

        EditorGUILayout.EndHorizontal();
    }

    private static string GenerateFromFields(SerializedObject so, string prefix, string[] fields)
    {
        if (fields == null) return "";

        foreach (var fieldName in fields)
        {
            var prop = so.FindProperty(fieldName);
            if (prop == null || prop.propertyType != SerializedPropertyType.String) continue;

            string value = prop.stringValue;
            if (string.IsNullOrEmpty(value)) continue;

            var slug = IdGenerator.Slugify(value);
            if (string.IsNullOrEmpty(slug)) continue;

            return string.IsNullOrEmpty(prefix)
                ? slug
                : $"{IdGenerator.Slugify(prefix)}_{slug}";
        }

        return "";
    }

    private static string GenerateInline(string prefix, string[] sources, int fallbackIndex)
    {
        if (sources != null)
        {
            foreach (var source in sources)
            {
                if (string.IsNullOrEmpty(source)) continue;
                var slug = IdGenerator.Slugify(source);
                if (!string.IsNullOrEmpty(slug))
                {
                    return string.IsNullOrEmpty(prefix)
                        ? slug
                        : $"{IdGenerator.Slugify(prefix)}_{slug}";
                }
            }
        }

        string fallbackPrefix = string.IsNullOrEmpty(prefix) ? "id" : IdGenerator.Slugify(prefix);
        return $"{fallbackPrefix}_{fallbackIndex}";
    }

    [MenuItem("Tools/Миграция/Сгенерировать все ID")]
    public static void GenerateAllIds()
    {
        bool proceed = EditorUtility.DisplayDialog(
            "Генерация ID",
            "Заполнить пустые ID во всех ассетах проекта?\n\n" +
            "• ItemSO → по itemName\n" +
            "• QuestSO → по questName\n" +
            "• QuestObjective → по description\n" +
            "• GameEvent → по eventName\n" +
            "• CraftingRecipe → по displayName\n\n" +
            "Существующие ID не будут изменены.",
            "Сгенерировать", "Отмена");

        if (!proceed) return;

        int items = 0, quests = 0, objectives = 0, events = 0, recipes = 0;

        foreach (var guid in AssetDatabase.FindAssets("t:ItemSO"))
        {
            var so = AssetDatabase.LoadAssetAtPath<ItemSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null || !string.IsNullOrEmpty(so.id)) continue;

            so.id = !string.IsNullOrEmpty(so.itemName)
                ? IdGenerator.Combine("item", so.itemName)
                : IdGenerator.FromAssetName(so, "item");

            if (!string.IsNullOrEmpty(so.id)) { EditorUtility.SetDirty(so); items++; }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:QuestSO"))
        {
            var so = AssetDatabase.LoadAssetAtPath<QuestSO>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null) continue;

            bool changed = false;

            if (string.IsNullOrEmpty(so.questID))
            {
                so.questID = !string.IsNullOrEmpty(so.questName)
                    ? IdGenerator.Combine("quest", so.questName)
                    : IdGenerator.FromAssetName(so, "quest");

                if (!string.IsNullOrEmpty(so.questID)) { changed = true; quests++; }
            }

            if (so.objectives != null)
            {
                for (int i = 0; i < so.objectives.Count; i++)
                {
                    var obj = so.objectives[i];
                    if (obj == null || !string.IsNullOrEmpty(obj.objectiveID)) continue;

                    obj.objectiveID = !string.IsNullOrEmpty(obj.description)
                        ? IdGenerator.Combine("obj", obj.description)
                        : $"obj_{obj.type.ToString().ToLowerInvariant()}_{i}";

                    changed = true;
                    objectives++;
                }
            }

            if (changed) EditorUtility.SetDirty(so);
        }

        foreach (var guid in AssetDatabase.FindAssets("t:GameEvent"))
        {
            var so = AssetDatabase.LoadAssetAtPath<GameEvent>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null || !string.IsNullOrEmpty(so.eventID)) continue;

            so.eventID = !string.IsNullOrEmpty(so.eventName)
                ? IdGenerator.Combine("event", so.eventName)
                : IdGenerator.FromAssetName(so, "event");

            if (!string.IsNullOrEmpty(so.eventID)) { EditorUtility.SetDirty(so); events++; }
        }

        foreach (var guid in AssetDatabase.FindAssets("t:CraftingRecipe"))
        {
            var so = AssetDatabase.LoadAssetAtPath<CraftingRecipe>(AssetDatabase.GUIDToAssetPath(guid));
            if (so == null || !string.IsNullOrEmpty(so.recipeID)) continue;

            so.recipeID = !string.IsNullOrEmpty(so.displayName)
                ? IdGenerator.Combine("recipe", so.displayName)
                : IdGenerator.FromAssetName(so, "recipe");

            if (!string.IsNullOrEmpty(so.recipeID)) { EditorUtility.SetDirty(so); recipes++; }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Logger.Log(LogModule.Editor, $"IdGenerator: ItemSO={items}, QuestSO={quests}, QuestObjective={objectives}, GameEvent={events}, CraftingRecipe={recipes}");
        EditorUtility.DisplayDialog("Готово",
            $"Сгенерировано:\n\nItemSO: {items}\nQuestSO: {quests}\nQuestObjective: {objectives}\nGameEvent: {events}\nCraftingRecipe: {recipes}",
            "OK");
    }
}
#endif