using UnityEngine;
using System.Collections.Generic;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance;

    private Dictionary<EquipmentType, GameObject> equippedItems = new Dictionary<EquipmentType, GameObject>();
    private Dictionary<EquipmentType, ItemType> equippedTypes = new Dictionary<EquipmentType, ItemType>();

    private Dictionary<EquipmentType, Transform> pivotCache = new Dictionary<EquipmentType, Transform>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.Inventory, "EquipmentSystem инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Inventory, "Уничтожение дублирующего EquipmentSystem");
            Destroy(gameObject);
        }
    }

    private Transform FindPivotByTag(EquipmentType slotType)
    {
        if (pivotCache.ContainsKey(slotType))
        {
            return pivotCache[slotType];
        }

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("EquipmentSlot");

        if (taggedObjects.Length == 0)
        {
            Logger.LogWarning(LogModule.Inventory, "Объекты с тегом 'EquipmentSlot' не найдены");
            return null;
        }

        foreach (GameObject parentObj in taggedObjects)
        {
            foreach (Transform child in parentObj.transform)
            {
                string childName = child.name;
                Transform found = null;

                switch (slotType)
                {
                    case EquipmentType.Head:
                        if (childName == "HeadPivot" || childName.Contains("Head")) found = child;
                        break;
                    case EquipmentType.Face:
                        if (childName == "FacePivot" || childName.Contains("Face")) found = child;
                        break;
                    case EquipmentType.Neck:
                        if (childName == "NeckPivot" || childName.Contains("Neck")) found = child;
                        break;
                    case EquipmentType.Chest:
                        if (childName == "ChestPivot" || childName.Contains("Chest")) found = child;
                        break;
                    case EquipmentType.Waist:
                        if (childName == "WaistPivot" || childName.Contains("Waist")) found = child;
                        break;
                    case EquipmentType.Legs:
                        if (childName == "LegsPivot" || childName.Contains("Legs")) found = child;
                        break;
                    case EquipmentType.Weapon:
                        if (childName == "WeaponPivot" || childName.Contains("Weapon")) found = child;
                        break;
                    case EquipmentType.Offhand:
                        if (childName == "OffhandPivot" || childName.Contains("Offhand")) found = child;
                        break;
                    case EquipmentType.Lantern:
                        if (childName == "LanternPivot" || childName.Contains("Lantern")) found = child;
                        break;
                    default:
                        return null;
                }

                if (found != null)
                {
                    Logger.Log(LogModule.Inventory, $"Найден пивот для {slotType}: {found.name} (дочерний от {parentObj.name})");
                    pivotCache[slotType] = found;
                    return found;
                }
            }
        }

        Logger.LogWarning(LogModule.Inventory, $"Пивот для {slotType} не найден в дочерних объектах с тегом 'EquipmentSlot'");
        return null;
    }

    private Transform GetPivotForSlot(EquipmentType slotType)
    {
        Transform pivot = FindPivotByTag(slotType);

        if (pivot == null)
        {
            Logger.LogError(LogModule.Inventory, $"Пивот для {slotType} не найден");
        }

        return pivot;
    }

    public bool EquipItem(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.Inventory, "Попытка экипировать null предмет");
            return false;
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null || !data.isEquippable)
        {
            Logger.Log(LogModule.Inventory, $"Предмет {item.itemType} нельзя экипировать");
            return false;
        }

        if (equippedItems.ContainsKey(data.equipmentType) && equippedItems[data.equipmentType] != null)
        {
            Logger.Log(LogModule.Inventory, $"Слот {data.equipmentType} занят, снятие текущего предмета");
            UnequipItem(data.equipmentType);
        }

        Transform pivot = GetPivotForSlot(data.equipmentType);
        if (pivot == null)
        {
            Logger.LogError(LogModule.Inventory, $"Невозможно экипировать {item.itemType}: пивот для {data.equipmentType} не найден");
            return false;
        }

        if (data.worldPrefab == null)
        {
            Logger.LogError(LogModule.Inventory, $"World префаб для {item.itemType} не найден");
            return false;
        }

        GameObject newItem = Instantiate(data.worldPrefab, pivot);
        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;
        newItem.transform.localScale = Vector3.one;

        DisableItemInteractions(newItem);

        equippedItems[data.equipmentType] = newItem;
        equippedTypes[data.equipmentType] = item.itemType;

        EventManager.Instance?.TriggerEvent(EventTriggerType.EquipItem,
            new EventContext().WithItem(item.itemType).WithEquipmentType(data.equipmentType));

        Logger.Log(LogModule.Inventory, $"Экипирован предмет: {item.itemType} в слот {data.equipmentType}");
        return true;
    }

    public void UnequipItem(EquipmentType slotType)
    {
        if (!equippedItems.ContainsKey(slotType))
        {
            Logger.Log(LogModule.Inventory, $"Слот {slotType} уже пуст");
            return;
        }

        GameObject item = equippedItems[slotType];
        if (item != null)
        {
            Destroy(item);
        }

        equippedItems.Remove(slotType);
        equippedTypes.Remove(slotType);

        Logger.Log(LogModule.Inventory, $"Предмет снят со слота {slotType}");
    }

    public void UnequipCurrent()
    {
        if (equippedItems.Count > 0)
        {
            var first = equippedItems.GetEnumerator();
            first.MoveNext();
            Logger.Log(LogModule.Inventory, $"Снятие предмета со слота {first.Current.Key}");
            UnequipItem(first.Current.Key);
        }
        else
        {
            Logger.Log(LogModule.Inventory, "Нет экипированных предметов для снятия");
        }
    }

    public bool IsEquipped(ItemType itemType)
    {
        return equippedTypes.ContainsValue(itemType);
    }

    public bool IsEquipped(EquipmentType slotType)
    {
        return equippedItems.ContainsKey(slotType) && equippedItems[slotType] != null;
    }

    public ItemType GetEquippedType(EquipmentType slotType)
    {
        return equippedTypes.ContainsKey(slotType) ? equippedTypes[slotType] : ItemType.None;
    }

    public GameObject GetEquippedItem(EquipmentType slotType)
    {
        return equippedItems.ContainsKey(slotType) ? equippedItems[slotType] : null;
    }

    public ItemType GetEquippedType()
    {
        if (equippedTypes.Count > 0)
        {
            var first = equippedTypes.GetEnumerator();
            first.MoveNext();
            return first.Current.Value;
        }
        return ItemType.None;
    }

    public GameObject GetEquippedItem()
    {
        if (equippedItems.Count > 0)
        {
            var first = equippedItems.GetEnumerator();
            first.MoveNext();
            return first.Current.Value;
        }
        return null;
    }

    private void DisableItemInteractions(GameObject item)
    {
        PickupItem[] pickups = item.GetComponentsInChildren<PickupItem>(true);
        foreach (PickupItem p in pickups) p.enabled = false;

        Collider2D[] colliders2D = item.GetComponentsInChildren<Collider2D>(true);
        foreach (Collider2D col in colliders2D) col.enabled = false;

        Collider[] colliders3D = item.GetComponentsInChildren<Collider>(true);
        foreach (Collider col in colliders3D) col.enabled = false;

        SpriteRenderer[] renderers = item.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (SpriteRenderer sr in renderers) sr.sortingOrder = 5;

        Rigidbody2D[] rb2D = item.GetComponentsInChildren<Rigidbody2D>(true);
        foreach (Rigidbody2D rb in rb2D)
        {
            rb.isKinematic = true;
            rb.simulated = false;
        }

        Rigidbody[] rb3D = item.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rb in rb3D)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void RefreshEquipmentVisuals()
    {
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value != null)
            {
                Transform pivot = GetPivotForSlot(kvp.Key);
                if (pivot != null)
                {
                    kvp.Value.transform.SetParent(pivot);
                    kvp.Value.transform.localPosition = Vector3.zero;
                    kvp.Value.transform.localRotation = Quaternion.identity;
                    kvp.Value.transform.localScale = Vector3.one;
                }
            }
        }
        Logger.Log(LogModule.Inventory, "Визуальное отображение экипировки обновлено");
    }

    private void OnDestroy()
    {
        foreach (var item in equippedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        equippedItems.Clear();
        equippedTypes.Clear();
        Logger.Log(LogModule.Inventory, "EquipmentSystem уничтожен, экипировка очищена");
    }
}
