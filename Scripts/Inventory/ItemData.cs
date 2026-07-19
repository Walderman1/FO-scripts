// ItemData.cs
using UnityEngine;

[System.Serializable]
public class ItemData
{
    public string itemName;
    public string description;
    public Sprite icon;
    public int maxStack = 99;
    public GameObject uiPrefab;
    public GameObject worldPrefab;
    public bool canStack = true;
    public bool isEquippable = false;
    public EquipmentType equipmentType = EquipmentType.None; // ← добавить
}

// EquipmentType.cs
public enum EquipmentType
{
    None,
    Head,      // голова (шлемы, шапки)
    Face,      // лицо (маски, очки)
    Neck,      // шея (амулеты, ожерелья)
    Chest,     // тело (броня, одежда)
    Waist,     // пояс (ремни, пояса)
    Legs,      // ноги (штаны, поножи)
    Boots,     // обувь (сапоги, ботинки)
    Weapon,    // оружие (мечи, посохи)
    Offhand,   // вторая рука (щиты, книги)
    Ring,      // кольцо
    Lantern    // лампа/фонарь
}