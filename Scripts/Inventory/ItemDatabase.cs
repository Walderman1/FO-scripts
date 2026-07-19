// ItemDatabase.cs - РУСИФИЦИРОВАННЫЙ
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ItemDatabase : MonoBehaviour
{
    public static ItemDatabase Instance;

    [Header("=== НАСТРОЙКИ БАЗЫ ДАННЫХ ===")]
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool validateOnLoad = true;

    [Header("=== ЗАПАСНОЙ ПРЕДМЕТ ===")]
    [SerializeField] private ItemSO fallbackItem;

    // Кэшированные словари для быстрого доступа
    private Dictionary<ItemType, ItemSO> itemsByType = new Dictionary<ItemType, ItemSO>();
    private Dictionary<string, ItemSO> itemsByName = new Dictionary<string, ItemSO>();

    // Событие при загрузке базы
    public System.Action OnDatabaseLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (autoLoadOnStart) LoadDatabase();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // ========== ЗАГРУЗКА ==========

    [ContextMenu("Загрузить базу данных")]
    public void LoadDatabase()
    {
        itemsByType.Clear();
        itemsByName.Clear();

        // Если список пуст, пытаемся загрузить из Resources
        if (allItems == null || allItems.Count == 0)
        {
            allItems = Resources.LoadAll<ItemSO>("Items").ToList();
            Debug.Log($"Автозагрузка: найдено {allItems.Count} предметов в Resources/Items");
        }

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (!itemsByType.ContainsKey(item.itemType))
            {
                itemsByType.Add(item.itemType, item);
            }
            else
            {
                Debug.LogWarning($"Дубликат типа предмета: {item.itemType} в базе данных");
            }

            if (!itemsByName.ContainsKey(item.itemName))
            {
                itemsByName.Add(item.itemName, item);
            }
        }

        if (validateOnLoad) ValidateDatabase();

        Debug.Log($"✅ База данных загружена: {itemsByType.Count} предметов");
        OnDatabaseLoaded?.Invoke();
    }

    // ========== ВАЛИДАЦИЯ ==========

    private void ValidateDatabase()
    {
        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (item.itemType == ItemType.None)
                Debug.LogWarning($"Предмет {item.name} имеет тип None");

            if (string.IsNullOrEmpty(item.itemName))
                Debug.LogWarning($"Предмет {item.name} не имеет названия");

            if (item.icon == null)
                Debug.LogWarning($"Предмет {item.name} не имеет иконки");

            if (item.isEquippable && item.equipmentType == EquipmentType.None)
                Debug.LogWarning($"Предмет {item.name} экипируемый, но не указан тип экипировки");
        }
    }

    // ========== ПОЛУЧЕНИЕ ДАННЫХ ==========

    public ItemSO GetItem(ItemType type)
    {
        if (itemsByType.TryGetValue(type, out ItemSO item))
            return item;

        Debug.LogWarning($"Предмет не найден: {type}");
        return fallbackItem;
    }

    public ItemSO GetItem(string name)
    {
        if (itemsByName.TryGetValue(name, out ItemSO item))
            return item;

        Debug.LogWarning($"Предмет не найден: {name}");
        return fallbackItem;
    }

    public ItemData GetItemData(ItemType type)
    {
        var item = GetItem(type);
        return item != null ? item.ToItemData() : null;
    }

    public Sprite GetItemIcon(ItemType type) => GetItem(type)?.icon;
    public GameObject GetUIPrefab(ItemType type) => GetItem(type)?.uiPrefab;
    public GameObject GetWorldPrefab(ItemType type) => GetItem(type)?.worldPrefab;

    // ========== СПИСКИ ==========

    public List<ItemSO> GetAllItems() => allItems;
    public List<ItemSO> GetEquippableItems() => allItems.Where(i => i != null && i.isEquippable).ToList();
    public List<ItemSO> GetItemsByType(EquipmentType equipmentType) =>
        allItems.Where(i => i != null && i.isEquippable && i.equipmentType == equipmentType).ToList();

    // ========== СОЗДАНИЕ ПРЕДМЕТОВ ==========

    public GameObject SpawnWorldItem(ItemType type, Vector3 position)
    {
        var item = GetItem(type);
        return item != null ? item.SpawnWorld(position, Quaternion.identity) : null;
    }

    public GameObject CreateUIItem(ItemType type, Transform parent)
    {
        var item = GetItem(type);
        return item != null ? item.CreateUIElement(parent) : null;
    }

    // ========== РЕДАКТОРНЫЕ МЕТОДЫ ==========

#if UNITY_EDITOR
    [ContextMenu("Обновить из Resources")]
    public void RefreshFromResources()
    {
        allItems = Resources.LoadAll<ItemSO>("Items").ToList();
        UnityEditor.EditorUtility.SetDirty(this);
        LoadDatabase();
        Debug.Log($"База данных обновлена: {allItems.Count} предметов");
    }

    [ContextMenu("Сортировать предметы")]
    public void SortItems()
    {
        allItems = allItems.OrderBy(i => i != null ? i.itemType : ItemType.None).ToList();
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}