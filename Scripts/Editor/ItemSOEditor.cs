// ItemSOEditor.cs - РУСИФИЦИРОВАННЫЙ
#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(ItemSO))]
public class ItemSOEditor : Editor
{
    private ItemSO item;
    private SerializedProperty itemTypeProp;
    private SerializedProperty itemNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty iconProp;
    private SerializedProperty canStackProp;
    private SerializedProperty maxStackProp;
    private SerializedProperty isEquippableProp;
    private SerializedProperty equipmentTypeProp;
    private SerializedProperty uiPrefabProp;
    private SerializedProperty worldPrefabProp;
    private SerializedProperty highlightColorProp;
    private SerializedProperty highlightScaleProp;
    private SerializedProperty animationSpeedProp;
    private SerializedProperty hoverEffectPathProp;
    private SerializedProperty pickupEffectPathProp;
    private SerializedProperty pickupSoundProp;
    private SerializedProperty healthRestoreProp;
    private SerializedProperty staminaRestoreProp;
    private SerializedProperty damageBonusProp;
    private SerializedProperty defenseBonusProp;
    private SerializedProperty customTagsProp;

    private bool showStats = false;
    private bool showTags = false;

    private void OnEnable()
    {
        item = (ItemSO)target;

        itemTypeProp = serializedObject.FindProperty("itemType");
        itemNameProp = serializedObject.FindProperty("itemName");
        descriptionProp = serializedObject.FindProperty("description");
        iconProp = serializedObject.FindProperty("icon");
        canStackProp = serializedObject.FindProperty("canStack");
        maxStackProp = serializedObject.FindProperty("maxStack");
        isEquippableProp = serializedObject.FindProperty("isEquippable");
        equipmentTypeProp = serializedObject.FindProperty("equipmentType");
        uiPrefabProp = serializedObject.FindProperty("uiPrefab");
        worldPrefabProp = serializedObject.FindProperty("worldPrefab");
        highlightColorProp = serializedObject.FindProperty("highlightColor");
        highlightScaleProp = serializedObject.FindProperty("highlightScale");
        animationSpeedProp = serializedObject.FindProperty("animationSpeed");
        hoverEffectPathProp = serializedObject.FindProperty("hoverEffectPath");
        pickupEffectPathProp = serializedObject.FindProperty("pickupEffectPath");
        pickupSoundProp = serializedObject.FindProperty("pickupSound");
        healthRestoreProp = serializedObject.FindProperty("healthRestore");
        staminaRestoreProp = serializedObject.FindProperty("staminaRestore");
        damageBonusProp = serializedObject.FindProperty("damageBonus");
        defenseBonusProp = serializedObject.FindProperty("defenseBonus");
        customTagsProp = serializedObject.FindProperty("customTags");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // Стили
        GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        headerStyle.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        // Заголовок
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField($"📦 {item.itemName}", headerStyle);
        EditorGUILayout.Space(5);

        // ===== ОСНОВНАЯ ИНФОРМАЦИЯ =====
        EditorGUILayout.LabelField("ОСНОВНАЯ ИНФОРМАЦИЯ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(itemTypeProp, new GUIContent("Тип", "Уникальный тип предмета"));
        EditorGUILayout.PropertyField(itemNameProp, new GUIContent("Название", "Отображаемое название"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Описание", "Описание предмета"));
        EditorGUILayout.PropertyField(iconProp, new GUIContent("Иконка", "Иконка для интерфейса"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== СТЭКИНГ =====
        EditorGUILayout.LabelField("СТЭКИНГ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(canStackProp, new GUIContent("Можно складывать", "Можно ли складывать предметы в стэк?"));
        if (item.canStack)
        {
            EditorGUILayout.PropertyField(maxStackProp, new GUIContent("Макс. в стэке", "Максимальное количество в одном стэке"));
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== ЭКИПИРОВКА =====
        EditorGUILayout.LabelField("ЭКИПИРОВКА", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(isEquippableProp, new GUIContent("Можно экипировать", "Можно ли экипировать этот предмет?"));
        if (item.isEquippable)
        {
            EditorGUILayout.PropertyField(equipmentTypeProp, new GUIContent("Тип экипировки", "Куда экипировать этот предмет"));
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== ПРЕФАБЫ =====
        EditorGUILayout.LabelField("ПРЕФАБЫ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(uiPrefabProp, new GUIContent("UI Префаб", "Отображение в интерфейсе"));
        EditorGUILayout.PropertyField(worldPrefabProp, new GUIContent("Мировой Префаб", "Отображение в мире"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ =====
        EditorGUILayout.LabelField("ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(highlightColorProp, new GUIContent("Цвет подсветки", "Цвет при наведении"));
        EditorGUILayout.PropertyField(highlightScaleProp, new GUIContent("Масштаб подсветки", "Масштаб при наведении"));
        EditorGUILayout.PropertyField(animationSpeedProp, new GUIContent("Скорость анимации", "Скорость пульсирующей анимации"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== ЭФФЕКТЫ =====
        EditorGUILayout.LabelField("ЭФФЕКТЫ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(hoverEffectPathProp, new GUIContent("Путь эффекта наведения", "Путь в Resources для эффекта наведения"));
        EditorGUILayout.PropertyField(pickupEffectPathProp, new GUIContent("Путь эффекта подбора", "Путь в Resources для эффекта подбора"));
        EditorGUILayout.PropertyField(pickupSoundProp, new GUIContent("Звук подбора", "Звук при подборе предмета"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // ===== ХАРАКТЕРИСТИКИ =====
        showStats = EditorGUILayout.Foldout(showStats, "📊 ХАРАКТЕРИСТИКИ (Опционально)");
        if (showStats)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(healthRestoreProp, new GUIContent("Восстановление здоровья", "Здоровья восстанавливается при использовании"));
            EditorGUILayout.PropertyField(staminaRestoreProp, new GUIContent("Восстановление выносливости", "Выносливости восстанавливается при использовании"));
            EditorGUILayout.PropertyField(damageBonusProp, new GUIContent("Бонус к урону", "Бонус к урону при экипировке"));
            EditorGUILayout.PropertyField(defenseBonusProp, new GUIContent("Бонус к защите", "Бонус к защите при экипировке"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(5);

        // ===== ТЭГИ =====
        showTags = EditorGUILayout.Foldout(showTags, "🏷️ ТЭГИ");
        if (showTags)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(customTagsProp, new GUIContent("Пользовательские тэги", "Тэги для группировки предметов"), true);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(10);

        // ===== КНОПКИ =====
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("🔄 Обновить", GUILayout.Height(30)))
        {
            RefreshItem();
        }
        if (GUILayout.Button("📋 Копировать данные", GUILayout.Height(30)))
        {
            CopyItemData();
        }
        if (GUILayout.Button("⚡ Создать тестовый", GUILayout.Height(30)))
        {
            SpawnTestItem();
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(5);

        // ===== ПОДСКАЗКИ =====
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField("💡 Подсказки:", EditorStyles.miniBoldLabel);
        EditorGUILayout.LabelField("• Перетащите иконку из Assets", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Отметьте 'Можно экипировать' для носимых предметов", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("• Поместите префабы в папку Resources/Items", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();

        serializedObject.ApplyModifiedProperties();
    }

    // ===== МЕТОДЫ КНОПОК =====

    private void RefreshItem()
    {
        EditorUtility.SetDirty(item);
        AssetDatabase.SaveAssets();
        Debug.Log($"✅ Обновлено: {item.itemName}");
    }

    private void CopyItemData()
    {
        string json = JsonUtility.ToJson(item, true);
        EditorGUIUtility.systemCopyBuffer = json;
        Debug.Log($"📋 Данные скопированы для {item.itemName}");
    }

    private void SpawnTestItem()
    {
        if (item.worldPrefab == null)
        {
            Debug.LogWarning("Мировой префаб не установлен!");
            return;
        }

        GameObject go = PrefabUtility.InstantiatePrefab(item.worldPrefab) as GameObject;
        if (go != null)
        {
            go.transform.position = Vector3.zero;
            Selection.activeGameObject = go;
            Debug.Log($"⚡ Создан тестовый предмет: {item.itemName}");
        }
    }
}
#endif