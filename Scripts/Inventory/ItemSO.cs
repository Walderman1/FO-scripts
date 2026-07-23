using UnityEngine;

[CreateAssetMenu(fileName = "НовыйПредмет", menuName = "Инвентарь/Предмет")]
public class ItemSO : ScriptableObject
{
    [Header("=== ОСНОВНАЯ ИНФОРМАЦИЯ ===")]
    public ItemType itemType = ItemType.None;
    public string itemName = "Новый предмет";
    [TextArea(2, 4)] public string description = "Описание предмета";
    public Sprite icon;

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

    [Header("=== ХАРАКТЕРИСТИКИ (Опционально) ===")]
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
            itemName = itemName,
            description = description,
            icon = icon,
            maxStack = maxStack,
            uiPrefab = uiPrefab,
            worldPrefab = worldPrefab,
            canStack = canStack,
            isEquippable = isEquippable,
            equipmentType = equipmentType
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

        pickup.itemType = itemType;
        pickup.highlightColor = highlightColor;
        pickup.highlightScale = highlightScale;
        pickup.animationSpeed = animationSpeed;
        pickup.hoverEffectPath = hoverEffectPath;
        pickup.pickupEffectPath = pickupEffectPath;
        pickup.pickupSound = pickupSound;

        Logger.Log(LogModule.Inventory, $"Создан предмет {itemName} в мире по позиции {position}");
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
        obj.transform.localScale = Vector3.one;

        InventoryItemMarker marker = obj.GetComponent<InventoryItemMarker>();
        if (marker == null) marker = obj.AddComponent<InventoryItemMarker>();

        marker.itemType = itemType;
        marker.count = 1;
        marker.UpdateUI();

        Logger.Log(LogModule.Inventory, $"Создан UI элемент для {itemName}");
        return obj;
    }

    private void OnValidate()
    {
        if (string.IsNullOrEmpty(itemName))
            itemName = itemType.ToString();

        if (icon == null)
            Logger.LogWarning(LogModule.Inventory, $"Иконка не установлена для {itemName}");

        if (isEquippable && equipmentType == EquipmentType.None)
            Logger.LogWarning(LogModule.Inventory, $"Тип экипировки не указан для экипируемого предмета {itemName}");
    }
}
