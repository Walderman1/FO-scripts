#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System;
using System.Collections.Generic;
using System.Linq;

[CustomEditor(typeof(GameEvent))]
public class GameEventEditor : Editor
{
    private GameEvent gameEvent;
    private SerializedProperty triggerTypeProperty;
    private EventStateManager _eventState;

    private int selectedActionTypeIndex = 0;
    private int selectedRequirementTypeIndex = 0;
    private int selectedTriggerConditionTypeIndex = 0;

    private string[] actionTypeNames;
    private Type[] actionTypes;
    private string[] requirementTypeNames;
    private Type[] requirementTypes;
    private string[] triggerConditionTypeNames;
    private Type[] triggerConditionTypes;

    private bool showMain = true;
    private bool showTrigger = true;
    private bool showExecution = false;
    private bool showActions = true;
    private bool showRequirements = true;

    private static readonly EventTriggerType[] triggerValues =
    {
        EventTriggerType.None,
        EventTriggerType.PickupItem,
        EventTriggerType.EnterLocation,
        EventTriggerType.EquipItem,
        EventTriggerType.UseItem,
        EventTriggerType.DialogueEnd,
        EventTriggerType.TimeReached,
        EventTriggerType.GlobalFlag,
        EventTriggerType.InventoryHas,
        EventTriggerType.InventoryCount,
        EventTriggerType.PlayerAction,
        EventTriggerType.Custom,
    };

    private static readonly string[] triggerDisplayNames =
    {
        "Нет",
        "Подбор предмета",
        "Вход в локацию",
        "Экипировка предмета",
        "Использование предмета",
        "Окончание диалога",
        "Достижение времени",
        "Глобальный флаг",
        "Есть предмет в инвентаре",
        "Количество предметов",
        "Действие игрока",
        "Пользовательский",
    };

    private Dictionary<string, string> actionDisplayNames = new Dictionary<string, string>
    {
        { "EventActionStartDialogue", "Начать диалог" },
        { "EventActionStartQuest", "Начать квест" },
        { "EventActionCompleteQuest", "Завершить квест" },
        { "EventActionAddItem", "Добавить предмет (устар.)" },
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
        { "TriggerConditionCustom", "Пользовательское условие" },
    };

    private Dictionary<string, string> fieldNamesRu = new Dictionary<string, string>
    {
        { "eventID", "ID события" }, { "eventName", "Название" }, { "description", "Описание" },
        { "triggerType", "Тип триггера" }, { "executionPolicy", "Политика" },
        { "maxExecutions", "Макс. выполнений" }, { "resetOnSceneLoad", "Сброс при загрузке" },
        { "dependsOnEventID", "Зависит от" }, { "mutuallyExclusiveWithEventID", "Несовместимо с" },
        { "executeOnce", "Выполнить один раз" }, { "executeOnAwake", "При загрузке" },
        { "delayBeforeExecute", "Задержка" },
        { "targetItem", "Предмет (legacy)" }, { "targetItemSO", "Предмет" },
        { "targetItemSOId", "ID предмета" }, { "requireNotEquipped", "Не экипирован" },
        { "locationName", "Локация" }, { "exactMatch", "Точное совпадение" },
        { "flagName", "Флаг" }, { "flagValue", "Значение флага" }, { "customMethodName", "Метод" },
        { "actionName", "Название" }, { "delay", "Задержка" },
        { "dialogueFileName", "Имя файла диалога" }, { "dialogueID", "ID диалога" },
        { "itemType", "Тип (legacy)" }, { "itemSO", "Предмет" }, { "itemSOId", "ID предмета" },
        { "amount", "Количество" }, { "soundClip", "Звук" }, { "volume", "Громкость" },
        { "prefabToSpawn", "Префаб" }, { "spawnPosition", "Позиция" }, { "parentTransform", "Родитель" },
        { "targetObject", "Целевой объект" }, { "enable", "Включить" },
        { "destroyDelay", "Задержка удаления" }, { "subActions", "Вложенные" },
        { "customKey", "Ключ" }, { "value", "Значение" },
        { "requiredState", "Требуемое состояние" }, { "actionIfTrue", "Если TRUE" },
        { "actionIfFalse", "Если FALSE" }, { "targetPosition", "Позиция" },
        { "usePositionOverride", "Использовать позицию" },
        { "minCount", "Мин. количество" }, { "maxCount", "Макс. количество" },
        { "executed", "Выполнено" }, { "chance", "Шанс (0-1)" },
        { "useSeed", "Использовать seed" }, { "seed", "Seed" }, { "methodName", "Метод" },
    };

