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

    private Dictionary<ItemType, ItemSO> itemsByType = new Dictionary<ItemType, ItemSO>();
    private Dictionary<string, ItemSO> itemsByName = new Dictionary<string, ItemSO>();

    public System.Action OnDatabaseLoaded;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            if (autoLoadOnStart) LoadDatabase();
            Logger.Log(LogModule.Inventory, "ItemDatabase инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Inventory, "Уничтожение дублирующего ItemDatabase");
            Destroy(gameObject);
        }
    }

    [ContextMenu("Загрузить базу данных")]
    public void LoadDatabase()
    {
        itemsByType.Clear();
        itemsByName.Clear();

        if (allItems == null || allItems.Count == 0)
        {
            allItems = Resources.LoadAll<ItemSO>("Items").ToList();
            Logger.Log(LogModule.Inventory, $"Автозагрузка: найдено {allItems.Count} предметов в Resources/Items");
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
                Logger.LogWarning(LogModule.Inventory, $"Дубликат типа предмета: {item.itemType} в базе данных");
            }

            if (!itemsByName.ContainsKey(item.itemName))
            {
                itemsByName.Add(item.itemName, item);
            }
        }

        if (validateOnLoad) ValidateDatabase();

        Logger.Log(LogModule.Inventory, $"База данных загружена: {itemsByType.Count} предметов");
        OnDatabaseLoaded?.Invoke();
    }

    private void ValidateDatabase()
    {
        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (item.itemType == ItemType.None)
                Logger.LogWarning(LogModule.Inventory, $"Предмет {item.name} имеет тип None");

            if (string.IsNullOrEmpty(item.itemName))
                Logger.LogWarning(LogModule.Inventory, $"Предмет {item.name} не имеет названия");

            if (item.icon == null)
                Logger.LogWarning(LogModule.Inventory, $"Предмет {item.name} не имеет иконки");

            if (item.isEquippable && item.equipmentType == EquipmentType.None)
                Logger.LogWarning(LogModule.Inventory, $"Предмет {item.name} экипируемый, но не указан тип экипировки");
        }
    }

    public ItemSO GetItem(ItemType type)
    {
        if (itemsByType.TryGetValue(type, out ItemSO item))
            return item;

        Logger.LogWarning(LogModule.Inventory, $"Предмет не найден: {type}");
        return fallbackItem;
    }

    public ItemSO GetItem(string name)
    {
        if (itemsByName.TryGetValue(name, out ItemSO item))
            return item;

        Logger.LogWarning(LogModule.Inventory, $"Предмет не найден: {name}");
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

    public List<ItemSO> GetAllItems() => allItems;
    public List<ItemSO> GetEquippableItems() => allItems.Where(i => i != null && i.isEquippable).ToList();
    public List<ItemSO> GetItemsByType(EquipmentType equipmentType) =>
        allItems.Where(i => i != null && i.isEquippable && i.equipmentType == equipmentType).ToList();

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

#if UNITY_EDITOR
    [ContextMenu("Обновить из Resources")]
    public void RefreshFromResources()
    {
        allItems = Resources.LoadAll<ItemSO>("Items").ToList();
        UnityEditor.EditorUtility.SetDirty(this);
        LoadDatabase();
        Logger.Log(LogModule.Inventory, $"База данных обновлена: {allItems.Count} предметов");
    }

    [ContextMenu("Сортировать предметы")]
    public void SortItems()
    {
        allItems = allItems.OrderBy(i => i != null ? i.itemType : ItemType.None).ToList();
        UnityEditor.EditorUtility.SetDirty(this);
        Logger.Log(LogModule.Inventory, "Предметы отсортированы по типу");
    }
#endif
}
