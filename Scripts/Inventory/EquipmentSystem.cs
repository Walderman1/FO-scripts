using UnityEngine;
using System.Collections.Generic;
using VContainer;

public class EquipmentSystem : MonoBehaviour
{
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private TooltipManager _tooltipManager;
    [Inject] private EventManager _eventManager;
    [Inject] private ItemDatabase _itemDatabase;

    private Dictionary<EquipmentType, GameObject> equippedItems = new Dictionary<EquipmentType, GameObject>();
    private Dictionary<EquipmentType, ItemSO> equippedTypes = new Dictionary<EquipmentType, ItemSO>();
    private Dictionary<EquipmentType, Transform> pivotCache = new Dictionary<EquipmentType, Transform>();

    private void OnEnable()
    {
        var refs = GameReferences.Instance;
        if (refs != null) refs.OnSceneReferencesRefreshed += ClearPivotCache;
    }

    private void OnDisable()
    {
        var refs = GameReferences.Instance;
        if (refs != null) refs.OnSceneReferencesRefreshed -= ClearPivotCache;
    }

    private void ClearPivotCache() => pivotCache.Clear();

    private Transform FindPivotByTag(EquipmentType slotType)
    {
        if (pivotCache.ContainsKey(slotType)) return pivotCache[slotType];

        GameObject[] taggedObjects = GameObject.FindGameObjectsWithTag("EquipmentSlot");
        if (taggedObjects.Length == 0) return null;

        foreach (GameObject parentObj in taggedObjects)
        {
            foreach (Transform child in parentObj.transform)
            {
                string childName = child.name;
                Transform found = null;

                switch (slotType)
                {
                    case EquipmentType.Head: if (childName.Contains("Head")) found = child; break;
                    case EquipmentType.Face: if (childName.Contains("Face")) found = child; break;
                    case EquipmentType.Neck: if (childName.Contains("Neck")) found = child; break;
                    case EquipmentType.Chest: if (childName.Contains("Chest")) found = child; break;
                    case EquipmentType.Waist: if (childName.Contains("Waist")) found = child; break;
                    case EquipmentType.Legs: if (childName.Contains("Legs")) found = child; break;
                    case EquipmentType.Weapon: if (childName.Contains("Weapon")) found = child; break;
                    case EquipmentType.Offhand: if (childName.Contains("Offhand")) found = child; break;
                    case EquipmentType.Lantern: if (childName.Contains("Lantern")) found = child; break;
                }

                if (found != null)
                {
                    pivotCache[slotType] = found;
                    return found;
                }
            }
        }

        return null;
    }

    private Transform GetPivotForSlot(EquipmentType slotType) => FindPivotByTag(slotType);

    public EquipmentData ExportEquipment()
    {
        var data = new EquipmentData();
        var slots = FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Exclude);

        foreach (var slot in slots)
        {
            if (!slot.isOccupied) continue;

            ItemSO so = GetEquippedSO(slot.slotType);
            if (so == null) continue;

            var marker = slot.GetCurrentItem();
            data.entries.Add(new EquipmentEntry
            {
                slotType = (int)slot.slotType,
                itemId = so.id,
                count = marker != null ? marker.count : 1
            });
        }

