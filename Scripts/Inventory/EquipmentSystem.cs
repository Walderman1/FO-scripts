using UnityEngine;
using System.Collections.Generic;

public class EquipmentSystem : MonoBehaviour
{
    public static EquipmentSystem Instance;

    // Храним экипированные предметы
    private Dictionary<EquipmentType, GameObject> equippedItems = new Dictionary<EquipmentType, GameObject>();
    private Dictionary<EquipmentType, ItemType> equippedTypes = new Dictionary<EquipmentType, ItemType>();

    // Кэш для найденных пивотов
    private Dictionary<EquipmentType, Transform> pivotCache = new Dictionary<EquipmentType, Transform>();

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    // ========== ПОИСК ПИВОТА В ДОЧЕРНИХ ОБЪЕКТАХ ==========
    private Transform FindPivotByTag(EquipmentType slotType)
    {
        // Проверяем кэш
        if (pivotCache.ContainsKey(slotType))
        {
            return pivotCache[slotType];
        }

        // Ищем ВСЕ объекты с тегом "EquipmentSlot"
        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("EquipmentSlot");

        // Если ничего не нашли с тегом
        if (taggedObjects.Length == 0)
        {
            Debug.LogWarning($"⚠️ No objects with tag 'EquipmentSlot' found!");
            return null;
        }

        // Проходим по всем объектам с тегом
        foreach (GameObject parentObj in taggedObjects)
        {
            // Ищем среди ДОЧЕРНИХ объектов
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
                    Debug.Log($"✅ Found pivot for {slotType}: {found.name} (child of {parentObj.name})");
                    pivotCache[slotType] = found;
                    return found;
                }
            }
        }

        Debug.LogWarning($"⚠️ No pivot found for {slotType} in children of objects with tag 'EquipmentSlot'");
        return null;
    }

    // ========== ПОЛУЧИТЬ ПИВОТ ДЛЯ СЛОТА ==========
    private Transform GetPivotForSlot(EquipmentType slotType)
    {
        Transform pivot = FindPivotByTag(slotType);

        if (pivot == null)
        {
            Debug.LogError($"❌ Pivot for {slotType} not found! Make sure there is an object with tag 'EquipmentSlot' and a child named '{slotType}Pivot'");
        }

        return pivot;
    }

    // ========== ЭКИПИРОВКА ==========
    public bool EquipItem(InventoryItemMarker item)
    {
        if (item == null) return false;

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null || !data.isEquippable)
        {
            Debug.Log($"Item {item.itemType} is not equippable!");
            return false;
        }

        if (equippedItems.ContainsKey(data.equipmentType) && equippedItems[data.equipmentType] != null)
        {
            UnequipItem(data.equipmentType);
        }

        Transform pivot = GetPivotForSlot(data.equipmentType);
        if (pivot == null)
        {
            Debug.LogError($"Cannot equip {item.itemType}: pivot for {data.equipmentType} not found!");
            return false;
        }

        if (data.worldPrefab == null)
        {
            Debug.LogError($"World prefab for {item.itemType} not found!");
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

        Debug.Log($"✅ Equipped: {item.itemType} in {data.equipmentType}");
        return true;
    }

    public void UnequipItem(EquipmentType slotType)
    {
        if (!equippedItems.ContainsKey(slotType)) return;

        GameObject item = equippedItems[slotType];
        if (item != null)
        {
            Destroy(item);
        }

        equippedItems.Remove(slotType);
        equippedTypes.Remove(slotType);

        Debug.Log($"Unequipped from {slotType}");
    }

    public void UnequipCurrent()
    {
        if (equippedItems.Count > 0)
        {
            var first = equippedItems.GetEnumerator();
            first.MoveNext();
            UnequipItem(first.Current.Key);
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
    }

    private void OnDestroy()
    {
        foreach (var item in equippedItems.Values)
        {
            if (item != null) Destroy(item);
        }
        equippedItems.Clear();
        equippedTypes.Clear();
    }
}