// QuestSOEditor.cs - С КНОПКАМИ-ПОДСКАЗКАМИ
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
    private bool showDialogue = true;

    // Словари для перевода
    private Dictionary<QuestType, string> questTypeNames = new Dictionary<QuestType, string>
    {
        { QuestType.Fetch, "Собрать" },
        { QuestType.Kill, "Убить" },
        { QuestType.Talk, "Поговорить" },
        { QuestType.Explore, "Исследовать" },
        { QuestType.Use, "Использовать" },
        { QuestType.Escort, "Сопроводить" },
        { QuestType.Collection, "Коллекция" },
        { QuestType.Story, "Сюжетный" },
    };

    private Dictionary<QuestObjectiveType, string> objectiveTypeNames = new Dictionary<QuestObjectiveType, string>
    {
        { QuestObjectiveType.Collect, "Собрать" },
        { QuestObjectiveType.TalkTo, "Поговорить с" },
        { QuestObjectiveType.GoTo, "Посетить" },
        { QuestObjectiveType.UseItem, "Использовать" },
        { QuestObjectiveType.Kill, "Убить" },
        { QuestObjectiveType.Interact, "Взаимодействовать" },
    };

    private Dictionary<ItemType, string> itemTypeNames = new Dictionary<ItemType, string>
    {
        { ItemType.None, "Нет" },
        { ItemType.Apple, "Яблоко" },
        { ItemType.Key, "Ключ" },
        { ItemType.Potion, "Зелье" },
        { ItemType.Bottle, "Бутылка" },
        { ItemType.Sword, "Меч" },
        { ItemType.Shield, "Щит" },
        { ItemType.GoldCoin, "Золотая монета" },
        { ItemType.Lamp, "Лампа" },
    };

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

        // Стили
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 16,
            alignment = TextAnchor.MiddleCenter,
            padding = new RectOffset(0, 0, 10, 10)
        };
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.4f, 0.7f, 1f) : new Color(0f, 0.3f, 0.8f);

        GUIStyle boxStyle = new GUIStyle("box")
        {
            padding = new RectOffset(10, 10, 10, 10),
            margin = new RectOffset(5, 5, 5, 5)
        };

        GUIStyle sectionStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 13,
            padding = new RectOffset(5, 0, 5, 0)
        };
        sectionStyle.normal.textColor = EditorGUIUtility.isProSkin ? new Color(0.7f, 0.85f, 1f) : new Color(0.1f, 0.3f, 0.6f);

        // ============================================================
        // ЗАГОЛОВОК
        // ============================================================
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("━═━═━═━═━═━═━═━═━═━═━═━═━═━", headerStyle);
        EditorGUILayout.LabelField($"📜 {quest.questName}", headerStyle);
        EditorGUILayout.LabelField("━═━═━═━═━═━═━═━═━═━═━═━═━═━", headerStyle);
        EditorGUILayout.Space(5);

        if (!string.IsNullOrEmpty(quest.questID))
        {
            EditorGUILayout.LabelField($"🔑 ID: {quest.questID}", EditorStyles.miniLabel);
        }
        EditorGUILayout.Space(10);

        // ============================================================
        // ОСНОВНАЯ ИНФОРМАЦИЯ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("📋 ОСНОВНАЯ ИНФОРМАЦИЯ", sectionStyle);
        EditorGUILayout.Space(3);

        DrawFieldWithHelpButton(questIDProp, "🔑 ID квеста", "Уникальный идентификатор. Используется в коде для старта/завершения квеста.");
        DrawFieldWithHelpButton(questNameProp, "📛 Название", "Отображаемое название квеста в интерфейсе.");
        DrawFieldWithHelpButton(descriptionProp, "📝 Описание", "Описание квеста для игрока. Отображается в панели квестов.");
        DrawFieldWithHelpButton(iconProp, "🖼️ Иконка", "Иконка для отображения в списке квестов.");
        DrawFieldWithHelpButton(priorityProp, "⭐ Приоритет", "Чем выше число, тем выше квест в списке (0 - низкий, 10 - высокий).");

        DrawQuestTypeField();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // ЦЕЛИ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        showObjectives = EditorGUILayout.Foldout(showObjectives, $"🎯 ЦЕЛИ ({quest.objectives.Count})", true, sectionStyle);
        if (showObjectives)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Цели, которые нужно выполнить для завершения квеста. Все обязательные цели должны быть выполнены.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            for (int i = 0; i < quest.objectives.Count; i++)
            {
                var obj = quest.objectives[i];
                if (obj == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"📌 Цель {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("✕ Удалить", GUILayout.Width(70)))
                {
                    quest.objectives.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawObjectiveFields(obj);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Добавить цель", GUILayout.Height(25)))
            {
                quest.objectives.Add(new QuestObjective());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("🗑️ Очистить все", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все цели?", "Да", "Нет"))
                {
                    quest.objectives.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // НАГРАДЫ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        showRewards = EditorGUILayout.Foldout(showRewards, $"🎁 НАГРАДЫ ({quest.rewards.Count})", true, sectionStyle);
        if (showRewards)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Награды, которые игрок получит после завершения квеста.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            for (int i = 0; i < quest.rewards.Count; i++)
            {
                var reward = quest.rewards[i];
                if (reward == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🎁 Награда {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("✕ Удалить", GUILayout.Width(70)))
                {
                    quest.rewards.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawRewardFields(reward);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Добавить награду", GUILayout.Height(25)))
            {
                quest.rewards.Add(new QuestReward());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("🗑️ Очистить все", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все награды?", "Да", "Нет"))
                {
                    quest.rewards.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // УСЛОВИЯ НАЧАЛА
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        showConditions = EditorGUILayout.Foldout(showConditions, $"🔒 УСЛОВИЯ НАЧАЛА ({quest.startConditions.Count})", true, sectionStyle);
        if (showConditions)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Условия, которые должны быть выполнены для старта квеста. Все условия должны быть выполнены.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            for (int i = 0; i < quest.startConditions.Count; i++)
            {
                var condition = quest.startConditions[i];
                if (condition == null) continue;

                EditorGUILayout.BeginVertical("box");
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"🔐 Условие {i + 1}", EditorStyles.boldLabel);
                if (GUILayout.Button("✕ Удалить", GUILayout.Width(70)))
                {
                    quest.startConditions.RemoveAt(i);
                    EditorUtility.SetDirty(quest);
                    EditorGUILayout.EndHorizontal();
                    EditorGUILayout.EndVertical();
                    continue;
                }
                EditorGUILayout.EndHorizontal();

                DrawConditionFields(condition);

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(3);
            }

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕ Добавить условие", GUILayout.Height(25)))
            {
                quest.startConditions.Add(new QuestCondition());
                EditorUtility.SetDirty(quest);
            }
            if (GUILayout.Button("🗑️ Очистить все", GUILayout.Height(25)))
            {
                if (EditorUtility.DisplayDialog("Подтверждение", "Удалить все условия?", "Да", "Нет"))
                {
                    quest.startConditions.Clear();
                    EditorUtility.SetDirty(quest);
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // ДИАЛОГИ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        showDialogue = EditorGUILayout.Foldout(showDialogue, "💬 ДИАЛОГИ", true, sectionStyle);
        if (showDialogue)
        {
            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Диалоги, которые запускаются при старте и завершении квеста. ID должен совпадать с ID в диалоговой системе.", EditorStyles.miniLabel);
            EditorGUILayout.Space(5);

            DrawFieldWithHelpButton(startDialogueIDProp, "🎬 Диалог старта", "ID диалога, который запускается при старте квеста. Например: 'trixie_quest_start'.");
            DrawFieldWithHelpButton(completeDialogueIDProp, "🏁 Диалог завершения", "ID диалога, который запускается после завершения квеста. Например: 'trixie_quest_complete'.");
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // НАСТРОЙКИ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("⚙️ НАСТРОЙКИ", sectionStyle);
        EditorGUILayout.Space(3);

        DrawFieldWithHelpButton(autoStartProp, "▶️ Автостарт", "Квест начнётся автоматически при загрузке сцены. Не требует ручного запуска.");
        DrawFieldWithHelpButton(repeatableProp, "🔄 Повторяемый", "Можно ли проходить квест несколько раз. Если нет, квест исчезнет после завершения.");
        DrawFieldWithHelpButton(isHiddenProp, "👁️ Скрытый", "Квест НЕ отображается в интерфейсе игрока. Полезно для скрытых сюжетных квестов.");

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(10);

        // ============================================================
        // КНОПКИ
        // ============================================================
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Обновить", GUILayout.Height(30)))
        {
            EditorUtility.SetDirty(quest);
            AssetDatabase.SaveAssets();
            Debug.Log($"✅ Квест обновлён: {quest.questName}");
        }
        if (GUILayout.Button("📋 Копировать ID", GUILayout.Height(30)))
        {
            EditorGUIUtility.systemCopyBuffer = quest.questID;
            Debug.Log($"📋 ID скопирован: {quest.questID}");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🧹 Сбросить настройки", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog("Подтверждение", "Сбросить все настройки квеста?", "Да", "Нет"))
            {
                ResetQuest();
            }
        }
        if (GUILayout.Button("📊 Статистика", GUILayout.Height(25)))
        {
            ShowStats();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(10);

        // ============================================================
        // ПОДСКАЗКИ
        // ============================================================
        EditorGUILayout.BeginVertical(boxStyle);
        EditorGUILayout.LabelField("💡 СОВЕТЫ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("• Используйте уникальные ID для каждого квеста", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Для целей с типом 'Собрать' укажите Target Item", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Награды выдаются автоматически при завершении", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Квесты с Автостарт = true запускаются сразу", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Условия начала проверяются при старте квеста", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(10);

        serializedObject.ApplyModifiedProperties();
    }

    // ============================================================
    // ОТРИСОВКА ПОЛЕЙ С КНОПКОЙ-ПОДСКАЗКОЙ
    // ============================================================

    private void DrawFieldWithHelpButton(SerializedProperty prop, string displayName, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent(displayName);
        EditorGUILayout.PropertyField(prop, content);

        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorUtility.DisplayDialog($"ℹ️ {displayName}", tooltip, "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawQuestTypeField()
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent("🎯 Тип квеста");
        EditorGUILayout.LabelField(content, GUILayout.Width(150));

        QuestType currentValue = (QuestType)questTypeProp.enumValueIndex;

        string[] displayNames = new string[questTypeNames.Count];
        QuestType[] values = new QuestType[questTypeNames.Count];

        int i = 0;
        foreach (var kvp in questTypeNames)
        {
            values[i] = kvp.Key;
            displayNames[i] = kvp.Value;
            i++;
        }

        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex == -1) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);

        if (newIndex != currentIndex)
        {
            questTypeProp.enumValueIndex = (int)values[newIndex];
        }

        // Кнопка-подсказка
        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            string message = "🎯 ТИП КВЕСТА\n\n" +
                            "• Собрать - собрать предметы\n" +
                            "• Убить - победить врагов\n" +
                            "• Поговорить - поговорить с NPC\n" +
                            "• Исследовать - посетить локацию\n" +
                            "• Использовать - использовать предмет\n" +
                            "• Сопроводить - сопроводить NPC\n" +
                            "• Коллекция - собрать коллекцию\n" +
                            "• Сюжетный - сюжетный квест";
            EditorUtility.DisplayDialog("ℹ️ Тип квеста", message, "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawObjectiveFields(QuestObjective obj)
    {
        obj.objectiveID = EditorGUILayout.TextField("🔑 ID цели", obj.objectiveID);
        obj.description = EditorGUILayout.TextField("📝 Описание", obj.description);

        DrawObjectiveTypeField(obj);

        obj.requiredCount = EditorGUILayout.IntField("📊 Требуется", obj.requiredCount);
        obj.currentCount = EditorGUILayout.IntField("📊 Текущий прогресс", obj.currentCount);

        // Опционально с кнопкой-подсказкой
        EditorGUILayout.BeginHorizontal();
        obj.isOptional = EditorGUILayout.Toggle("🔄 Опционально", obj.isOptional);
        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorUtility.DisplayDialog("ℹ️ Опционально",
                "Если отмечено, эту цель НЕ обязательно выполнять для завершения квеста.\n\n" +
                "Полезно для дополнительных заданий или бонусных целей.", "OK");
        }
        EditorGUILayout.EndHorizontal();

        if (obj.type == QuestObjectiveType.Collect)
        {
            DrawItemTypeField(obj, "📦 Предмет", "Предмет, который нужно собрать.");
        }
        else if (obj.type == QuestObjectiveType.TalkTo)
        {
            obj.targetNPC = EditorGUILayout.TextField("👤 NPC", obj.targetNPC);
        }
        else if (obj.type == QuestObjectiveType.GoTo)
        {
            obj.targetLocation = EditorGUILayout.TextField("📍 Локация", obj.targetLocation);
        }
        else if (obj.type == QuestObjectiveType.UseItem)
        {
            DrawItemTypeField(obj, "📦 Предмет", "Предмет, который нужно использовать.");
        }
    }

    private void DrawObjectiveTypeField(QuestObjective obj)
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent("🎯 Тип");
        EditorGUILayout.LabelField(content, GUILayout.Width(150));

        QuestObjectiveType currentValue = obj.type;

        string[] displayNames = new string[objectiveTypeNames.Count];
        QuestObjectiveType[] values = new QuestObjectiveType[objectiveTypeNames.Count];

        int i = 0;
        foreach (var kvp in objectiveTypeNames)
        {
            values[i] = kvp.Key;
            displayNames[i] = kvp.Value;
            i++;
        }

        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex == -1) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);

        if (newIndex != currentIndex)
        {
            obj.type = values[newIndex];
        }

        // Кнопка-подсказка
        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            string message = "🎯 ТИП ЦЕЛИ\n\n" +
                            "• Собрать - собрать указанное количество предметов\n" +
                            "• Поговорить с - поговорить с NPC\n" +
                            "• Посетить - посетить локацию\n" +
                            "• Использовать - использовать предмет\n" +
                            "• Убить - победить врага\n" +
                            "• Взаимодействовать - взаимодействовать с объектом";
            EditorUtility.DisplayDialog("ℹ️ Тип цели", message, "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawItemTypeField(QuestObjective obj, string label, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent(label);
        EditorGUILayout.LabelField(content, GUILayout.Width(150));

        ItemType currentValue = obj.targetItem;

        string[] displayNames = new string[itemTypeNames.Count];
        ItemType[] values = new ItemType[itemTypeNames.Count];

        int i = 0;
        foreach (var kvp in itemTypeNames)
        {
            values[i] = kvp.Key;
            displayNames[i] = kvp.Value;
            i++;
        }

        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex == -1) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);

        if (newIndex != currentIndex)
        {
            obj.targetItem = values[newIndex];
        }

        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorUtility.DisplayDialog($"ℹ️ {label}", tooltip, "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawRewardFields(QuestReward reward)
    {
        DrawItemTypeField(reward);
        reward.amount = EditorGUILayout.IntField("📊 Количество", reward.amount);
        reward.experience = EditorGUILayout.IntField("⭐ Опыт", reward.experience);
        reward.flagToSet = EditorGUILayout.TextField("🚩 Флаг", reward.flagToSet);
    }

    private void DrawItemTypeField(QuestReward reward)
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent("📦 Предмет");
        EditorGUILayout.LabelField(content, GUILayout.Width(150));

        ItemType currentValue = reward.item;

        string[] displayNames = new string[itemTypeNames.Count];
        ItemType[] values = new ItemType[itemTypeNames.Count];

        int i = 0;
        foreach (var kvp in itemTypeNames)
        {
            values[i] = kvp.Key;
            displayNames[i] = kvp.Value;
            i++;
        }

        int currentIndex = System.Array.IndexOf(values, currentValue);
        if (currentIndex == -1) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);

        if (newIndex != currentIndex)
        {
            reward.item = values[newIndex];
        }

        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorUtility.DisplayDialog("ℹ️ Предмет",
                "Предмет, который игрок получит в качестве награды.\n\n" +
                "Предмет будет добавлен в инвентарь автоматически.", "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawConditionFields(QuestCondition condition)
    {
        condition.flagName = EditorGUILayout.TextField("🚩 Имя флага", condition.flagName);
        condition.flagValue = EditorGUILayout.Toggle("✅ Значение", condition.flagValue);
        condition.questID = EditorGUILayout.TextField("🔑 ID квеста", condition.questID);
        condition.questCompleted = EditorGUILayout.Toggle("🏁 Завершён", condition.questCompleted);
    }

    // ============================================================
    // ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ
    // ============================================================

    private void ResetQuest()
    {
        quest.questID = "";
        quest.questName = "Новый квест";
        quest.description = "";
        quest.icon = null;
        quest.priority = 0;
        quest.questType = QuestType.Fetch;
        quest.objectives.Clear();
        quest.rewards.Clear();
        quest.startConditions.Clear();
        quest.startDialogueID = "";
        quest.completeDialogueID = "";
        quest.autoStart = false;
        quest.repeatable = false;
        quest.isHidden = false;

        EditorUtility.SetDirty(quest);
        Debug.Log($"🧹 Квест сброшен: {quest.questName}");
    }

    private void ShowStats()
    {
        int totalObjectives = quest.objectives.Count;
        int totalRewards = quest.rewards.Count;
        int totalConditions = quest.startConditions.Count;

        string message = $"📊 СТАТИСТИКА КВЕСТА\n\n" +
                         $"📛 Название: {quest.questName}\n" +
                         $"🔑 ID: {quest.questID}\n" +
                         $"🎯 Тип: {GetQuestTypeName(quest.questType)}\n" +
                         $"⭐ Приоритет: {quest.priority}\n\n" +
                         $"🎯 Целей: {totalObjectives}\n" +
                         $"🎁 Наград: {totalRewards}\n" +
                         $"🔒 Условий: {totalConditions}\n" +
                         $"▶️ Автостарт: {(quest.autoStart ? "Да" : "Нет")}\n" +
                         $"🔄 Повторяемый: {(quest.repeatable ? "Да" : "Нет")}";

        EditorUtility.DisplayDialog("📊 Статистика квеста", message, "OK");
    }

    private string GetQuestTypeName(QuestType type)
    {
        return questTypeNames.ContainsKey(type) ? questTypeNames[type] : type.ToString();
    }
}
#endif