        return data;
    }

    public void ImportEquipment(EquipmentData data)
    {
        if (_inventoryUIManager == null) return;

        var slots = FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Exclude);

        foreach (var slot in slots)
        {
            if (!slot.isOccupied) continue;
            var child = slot.GetCurrentItem();
            if (child != null)
            {
                child.gameObject.SetActive(false);
                Destroy(child.gameObject);
            }
            slot.isOccupied = false;
        }

        foreach (EquipmentType type in System.Enum.GetValues(typeof(EquipmentType)))
        {
            if (type == EquipmentType.None) continue;
            UnequipItem(type);
        }

        if (data?.entries == null) return;

        foreach (var entry in data.entries)
        {
            var so = _itemDatabase?.GetItem(entry.itemId);
            if (so != null) _inventoryUIManager.AddItem(so, entry.count);
        }

        foreach (var entry in data.entries)
        {
            var so = _itemDatabase?.GetItem(entry.itemId);
            if (so == null) continue;

            EquipmentType type = (EquipmentType)entry.slotType;
            EquipmentSlot targetSlot = null;
            foreach (var s in slots) if (s.slotType == type) { targetSlot = s; break; }
            if (targetSlot == null) continue;

            InventoryItemMarker marker = FindInventoryMarker(so);
            if (marker == null) continue;

            if (marker.count > entry.count)
            {
                marker.count -= entry.count;
                marker.UpdateUI();

                var invSlot = marker.GetComponentInParent<InventorySlot>();
                if (invSlot != null) invSlot.UpdateSlotText();

                var newMarker = CreateInventoryMarker(so, entry.count);
                if (newMarker != null) targetSlot.EquipItem(newMarker);
            }
            else targetSlot.EquipItem(marker);
        }
    }

    private InventoryItemMarker FindInventoryMarker(ItemSO so)
    {
        if (_inventoryUIManager == null || _inventoryUIManager.inventoryGrid == null) return null;

        foreach (Transform slot in _inventoryUIManager.inventoryGrid.transform)
        {
            var marker = slot.GetComponentInChildren<InventoryItemMarker>();
            if (marker != null && marker.ItemSO == so) return marker;
        }
        return null;
    }

    private InventoryItemMarker CreateInventoryMarker(ItemSO so, int count)
    {
        if (so == null || so.uiPrefab == null) return null;

        var go = Instantiate(so.uiPrefab);
        SceneInjector.InjectInto(go);

        var marker = go.GetComponent<InventoryItemMarker>();
        if (marker == null) marker = go.AddComponent<InventoryItemMarker>();

        marker.itemSO = so;
        marker.count = count;
        marker.UpdateUI();

        return marker;
    }

    public bool EquipItem(InventoryItemMarker item)
    {
        if (item == null) return false;

        var so = item.ItemSO;
        if (so == null || !so.isEquippable) return false;

        bool willSwap = equippedItems.ContainsKey(so.equipmentType) && equippedItems[so.equipmentType] != null;
        ItemSO oldType = willSwap && equippedTypes.TryGetValue(so.equipmentType, out var t) ? t : null;

        if (willSwap && oldType != null)
        {
            if (_inventoryUIManager != null)
            {
                int space = _inventoryUIManager.GetFreeSpaceFor(oldType, 1);
                if (space < 1)
                {
                    _tooltipManager?.ShowTooltip("Инвентарь полон", Input.mousePosition);
                    return false;
                }
            }
        }

        if (willSwap)
        {
            UnequipItem(so.equipmentType);
            if (oldType != null) _inventoryUIManager?.AddItem(oldType, 1);
        }

        Transform pivot = GetPivotForSlot(so.equipmentType);
        if (pivot == null) return false;
        if (so.worldPrefab == null) return false;

        GameObject newItem = Instantiate(so.worldPrefab, pivot);
        newItem.transform.localPosition = Vector3.zero;
        newItem.transform.localRotation = Quaternion.identity;

        DisableItemInteractions(newItem);

        equippedItems[so.equipmentType] = newItem;
        equippedTypes[so.equipmentType] = so;

        _eventManager?.TriggerEvent(
            EventTriggerType.EquipItem,
            new EventContext().WithItem(so).WithEquipmentType(so.equipmentType));

        return true;
    }

    public void UnequipItem(EquipmentType slotType)
    {
        if (!equippedItems.ContainsKey(slotType)) return;

        GameObject item = equippedItems[slotType];
        if (item != null) Destroy(item);

        equippedItems.Remove(slotType);
        equippedTypes.Remove(slotType);
    }

    public void UnequipCurrent()
    {
        if (equippedItems.Count == 0) return;
        var first = equippedItems.GetEnumerator();
        first.MoveNext();
        UnequipItem(first.Current.Key);
    }

    public bool IsEquipped(ItemSO item)
    {
        if (item == null) return false;
        return equippedTypes.ContainsValue(item);
    }

    public bool IsEquipped(EquipmentType slotType) =>
        equippedItems.ContainsKey(slotType) && equippedItems[slotType] != null;

    public ItemSO GetEquippedSO(EquipmentType slotType) =>
        equippedTypes.ContainsKey(slotType) ? equippedTypes[slotType] : null;

    public GameObject GetEquippedItem(EquipmentType slotType) =>
        equippedItems.ContainsKey(slotType) ? equippedItems[slotType] : null;

    private void DisableItemInteractions(GameObject item)
    {
        foreach (var p in item.GetComponentsInChildren<PickupItem>(true)) p.enabled = false;
        foreach (var col in item.GetComponentsInChildren<Collider2D>(true)) col.enabled = false;
        foreach (var col in item.GetComponentsInChildren<Collider>(true)) col.enabled = false;
        foreach (var sr in item.GetComponentsInChildren<SpriteRenderer>(true)) sr.sortingOrder = 5;

        foreach (var rb in item.GetComponentsInChildren<Rigidbody2D>(true))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.simulated = false;
        }

        foreach (var rb in item.GetComponentsInChildren<Rigidbody>(true))
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }
    }

    public void RefreshEquipmentVisuals()
    {
        foreach (var kvp in equippedItems)
        {
            if (kvp.Value == null) continue;
            Transform pivot = GetPivotForSlot(kvp.Key);
            if (pivot == null) continue;
            kvp.Value.transform.SetParent(pivot);
            kvp.Value.transform.localPosition = Vector3.zero;
            kvp.Value.transform.localRotation = Quaternion.identity;
        }
    }

    private void OnDestroy()
    {
        foreach (var item in equippedItems.Values)
            if (item != null) Destroy(item);
        equippedItems.Clear();
        equippedTypes.Clear();
    }
}