    private void OnEnable()
    {
        gameEvent = (GameEvent)target;
        triggerTypeProperty = serializedObject.FindProperty("triggerType");
        _eventState = UnityEngine.Object.FindAnyObjectByType<EventStateManager>(FindObjectsInactive.Include);

        BuildActionTypes();
        BuildRequirementTypes();
        BuildTriggerConditionTypes();
    }

    private void BuildActionTypes()
    {
        var typeList = new List<Type>();
        var nameList = new List<string>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<EventAction>())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            typeList.Add(type);
            nameList.Add(actionDisplayNames.ContainsKey(type.Name) ? actionDisplayNames[type.Name] : type.Name);
        }
        actionTypes = typeList.ToArray();
        actionTypeNames = nameList.ToArray();
    }

    private void BuildRequirementTypes()
    {
        var typeList = new List<Type>();
        var nameList = new List<string>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<EventRequirement>())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            typeList.Add(type);
            nameList.Add(requirementDisplayNames.ContainsKey(type.Name) ? requirementDisplayNames[type.Name] : type.Name);
        }
        requirementTypes = typeList.ToArray();
        requirementTypeNames = nameList.ToArray();
    }

    private void BuildTriggerConditionTypes()
    {
        var typeList = new List<Type>();
        var nameList = new List<string>();
        foreach (var type in TypeCache.GetTypesDerivedFrom<TriggerCondition>())
        {
            if (!type.IsClass || type.IsAbstract) continue;
            typeList.Add(type);
            nameList.Add(triggerConditionDisplayNames.ContainsKey(type.Name) ? triggerConditionDisplayNames[type.Name] : type.Name);
        }
        triggerConditionTypes = typeList.ToArray();
        triggerConditionTypeNames = nameList.ToArray();
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(6);
        var style = new GUIStyle(EditorStyles.boldLabel) { fontSize = 13, alignment = TextAnchor.MiddleCenter };
        style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;
        EditorGUILayout.LabelField($"Событие: {gameEvent.eventName}", style);

        bool isExecuted = gameEvent.IsExecuted();
        int execCount = _eventState != null ? _eventState.GetExecutionCount(gameEvent.eventID) : 0;
        EditorGUILayout.LabelField($"Статус: {(isExecuted ? "ВЫПОЛНЕНО" : "ОЖИДАНИЕ")}  |  Выполнений: {execCount}/{gameEvent.maxExecutions}");

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Сбросить")) gameEvent.ResetExecution();
        if (GUILayout.Button("Выполнить")) gameEvent.Execute(new EventContext().WithValue("Editor"));
        if (GUILayout.Button("Сбросить ВСЕ")) _eventState?.ResetAllEvents();
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(6);

        DrawMainInfo();
        DrawTriggerSection();
        DrawExecutionSection();
        DrawActionsSection();
        DrawRequirementsSection();

        serializedObject.ApplyModifiedProperties();
        if (GUI.changed) EditorUtility.SetDirty(gameEvent);
    }

    private void DrawMainInfo()
    {
        showMain = EditorGUILayout.Foldout(showMain, "ОСНОВНАЯ ИНФОРМАЦИЯ", true);
        if (!showMain) return;

        EditorGUILayout.BeginVertical("box");

        var idProp = serializedObject.FindProperty("eventID");
        IdEditorHelper.DrawIdField(serializedObject, idProp, "ID события", "event", "eventName");

        DrawField("eventName", "Название");
        DrawField("description", "Описание");
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawTriggerSection()
    {
        showTrigger = EditorGUILayout.Foldout(showTrigger, "УСЛОВИЯ СРАБАТЫВАНИЯ", true);
        if (!showTrigger) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Тип триггера", GUILayout.Width(150));

        EventTriggerType current = (EventTriggerType)triggerTypeProperty.enumValueIndex;
        int curIdx = Array.IndexOf(triggerValues, current);
        if (curIdx == -1) curIdx = 0;
        int newIdx = EditorGUILayout.Popup(curIdx, triggerDisplayNames);
        if (newIdx != curIdx) triggerTypeProperty.enumValueIndex = (int)triggerValues[newIdx];
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Дополнительное условие:", EditorStyles.boldLabel);

        if (gameEvent.triggerCondition != null)
        {
            EditorGUILayout.BeginVertical("helpbox");

            string typeName = gameEvent.triggerCondition.GetType().Name;
            string displayName = triggerConditionDisplayNames.ContainsKey(typeName)
                ? triggerConditionDisplayNames[typeName] : typeName;

            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField(displayName, EditorStyles.boldLabel);
            if (GUILayout.Button("Удалить", GUILayout.Width(70)))
            {
                serializedObject.ApplyModifiedProperties();
                gameEvent.triggerCondition = null;
                EditorUtility.SetDirty(gameEvent);
                serializedObject.Update();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4);
                return;
            }
            EditorGUILayout.EndHorizontal();

            DrawTriggerConditionFields(gameEvent.triggerCondition);
            EditorGUILayout.EndVertical();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            selectedTriggerConditionTypeIndex = EditorGUILayout.Popup(selectedTriggerConditionTypeIndex, triggerConditionTypeNames);
            if (GUILayout.Button("Создать", GUILayout.Width(100)))
                CreateTriggerCondition(selectedTriggerConditionTypeIndex);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawExecutionSection()
    {
        showExecution = EditorGUILayout.Foldout(showExecution, "КОНТРОЛЬ ВЫПОЛНЕНИЯ", true);
        if (!showExecution) return;

        EditorGUILayout.BeginVertical("box");
        DrawField("executionPolicy", "Политика выполнения");
        DrawField("maxExecutions", "Макс. выполнений");
        DrawField("resetOnSceneLoad", "Сброс при загрузке");
        DrawField("dependsOnEventID", "Зависит от");
        DrawField("mutuallyExclusiveWithEventID", "Несовместимо с");
        DrawField("executeOnAwake", "Выполнить при загрузке");
        DrawField("delayBeforeExecute", "Задержка");
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawActionsSection()
    {
        showActions = EditorGUILayout.Foldout(showActions,
            $"ДЕЙСТВИЯ ({gameEvent.actions.Count})", true);
        if (!showActions) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeNames);
        if (GUILayout.Button("Добавить", GUILayout.Width(100)))
            AddAction(selectedActionTypeIndex);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        DrawActionsList(gameEvent.actions, 0);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawRequirementsSection()
    {
        showRequirements = EditorGUILayout.Foldout(showRequirements,
            $"ТРЕБОВАНИЯ ({gameEvent.requirements.Count})", true);
        if (!showRequirements) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.BeginHorizontal();
        selectedRequirementTypeIndex = EditorGUILayout.Popup(selectedRequirementTypeIndex, requirementTypeNames);
        if (GUILayout.Button("Добавить", GUILayout.Width(100)))
            AddRequirement(selectedRequirementTypeIndex);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        for (int i = 0; i < gameEvent.requirements.Count; i++)
        {
            var req = gameEvent.requirements[i];
            if (req == null) continue;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();

            string typeName = req.GetType().Name;
            string displayName = requirementDisplayNames.ContainsKey(typeName)
                ? requirementDisplayNames[typeName] : typeName;
            EditorGUILayout.LabelField($"[{i}] {displayName}", EditorStyles.boldLabel);

            if (GUILayout.Button("Удалить", GUILayout.Width(70)))
            {
                serializedObject.ApplyModifiedProperties();
                gameEvent.requirements.RemoveAt(i);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.Update();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                i--;
                continue;
            }

            EditorGUILayout.EndHorizontal();

            DrawRequirementFields(req);
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawField(string propertyName, string displayName)
    {
        var prop = serializedObject.FindProperty(propertyName);
        if (prop != null) EditorGUILayout.PropertyField(prop, new GUIContent(displayName));
    }

    private void DrawTriggerConditionFields(TriggerCondition condition)
    {
        foreach (var field in condition.GetType().GetFields())
        {
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute))) continue;

            EditorGUILayout.BeginHorizontal();
            string fName = fieldNamesRu.ContainsKey(field.Name) ? fieldNamesRu[field.Name] : field.Name;
            EditorGUILayout.LabelField(fName, GUILayout.Width(180));

            if (field.Name == "locationName" && field.FieldType == typeof(string))
            {
                DrawLocationDropdown(field, condition);
            }
            else
            {
                var value = field.GetValue(condition);
                value = DrawFieldValue(field.FieldType, value);
                field.SetValue(condition, value);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawRequirementFields(EventRequirement req)
    {
        foreach (var field in req.GetType().GetFields())
        {
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute))) continue;

            EditorGUILayout.BeginHorizontal();
            string fName = fieldNamesRu.ContainsKey(field.Name) ? fieldNamesRu[field.Name] : field.Name;
            EditorGUILayout.LabelField(fName, GUILayout.Width(180));

            if (field.Name == "locationName" && field.FieldType == typeof(string))
            {
                DrawLocationDropdown(field, req);
            }
            else
            {
                var value = field.GetValue(req);
                value = DrawFieldValue(field.FieldType, value);
                field.SetValue(req, value);
            }
            EditorGUILayout.EndHorizontal();
        }
    }

    private void DrawActionsList(List<EventAction> actions, int depth)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            var action = actions[i];
            if (action == null) continue;

            string typeName = action.GetType().Name;
            string displayName = actionDisplayNames.ContainsKey(typeName) ? actionDisplayNames[typeName] : typeName;

            EditorGUILayout.BeginVertical("helpbox");
            EditorGUILayout.BeginHorizontal();

            if (depth > 0) GUILayout.Space(depth * 20);
            EditorGUILayout.LabelField($"[{i}] {displayName}", EditorStyles.boldLabel);

            if (GUILayout.Button("Удалить", GUILayout.Width(70)))
            {
                serializedObject.ApplyModifiedProperties();
                actions.RemoveAt(i);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.Update();
                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                i--;
                continue;
            }
            EditorGUILayout.EndHorizontal();

            DrawActionFields(action, depth + 1);

            if (action is EventActionMultiple multi)
            {
                EditorGUILayout.Space(4);
                EditorGUILayout.LabelField($"Вложенные действия ({multi.subActions.Count})", EditorStyles.miniBoldLabel);

                EditorGUILayout.BeginHorizontal();
                if (depth > 0) GUILayout.Space(depth * 20);
                selectedActionTypeIndex = EditorGUILayout.Popup(selectedActionTypeIndex, actionTypeNames);
                if (GUILayout.Button("Добавить", GUILayout.Width(100)))
                {
                    serializedObject.ApplyModifiedProperties();
                    var sub = (EventAction)Activator.CreateInstance(actionTypes[selectedActionTypeIndex]);
                    multi.subActions.Add(sub);
                    EditorUtility.SetDirty(gameEvent);
                    serializedObject.Update();
                }
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(3);
                DrawActionsList(multi.subActions, depth + 1);
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(2);
        }
    }

    private void DrawActionFields(EventAction action, int depth)
    {
        foreach (var field in action.GetType().GetFields())
        {
            if (field.Name == "subActions") continue;
            if (Attribute.IsDefined(field, typeof(NonSerializedAttribute))) continue;

            EditorGUILayout.BeginHorizontal();

            string indent = new string(' ', depth * 2);
            string fName = fieldNamesRu.ContainsKey(field.Name) ? fieldNamesRu[field.Name] : field.Name;
            EditorGUILayout.LabelField(indent + fName, GUILayout.Width(180));

            var value = field.GetValue(action);
            var fieldType = field.FieldType;

            if (fieldType == typeof(EventAction))
            {
                var nested = (EventAction)value;
                if (nested != null)
                {
                    string nName = actionDisplayNames.ContainsKey(nested.GetType().Name)
                        ? actionDisplayNames[nested.GetType().Name] : nested.GetType().Name;
                    EditorGUILayout.LabelField(nName, GUILayout.Width(150));
                }
                else EditorGUILayout.LabelField("(пусто)", GUILayout.Width(150));

                if (GUILayout.Button("Выбрать", GUILayout.Width(80)))
                    ShowActionSelectionMenu(field, action);
            }
            else if (action is EventActionStartDialogue startDlg && field.Name == "dialogueID")
            {
                string currentId = (string)field.GetValue(action);
                string newId = EditorGUILayout.TextField(currentId);
                if (newId != currentId)
                {
                    field.SetValue(action, newId);
                    GUI.changed = true;
                }

                if (GUILayout.Button(new GUIContent("📝", "Сгенерировать из имени файла диалога"), GUILayout.Width(26), GUILayout.Height(18)))
                {
                    string generated = IdGenerator.Combine("dlg", startDlg.dialogueFileName);
                    field.SetValue(action, generated);
                    GUI.changed = true;
                }
            }
            else if (field.Name == "locationName" && fieldType == typeof(string))
            {
                DrawLocationDropdown(field, action);
            }
            else
            {
                value = DrawFieldValue(fieldType, value);
                field.SetValue(action, value);
            }

            EditorGUILayout.EndHorizontal();

            if (fieldType == typeof(EventAction))
            {
                var nestedAction = (EventAction)field.GetValue(action);
                if (nestedAction != null)
                {
                    EditorGUILayout.BeginVertical("box");
                    EditorGUILayout.LabelField("  Вложенное действие:", EditorStyles.miniLabel);
                    DrawActionFields(nestedAction, depth + 1);
                    EditorGUILayout.EndVertical();
                }
            }
        }
    }

    private void ShowActionSelectionMenu(System.Reflection.FieldInfo field, object parent)
    {
        GenericMenu menu = new GenericMenu();
        menu.AddItem(new GUIContent("Очистить"), false, () =>
        {
            serializedObject.ApplyModifiedProperties();
            field.SetValue(parent, null);
            EditorUtility.SetDirty(gameEvent);
            serializedObject.Update();
        });
        menu.AddSeparator("");
        foreach (var type in actionTypes)
        {
            if (type == typeof(EventActionMultiple)) continue;
            string displayName = actionDisplayNames.ContainsKey(type.Name) ? actionDisplayNames[type.Name] : type.Name;
            var captured = type;
            menu.AddItem(new GUIContent(displayName), false, () =>
            {
                serializedObject.ApplyModifiedProperties();
                var newAction = (EventAction)Activator.CreateInstance(captured);
                field.SetValue(parent, newAction);
                EditorUtility.SetDirty(gameEvent);
                serializedObject.Update();
            });
        }
        menu.ShowAsContext();
    }

    private object DrawFieldValue(Type fieldType, object value)
    {
        if (fieldType == typeof(string)) return EditorGUILayout.TextField((string)value);
        if (fieldType == typeof(int)) return EditorGUILayout.IntField((int)value);
        if (fieldType == typeof(float)) return EditorGUILayout.FloatField((float)value);
        if (fieldType == typeof(bool)) return EditorGUILayout.Toggle((bool)value);
        if (fieldType.IsEnum) return EditorGUILayout.EnumPopup((Enum)value);
        if (fieldType == typeof(AudioClip)) return EditorGUILayout.ObjectField((AudioClip)value, typeof(AudioClip), false);
        if (fieldType == typeof(GameObject)) return EditorGUILayout.ObjectField((GameObject)value, typeof(GameObject), true);
        if (fieldType == typeof(Transform)) return EditorGUILayout.ObjectField((Transform)value, typeof(Transform), true);
        if (fieldType == typeof(ItemSO)) return EditorGUILayout.ObjectField((ItemSO)value, typeof(ItemSO), false);
        if (fieldType == typeof(Vector2))
        {
            var v = (Vector2)value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            v.x = EditorGUILayout.FloatField(v.x, GUILayout.Width(60));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            v.y = EditorGUILayout.FloatField(v.y, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            return v;
        }
        if (fieldType == typeof(Vector3))
        {
            var v = (Vector3)value;
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField("X", GUILayout.Width(15));
            v.x = EditorGUILayout.FloatField(v.x, GUILayout.Width(60));
            EditorGUILayout.LabelField("Y", GUILayout.Width(15));
            v.y = EditorGUILayout.FloatField(v.y, GUILayout.Width(60));
            EditorGUILayout.LabelField("Z", GUILayout.Width(15));
            v.z = EditorGUILayout.FloatField(v.z, GUILayout.Width(60));
            EditorGUILayout.EndHorizontal();
            return v;
        }
        if (typeof(UnityEngine.Object).IsAssignableFrom(fieldType))
            return EditorGUILayout.ObjectField((UnityEngine.Object)value, fieldType, true);
        EditorGUILayout.LabelField(fieldType.Name);
        return value;
    }

    private void CreateTriggerCondition(int idx)
    {
        if (idx < 0 || idx >= triggerConditionTypes.Length) return;
        serializedObject.ApplyModifiedProperties();
        gameEvent.triggerCondition = (TriggerCondition)Activator.CreateInstance(triggerConditionTypes[idx]);
        EditorUtility.SetDirty(gameEvent);
        serializedObject.Update();
    }

    private void AddAction(int idx)
    {
        if (idx < 0 || idx >= actionTypes.Length) return;
        serializedObject.ApplyModifiedProperties();
        gameEvent.actions.Add((EventAction)Activator.CreateInstance(actionTypes[idx]));
        EditorUtility.SetDirty(gameEvent);
        serializedObject.Update();
    }

    private void AddRequirement(int idx)
    {
        if (idx < 0 || idx >= requirementTypes.Length) return;
        serializedObject.ApplyModifiedProperties();
        gameEvent.requirements.Add((EventRequirement)Activator.CreateInstance(requirementTypes[idx]));
        EditorUtility.SetDirty(gameEvent);
        serializedObject.Update();
    }

    private void DrawLocationDropdown(System.Reflection.FieldInfo field, object targetObject)
    {
        string currentValue = (string)field.GetValue(targetObject) ?? "";
        LocationEditorHelper.DrawLocationDropdown(
            "",
            currentValue,
            v =>
            {
                field.SetValue(targetObject, v);
                GUI.changed = true;
            });
    }
}
#endif