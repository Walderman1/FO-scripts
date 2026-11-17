using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class InventoryUIManager : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] public GameObject inventoryGrid;
    [SerializeField] private GameObject itemSlotPrefab;

    [Header("Inventory Settings")]
    [SerializeField] private int inventorySize = 30;
    [SerializeField] private float fadeDuration = 0.3f;

    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private InventoryButtonPanel _inventoryButtonPanel;

    private CanvasGroup canvasGroup;
    private bool isInventoryOpen = false;
    private Coroutine fadeCoroutine = null;
    private bool isAnimating = false;

    public System.Action<InventorySortMode> OnSortModeChanged;
    public System.Action<bool> OnVisibilityChanged;

    public InventorySortMode CurrentSortMode => currentSortMode;
    private InventorySortMode currentSortMode = InventorySortMode.Category;

    private static T SceneResolve<T>() where T : class
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }
    private CraftingUI Crafting => SceneResolve<CraftingUI>();
    private MagicSpellPanel SpellPanel => SceneResolve<MagicSpellPanel>();

    private RadialMenuController RadialMenuCtrl => SceneResolve<RadialMenuController>();

    private void Awake()
    {
        if (inventoryGrid == null)
        {
            GridLayoutGroup grid = GetComponentInChildren<GridLayoutGroup>();
            if (grid != null) inventoryGrid = grid.gameObject;
        }

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isInventoryOpen = false;
    }

    private void Start()
    {
        ClearInventory();
        CreateInventorySlots();
    }

    public InventoryData ExportInventory()
    {
        var data = new InventoryData();
        if (inventoryGrid == null) return data;

        foreach (Transform slot in inventoryGrid.transform)
        {
            var marker = slot.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null) continue;

            string id = marker.ItemId;
            if (string.IsNullOrEmpty(id)) continue;

            data.entries.Add(new InventoryEntry { itemId = id, count = marker.count });
        }

        return data;
    }

    public void ImportInventory(InventoryData data)
    {
        if (inventoryGrid == null) return;

        foreach (Transform slot in inventoryGrid.transform)
        {
            var marker = slot.GetComponentInChildren<InventoryItemMarker>();
            if (marker != null)
            {
                marker.gameObject.SetActive(false);
                Destroy(marker.gameObject);
            }

            var invSlot = slot.GetComponent<InventorySlot>();
            if (invSlot != null)
            {
                invSlot.isOccupied = false;
                invSlot.UpdateSlotText();
            }
        }

        if (data == null || data.entries == null) return;

        foreach (var entry in data.entries)
        {
            var so = _itemDatabase?.GetItem(entry.itemId);
            if (so != null) AddItem(so, entry.count);
        }
    }

    public void OpenInventory()
    {
        if (!isInventoryOpen && !isAnimating)
        {
            UICoordinator.BeforeOpen(UIKind.Inventory);
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f, fadeDuration));
        }
    }

    public void CloseInventory()
    {
        if (isInventoryOpen && !isAnimating)
        {
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f, fadeDuration));
            RadialMenuCtrl?.CloseAllMenus();
        }
    }

    public void ToggleInventory()
    {
        if (isAnimating) return;
        if (isInventoryOpen) CloseInventory();
        else OpenInventory();
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha, float duration)
    {
        isAnimating = true;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha >= 1f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
            _inventoryButtonPanel?.SetVisible(true);
        }

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            _inventoryButtonPanel?.SetVisible(false);
            Crafting?.SetVisible(false);
            SpellPanel?.SetVisible(false);
        }

        isInventoryOpen = (targetAlpha >= 1f);
        isAnimating = false;
        fadeCoroutine = null;
        OnVisibilityChanged?.Invoke(isInventoryOpen);
    }

    public bool IsInventoryOpen => isInventoryOpen;

    public bool IsPointerOverPanel(Vector2 screenPos)
    {
        if (!isInventoryOpen) return false;
        RectTransform rt = transform as RectTransform;
        if (rt == null) return false;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, null);
    }

    public void UpdateAllSlots()
    {
        if (inventoryGrid == null) return;

        InventorySlot[] slots = inventoryGrid.GetComponentsInChildren<InventorySlot>();
        foreach (InventorySlot slot in slots)
        {
            InventoryItemMarker marker = slot.GetComponentInChildren<InventoryItemMarker>();
            slot.isOccupied = marker != null;
            slot.UpdateSlotText();
        }
    }

    private void ClearInventory()
    {
        if (inventoryGrid == null) return;
        foreach (Transform child in inventoryGrid.transform)
            Destroy(child.gameObject);
    }

    private void CreateInventorySlots()
    {
        if (inventoryGrid == null || itemSlotPrefab == null) return;

        for (int i = 0; i < inventorySize; i++)
        {
            GameObject newSlot = Instantiate(itemSlotPrefab, inventoryGrid.transform);
            newSlot.name = $"Slot_{i}";
            if (newSlot.GetComponent<InventorySlot>() == null)
                newSlot.AddComponent<InventorySlot>();
            SceneInjector.InjectInto(newSlot);
        }
    }

    public void CycleSortMode()
    {
        int total = System.Enum.GetValues(typeof(InventorySortMode)).Length;
        int next = ((int)currentSortMode + 1) % total;
        SortInventory((InventorySortMode)next);
    }

    public void SetSortMode(InventorySortMode mode) => SortInventory(mode);

    public string GetSortModeName()
    {
        switch (currentSortMode)
        {
            case InventorySortMode.Category: return "по категории";
            case InventorySortMode.Name: return "по названию";
            case InventorySortMode.Count: return "по количеству";
            case InventorySortMode.Priority: return "по приоритету";
            default: return currentSortMode.ToString();
        }
    }

    public void SortInventory(InventorySortMode mode)
    {
        if (inventoryGrid == null) return;

        currentSortMode = mode;

        var aggregated = new Dictionary<ItemSO, int>();
        var toClear = new List<GameObject>();

        foreach (Transform child in inventoryGrid.transform)
        {
            var marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null || marker.ItemSO == null) continue;

            if (aggregated.TryGetValue(marker.ItemSO, out int existing))
                aggregated[marker.ItemSO] = existing + marker.count;
            else
                aggregated[marker.ItemSO] = marker.count;

            toClear.Add(marker.gameObject);
        }

        foreach (var go in toClear)
        {
            go.SetActive(false);
            Destroy(go);
        }

        foreach (Transform child in inventoryGrid.transform)
        {
            var invSlot = child.GetComponent<InventorySlot>();
            if (invSlot != null)
            {
                invSlot.isOccupied = false;
                invSlot.UpdateSlotText();
            }
        }

        if (aggregated.Count == 0) return;

        var sorted = new List<KeyValuePair<ItemSO, int>>(aggregated);
        sorted.Sort((a, b) => CompareItems(a.Key, b.Key, a.Value, b.Value, mode));

        foreach (var kvp in sorted)
            AddItem(kvp.Key, kvp.Value);

        UpdateAllSlots();
        OnSortModeChanged?.Invoke(mode);
    }

    private int CompareItems(ItemSO a, ItemSO b, int countA, int countB, InventorySortMode mode)
    {
        switch (mode)
        {
            case InventorySortMode.Category:
                int catCmp = GetCategoryOrder(a.category).CompareTo(GetCategoryOrder(b.category));
                if (catCmp != 0) return catCmp;
                return string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal);

            case InventorySortMode.Name:
                return string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal);

            case InventorySortMode.Count:
                int cntCmp = countB.CompareTo(countA);
                if (cntCmp != 0) return cntCmp;
                return string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal);

            case InventorySortMode.Priority:
                int priCmp = GetItemPriority(a).CompareTo(GetItemPriority(b));
                if (priCmp != 0) return priCmp;
                return string.Compare(a.itemName, b.itemName, System.StringComparison.Ordinal);
        }

        return 0;
    }

    private int GetCategoryOrder(ItemCategory category)
    {
        if (category == ItemCategory.None) return 999;
        return (int)category;
    }

    private int GetItemPriority(ItemSO so)
    {
        if (so.isUsable) return 0;
        if (so.isEquippable) return 1;
        if (so.category == ItemCategory.Quest) return 2;
        return 3;
    }

    public int AddItem(string id, int amount = 1)
    {
        var so = _itemDatabase?.GetItem(id);
        if (so == null)
        {
            Logger.LogWarning(LogModule.Inventory, $"AddItem: id '{id}' не найден");
            return 0;
        }
        return AddItem(so, amount);
    }

    public int AddItem(ItemSO item, int amount = 1)
    {
        if (item == null || amount <= 0) return 0;
        if (item.uiPrefab == null) return 0;
        if (inventoryGrid == null) return 0;

        int remaining = amount;
        int added = 0;

        if (item.canStack)
        {
            foreach (Transform child in inventoryGrid.transform)
            {
                if (remaining <= 0) break;

                var marker = child.GetComponentInChildren<InventoryItemMarker>();
                if (marker == null || marker.ItemSO != item || marker.count >= item.maxStack) continue;

                int space = item.maxStack - marker.count;
                int toAdd = Mathf.Min(space, remaining);
                marker.AddCount(toAdd);

                var slot = child.GetComponent<InventorySlot>();
                if (slot != null) slot.UpdateSlotText();

                remaining -= toAdd;
                added += toAdd;
            }
        }

        while (remaining > 0)
        {
            Transform emptySlot = FindEmptySlotTransform();
            if (emptySlot == null) break;

            int stackSize = item.canStack ? Mathf.Min(item.maxStack, remaining) : 1;
            if (!CreateStackInSlot(item, stackSize, emptySlot)) break;

            remaining -= stackSize;
            added += stackSize;
        }

        return added;
    }

    public bool TryMergeStacks(InventoryItemMarker source, InventoryItemMarker target)
    {
        if (source == null || target == null) return false;
        if (source == target) return false;

        var srcSO = source.ItemSO;
        var dstSO = target.ItemSO;
        if (srcSO == null || dstSO == null) return false;
        if (srcSO != dstSO) return false;
        if (!srcSO.canStack) return false;

        int total = target.count + source.count;
        if (total <= srcSO.maxStack)
        {
            target.count = total;
            target.UpdateUI();

            var targetSlot = target.GetComponentInParent<InventorySlot>();
            if (targetSlot != null) targetSlot.UpdateSlotText();

            source.DestroyDragClone();
            source.gameObject.SetActive(false);
            Destroy(source.gameObject);

            UpdateAllSlots();
            return true;
        }

        int space = srcSO.maxStack - target.count;
        if (space <= 0) return false;

        target.count = srcSO.maxStack;
        source.count -= space;

        target.UpdateUI();
        source.UpdateUI();

        var targetSlot2 = target.GetComponentInParent<InventorySlot>();
        if (targetSlot2 != null) targetSlot2.UpdateSlotText();

        var sourceSlot = source.GetComponentInParent<InventorySlot>();
        if (sourceSlot != null) sourceSlot.UpdateSlotText();

        UpdateAllSlots();
        return true;
    }

    public bool SwapItems(InventoryItemMarker first, InventoryItemMarker second)
    {
        if (first == null || second == null) return false;
        if (first == second) return false;

        Transform firstSlot = first.transform.parent;
        Transform secondSlot = second.transform.parent;
        if (firstSlot == null || secondSlot == null) return false;

        first.SetBlocksRaycasts(false);
        second.SetBlocksRaycasts(false);

        first.transform.SetParent(secondSlot, false);
        first.transform.localPosition = Vector3.zero;
        first.originalSlotParent = secondSlot;

        second.transform.SetParent(firstSlot, false);
        second.transform.localPosition = Vector3.zero;
        second.originalSlotParent = firstSlot;

        first.SetBlocksRaycasts(true);
        second.SetBlocksRaycasts(true);

        UpdateAllSlots();
        return true;
    }

    public bool MoveToEmptySlot(InventoryItemMarker marker, InventorySlot targetSlot)
    {
        if (marker == null || targetSlot == null) return false;
        if (targetSlot.GetComponentInChildren<InventoryItemMarker>() != null) return false;

        marker.transform.SetParent(targetSlot.transform, false);
        marker.transform.localPosition = Vector3.zero;
        marker.originalSlotParent = targetSlot.transform;

        targetSlot.isOccupied = true;
        targetSlot.UpdateSlotText();

        UpdateAllSlots();
        return true;
    }

    public bool SplitStackToEmpty(InventoryItemMarker source, int splitCount, InventorySlot targetSlot)
    {
        if (source == null || targetSlot == null) return false;
        if (splitCount <= 0 || splitCount >= source.count) return false;

        var so = source.ItemSO;
        if (so == null || !so.canStack) return false;
        if (so.uiPrefab == null) return false;
        if (targetSlot.GetComponentInChildren<InventoryItemMarker>() != null) return false;

        GameObject newItem = Instantiate(so.uiPrefab, targetSlot.transform);
        newItem.transform.localPosition = Vector3.zero;

        var newMarker = newItem.GetComponent<InventoryItemMarker>();
        if (newMarker == null)
        {
            Destroy(newItem);
            return false;
        }

        newMarker.itemSO = so;
        newMarker.count = splitCount;
        newMarker.UpdateUI();

        source.count -= splitCount;
        source.UpdateUI();

        var sourceSlot = source.GetComponentInParent<InventorySlot>();
        if (sourceSlot != null)
        {
            sourceSlot.UpdateSlotText();
            sourceSlot.isOccupied = source.count > 0;
        }

        targetSlot.UpdateSlotText();
        targetSlot.isOccupied = true;

        SceneInjector.InjectInto(newItem);

        if (source.count <= 0)
        {
            if (sourceSlot != null) sourceSlot.isOccupied = false;
            source.gameObject.SetActive(false);
            Destroy(source.gameObject);
        }

        UpdateAllSlots();
        return true;
    }

    public InventorySlot FindEmptySlot()
    {
        if (inventoryGrid == null) return null;
        foreach (Transform child in inventoryGrid.transform)
        {
            if (child.GetComponentInChildren<InventoryItemMarker>() == null)
                return child.GetComponent<InventorySlot>();
        }
        return null;
    }

    public InventoryItemMarker FindStackWithSpace(ItemSO so, InventoryItemMarker except = null)
    {
        if (so == null || inventoryGrid == null) return null;

        foreach (Transform child in inventoryGrid.transform)
        {
            var marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null || marker == except) continue;
            if (marker.ItemSO != so) continue;
            if (marker.count < so.maxStack) return marker;
        }
        return null;
    }

    private Transform FindEmptySlotTransform()
    {
        foreach (Transform child in inventoryGrid.transform)
        {
            if (child.GetComponentInChildren<InventoryItemMarker>() == null) return child;
        }
        return null;
    }

    private bool CreateStackInSlot(ItemSO item, int count, Transform slotTransform)
    {
        if (item.uiPrefab == null || slotTransform == null) return false;

        GameObject newItem = Instantiate(item.uiPrefab, slotTransform);
        newItem.transform.localPosition = Vector3.zero;

        var marker = newItem.GetComponent<InventoryItemMarker>();
        if (marker != null)
        {
            marker.itemSO = item;
            marker.count = count;
            marker.UpdateUI();
        }

        var slot = slotTransform.GetComponent<InventorySlot>();
        if (slot != null) slot.UpdateSlotText();

        SceneInjector.InjectInto(newItem);
        return true;
    }

    private bool AddToEmptySlotBySO(ItemSO item)
    {
        if (inventoryGrid == null || item == null || item.uiPrefab == null) return false;

        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null)
            {
                GameObject newItem = Instantiate(item.uiPrefab, child);
                newItem.transform.localPosition = Vector3.zero;

                InventoryItemMarker newMarker = newItem.GetComponent<InventoryItemMarker>();
                if (newMarker != null)
                {
                    newMarker.itemSO = item;
                    newMarker.count = 1;
                    newMarker.UpdateUI();
                }

                var slot = child.GetComponent<InventorySlot>();
                if (slot != null) slot.UpdateSlotText();

                SceneInjector.InjectInto(newItem);
                return true;
            }
        }
        return false;
    }

    public int GetItemCount(string id)
    {
        var so = _itemDatabase?.GetItem(id);
        return so != null ? GetItemCount(so) : 0;
    }

    public int GetItemCount(ItemSO item)
    {
        if (item == null || inventoryGrid == null) return 0;

        int total = 0;
        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker != null && marker.ItemSO == item)
                total += marker.count;
        }
        return total;
    }

    public bool RemoveItem(string id, int amount)
    {
        var so = _itemDatabase?.GetItem(id);
        if (so == null) return false;
        return RemoveItem(so, amount);
    }

    public bool RemoveItem(ItemSO item, int amount)
    {
        if (item == null || amount <= 0) return amount <= 0;
        if (inventoryGrid == null) return false;

        int remaining = amount;

        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null || marker.ItemSO != item) continue;

            int toRemove = Mathf.Min(marker.count, remaining);
            marker.count -= toRemove;
            remaining -= toRemove;

            var slot = child.GetComponent<InventorySlot>();
            if (marker.count <= 0)
            {
                marker.gameObject.SetActive(false);
                Destroy(marker.gameObject);
                if (slot != null) { slot.isOccupied = false; slot.UpdateSlotText(); }
            }
            else
            {
                marker.UpdateUI();
                if (slot != null) slot.UpdateSlotText();
            }

            if (remaining <= 0) break;
        }

        UpdateAllSlots();
        return remaining <= 0;
    }

    public int GetFreeSpaceFor(string id, int maxToCheck)
    {
        var so = _itemDatabase?.GetItem(id);
        return GetFreeSpaceFor(so, maxToCheck);
    }

    public int GetFreeSpaceFor(ItemSO item, int maxToCheck)
    {
        if (inventoryGrid == null || maxToCheck <= 0 || item == null) return 0;

        int freeSpace = 0;

        if (item.canStack)
        {
            foreach (Transform child in inventoryGrid.transform)
            {
                InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
                if (marker != null && marker.ItemSO == item && marker.count < item.maxStack)
                {
                    freeSpace += item.maxStack - marker.count;
                    if (freeSpace >= maxToCheck) return maxToCheck;
                }
            }
        }

        int emptySlots = 0;
        foreach (Transform child in inventoryGrid.transform)
        {
            InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null) emptySlots++;
        }
        freeSpace += emptySlots * item.maxStack;

        return Mathf.Min(freeSpace, maxToCheck);
    }
}