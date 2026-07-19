// GameEventEditor.cs - С ПОДДЕРЖКОЙ ВЛОЖЕННЫХ ДЕЙСТВИЙ И КНОПКАМИ УПРАВЛЕНИЯ
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(GameEvent))]
public class GameEventEditor : Editor
{
    private GameEvent gameEvent;
    private SerializedProperty actionsProperty;
    private SerializedProperty requirementsProperty;
    private SerializedProperty triggerConditionProperty;
    private SerializedProperty triggerTypeProperty;

    private int selectedActionTypeIndex = 0;
    private int selectedRequirementTypeIndex = 0;
    private int selectedTriggerConditionTypeIndex = 0;

    private string[] actionTypeNames;
    private Type[] actionTypes;

    private string[] requirementTypeNames;
    private Type[] requirementTypes;

    private string[] triggerConditionTypeNames;
    private Type[] triggerConditionTypes;

    // РУСИФИЦИРОВАННЫЕ НАЗВАНИЯ ТИПОВ
    private Dictionary<string, string> actionDisplayNames = new Dictionary<string, string>
    {
        { "EventActionStartDialogue", "Начать диалог" },
        { "EventActionAddItem", "Добавить предмет" },
        { "EventActionRemoveItem", "Удалить предмет" },
        { "EventActionSetFlag", "Установить флаг" },
        { "EventActionTeleport", "Телепорт" },
        { "EventActionMultiple", "Группа действий" },
        { "EventActionPlaySound", "Воспроизвести звук" },
        { "EventActionSpawnObject", "Создать объект" },
        { "EventActionDestroyObject", "Удалить объект" },
        { "EventActionEnableObject", "Включить объект" },
        { "EventActionDisableObject", "Выключить объект" },
        { "EventActionEnableEvent", "Включить событие" },
        { "EventActionDisableEvent", "Выключить событие" },
        { "EventActionCheckEvent", "Проверить событие" },
        { "EventActionResetEvent", "Сбросить событие" },
        { "EventActionSaveState", "Сохранить состояние" },
    };

    private Dictionary<string, string> requirementDisplayNames = new Dictionary<string, string>
    {
        { "RequirementHasItem", "Есть предмет" },
        { "RequirementFlag", "Флаг" },
        { "RequirementLocation", "Локация" },
        { "RequirementEventExecuted", "Событие выполнено" },
        { "RequirementEventExecutionCount", "Количество выполнений" },
        { "RequirementRandomChance", "Случайный шанс" },
        { "RequirementCustomCondition", "Пользовательское условие" },
    };

    private Dictionary<string, string> triggerConditionDisplayNames = new Dictionary<string, string>
    {
        { "TriggerConditionItem", "Предмет" },
        { "TriggerConditionLocation", "Локация" },
        { "TriggerConditionFlag", "Флаг" },
        { "TriggerConditionGlobalFlag", "Глобальный флаг" },
        { "TriggerConditionCustom", "Пользовательское условие" },
    };

    private Dictionary<EventTriggerType, string> triggerTypeDisplayNames = new Dictionary<EventTriggerType, string>
    {
        { EventTriggerType.None, "Нет" },
        { EventTriggerType.PickupItem, "Подбор предмета" },
        { EventTriggerType.EnterLocation, "Вход в локацию" },
        { EventTriggerType.EquipItem, "Экипировка предмета" },
        { EventTriggerType.UseItem, "Использование предмета" },
        { EventTriggerType.DialogueEnd, "Окончание диалога" },
        { EventTriggerType.TimeReached, "Достижение времени" },
        { EventTriggerType.GlobalFlag, "Глобальный флаг" },
        { EventTriggerType.InventoryHas, "Есть предмет в инвентаре" },
        { EventTriggerType.InventoryCount, "Количество предметов" },
        { EventTriggerType.PlayerAction, "Действие игрока" },
        { EventTriggerType.Custom, "Пользовательский" },
    };

