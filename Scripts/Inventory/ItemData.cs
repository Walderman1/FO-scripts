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
    public EquipmentType equipmentType = EquipmentType.None;
}

public enum EquipmentType
{
    None,
    Head,
    Face,
    Neck,
    Chest,
    Waist,
    Legs,
    Boots,
    Weapon,
    Offhand,
    Ring,
    Lantern
}
