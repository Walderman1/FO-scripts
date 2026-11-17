using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using VContainer;

public class ItemDatabase : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ БАЗЫ ДАННЫХ ===")]
    [SerializeField] private List<ItemSO> allItems = new List<ItemSO>();
    [SerializeField] private bool autoLoadOnStart = true;
    [SerializeField] private bool validateOnLoad = true;
    [SerializeField] private string resourcesPath = "Items/ScriptableObjects";
    [SerializeField] private ItemSO fallbackItem;

    private Dictionary<string, ItemSO> itemsById = new Dictionary<string, ItemSO>();
    private Dictionary<string, ItemSO> itemsByName = new Dictionary<string, ItemSO>();

    public System.Action OnDatabaseLoaded;

    private void Awake()
    {
        if (autoLoadOnStart) LoadDatabase();
    }

    [ContextMenu("Загрузить базу данных")]
    public void LoadDatabase()
    {
        allItems = Resources.LoadAll<ItemSO>(resourcesPath).ToList();
        Logger.Log(LogModule.Inventory, $"Загружено {allItems.Count} предметов из Resources/{resourcesPath}");
        BuildIndexes();
    }

    private void BuildIndexes()
    {
        itemsById.Clear();
        itemsByName.Clear();

        foreach (var item in allItems)
        {
            if (item == null) continue;

            if (string.IsNullOrEmpty(item.id))
            {
                Logger.LogWarning(LogModule.Inventory, $"У предмета '{item.name}' пустой id — пропущен");
                continue;
            }

            if (!itemsById.ContainsKey(item.id)) itemsById.Add(item.id, item);
            else Logger.LogWarning(LogModule.Inventory, $"Дубликат id '{item.id}' (у {item.name})");

            if (!string.IsNullOrEmpty(item.itemName) && !itemsByName.ContainsKey(item.itemName))
                itemsByName.Add(item.itemName, item);
        }

        if (validateOnLoad) ValidateDatabase();
        OnDatabaseLoaded?.Invoke();
    }

    private void ValidateDatabase()
    {
        foreach (var item in allItems)
        {
            if (item == null) continue;
            if (string.IsNullOrEmpty(item.id))
                Logger.LogWarning(LogModule.Inventory, $"'{item.name}' без id");
            if (string.IsNullOrEmpty(item.itemName))
                Logger.LogWarning(LogModule.Inventory, $"'{item.name}' без itemName");
            if (item.icon == null)
                Logger.LogWarning(LogModule.Inventory, $"'{item.name}' без иконки");
        }
    }

    public ItemSO GetItem(string id)
    {
        if (string.IsNullOrEmpty(id)) return fallbackItem;
        if (itemsById.TryGetValue(id, out var item)) return item;
        Logger.LogWarning(LogModule.Inventory, $"Предмет по id '{id}' не найден");
        return fallbackItem;
    }

    public ItemSO GetItemByName(string name)
    {
        if (string.IsNullOrEmpty(name)) return fallbackItem;
        if (itemsByName.TryGetValue(name, out var item)) return item;
        return fallbackItem;
    }

    public ItemSO GetItemByLegacyType(ItemType legacyType)
    {
        if (legacyType == ItemType.None) return fallbackItem;

        string name = legacyType.ToString();

        // 1. Точное совпадение по имени
        if (itemsByName.TryGetValue(name, out var item)) return item;

        // 2. Точное совпадение по id
        string idLower = name.ToLowerInvariant();
        if (itemsById.TryGetValue(idLower, out item)) return item;

        // 3. Поиск по префиксу id: Potion → potion_item_0, Lamp → lamp_0
        string prefix = idLower + "_";
        foreach (var kv in itemsById)
        {
            if (kv.Key.StartsWith(prefix, System.StringComparison.OrdinalIgnoreCase))
            {
                Logger.LogVerbose(LogModule.Inventory,
                    $"Legacy ItemType.{name} резолвится по префиксу в '{kv.Key}'");
                return kv.Value;
            }
        }

        Logger.LogWarning(LogModule.Inventory, $"Legacy ItemType.{name} не найден");
        return fallbackItem;
    }

    public ItemData GetItemData(string id) => GetItem(id)?.ToItemData();
    public ItemData GetItemDataByLegacyType(ItemType legacyType) => GetItemByLegacyType(legacyType)?.ToItemData();
    public List<ItemSO> GetAllItems() => allItems;

    public List<ItemSO> GetItemsByCategory(ItemCategory category) =>
        allItems.Where(i => i != null && i.category == category).ToList();

    public List<ItemSO> GetEquippableItems() =>
        allItems.Where(i => i != null && i.isEquippable).ToList();

    public List<ItemSO> GetItemsByEquipmentType(EquipmentType equipmentType) =>
        allItems.Where(i => i != null && i.isEquippable && i.equipmentType == equipmentType).ToList();

    public GameObject SpawnWorldItem(string id, Vector3 position)
    {
        var item = GetItem(id);
        return item != null ? item.SpawnWorld(position, Quaternion.identity) : null;
    }

    public GameObject CreateUIItem(string id, Transform parent)
    {
        var item = GetItem(id);
        return item != null ? item.CreateUIElement(parent) : null;
    }

#if UNITY_EDITOR
    [ContextMenu("Обновить из Resources")]
    public void RefreshFromResources() => LoadDatabase();
#endif
}