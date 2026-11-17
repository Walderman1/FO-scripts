using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public EquipmentType slotType;
    public bool isOccupied = false;

    [Inject] private EquipmentSystem _equipmentSystem;
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private TooltipManager _tooltipManager;

    private Image slotImage;
    private Color originalColor;
    private InventoryItemMarker currentItem;

    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_DELAY = 0.3f;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null) slotImage = gameObject.AddComponent<Image>();

        originalColor = slotImage.color;

        Color color = slotImage.color;
        color.a = 0.1f;
        slotImage.color = color;
        slotImage.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        InventoryItemMarker draggedItem = dropped.GetComponent<InventoryItemMarker>();
        if (draggedItem == null) return;

        var so = draggedItem.ItemSO;
        if (so == null || !so.isEquippable) return;
        if (so.equipmentType != slotType) return;

        if (isOccupied) UnequipCurrent();
        EquipItem(draggedItem);
    }

    public void EquipItem(InventoryItemMarker item)
    {
        item.DestroyDragClone();

        currentItem = item;

        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;

        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        item.enabled = false;
        isOccupied = true;

        InventorySlot originalSlot = item.GetComponentInParent<InventorySlot>();
        if (originalSlot != null)
        {
            originalSlot.isOccupied = false;
            originalSlot.UpdateSlotText();
        }

        _equipmentSystem?.EquipItem(item);
    }

    public void UnequipCurrent()
    {
        if (!isOccupied || currentItem == null) return;

        var so = currentItem.ItemSO;
        if (so == null) return;
        if (_inventoryUIManager == null) return;

        int space = _inventoryUIManager.GetFreeSpaceFor(so, 1);
        if (space < 1)
        {
            _tooltipManager?.ShowTooltip("Инвентарь полон", Input.mousePosition);
            return;
        }

        int added = _inventoryUIManager.AddItem(so, 1);
        if (added < 1) return;

        _equipmentSystem?.UnequipItem(slotType);

        currentItem.gameObject.SetActive(false);
        Destroy(currentItem.gameObject);
        isOccupied = false;
        currentItem = null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOccupied) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (currentItem != null)
                _radialMenuController?.OpenContextMenu(currentItem, Input.mousePosition);
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < DOUBLE_CLICK_DELAY) UnequipCurrent();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.3f;
            slotImage.color = color;
        }

        if (isOccupied && currentItem != null)
        {
            var so = currentItem.ItemSO;
            if (so != null)
            {
                string tooltip = $"<b>{so.itemName}</b>\n{so.description}";
                _tooltipManager?.ShowTooltip(tooltip, Input.mousePosition);
            }
        }
    }

    public InventoryItemMarker GetCurrentItem() => currentItem;

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.1f;
            slotImage.color = color;
        }

        _tooltipManager?.HideTooltip();
    }

    public bool IsEquipped() => isOccupied;
    public ItemSO GetEquippedItemSO() => currentItem != null ? currentItem.ItemSO : null;
}