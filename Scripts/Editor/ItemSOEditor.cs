#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using System.IO;

[CustomEditor(typeof(ItemSO))]
public class ItemSOEditor : Editor
{
    private ItemSO item;

    private SerializedProperty idProp;
    private SerializedProperty categoryProp;
    private SerializedProperty itemNameProp;
    private SerializedProperty descriptionProp;
    private SerializedProperty iconProp;
    private SerializedProperty canStackProp;
    private SerializedProperty maxStackProp;
    private SerializedProperty isEquippableProp;
    private SerializedProperty equipmentTypeProp;
    private SerializedProperty isUsableProp;
    private SerializedProperty useButtonTextProp;
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
    private bool showQuickCreate = false;

    private Sprite newItemIcon;
    private ItemCategory newItemCategory = ItemCategory.None;
    private string newItemId = "";

    private const string ITEMS_PATH = "Assets/Resources/Items";
    private const string INVENTORY_PREFABS_PATH = "Assets/Resources/Items/Inventory";
    private const string WORLD_PREFABS_PATH = "Assets/Resources/Items/World";
    private const string NULL_UI_PREFAB_PATH = "Assets/Resources/Items/NullUIItem.prefab";
    private const string NULL_WORLD_PREFAB_PATH = "Assets/Resources/Items/NullWorldItem.prefab";

    private void OnEnable()
    {
        item = (ItemSO)target;

        idProp = serializedObject.FindProperty("id");
        categoryProp = serializedObject.FindProperty("category");
        itemNameProp = serializedObject.FindProperty("itemName");
        descriptionProp = serializedObject.FindProperty("description");
        iconProp = serializedObject.FindProperty("icon");
        canStackProp = serializedObject.FindProperty("canStack");
        maxStackProp = serializedObject.FindProperty("maxStack");
        isEquippableProp = serializedObject.FindProperty("isEquippable");
        equipmentTypeProp = serializedObject.FindProperty("equipmentType");
        isUsableProp = serializedObject.FindProperty("isUsable");
        useButtonTextProp = serializedObject.FindProperty("useButtonText");
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

        if (item.uiPrefab == null || item.worldPrefab == null)
            showQuickCreate = true;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawItemHeader();
        DrawMainInfo();
        DrawUsage();
        DrawStacking();
        DrawEquipment();
        DrawPrefabs();
        DrawVisualFeedback();
        DrawEffects();
        DrawStats();
        DrawTags();
        DrawQuickCreate();
        DrawActions();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawItemHeader()
    {
        EditorGUILayout.Space(8);
        var style = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleCenter
        };
        style.normal.textColor = EditorGUIUtility.isProSkin ? Color.white : Color.black;

        string cat = item.category != ItemCategory.None ? $" [{item.category}]" : " [нет категории]";
        EditorGUILayout.LabelField($"📦 {item.itemName}{cat}", style);
        EditorGUILayout.Space(6);
    }

