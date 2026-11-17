using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using VContainer;

public class InventoryItemMarker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Tooltip("Ссылка на ItemSO. Приоритетна.")]
    public ItemSO itemSO;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    public int count = 1;
    public Transform originalSlotParent;

    public static bool IsAnyDragging = false;

    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private TooltipManager _tooltipManager;
    [Inject] private ItemViewPanel _itemViewPanel;
    [Inject] private CraftingUI _craftingUI;

    private TextMeshProUGUI countText;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private GameObject dragVisual;
    private Canvas parentCanvas;
    private bool isDragging = false;
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_DELAY = 0.3f;

    public ItemSO ItemSO
    {
        get
        {
            if (itemSO != null) return itemSO;

#pragma warning disable CS0618
            if (itemType != ItemType.None)
                itemSO = _itemDatabase?.GetItemByLegacyType(itemType);
#pragma warning restore CS0618

            return itemSO;
        }
    }

    public string ItemId => ItemSO != null ? ItemSO.id : "";

    private void Awake()
    {
        countText = GetComponent<TextMeshProUGUI>();
        if (countText == null) countText = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        parentCanvas = GetComponentInParent<Canvas>();
        UpdateUI();
    }

    private void OnDestroy()
    {
        DestroyDragClone();
    }

    public void Initialize(ItemSO so, int cnt = 1)
    {
        itemSO = so;
        count = cnt;
        UpdateUI();
    }

    public void SetBlocksRaycasts(bool value)
    {
        if (canvasGroup != null) canvasGroup.blocksRaycasts = value;
    }

    public void DestroyDragClone()
    {
        if (dragVisual != null) { Destroy(dragVisual); dragVisual = null; }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        isDragging = true;
        IsAnyDragging = true;

        originalSlotParent = transform.parent;
        originalParent = transform.parent;

        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;

        dragVisual = CreateDragVisual(count);

        if (dragVisual != null)
        {
            dragVisual.transform.SetParent(parentCanvas.transform, false);
            dragVisual.transform.SetAsLastSibling();

            RectTransform rectTransform = dragVisual.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    Input.mousePosition,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint);

                rectTransform.anchoredPosition = localPoint;
                rectTransform.sizeDelta = new Vector2(80, 80);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            }

            CanvasGroup cg = dragVisual.GetComponent<CanvasGroup>();
            if (cg != null)
            {
                cg.alpha = 0.9f;
                cg.blocksRaycasts = false;
                cg.interactable = false;
            }
        }
    }

    private GameObject CreateDragVisual(int itemCount)
    {
        var so = ItemSO;
        GameObject visual;

        if (so != null && so.uiPrefab != null)
        {
            visual = Instantiate(so.uiPrefab, parentCanvas.transform, false);
            visual.name = "DragVisual_" + so.itemName;
        }
        else
        {
            visual = new GameObject("DragVisual_Default");
            visual.transform.SetParent(parentCanvas.transform, false);

            RectTransform rt = visual.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            Image img = visual.AddComponent<Image>();
            if (so != null && so.icon != null)
            {
                img.sprite = so.icon;
                img.color = Color.white;
            }
            else img.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);
            img.raycastTarget = false;
        }

        if (visual == null) return null;

        MonoBehaviour[] scripts = visual.GetComponents<MonoBehaviour>();
        foreach (var script in scripts) script.enabled = false;

        CanvasGroup cg = visual.GetComponent<CanvasGroup>();
        if (cg == null) cg = visual.AddComponent<CanvasGroup>();
        cg.alpha = 0.9f;
        cg.blocksRaycasts = false;
        cg.interactable = false;

        TextMeshProUGUI tmpText = visual.GetComponentInChildren<TextMeshProUGUI>();
        if (tmpText != null)
        {
            tmpText.text = itemCount > 1 ? itemCount.ToString() : "";
            tmpText.raycastTarget = false;
        }

        return visual;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisual != null)
        {
            RectTransform rectTransform = dragVisual.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    parentCanvas.transform as RectTransform,
                    Input.mousePosition,
                    parentCanvas.worldCamera,
                    out Vector2 localPoint);

                rectTransform.anchoredPosition = localPoint;
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false;
        IsAnyDragging = false;

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        DestroyDragClone();

        GameObject target = eventData.pointerEnter;
        InventorySlot targetSlot = null;

        if (target != null)
        {
            targetSlot = target.GetComponent<InventorySlot>();
            if (targetSlot == null) targetSlot = target.GetComponentInParent<InventorySlot>();
        }

        if (targetSlot != null)
        {
            StartCoroutine(CleanAllSlotsDelayed());
            return;
        }

        InventorySlot originalSlot = originalParent != null ? originalParent.GetComponent<InventorySlot>() : null;
        if (originalSlot != null)
        {
            transform.SetParent(originalParent);
            transform.localPosition = Vector3.zero;
            originalSlotParent = originalParent;

            originalSlot.isOccupied = true;
            originalSlot.UpdateSlotText();
        }

        StartCoroutine(CleanAllSlotsDelayed());
    }

    private IEnumerator CleanAllSlotsDelayed()
    {
        yield return new WaitForEndOfFrame();
        ForceCleanAllSlots();
    }

    private void ForceCleanAllSlots()
    {
        if (_inventoryUIManager == null || _inventoryUIManager.inventoryGrid == null) return;

        foreach (Transform slotTransform in _inventoryUIManager.inventoryGrid.transform)
        {
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            if (slot == null) continue;

            InventoryItemMarker marker = slot.GetComponentInChildren<InventoryItemMarker>();
            slot.isOccupied = marker != null;
            slot.UpdateSlotText();
        }

        _inventoryUIManager.UpdateAllSlots();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot slot = GetComponentInParent<InventorySlot>();
        if (slot != null) slot.OnDrop(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        if (_craftingUI != null && _craftingUI.IsOpen && eventData.button == PointerEventData.InputButton.Left)
        {
            _craftingUI.TryAddItemToCraft(ItemId, 1);
            return;
        }

        bool shiftPressed = IsSplitHalfPressed();
        bool altPressed = IsSplitOnePressed();

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (shiftPressed && count > 1) { SplitStack(count / 2); return; }
            if (altPressed && count > 1) { SplitStack(1); return; }

            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < DOUBLE_CLICK_DELAY)
                _radialMenuController?.OpenItemView(this);

            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventorySlot slot = GetComponentInParent<InventorySlot>();
            if (slot != null) slot.HandleRightClickFromItem();
        }
    }

    private bool IsSplitHalfPressed()
    {
        var cfg = GameReferences.Instance?.InputManager?.Config;
        if (cfg != null) return cfg.SplitHalfPressed();
        return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
    }

    private bool IsSplitOnePressed()
    {
        var cfg = GameReferences.Instance?.InputManager?.Config;
        if (cfg != null) return cfg.SplitOnePressed();
        return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    }

    private void SplitStack(int splitCount)
    {
        if (splitCount <= 0 || splitCount >= count) return;
        if (_inventoryUIManager == null) return;

        var so = ItemSO;
        if (so == null || !so.canStack) return;

        var targetStack = _inventoryUIManager.FindStackWithSpace(so, this);
        if (targetStack != null)
        {
            int space = so.maxStack - targetStack.count;
            int actual = Mathf.Min(splitCount, space);
            if (actual > 0)
            {
                targetStack.AddCount(actual);
                var targetSlot = targetStack.GetComponentInParent<InventorySlot>();
                if (targetSlot != null) targetSlot.UpdateSlotText();

                count -= actual;
                UpdateUI();

                var originalSlot = GetComponentInParent<InventorySlot>();
                if (originalSlot != null)
                {
                    originalSlot.UpdateSlotText();
                    originalSlot.isOccupied = count > 0;
                }

                if (count <= 0) DestroySelf();

                _inventoryUIManager.UpdateAllSlots();
                return;
            }
        }

        var emptySlot = _inventoryUIManager.FindEmptySlot();
        if (emptySlot != null)
            _inventoryUIManager.SplitStackToEmpty(this, splitCount, emptySlot);
    }

    private void DestroySelf()
    {
        var slot = GetComponentInParent<InventorySlot>();
        if (slot != null)
        {
            slot.isOccupied = false;
            slot.UpdateSlotText();
        }
        gameObject.SetActive(false);
        Destroy(gameObject);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        var so = ItemSO;
        if (so != null)
        {
            string tooltip = $"<b>{so.itemName}</b>\n{so.description}";
            _tooltipManager?.ShowTooltip(tooltip, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _tooltipManager?.HideTooltip();
    }

    public void AddCount(int amount) { count += amount; UpdateUI(); }

    public void UpdateUI()
    {
        if (countText != null)
            countText.text = count > 1 ? count.ToString() : "";
    }

    public bool IsDragging() => isDragging;
}