    private void OnEnable()
    {
        gameEvent = (GameEvent)target;
        actionsProperty = serializedObject.FindProperty("actions");
        requirementsProperty = serializedObject.FindProperty("requirements");
        triggerConditionProperty = serializedObject.FindProperty("triggerCondition");
        triggerTypeProperty = serializedObject.FindProperty("triggerType");

        // Находим все типы, наследующие EventAction
        var actionTypeList = new List<Type>();
        var actionNameList = new List<string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && typeof(EventAction).IsAssignableFrom(type))
                {
                    actionTypeList.Add(type);
                    string displayName = actionDisplayNames.ContainsKey(type.Name) ? actionDisplayNames[type.Name] : type.Name;
                    actionNameList.Add(displayName);
                }
            }
        }

        actionTypes = actionTypeList.ToArray();
        actionTypeNames = actionNameList.ToArray();

        // Находим все типы, наследующие EventRequirement
        var requirementTypeList = new List<Type>();
        var requirementNameList = new List<string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && typeof(EventRequirement).IsAssignableFrom(type))
                {
                    requirementTypeList.Add(type);
                    string displayName = requirementDisplayNames.ContainsKey(type.Name) ? requirementDisplayNames[type.Name] : type.Name;
                    requirementNameList.Add(displayName);
                }
            }
        }

        requirementTypes = requirementTypeList.ToArray();
        requirementTypeNames = requirementNameList.ToArray();

        // Находим все типы, наследующие TriggerCondition
        var triggerConditionTypeList = new List<Type>();
        var triggerConditionNameList = new List<string>();

        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in assembly.GetTypes())
            {
                if (type.IsClass && !type.IsAbstract && typeof(TriggerCondition).IsAssignableFrom(type))
                {
                    triggerConditionTypeList.Add(type);
                    string displayName = triggerConditionDisplayNames.ContainsKey(type.Name) ? triggerConditionDisplayNames[type.Name] : type.Name;
                    triggerConditionNameList.Add(displayName);
                }
            }
        }

        triggerConditionTypes = triggerConditionTypeList.ToArray();
        triggerConditionTypeNames = triggerConditionNameList.ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ============ ЗАГОЛОВОК ============
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label($"📌 {gameEvent.eventName}", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // ============ ИНФОРМАЦИЯ О СОСТОЯНИИ ============
        DrawStateInfo();

        EditorGUILayout.Space(5);

        // ============ КНОПКИ УПРАВЛЕНИЯ ============
        DrawControlButtons();

        EditorGUILayout.Space(10);

        // ============ ОСНОВНАЯ ИНФОРМАЦИЯ ============
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("📋 ОСНОВНАЯ ИНФОРМАЦИЯ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);

        DrawFieldWithTooltip("eventID", "🔑 ID события", "Уникальный идентификатор события. Используется для поиска и сохранения состояния.");
        DrawFieldWithTooltip("eventName", "📛 Название события", "Человеческое название. Отображается в логах.");
        DrawFieldWithTooltip("description", "📝 Описание", "Подробное описание того, что делает событие.");

        EditorGUILayout.Space(10);

        // ============ УСЛОВИЯ СРАБАТЫВАНИЯ ============
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("🎯 УСЛОВИЯ СРАБАТЫВАНИЯ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Здесь настраивается КОГДА и ПРИ КАКИХ УСЛОВИЯХ сработает событие", MessageType.Info);

        EditorGUILayout.Space(5);

        DrawTriggerTypeField();

        EditorGUILayout.Space(5);

        EditorGUILayout.LabelField("🔍 Дополнительное условие:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Уточняет триггер. Например, если тип триггера 'PickupItem', здесь можно указать какой именно предмет.", MessageType.None);

        if (gameEvent.triggerCondition != null)
        {
            EditorGUILayout.BeginVertical("box");

            string typeName = gameEvent.triggerCondition.GetType().Name;
            string displayName = triggerConditionDisplayNames.ContainsKey(typeName) ? triggerConditionDisplayNames[typeName] : typeName;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"📌 Тип: {displayName}", EditorStyles.boldLabel);

            if (GUILayout.Button("✕ Удалить", GUILayout.Width(80)))
            {
                gameEvent.triggerCondition = null;
                EditorUtility.SetDirty(gameEvent);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                goto afterTriggerCondition;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);

            DrawTriggerConditionFields(gameEvent.triggerCondition);

            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.HelpBox("Нет дополнительного условия. Событие будет срабатывать на ЛЮБОЙ триггер выбранного типа.", MessageType.Info);
        }

        EditorGUILayout.BeginHorizontal();
        selectedTriggerConditionTypeIndex = EditorGUILayout.Popup(selectedTriggerConditionTypeIndex, triggerConditionTypeNames);
        if (GUILayout.Button("➕ Создать условие", GUILayout.Width(150)))
        {
            CreateTriggerCondition(selectedTriggerConditionTypeIndex);
        }
        EditorGUILayout.EndHorizontal();

    afterTriggerCondition:
        EditorGUILayout.Space(10);

        // ============ КОНТРОЛЬ ВЫПОЛНЕНИЯ ============
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("🎮 КОНТРОЛЬ ВЫПОЛНЕНИЯ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Настройка СКОЛЬКО РАЗ и ПРИ КАКИХ УСЛОВИЯХ может выполняться событие", MessageType.Info);

        EditorGUILayout.Space(5);

        DrawFieldWithTooltip("executionPolicy", "🔄 Политика выполнения",
            "Как часто может выполняться событие:\n" +
            "• ExecuteOnce - только 1 раз за всю игру\n" +
            "• ExecuteMultiple - несколько раз (ограничено)\n" +
            "• ExecutePerSession - 1 раз за сессию\n" +
            "• ExecuteEveryTime - всегда при срабатывании");

        DrawFieldWithTooltip("maxExecutions", "🔢 Максимум выполнений", "Сколько раз может выполниться событие (для политики ExecuteMultiple)");
        DrawFieldWithTooltip("resetOnSceneLoad", "🔄 Сброс при загрузке", "Сбрасывать ли состояние при загрузке новой сцены");
        DrawFieldWithTooltip("dependsOnEventID", "🔗 Зависит от события", "ID события, без выполнения которого это событие НЕ сработает");
        DrawFieldWithTooltip("mutuallyExclusiveWithEventID", "🚫 Несовместимо с событием", "ID события, с которым это событие НЕ может выполняться вместе");

        EditorGUILayout.Space(10);

        // ============ ДЕЙСТВИЯ ============
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("⚡ ДЕЙСТВИЯ", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Список действий, которые будут выполнены при срабатывании события", MessageType.Info);

        EditorGUILayout.Space(5);

        DrawFieldWithTooltip("executeOnce", "✅ Выполнить один раз", "Устаревшее. Используйте 'Политику выполнения' выше");
        DrawFieldWithTooltip("executeOnAwake", "🚀 Выполнить при загрузке", "Выполнить событие сразу при загрузке сцены (без триггера)");
        DrawFieldWithTooltip("delayBeforeExecute", "⏳ Задержка перед выполнением", "Задержка в секундах перед выполнением действий");

        EditorGUILayout.Space(5);

        // Кнопка добавления действия
        EditorGUILayout.BeginHorizontal();
        selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeNames);
        if (GUILayout.Button("➕ Добавить действие", GUILayout.Width(150)))
        {
            AddAction(selectedActionTypeIndex);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // Отображаем действия (с поддержкой вложенных)
        DrawActionsList(gameEvent.actions, 0);

        EditorGUILayout.Space(10);

        // ============ ТРЕБОВАНИЯ ============
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("✅ ТРЕБОВАНИЯ (ДОПОЛНИТЕЛЬНЫЕ УСЛОВИЯ)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox("Дополнительные условия, которые должны быть выполнены ВСЕ (логическое И)", MessageType.Info);

        EditorGUILayout.Space(5);

        EditorGUILayout.BeginHorizontal();
        selectedRequirementTypeIndex = EditorGUILayout.Popup(selectedRequirementTypeIndex, requirementTypeNames);
        if (GUILayout.Button("➕ Добавить требование", GUILayout.Width(160)))
        {
            AddRequirement(selectedRequirementTypeIndex);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        for (int i = 0; i < gameEvent.requirements.Count; i++)
        {
            var req = gameEvent.requirements[i];
            if (req == null) continue;

            EditorGUILayout.BeginVertical("box");

            string typeName = req.GetType().Name;
            string displayName = requirementDisplayNames.ContainsKey(typeName) ? requirementDisplayNames[typeName] : typeName;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"[{i}] {displayName}", EditorStyles.boldLabel);

            if (GUILayout.Button("✕ Удалить", GUILayout.Width(70)))
            {
                gameEvent.requirements.RemoveAt(i);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Параметры требования:", EditorStyles.miniLabel);

            DrawRequirementFields(req);

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }

        if (gameEvent.requirements.Count == 0)
        {
            EditorGUILayout.HelpBox("Нет требований. Событие будет выполняться без дополнительных условий.", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed)
        {
            EditorUtility.SetDirty(gameEvent);
        }
    }

    // ============ ИНФОРМАЦИЯ О СОСТОЯНИИ ============
    private void DrawStateInfo()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        bool isExecuted = gameEvent.IsExecuted();
        int execCount = EventStateManager.Instance?.GetExecutionCount(gameEvent.eventID) ?? 0;

        EditorGUILayout.LabelField("📊 Состояние события", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Статус:", GUILayout.Width(80));
        GUI.color = isExecuted ? Color.green : Color.gray;
        EditorGUILayout.LabelField(isExecuted ? "✅ ВЫПОЛНЕНО" : "⏳ ОЖИДАНИЕ", EditorStyles.boldLabel);
        GUI.color = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Выполнений:", GUILayout.Width(80));
        EditorGUILayout.LabelField($"{execCount} / {gameEvent.maxExecutions}");
        EditorGUILayout.EndHorizontal();

        if (!string.IsNullOrEmpty(gameEvent.dependsOnEventID))
        {
            bool dependsExecuted = EventStateManager.Instance?.IsExecuted(gameEvent.dependsOnEventID) ?? false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Зависит от:", GUILayout.Width(80));
            GUI.color = dependsExecuted ? Color.green : Color.yellow;
            EditorGUILayout.LabelField($"{gameEvent.dependsOnEventID} → {(dependsExecuted ? "✅" : "⏳")}");
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        if (!string.IsNullOrEmpty(gameEvent.mutuallyExclusiveWithEventID))
        {
            bool exclusiveExecuted = EventStateManager.Instance?.IsExecuted(gameEvent.mutuallyExclusiveWithEventID) ?? false;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("Исключает:", GUILayout.Width(80));
            GUI.color = exclusiveExecuted ? Color.red : Color.gray;
            EditorGUILayout.LabelField($"{gameEvent.mutuallyExclusiveWithEventID} → {(exclusiveExecuted ? "⚠️" : "✅")}");
            GUI.color = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
    }

    // ============ КНОПКИ УПРАВЛЕНИЯ ============
    private void DrawControlButtons()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("🎮 Управление событием", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.BeginHorizontal();

        // Кнопка "Сбросить событие"
        GUI.backgroundColor = new Color(1f, 0.8f, 0f);
        if (GUILayout.Button("🔄 Сбросить событие", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Сброс события",
                $"Вы уверены, что хотите сбросить событие '{gameEvent.eventID}'?\n\nЭто удалит состояние выполнения и позволит запустить его снова.",
                "Сбросить",
                "Отмена"))
            {
                gameEvent.ResetExecution();
                Debug.Log($"🔄 Событие '{gameEvent.eventID}' сброшено через редактор");
                EditorUtility.SetDirty(gameEvent);
            }
        }
        GUI.backgroundColor = Color.white;

        // Кнопка "Принудительно выполнить"
        GUI.backgroundColor = new Color(0.2f, 0.8f, 0.2f);
        if (GUILayout.Button("▶️ Принудительно выполнить", GUILayout.Height(30)))
        {
            if (EditorUtility.DisplayDialog(
                "Принудительное выполнение",
                $"Вы уверены, что хотите принудительно выполнить событие '{gameEvent.eventID}'?\n\nЭто выполнит все действия, даже если условия не выполнены.",
                "Выполнить",
                "Отмена"))
            {
                // ✅ ИСПРАВЛЕНО: используем существующие методы EventContext
                var context = new EventContext()
                    .WithValue("Editor");  // Используем WithValue для строки

                gameEvent.Execute(context);
                Debug.Log($"▶️ Событие '{gameEvent.eventID}' принудительно выполнено через редактор");
                EditorUtility.SetDirty(gameEvent);
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(2);

        // Кнопка "Очистить все события" (опасная)
        GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
        if (GUILayout.Button("⚠️ Сбросить ВСЕ события", GUILayout.Height(25)))
        {
            if (EditorUtility.DisplayDialog(
                "⚠️ СБРОС ВСЕХ СОБЫТИЙ",
                "Вы уверены, что хотите сбросить ВСЕ события в игре?\n\nЭто действие НЕЛЬЗЯ отменить!",
                "Да, сбросить всё",
                "Отмена"))
            {
                EventStateManager.Instance?.ResetAllEvents();
                Debug.Log("⚠️ Все события сброшены через редактор GameEvent");
            }
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.EndVertical();
    }

    // ============ ОСТАЛЬНЫЕ МЕТОДЫ ============

    private void DrawActionsList(List<EventAction> actions, int depth)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action == null) continue;

            string typeName = action.GetType().Name;
            string displayName = actionDisplayNames.ContainsKey(typeName) ? actionDisplayNames[typeName] : typeName;

            EditorGUILayout.BeginVertical("box");

            EditorGUILayout.BeginHorizontal();

            // Отступ для вложенных действий
            if (depth > 0)
            {
                GUILayout.Space(depth * 20);
                EditorGUILayout.LabelField("↳", GUILayout.Width(20));
            }

            EditorGUILayout.LabelField($"[{i}] {displayName}", EditorStyles.boldLabel);

            // Кнопка удаления
            if (GUILayout.Button("✕ Удалить", GUILayout.Width(70)))
            {
                actions.RemoveAt(i);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.ApplyModifiedProperties();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                continue;
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space(3);
            EditorGUILayout.LabelField("Параметры:", EditorStyles.miniLabel);

            // Рисуем поля действия
            DrawActionFields(action, depth + 1);

            // ЕСЛИ ЭТО ГРУППА ДЕЙСТВИЙ - рисуем вложенные действия
            if (action is EventActionMultiple multipleAction)
            {
                EditorGUILayout.Space(5);
                EditorGUILayout.LabelField($"━━━ Вложенные действия ({multipleAction.subActions.Count}) ━━━", EditorStyles.miniBoldLabel);

                // Кнопка добавления вложенного действия
                EditorGUILayout.BeginHorizontal();
                if (depth > 0)
                {
                    GUILayout.Space(depth * 20 + 20);
                }
                selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeNames);
                if (GUILayout.Button("➕ Добавить вложенное действие", GUILayout.Width(180)))
                {
                    var subAction = (EventAction)Activator.CreateInstance(actionTypes[selectedActionTypeIndex]);
                    multipleAction.subActions.Add(subAction);
                    EditorUtility.SetDirty(gameEvent);
                    serializedObject.ApplyModifiedProperties();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(5);

                // Рекурсивно отображаем вложенные действия
                DrawActionsList(multipleAction.subActions, depth + 1);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(5);
        }
    }

    private void DrawActionFields(EventAction action, int depth)
    {
        var fields = action.GetType().GetFields();

        foreach (var field in fields)
        {
            // Пропускаем поле subActions (оно отображается отдельно)
            if (field.Name == "subActions") continue;

            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                continue;

            EditorGUILayout.BeginHorizontal();

            string indent = new string(' ', depth * 4);
            string tooltip = GetFieldTooltip(field.Name);
            var content = new GUIContent(indent + GetRussianFieldName(field.Name), tooltip);

            var value = field.GetValue(action);
            var fieldType = field.FieldType;

            EditorGUILayout.LabelField(content, GUILayout.Width(160));

            // ЕСЛИ ПОЛЕ — ЭТО ВЛОЖЕННОЕ ДЕЙСТВИЕ (actionIfTrue, actionIfFalse)
            if (fieldType == typeof(EventAction))
            {
                var nestedAction = (EventAction)value;

                // Показываем текущее действие или "пусто"
                if (nestedAction != null)
                {
                    string nestedName = actionDisplayNames.ContainsKey(nestedAction.GetType().Name)
                        ? actionDisplayNames[nestedAction.GetType().Name]
                        : nestedAction.GetType().Name;
                    EditorGUILayout.LabelField($"✅ {nestedName}", GUILayout.Width(150));
                }
                else
                {
                    EditorGUILayout.LabelField("(пусто)", GUILayout.Width(150));
                }

                // Кнопка выбора действия
                if (GUILayout.Button("📝 Выбрать", GUILayout.Width(70)))
                {
                    ShowActionSelectionMenu(field, action);
                }

                // Кнопка удаления
                if (nestedAction != null && GUILayout.Button("✕", GUILayout.Width(25)))
                {
                    field.SetValue(action, null);
                    EditorUtility.SetDirty(gameEvent);
                    serializedObject.ApplyModifiedProperties();
                }
            }
            else
            {
                value = DrawFieldValue(fieldType, value);
                field.SetValue(action, value);
            }

            EditorGUILayout.EndHorizontal();

            // ЕСЛИ ЭТО ВЛОЖЕННОЕ ДЕЙСТВИЕ — ПОКАЗЫВАЕМ ЕГО ПАРАМЕТРЫ
            if (fieldType == typeof(EventAction))
            {
                var nestedAction = (EventAction)field.GetValue(action);
                if (nestedAction != null)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("  ─── Параметры вложенного действия ───", EditorStyles.miniLabel);
                    DrawActionFields(nestedAction, depth + 1);
                    EditorGUILayout.EndVertical();
                }
            }
        }
    }

    private void ShowActionSelectionMenu(System.Reflection.FieldInfo field, object parentObject)
    {
        GenericMenu menu = new GenericMenu();

        // Добавляем пункт "Очистить"
        menu.AddItem(new GUIContent("Очистить"), false, () =>
        {
            field.SetValue(parentObject, null);
            EditorUtility.SetDirty(gameEvent);
            serializedObject.ApplyModifiedProperties();
        });

        menu.AddSeparator("");

        // Добавляем все доступные действия (кроме "Группа действий")
        for (int i = 0; i < actionTypes.Length; i++)
        {
            var type = actionTypes[i];

            // Пропускаем "Группа действий" — нельзя вкладывать группы друг в друга
            if (type == typeof(EventActionMultiple)) continue;

            string displayName = actionDisplayNames.ContainsKey(type.Name) ? actionDisplayNames[type.Name] : type.Name;
            var capturedType = type;

            menu.AddItem(new GUIContent(displayName), false, () =>
            {
                var newAction = (EventAction)Activator.CreateInstance(capturedType);
                field.SetValue(parentObject, newAction);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.ApplyModifiedProperties();
            });
        }

        menu.ShowAsContext();
    }

    private void DrawTriggerTypeField()
    {
        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent("⚡ Тип триггера", "Что должно произойти чтобы событие сработало");
        EditorGUILayout.LabelField(content, GUILayout.Width(150));

        EventTriggerType currentValue = (EventTriggerType)triggerTypeProperty.enumValueIndex;

        string[] displayNames = new string[triggerTypeDisplayNames.Count];
        EventTriggerType[] values = new EventTriggerType[triggerTypeDisplayNames.Count];

        int i = 0;
        foreach (var kvp in triggerTypeDisplayNames)
        {
            values[i] = kvp.Key;
            displayNames[i] = kvp.Value;
            i++;
        }

        int currentIndex = Array.IndexOf(values, currentValue);
        if (currentIndex == -1) currentIndex = 0;

        int newIndex = EditorGUILayout.Popup(currentIndex, displayNames);

        if (newIndex != currentIndex)
        {
            triggerTypeProperty.enumValueIndex = (int)values[newIndex];
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawFieldWithTooltip(string propertyName, string displayName, string tooltip)
    {
        var property = serializedObject.FindProperty(propertyName);
        if (property == null) return;

        EditorGUILayout.BeginHorizontal();

        var content = new GUIContent(displayName, tooltip);
        EditorGUILayout.PropertyField(property, content);

        if (GUILayout.Button("❓", GUILayout.Width(25), GUILayout.Height(18)))
        {
            EditorUtility.DisplayDialog("Подсказка", tooltip, "OK");
        }

        EditorGUILayout.EndHorizontal();
    }

    private void DrawTriggerConditionFields(TriggerCondition condition)
    {
        var fields = condition.GetType().GetFields();

        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                continue;

            EditorGUILayout.BeginHorizontal();

            string tooltip = GetFieldTooltip(field.Name);
            var content = new GUIContent("  " + GetRussianFieldName(field.Name), tooltip);

            var value = field.GetValue(condition);
            var fieldType = field.FieldType;

            EditorGUILayout.LabelField(content, GUILayout.Width(150));

            value = DrawFieldValue(fieldType, value);

            field.SetValue(condition, value);
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawRequirementFields(EventRequirement req)
    {
        var fields = req.GetType().GetFields();

        foreach (var field in fields)
        {
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute)))
                continue;

            EditorGUILayout.BeginHorizontal();

            string tooltip = GetFieldTooltip(field.Name);
            var content = new GUIContent("  " + GetRussianFieldName(field.Name), tooltip);

            var value = field.GetValue(req);
            var fieldType = field.FieldType;

            EditorGUILayout.LabelField(content, GUILayout.Width(150));

            value = DrawFieldValue(fieldType, value);

            field.SetValue(req, value);
            EditorGUILayout.EndHorizontal();
        }
    }

    private object DrawFieldValue(Type fieldType, object value)
    {
        if (fieldType == typeof(string))
        {
            return EditorGUILayout.TextField((string)value);
        }
        else if (fieldType == typeof(int))
        {
            return EditorGUILayout.IntField((int)value);
        }
        else if (fieldType == typeof(float))
        {
            return EditorGUILayout.FloatField((float)value);
        }
        else if (fieldType == typeof(bool))
        {
            return EditorGUILayout.Toggle((bool)value);
        }
        else if (fieldType.IsEnum)
        {
            return EditorGUILayout.EnumPopup((Enum)value);
        }
        else if (fieldType == typeof(AudioClip))
        {
            return EditorGUILayout.ObjectField((AudioClip)value, typeof(AudioClip), false);
        }
        else if (fieldType == typeof(GameObject))
        {
            return EditorGUILayout.ObjectField((GameObject)value, typeof(GameObject), true);
        }
        else if (fieldType == typeof(Transform))
        {
            return EditorGUILayout.ObjectField((Transform)value, typeof(Transform), true);
        }
        else if (fieldType == typeof(ItemType))
        {
            return EditorGUILayout.EnumPopup((ItemType)value);
        }
        else if (fieldType == typeof(Vector2))
        {
            Vector2 vec = (Vector2)value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            vec.x = EditorGUILayout.FloatField(vec.x, GUILayout.Width(60));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            vec.y = EditorGUILayout.FloatField(vec.y, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            return vec;
        }
        else if (fieldType == typeof(Vector3))
        {
            Vector3 vec = (Vector3)value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            vec.x = EditorGUILayout.FloatField(vec.x, GUILayout.Width(60));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            vec.y = EditorGUILayout.FloatField(vec.y, GUILayout.Width(60));
            EditorGUILayout.LabelField("Z", GUILayout.Width(15));
            vec.z = EditorGUILayout.FloatField(vec.z, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            return vec;
        }
        else if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
        {
            return EditorGUILayout.ObjectField((UnityEngine.Object)value, fieldType, true);
        }
        else if (fieldType == typeof(EventAction))
        {
            EditorGUILayout.LabelField("(вложенное действие)");
            return value;
        }
        else if (fieldType == typeof(List<EventAction>))
        {
            var list = (List<EventAction>)value;
            EditorGUILayout.LabelField($"({list?.Count ?? 0} действий)");
            return value;
        }
        else
        {
            EditorGUILayout.LabelField(fieldType.Name);
            return value;
        }
    }

    private string GetRussianFieldName(string fieldName)
    {
        var dict = new Dictionary<string, string>
        {
            { "eventID", "ID события" },
            { "eventName", "Название" },
            { "description", "Описание" },
            { "triggerType", "Тип триггера" },
            { "executionPolicy", "Политика выполнения" },
            { "maxExecutions", "Максимум выполнений" },
            { "resetOnSceneLoad", "Сброс при загрузке" },
            { "dependsOnEventID", "Зависит от события" },
            { "mutuallyExclusiveWithEventID", "Несовместимо с" },
            { "executeOnce", "Выполнить один раз" },
            { "executeOnAwake", "При загрузке" },
            { "delayBeforeExecute", "Задержка" },

            { "targetItem", "Целевой предмет" },
            { "requireNotEquipped", "Не экипирован" },
            { "locationName", "Название локации" },
            { "exactMatch", "Точное совпадение" },
            { "flagName", "Имя флага" },
            { "flagValue", "Значение флага" },
            { "customMethodName", "Имя метода" },

            { "actionName", "Название действия" },
            { "delay", "Задержка" },
            { "dialogueFileIndex", "Номер файла диалога" },
            { "dialogueID", "ID диалога" },
            { "itemType", "Тип предмета" },
            { "amount", "Количество" },
            { "soundClip", "Звуковой файл" },
            { "volume", "Громкость" },
            { "prefabToSpawn", "Префаб для создания" },
            { "spawnPosition", "Позиция создания" },
            { "parentTransform", "Родительский объект" },
            { "targetObject", "Целевой объект" },
            { "enable", "Включить" },
            { "destroyDelay", "Задержка удаления" },
            { "subActions", "Вложенные действия" },
            { "customKey", "Ключ" },
            { "value", "Значение" },
            { "requiredState", "Требуемое состояние" },
            { "actionIfTrue", "Действие если TRUE" },
            { "actionIfFalse", "Действие если FALSE" },
            { "targetPosition", "Позиция телепорта" },
            { "usePositionOverride", "Использовать позицию" },

            { "minCount", "Минимальное количество" },
            { "maxCount", "Максимальное количество" },
            { "executed", "Выполнено" },
            { "chance", "Шанс (0-1)" },
            { "useSeed", "Использовать seed" },
            { "seed", "Seed" },
            { "methodName", "Имя метода" },
        };

        return dict.ContainsKey(fieldName) ? dict[fieldName] : fieldName;
    }

    private string GetFieldTooltip(string fieldName)
    {
        var dict = new Dictionary<string, string>
        {
            { "eventID", "Уникальный идентификатор. Используется для сохранения состояния и зависимостей." },
            { "eventName", "Название события для удобства разработчика." },
            { "description", "Описание того, что делает событие." },
            { "triggerType", "Тип события, которое активирует это событие." },
            { "executionPolicy", "Как часто событие может выполняться." },
            { "maxExecutions", "Максимальное количество выполнений (для политики ExecuteMultiple)." },
            { "resetOnSceneLoad", "Сбрасывать состояние при загрузке новой сцены." },
            { "dependsOnEventID", "Событие выполнится ТОЛЬКО после выполнения указанного события." },
            { "mutuallyExclusiveWithEventID", "Событие НЕ выполнится, если указанное событие уже выполнено." },
            { "executeOnce", "Устаревшее поле. Используйте 'Политику выполнения'." },
            { "executeOnAwake", "Выполнить сразу при загрузке сцены (без ожидания триггера)." },
            { "delayBeforeExecute", "Задержка в секундах перед выполнением всех действий." },
            { "targetItem", "Какой предмет должен быть задействован." },
            { "requireNotEquipped", "Требовать, чтобы предмет НЕ был экипирован." },
            { "locationName", "Название локации (должно совпадать с именем объекта)." },
            { "exactMatch", "Точное совпадение названия или частичное." },
            { "flagName", "Имя флага для проверки." },
            { "flagValue", "Какое значение должен иметь флаг." },
            { "customMethodName", "Имя метода для пользовательской проверки." },
            { "actionName", "Название действия для удобства." },
            { "delay", "Задержка перед выполнением этого конкретного действия." },
            { "dialogueFileIndex", "Номер файла диалога в TextBeginner." },
            { "dialogueID", "ID диалога (альтернативный способ)." },
            { "itemType", "Тип предмета (Apple, Potion, Key и т.д.)." },
            { "amount", "Количество предметов для добавления/удаления." },
            { "soundClip", "Аудиофайл для воспроизведения." },
            { "volume", "Громкость звука (0-1)." },
            { "prefabToSpawn", "Префаб для создания в мире." },
            { "spawnPosition", "Позиция для создания объекта." },
            { "parentTransform", "Родительский объект для создаваемого объекта." },
            { "targetObject", "Объект, с которым нужно взаимодействовать." },
            { "enable", "Включить (true) или выключить (false) объект." },
            { "destroyDelay", "Задержка перед удалением объекта." },
            { "subActions", "Список вложенных действий (для группы)." },
            { "customKey", "Уникальный ключ для сохранения." },
            { "value", "Значение для сохранения." },
            { "requiredState", "Какое состояние должно быть у события." },
            { "actionIfTrue", "Действие, если условие истинно." },
            { "actionIfFalse", "Действие, если условие ложно." },
            { "targetPosition", "Координаты для телепортации (если usePositionOverride = true)" },
            { "usePositionOverride", "Использовать указанную позицию вместо LocationReference" },
            { "minCount", "Минимальное количество предметов." },
            { "maxCount", "Максимальное количество предметов." },
            { "executed", "Должно ли быть выполнено событие." },
            { "chance", "Вероятность выполнения (0 = 0%, 1 = 100%)." },
            { "useSeed", "Использовать фиксированный seed для повторяемости." },
            { "seed", "Seed для генерации случайных чисел." },
            { "methodName", "Имя метода для пользовательской проверки." },
        };

        return dict.ContainsKey(fieldName) ? dict[fieldName] : "Нет подсказки для этого поля.";
    }

    private void CreateTriggerCondition(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= triggerConditionTypes.Length) return;

        gameEvent.triggerCondition = (TriggerCondition)Activator.CreateInstance(triggerConditionTypes[typeIndex]);
        EditorUtility.SetDirty(gameEvent);
        serializedObject.ApplyModifiedProperties();
    }

    private void AddAction(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= actionTypes.Length) return;

        var action = (EventAction)Activator.CreateInstance(actionTypes[typeIndex]);
        gameEvent.actions.Add(action);
        EditorUtility.SetDirty(gameEvent);
        serializedObject.ApplyModifiedProperties();
    }

    private void AddRequirement(int typeIndex)
    {
        if (typeIndex < 0 || typeIndex >= requirementTypes.Length) return;

        var req = (EventRequirement)Activator.CreateInstance(requirementTypes[typeIndex]);
        gameEvent.requirements.Add(req);
        EditorUtility.SetDirty(gameEvent);
        serializedObject.ApplyModifiedProperties();
    }
}