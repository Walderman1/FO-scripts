using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class InventoryItemMarker : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    public ItemType itemType = ItemType.None;
    public int count = 1;
    public Transform originalSlotParent;

    public static bool IsAnyDragging = false;

    private TextMeshProUGUI countText;
    private CanvasGroup canvasGroup;
    private Transform originalParent;
    private GameObject dragVisual;
    private Canvas parentCanvas;
    private bool isDragging = false;
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_DELAY = 0.3f;

    private void Awake()
    {
        countText = GetComponent<TextMeshProUGUI>();
        if (countText == null) countText = GetComponentInChildren<TextMeshProUGUI>();

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        parentCanvas = GetComponentInParent<Canvas>();
        UpdateUI();
    }

    // ✅ Удаляем dragVisual при уничтожении объекта
    private void OnDestroy()
    {
        DestroyDragClone();
    }

    public void SetBlocksRaycasts(bool value)
    {
        if (canvasGroup != null)
            canvasGroup.blocksRaycasts = value;
    }

    public void DestroyDragClone()
    {
        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
        }
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
                    out Vector2 localPoint
                );

                rectTransform.anchoredPosition = localPoint;
                rectTransform.sizeDelta = new Vector2(80, 80);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
                rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                rectTransform.localScale = Vector3.one;
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
        ItemData data = ItemDatabase.Instance?.GetItemData(itemType);
        GameObject visual = null;

        if (data != null && data.uiPrefab != null)
        {
            visual = Instantiate(data.uiPrefab, parentCanvas.transform, false);
            visual.name = "DragVisual_" + data.itemName;
        }
        else
        {
            visual = new GameObject("DragVisual_Default");
            visual.transform.SetParent(parentCanvas.transform, false);

            RectTransform rt = visual.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(80, 80);

            Image img = visual.AddComponent<Image>();
            if (data != null && data.icon != null)
            {
                img.sprite = data.icon;
                img.color = Color.white;
            }
            else
            {
                img.color = new Color(0.5f, 0.5f, 0.5f, 0.9f);
            }
            img.raycastTarget = false;
        }

        if (visual == null) return null;

        MonoBehaviour[] scripts = visual.GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            script.enabled = false;
        }

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
        else
        {
            GameObject textObj = new GameObject("CountText");
            textObj.transform.SetParent(visual.transform, false);

            TextMeshProUGUI newText = textObj.AddComponent<TextMeshProUGUI>();
            newText.text = itemCount > 1 ? itemCount.ToString() : "";
            newText.fontSize = 24;
            newText.color = Color.white;
            newText.alignment = TextAlignmentOptions.BottomRight;
            newText.raycastTarget = false;

            RectTransform textRect = textObj.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0, 0);
            textRect.anchorMax = new Vector2(1, 1);
            textRect.offsetMin = new Vector2(0, 0);
            textRect.offsetMax = new Vector2(0, 0);
            textRect.pivot = new Vector2(0.5f, 0.5f);
        }

        Outline outline = visual.GetComponent<Outline>();
        if (outline == null)
        {
            outline = visual.AddComponent<Outline>();
            outline.effectColor = Color.black;
            outline.effectDistance = new Vector2(2, -2);
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
                    out Vector2 localPoint
                );

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
            transform.localScale = Vector3.one;
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
        InventoryUIManager uiManager = FindFirstObjectByType<InventoryUIManager>();
        if (uiManager == null || uiManager.inventoryGrid == null) return;

        foreach (Transform slotTransform in uiManager.inventoryGrid.transform)
        {
            InventorySlot slot = slotTransform.GetComponent<InventorySlot>();
            if (slot == null) continue;

            InventoryItemMarker marker = slot.GetComponentInChildren<InventoryItemMarker>();
            slot.isOccupied = marker != null;
            slot.UpdateSlotText();
        }

        uiManager.UpdateAllSlots();
    }

    public void OnDrop(PointerEventData eventData)
    {
        InventorySlot slot = GetComponentInParent<InventorySlot>();
        if (slot != null)
        {
            slot.OnDrop(eventData);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isDragging) return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        bool altPressed = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (shiftPressed && count > 1)
            {
                SplitStack(count / 2);
                return;
            }

            if (altPressed && count > 1)
            {
                SplitStack(1);
                return;
            }

            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < DOUBLE_CLICK_DELAY)
            {
                MenuManager.Instance?.OpenItemView(this);
            }
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            InventorySlot slot = GetComponentInParent<InventorySlot>();
            if (slot != null) slot.HandleRightClickFromItem();
        }
    }

    private void SplitStack(int splitCount)
    {
        if (splitCount <= 0 || splitCount >= count) return;

        InventoryUIManager uiManager = FindFirstObjectByType<InventoryUIManager>();
        if (uiManager == null || uiManager.inventoryGrid == null) return;

        Transform emptySlot = null;
        foreach (Transform slot in uiManager.inventoryGrid.transform)
        {
            InventoryItemMarker marker = slot.GetComponentInChildren<InventoryItemMarker>();
            if (marker == null)
            {
                emptySlot = slot;
                break;
            }
        }

        if (emptySlot == null)
        {
            Debug.Log("Нет свободных слотов для разделения!");
            return;
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(itemType);
        if (data != null && data.uiPrefab != null)
        {
            GameObject newItem = Instantiate(data.uiPrefab, emptySlot);
            newItem.transform.localPosition = Vector3.zero;
            newItem.transform.localScale = Vector3.one;

            InventoryItemMarker newMarker = newItem.GetComponent<InventoryItemMarker>();
            if (newMarker != null)
            {
                newMarker.itemType = itemType;
                newMarker.count = splitCount;
                newMarker.UpdateUI();
            }

            count -= splitCount;
            UpdateUI();

            InventorySlot originalSlot = GetComponentInParent<InventorySlot>();
            if (originalSlot != null)
            {
                originalSlot.UpdateSlotText();
                originalSlot.isOccupied = count > 0;
            }

            InventorySlot newSlot = emptySlot.GetComponent<InventorySlot>();
            if (newSlot != null)
            {
                newSlot.UpdateSlotText();
                newSlot.isOccupied = true;
            }

            if (count <= 0)
            {
                if (originalSlot != null)
                {
                    originalSlot.isOccupied = false;
                    originalSlot.UpdateSlotText();
                }
                Destroy(gameObject);
            }

            StartCoroutine(CleanAllSlotsDelayed());
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ItemData data = ItemDatabase.Instance?.GetItemData(itemType);
        if (data != null)
        {
            string tooltip = $"<b>{data.itemName}</b>\n{data.description}";
            TooltipManager.Instance?.ShowTooltip(tooltip, Input.mousePosition);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        TooltipManager.Instance?.HideTooltip();
    }

    public void AddCount(int amount)
    {
        count += amount;
        UpdateUI();
    }

    public void UpdateUI()
    {
        if (countText != null)
        {
            countText.text = count > 1 ? count.ToString() : "";
        }
    }

    public bool IsDragging()
    {
        return isDragging;
    }
}