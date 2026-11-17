using UnityEngine;

public enum ItemCategory
{
    [InspectorName("Нет")] None,
    [InspectorName("Ингредиент")] Ingredient,
    [InspectorName("Расходник")] Consumable,
    [InspectorName("Оружие")] Weapon,
    [InspectorName("Броня")] Armor,
    [InspectorName("Аксессуар")] Accessory,
    [InspectorName("Инструмент")] Tool,
    [InspectorName("Квестовый")] Quest,
    [InspectorName("Валюта")] Currency,
    [InspectorName("Разное")] Misc
}