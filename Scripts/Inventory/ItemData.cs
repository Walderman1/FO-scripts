using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string id;
    public string itemName;
    public string description;
    public Sprite icon;
    public int maxStack = 99;
    public GameObject uiPrefab;
    public GameObject worldPrefab;
    public bool canStack = true;
    public bool isEquippable = false;
    public EquipmentType equipmentType = EquipmentType.None;
    public ItemCategory category = ItemCategory.None;
    public bool isUsable = false;
    public string useButtonText = "Использовать";
}

public enum EquipmentType
{
    [InspectorName("Нет")] None,
    [InspectorName("Голова")] Head,
    [InspectorName("Лицо")] Face,
    [InspectorName("Шея")] Neck,
    [InspectorName("Грудь")] Chest,
    [InspectorName("Пояс")] Waist,
    [InspectorName("Ноги")] Legs,
    [InspectorName("Обувь")] Boots,
    [InspectorName("Оружие")] Weapon,
    [InspectorName("Вторая рука")] Offhand,
    [InspectorName("Кольцо")] Ring,
    [InspectorName("Фонарь")] Lantern
}