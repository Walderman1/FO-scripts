using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using VContainer;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public bool isOccupied = false;

    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private InventoryUIManager _inventoryUIManager;

    private Image slotImage;
    private bool isProcessingRightClick = false;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null) slotImage = gameObject.AddComponent<Image>();

        Color color = slotImage.color;
        color.a = 0.1f;
        slotImage.color = color;
        slotImage.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        var draggedItem = dropped.GetComponent<InventoryItemMarker>();
        if (draggedItem == null) return;
        if (_inventoryUIManager == null) return;

        var existingItem = GetComponentInChildren<InventoryItemMarker>();
        if (existingItem == null)
        {
            _inventoryUIManager.MoveToEmptySlot(draggedItem, this);
            return;
        }

        if (existingItem == draggedItem) return;

        if (_inventoryUIManager.TryMergeStacks(draggedItem, existingItem))
            return;

        _inventoryUIManager.SwapItems(existingItem, draggedItem);
    }

    public void UpdateSlotText()
    {
        var item = GetComponentInChildren<InventoryItemMarker>();
        if (item != null) item.UpdateUI();

        TMP_Text slotText = FindOwnCountText();
        if (slotText == null) return;

        slotText.text = (item != null && item.count > 1) ? item.count.ToString() : "";
    }

    private TMP_Text FindOwnCountText()
    {
        foreach (var t in GetComponentsInChildren<TMP_Text>(true))
            if (t.GetComponentInParent<InventoryItemMarker>() == null) return t;
        return null;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isProcessingRightClick) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (_radialMenuController != null && _radialMenuController.IsContextMenuOpen())
            {
                _radialMenuController.CloseContextMenu();
                return;
            }
        }

        if (eventData.button == PointerEventData.InputButton.Right)
            OnRightClick();
    }

    public void HandleRightClickFromItem()
    {
        if (isProcessingRightClick) return;
        OnRightClick();
    }

    private void OnRightClick()
    {
        isProcessingRightClick = true;

        if (_radialMenuController != null && _radialMenuController.IsContextMenuOpen())
        {
            _radialMenuController.CloseContextMenu();
            isProcessingRightClick = false;
            return;
        }

        InventoryItemMarker item = GetComponentInChildren<InventoryItemMarker>();
        if (item != null) _radialMenuController?.OpenContextMenu(item, Input.mousePosition);

        isProcessingRightClick = false;
    }
}