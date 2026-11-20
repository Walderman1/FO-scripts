using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Необязательный файл настроек рядом с картинкой.
/// Например для "Красный цветок.png" можно положить "Красный цветок.json":
///
/// {
///   "name": "Красный цветок",
///   "description": "Пахнет болотом.",
///   "category": "Ingredient",
///   "canStack": true,
///   "maxStack": 99,
///   "usable": false,
///   "useButtonText": "Использовать",
///   "equippable": false,
///   "equipmentType": "None",
///   "highlightColor": "#FFFF00",
///   "tags": ["flower", "swamp"]
/// }
///
/// Все поля необязательны: если поле не указано, берётся значение по умолчанию.
/// </summary>
[Serializable]
public class RuntimeItemDefinition
{
    public string name;
    public string description;
    public string category;

    public bool canStack;
    public int maxStack;

    public bool usable;
    public string useButtonText;

    public bool equippable;
    public string equipmentType;

    /// <summary>
    /// Принудительный размер предмета в мире (в юнитах). 0/не указано — размер как у обычных
    /// предметов: картинка / 100 ppu * 0.2.
    /// </summary>
    public float worldSize;

    /// <summary>
    /// Принудительный размер иконки в UI (пиксели экрана). 0/не указано — размер как у обычных
    /// предметов: иконка = размер картинки в пикселях, умноженный на 0.2.
    /// </summary>
    public float uiSize;

    public string highlightColor;
    public float highlightScale;
    public float animationSpeed;

    public string hoverEffectPath;
    public string pickupEffectPath;

    public string[] tags;

    public int healthRestore;
    public int staminaRestore;
    public int damageBonus;
    public int defenseBonus;

    private static readonly Regex KeyCacheRegex = new Regex("\"(?<key>[^\"]+)\"\\s*:", RegexOptions.Compiled);

    /// <summary>
    /// Проверяет, было ли поле реально указано в JSON.
    /// Нужно потому, что JsonUtility не применяет значения по умолчанию из полей класса.
    /// </summary>
    public static bool HasKey(string json, string key)
    {
        if (string.IsNullOrEmpty(json) || string.IsNullOrEmpty(key)) return false;

        MatchCollection matches = KeyCacheRegex.Matches(json);
        foreach (Match match in matches)
        {
            if (string.Equals(match.Groups["key"].Value, key, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    /// <summary>Разбирает JSON. Возвращает null и пишет предупреждение, если файл битый.</summary>
    public static RuntimeItemDefinition Parse(string json, string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;

        try
        {
            return JsonUtility.FromJson<RuntimeItemDefinition>(json);
        }
        catch (Exception e)
        {
            Logger.LogWarning(LogModule.Inventory,
                $"Не удалось прочитать настройки предмета '{sourcePath}': {e.Message}");
            return null;
        }
    }
}
