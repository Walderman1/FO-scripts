#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(QuestSO))]
public class QuestSOEditor : Editor
{
    private QuestSO quest;

    private SerializedProperty questIDProp;
    private SerializedProperty questNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty iconProp;
    private SerializedProperty priorityProp;
    private SerializedProperty questTypeProp;
    private SerializedProperty objectivesProp;
    private SerializedProperty rewardsProp;
    private SerializedProperty startConditionsProp;
    private SerializedProperty startDialogueIDProp;
    private SerializedProperty completeDialogueIDProp;
    private SerializedProperty autoStartProp;
    private SerializedProperty repeatableProp;
    private SerializedProperty isHiddenProp;

    private bool showObjectives = true;
    private bool showRewards = true;
    private bool showConditions = true;
    private bool showDialogues = true;
    private bool showSettings = true;

    private void OnEnable()
    {
        quest = (QuestSO)target;
        questIDProp = serializedObject.FindProperty("questID");
        questNameProp = serializedObject.FindProperty("questName");
        descriptionProp = serializedObject.FindProperty("description");
        iconProp = serializedObject.FindProperty("icon");
        priorityProp = serializedObject.FindProperty("priority");
        questTypeProp = serializedObject.FindProperty("questType");
        objectivesProp = serializedObject.FindProperty("objectives");
        rewardsProp = serializedObject.FindProperty("rewards");
        startConditionsProp = serializedObject.FindProperty("startConditions");
        startDialogueIDProp = serializedObject.FindProperty("startDialogueID");
        completeDialogueIDProp = serializedObject.FindProperty("completeDialogueID");
        autoStartProp = serializedObject.FindProperty("autoStart");
        repeatableProp = serializedObject.FindProperty("repeatable");
        isHiddenProp = serializedObject.FindProperty("isHidden");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(8);
        var header = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        header.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        EditorGUILayout.LabelField($"📜 {quest.questName}", header);

        if (!string.IsNullOrEmpty(quest.questID))
            EditorGUILayout.LabelField($"ID: {quest.questID}", EditorStyles.miniLabel);

        EditorGUILayout.Space(8);

        DrawMainInfo();
        DrawQuestType();
        DrawObjectives();
        DrawRewards();
        DrawConditions();
        DrawDialogues();
        DrawSettings();
        DrawActions();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMainInfo()
    {
        EditorGUILayout.LabelField("ОСНОВНАЯ ИНФОРМАЦИЯ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        IdEditorHelper.DrawIdField(serializedObject, questIDProp, "ID", "quest", "questName");
        EditorGUILayout.PropertyField(questNameProp, new GUIContent("Название"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Описание"));
        EditorGUILayout.PropertyField(iconProp, new GUIContent("Иконка"));
        EditorGUILayout.PropertyField(priorityProp, new GUIContent("Приоритет"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawQuestType()
    {
        EditorGUILayout.LabelField("ТИП КВЕСТА", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(questTypeProp, new GUIContent("Тип"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawObjectives()
    {
        showObjectives = EditorGUILayout.Foldout(showObjectives,
            $"ЦЕЛИ ({quest.objectives.Count})", true);

        if (showObjectives)
        {
            EditorGUILayout.BeginVertical("box");

            if (quest.objectives.Count == 0)
                EditorGUILayout.HelpBox("Нет целей. Квест не сможет завершиться сам.", MessageType.Warning);

            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var obj = quest.objectives[i];
                if (obj == null) continue;

                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Цель {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Удалить", GUILayout.Width(70)))
                {
                    quest.objectives.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawObjectiveFields(obj, i);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Добавить цель"))
            {
                quest.objectives.Add(new QuestObjective());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("Очистить"))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все цели?", "Да", "Нет"))
                {
                    quest.objectives.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawObjectiveFields(QuestObjective obj, int index)
    {
        var objProp = objectivesProp.GetArrayElementAtIndex(index);

        EditorGUILayout.PropertyField(objProp.FindPropertyRelative("description"), new GUIContent("Описание"));

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Тип", GUILayout.Width(120));
        obj.type = (QuestObjectiveType)EditorGUILayout.EnumPopup(obj.type);
        EditorGUILayout.EndHorizontal();

        IdEditorHelper.DrawInline(
            "ID",
            obj.objectiveID,
            v => { obj.objectiveID = v; },
            "obj",
            new[] { obj.description, obj.type.ToString() },
            index);

        obj.requiredCount = EditorGUILayout.IntField("Требуется", obj.requiredCount);
        obj.currentCount = EditorGUILayout.IntField("Текущий прогресс", obj.currentCount);
        obj.isOptional = EditorGUILayout.Toggle("Опционально", obj.isOptional);

        switch (obj.type)
        {
            case QuestObjectiveType.Collect:
            case QuestObjectiveType.UseItem:
                DrawItemSOWithMigration(objProp, "Предмет");
                break;
            case QuestObjectiveType.TalkTo:
                obj.targetNPC = EditorGUILayout.TextField("NPC", obj.targetNPC);
                break;
            case QuestObjectiveType.GoTo:
                LocationEditorHelper.DrawLocationDropdown(
                    "Локация",
                    obj.targetLocation,
                    v =>
                    {
                        obj.targetLocation = v;
                        EditorUtility.SetDirty(quest);
                    },
                    120f);
                break;
        }
    }

    private void DrawRewards()
    {
        showRewards = EditorGUILayout.Foldout(showRewards,
            $"НАГРАДЫ ({quest.rewards.Count})", true);

        if (showRewards)
        {
            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < quest.rewards.Count; i++)
            {
                var reward = quest.rewards[i];
                if (reward == null) continue;

                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Награда {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Удалить", GUILayout.Width(70)))
                {
                    quest.rewards.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawRewardFields(reward, i);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Добавить награду"))
            {
                quest.rewards.Add(new QuestReward());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("Очистить"))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все награды?", "Да", "Нет"))
                {
                    quest.rewards.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawRewardFields(QuestReward reward, int index)
    {
        var rewardProp = rewardsProp.GetArrayElementAtIndex(index);

        DrawItemSOWithMigration(rewardProp, "Предмет");

        reward.amount = EditorGUILayout.IntField("Количество", reward.amount);
        reward.experience = EditorGUILayout.IntField("Опыт", reward.experience);
        reward.flagToSet = EditorGUILayout.TextField("Флаг", reward.flagToSet);
    }

    private void DrawConditions()
    {
        showConditions = EditorGUILayout.Foldout(showConditions,
            $"УСЛОВИЯ СТАРТА ({quest.startConditions.Count})", true);

        if (showConditions)
        {
            EditorGUILayout.BeginVertical("box");

            for (int i = 0; i < quest.startConditions.Count; i++)
            {
                var condition = quest.startConditions[i];
                if (condition == null) continue;

                EditorGUILayout.BeginVertical("helpbox");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Условие {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("Удалить", GUILayout.Width(70)))
                {
                    quest.startConditions.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    i--;
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                condition.flagName = EditorGUILayout.TextField("Флаг", condition.flagName);
                condition.flagValue = EditorGUILayout.Toggle("Значение флага", condition.flagValue);
                condition.questID = EditorGUILayout.TextField("ID квеста", condition.questID);
                condition.questCompleted = EditorGUILayout.Toggle("Завершён", condition.questCompleted);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Добавить условие"))
            {
                quest.startConditions.Add(new QuestCondition());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("Очистить"))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все условия?", "Да", "Нет"))
                {
                    quest.startConditions.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawDialogues()
    {
        showDialogues = EditorGUILayout.Foldout(showDialogues, "ДИАЛОГИ", true);

        if (showDialogues)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(startDialogueIDProp, new GUIContent("Диалог старта"));
            EditorGUILayout.PropertyField(completeDialogueIDProp, new GUIContent("Диалог завершения"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawSettings()
    {
        showSettings = EditorGUILayout.Foldout(showSettings, "НАСТРОЙКИ", true);

        if (showSettings)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(autoStartProp, new GUIContent("Автостарт"));
            EditorGUILayout.PropertyField(repeatableProp, new GUIContent("Повторяемый"));
            EditorGUILayout.PropertyField(isHiddenProp, new GUIContent("Скрытый"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Обновить"))
        {
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Копировать ID"))
            EditorGUIUtility.systemCopyBuffer = quest.questID;
        if (GUILayout.Button("Статистика"))
            ShowStats();
        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemSOWithMigration(SerializedProperty parentProp, string label)
    {
        var itemSOProp = parentProp.FindPropertyRelative("itemSO")
                      ?? parentProp.FindPropertyRelative("targetItemSO");
        var itemLegacyProp = parentProp.FindPropertyRelative("item")
                          ?? parentProp.FindPropertyRelative("targetItem");

        if (itemSOProp == null) return;

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(label, GUILayout.Width(120));
        EditorGUILayout.PropertyField(itemSOProp, GUIContent.none);
        EditorGUILayout.EndHorizontal();

        if (itemSOProp.objectReferenceValue == null && itemLegacyProp != null)
        {
#pragma warning disable CS0618
            var legacyVal = (ItemType)itemLegacyProp.enumValueIndex;
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
                        EditorUtility.SetDirty(quest);
                    }
                }
            }
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

    private void ShowStats()
    {
        string msg =
            $"Квест: {quest.questName}\n" +
            $"ID: {quest.questID}\n" +
            $"Тип: {quest.questType}\n" +
            $"Приоритет: {quest.priority}\n\n" +
            $"Целей: {quest.objectives.Count}\n" +
            $"Наград: {quest.rewards.Count}\n" +
            $"Условий: {quest.startConditions.Count}";
        EditorUtility.DisplayDialog("Статистика", msg, "OK");
    }
}
#endif