    private void DrawMainInfo()
    {
        EditorGUILayout.LabelField("ОСНОВНАЯ ИНФОРМАЦИЯ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        IdEditorHelper.DrawIdField(serializedObject, idProp, "ID", "item", "itemName");
        EditorGUILayout.PropertyField(categoryProp, new GUIContent("Категория"));
        EditorGUILayout.PropertyField(itemNameProp, new GUIContent("Название"));
        EditorGUILayout.PropertyField(descriptionProp, new GUIContent("Описание"));
        EditorGUILayout.PropertyField(iconProp, new GUIContent("Иконка"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawUsage()
    {
        EditorGUILayout.LabelField("ИСПОЛЬЗОВАНИЕ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(isUsableProp, new GUIContent("Можно использовать"));
        if (item.isUsable)
            EditorGUILayout.PropertyField(useButtonTextProp, new GUIContent("Текст кнопки"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawStacking()
    {
        EditorGUILayout.LabelField("СТЭКИНГ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(canStackProp, new GUIContent("Можно складывать"));
        if (item.canStack)
            EditorGUILayout.PropertyField(maxStackProp, new GUIContent("Макс. в стэке"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawEquipment()
    {
        EditorGUILayout.LabelField("ЭКИПИРОВКА", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(isEquippableProp, new GUIContent("Можно экипировать"));
        if (item.isEquippable)
            EditorGUILayout.PropertyField(equipmentTypeProp, new GUIContent("Тип экипировки"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawPrefabs()
    {
        EditorGUILayout.LabelField("ПРЕФАБЫ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(uiPrefabProp, new GUIContent("UI Префаб"));
        EditorGUILayout.PropertyField(worldPrefabProp, new GUIContent("Мировой Префаб"));
        if (item.uiPrefab == null)
            EditorGUILayout.HelpBox("UI префаб не задан — предмет нельзя добавить в инвентарь.", MessageType.Warning);
        if (item.worldPrefab == null)
            EditorGUILayout.HelpBox("Мировой префаб не задан — предмет нельзя выбросить.", MessageType.Warning);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawVisualFeedback()
    {
        EditorGUILayout.LabelField("ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(highlightColorProp, new GUIContent("Цвет подсветки"));
        EditorGUILayout.PropertyField(highlightScaleProp, new GUIContent("Масштаб подсветки"));
        EditorGUILayout.PropertyField(animationSpeedProp, new GUIContent("Скорость анимации"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawEffects()
    {
        EditorGUILayout.LabelField("ЭФФЕКТЫ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(hoverEffectPathProp, new GUIContent("Эффект наведения"));
        EditorGUILayout.PropertyField(pickupEffectPathProp, new GUIContent("Эффект подбора"));
        EditorGUILayout.PropertyField(pickupSoundProp, new GUIContent("Звук подбора"));
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(6);
    }

    private void DrawStats()
    {
        showStats = EditorGUILayout.Foldout(showStats, "ХАРАКТЕРИСТИКИ", true);
        if (showStats)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(healthRestoreProp, new GUIContent("Восстановление HP"));
            EditorGUILayout.PropertyField(staminaRestoreProp, new GUIContent("Восстановление стамины"));
            EditorGUILayout.PropertyField(damageBonusProp, new GUIContent("Бонус к урону"));
            EditorGUILayout.PropertyField(defenseBonusProp, new GUIContent("Бонус к защите"));
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawTags()
    {
        showTags = EditorGUILayout.Foldout(showTags, "ТЭГИ", true);
        if (showTags)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.PropertyField(customTagsProp, new GUIContent("Пользовательские тэги"), true);
            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.Space(4);
    }

    private void DrawQuickCreate()
    {
        showQuickCreate = EditorGUILayout.Foldout(showQuickCreate, "БЫСТРОЕ СОЗДАНИЕ ПРЕФАБОВ", true);
        if (!showQuickCreate) return;

        EditorGUILayout.BeginVertical("box");

        EditorGUILayout.HelpBox(
            "Укажите иконку — инструмент создаст UI и World префабы " +
            "и сохранит их в Assets/Resources/Items.",
            MessageType.Info);

        newItemIcon = (Sprite)EditorGUILayout.ObjectField("Иконка", newItemIcon, typeof(Sprite), false);
        newItemCategory = (ItemCategory)EditorGUILayout.EnumPopup("Категория", newItemCategory);

        if (newItemIcon != null)
        {
            string suggested = IdGenerator.Slugify(newItemIcon.name);
            if (string.IsNullOrEmpty(newItemId)) newItemId = suggested;
            newItemId = EditorGUILayout.TextField("ID", newItemId);

            EditorGUILayout.LabelField("Будет создано:");
            EditorGUILayout.LabelField($"  id: {newItemId}", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  UI:    Resources/Items/Inventory/{newItemId}_UI.prefab", EditorStyles.miniLabel);
            EditorGUILayout.LabelField($"  World: Resources/Items/World/{newItemId}_World.prefab", EditorStyles.miniLabel);
        }

        EditorGUILayout.Space(4);

        using (new EditorGUI.DisabledScope(newItemIcon == null || string.IsNullOrEmpty(newItemId)))
        {
            if (GUILayout.Button("Создать префабы и заполнить SO", GUILayout.Height(30)))
                CreateItemAssets();
        }

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }

    private void DrawActions()
    {
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Обновить"))
        {
            EditorUtility.SetDirty(item);
            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("Копировать JSON"))
        {
            EditorGUIUtility.systemCopyBuffer = JsonUtility.ToJson(item, true);
        }
        if (GUILayout.Button("Spawn Test"))
            SpawnTestItem();
        EditorGUILayout.EndHorizontal();
    }

    private void CreateItemAssets()
    {
        if (!ValidatePrerequisites()) return;
        if (string.IsNullOrEmpty(newItemId)) return;

        EnsureItemsFolders();

        string uiPrefabPath = $"{INVENTORY_PREFABS_PATH}/{newItemId}_UI.prefab";
        string worldPrefabPath = $"{WORLD_PREFABS_PATH}/{newItemId}_World.prefab";

        if (File.Exists(uiPrefabPath) || File.Exists(worldPrefabPath))
        {
            bool overwrite = EditorUtility.DisplayDialog("Префабы уже существуют",
                $"{uiPrefabPath}\n{worldPrefabPath}\n\nПерезаписать?", "Да", "Отмена");
            if (!overwrite) return;
        }

        GameObject uiAsset = CreateUIPrefab(uiPrefabPath);
        if (uiAsset == null) return;

        GameObject worldAsset = CreateWorldPrefab(worldPrefabPath);
        if (worldAsset == null) return;

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        serializedObject.Update();
        idProp.stringValue = newItemId;
        categoryProp.enumValueIndex = (int)newItemCategory;
        itemNameProp.stringValue = newItemId;
        iconProp.objectReferenceValue = newItemIcon;
        uiPrefabProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(uiPrefabPath);
        worldPrefabProp.objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameObject>(worldPrefabPath);
        serializedObject.ApplyModifiedProperties();
        EditorUtility.SetDirty(item);
    }

    private bool ValidatePrerequisites()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(NULL_UI_PREFAB_PATH) == null)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не найден: {NULL_UI_PREFAB_PATH}", "OK");
            return false;
        }
        if (AssetDatabase.LoadAssetAtPath<GameObject>(NULL_WORLD_PREFAB_PATH) == null)
        {
            EditorUtility.DisplayDialog("Ошибка", $"Не найден: {NULL_WORLD_PREFAB_PATH}", "OK");
            return false;
        }
        return true;
    }

    private void EnsureItemsFolders()
    {
        if (!AssetDatabase.IsValidFolder(ITEMS_PATH))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Items");
        }
        if (!AssetDatabase.IsValidFolder(INVENTORY_PREFABS_PATH))
            AssetDatabase.CreateFolder(ITEMS_PATH, "Inventory");
        if (!AssetDatabase.IsValidFolder(WORLD_PREFABS_PATH))
            AssetDatabase.CreateFolder(ITEMS_PATH, "World");
    }

    private GameObject CreateUIPrefab(string savePath)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(NULL_UI_PREFAB_PATH);
        if (template == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = newItemId + "_UI";

        try
        {
            Image image = instance.GetComponentInChildren<Image>(true);
            if (image == null) return null;

            image.sprite = newItemIcon;
            image.color = Color.white;
            image.preserveAspect = true;

            RectTransform rt = image.rectTransform;
            float maxSide = Mathf.Max(newItemIcon.rect.width, newItemIcon.rect.height);
            float normalizedSize = 64f;
            float scale = normalizedSize / Mathf.Max(maxSide, 1f);

            rt.sizeDelta = new Vector2(
                newItemIcon.rect.width * scale,
                newItemIcon.rect.height * scale);
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;

            InventoryItemMarker marker = instance.GetComponent<InventoryItemMarker>();
            if (marker == null) marker = instance.GetComponentInChildren<InventoryItemMarker>(true);
            if (marker != null)
            {
                marker.itemSO = item;
                marker.count = 1;
                marker.UpdateUI();
            }

            return PrefabUtility.SaveAsPrefabAsset(instance, savePath);
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }
    }

    private GameObject CreateWorldPrefab(string savePath)
    {
        GameObject template = AssetDatabase.LoadAssetAtPath<GameObject>(NULL_WORLD_PREFAB_PATH);
        if (template == null) return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
        instance.name = newItemId + "_World";

        try
        {
            SpriteRenderer sr = instance.GetComponent<SpriteRenderer>();
            if (sr == null) sr = instance.GetComponentInChildren<SpriteRenderer>(true);
            if (sr != null)
            {
                sr.sprite = newItemIcon;
                sr.color = Color.white;
            }

            BoxCollider2D box = instance.GetComponent<BoxCollider2D>();
            if (box == null) box = instance.GetComponentInChildren<BoxCollider2D>(true);
            if (box != null && newItemIcon != null)
            {
                box.size = newItemIcon.bounds.size;
                box.offset = newItemIcon.bounds.center;
                box.isTrigger = true;
            }

            PickupItem pickup = instance.GetComponent<PickupItem>();
            if (pickup == null) pickup = instance.GetComponentInChildren<PickupItem>(true);
            if (pickup != null) pickup.itemSO = item;

            return PrefabUtility.SaveAsPrefabAsset(instance, savePath);
        }
        finally
        {
            if (instance != null) Object.DestroyImmediate(instance);
        }
    }

    private void SpawnTestItem()
    {
        if (item.worldPrefab == null) return;
        GameObject go = PrefabUtility.InstantiatePrefab(item.worldPrefab) as GameObject;
        if (go != null)
        {
            go.transform.position = Vector3.zero;
            Selection.activeGameObject = go;
        }
    }
}
#endif