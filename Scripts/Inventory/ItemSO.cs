using UnityEngine;

[CreateAssetMenu(fileName = "Предмет", menuName = "|Инвентарь|/Предмет")]
public class ItemSO : ScriptableObject
{
    [Header("=== ИДЕНТИФИКАЦИЯ ===")]
    [Tooltip("Стабильный уникальный ID. НЕ МЕНЯТЬ после релиза.")]
    public string id = "";

    [Header("=== ОСНОВНАЯ ИНФОРМАЦИЯ ===")]
    public ItemCategory category = ItemCategory.None;
    public string itemName = "Новый предмет";
    [TextArea(2, 4)] public string description = "Описание предмета";
    public Sprite icon;

    [Header("=== ИСПОЛЬЗОВАНИЕ ===")]
    public bool isUsable = false;
    public string useButtonText = "Использовать";

    [Header("=== СТЭКИНГ ===")]
    public bool canStack = true;
    public int maxStack = 99;

    [Header("=== ЭКИПИРОВКА ===")]
    public bool isEquippable = false;
    public EquipmentType equipmentType = EquipmentType.None;

    [Header("=== ПРЕФАБЫ ===")]
    public GameObject uiPrefab;
    public GameObject worldPrefab;

    [Header("=== ВИЗУАЛЬНАЯ ОБРАТНАЯ СВЯЗЬ ===")]
    public Color highlightColor = Color.yellow;
    [Range(1f, 2f)] public float highlightScale = 1.15f;
    [Range(1f, 10f)] public float animationSpeed = 4f;

    [Header("=== ЭФФЕКТЫ ===")]
    public string hoverEffectPath = "Particles/HoverEffect";
    public string pickupEffectPath = "Particles/PickupEffect";
    public AudioClip pickupSound;

    [Header("=== ХАРАКТЕРИСТИКИ ===")]
    public int healthRestore = 0;
    public int staminaRestore = 0;
    public int damageBonus = 0;
    public int defenseBonus = 0;

    [Header("=== ТЭГИ ===")]
    public string[] customTags;

    public ItemData ToItemData()
    {
        return new ItemData
        {
            id = id,
            itemName = itemName,
            description = description,
            icon = icon,
            maxStack = maxStack,
            uiPrefab = uiPrefab,
            worldPrefab = worldPrefab,
            canStack = canStack,
            isEquippable = isEquippable,
            equipmentType = equipmentType,
            category = category,
            isUsable = isUsable,
            useButtonText = useButtonText
        };
    }

    public GameObject SpawnWorld(Vector3 position, Quaternion rotation)
    {
        if (worldPrefab == null)
        {
            Logger.LogWarning(LogModule.Inventory, $"Мировой префаб не установлен для {itemName}");
            return null;
        }

        GameObject obj = Instantiate(worldPrefab, position, rotation);

        PickupItem pickup = obj.GetComponent<PickupItem>();
        if (pickup == null) pickup = obj.AddComponent<PickupItem>();

        pickup.itemSO = this;
        pickup.highlightColor = highlightColor;
        pickup.highlightScale = highlightScale;
        pickup.animationSpeed = animationSpeed;
        pickup.hoverEffectPath = hoverEffectPath;
        pickup.pickupEffectPath = pickupEffectPath;
        pickup.pickupSound = pickupSound;

        return obj;
    }

    public GameObject CreateUIElement(Transform parent)
    {
        if (uiPrefab == null)
        {
            Logger.LogWarning(LogModule.Inventory, $"UI префаб не установлен для {itemName}");
            return null;
        }

        GameObject obj = Instantiate(uiPrefab, parent);
        obj.transform.localPosition = Vector3.zero;

        InventoryItemMarker marker = obj.GetComponent<InventoryItemMarker>();
        if (marker == null) marker = obj.AddComponent<InventoryItemMarker>();

        marker.itemSO = this;
        marker.count = 1;
        marker.UpdateUI();

        return obj;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemName))
            itemName = name;

        if (string.IsNullOrEmpty(id))
            id = GenerateIdFromName(name);

        if (isEquippable && equipmentType == EquipmentType.None)
            Logger.LogWarning(LogModule.Inventory, $"Тип экипировки не указан для {itemName}");

        if (category == ItemCategory.None)
        {
            if (isEquippable && (equipmentType == EquipmentType.Weapon || equipmentType == EquipmentType.Offhand))
                category = ItemCategory.Weapon;
            else if (isEquippable && equipmentType == EquipmentType.Lantern)
                category = ItemCategory.Tool;
            else if (isEquippable && equipmentType != EquipmentType.None)
                category = ItemCategory.Armor;
        }
    }

    private string GenerateIdFromName(string assetName)
    {
        var chars = assetName.ToLowerInvariant().ToCharArray();
        for (int i = 0; i < chars.Length; i++)
        {
            char c = chars[i];
            if (!char.IsLetterOrDigit(c) && c != '_')
                chars[i] = '_';
        }
        return new string(chars);
    }
}