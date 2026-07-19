using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventorySlot : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    public bool isOccupied = false;
    private Image slotImage;
    private bool isProcessingRightClick = false;
    private GameObject equipmentVisual;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
            slotImage = gameObject.AddComponent<Image>();

        Color color = slotImage.color;
        color.a = 0.1f;
        slotImage.color = color;
        slotImage.raycastTarget = true;

        FindEquipmentVisual();
    }

    private void FindEquipmentVisual()
    {
        // Ищем дочерний объект по имени
        Transform child = transform.Find("EquipmentVisual");
        if (child != null)
        {
            equipmentVisual = child.gameObject;
            equipmentVisual.SetActive(false);
            return;
        }

        // Создаем новый визуальный элемент
        GameObject newVisual = new GameObject("EquipmentVisual");
        newVisual.transform.SetParent(transform);
        newVisual.transform.localPosition = Vector3.zero;
        newVisual.transform.localScale = Vector3.one;

        Image img = newVisual.AddComponent<Image>();
        img.color = new Color(1, 1, 1, 0.5f);
        img.raycastTarget = false;

        Outline outline = newVisual.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2, -2);

        RectTransform rt = newVisual.GetComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        newVisual.SetActive(false);
        equipmentVisual = newVisual;
    }

    public void EquipItem(InventoryItemMarker item)
    {
        if (item == null) return;

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null || !data.isEquippable)
        {
            Debug.Log($"Item {item.itemType} is not equippable!");
            return;
        }

        if (equipmentVisual != null)
        {
            equipmentVisual.SetActive(true);

            Image img = equipmentVisual.GetComponent<Image>();
            if (img != null && data.icon != null)
            {
                img.sprite = data.icon;
                img.color = Color.white;
            }
        }

        Debug.Log($"Equipped: {item.itemType} in slot {gameObject.name}");
    }

    public void UnequipItem()
    {
        if (equipmentVisual != null)
        {
            equipmentVisual.SetActive(false);
        }
        Debug.Log($"Unequipped from slot {gameObject.name}");
    }

    public bool IsEquipped()
    {
        return equipmentVisual != null && equipmentVisual.activeSelf;
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        InventoryItemMarker draggedItem = dropped.GetComponent<InventoryItemMarker>();
        if (draggedItem == null) return;

        InventoryItemMarker existingItem = GetComponentInChildren<InventoryItemMarker>();

        if (existingItem == null)
        {
            draggedItem.transform.SetParent(transform);
            draggedItem.transform.localPosition = Vector3.zero;
            draggedItem.transform.localScale = Vector3.one;
            draggedItem.originalSlotParent = transform;

            isOccupied = true;
            UpdateSlotText();
            UpdateAllSlots();
            return;
        }

        if (existingItem == draggedItem) return;

        if (existingItem.itemType == draggedItem.itemType)
        {
            ItemData data = ItemDatabase.Instance?.GetItemData(existingItem.itemType);
            if (data != null && data.canStack)
            {
                int total = existingItem.count + draggedItem.count;

                if (total <= data.maxStack)
                {
                    existingItem.count = total;
                    existingItem.UpdateUI();
                    existingItem.originalSlotParent = transform;

                    draggedItem.DestroyDragClone();
                    Destroy(draggedItem.gameObject);
                    UpdateSlotText();
                    UpdateAllSlots();
                    return;
                }
                else
                {
                    int spaceLeft = data.maxStack - existingItem.count;
                    existingItem.count = data.maxStack;
                    draggedItem.count -= spaceLeft;
                    existingItem.UpdateUI();
                    draggedItem.UpdateUI();

                    UpdateSlotText();
                    UpdateAllSlots();
                    return;
                }
            }
        }

        Transform sourceSlot = draggedItem.transform.parent;
        InventorySlot sourceInventorySlot = sourceSlot.GetComponent<InventorySlot>();

        existingItem.SetBlocksRaycasts(false);
        draggedItem.SetBlocksRaycasts(false);

        existingItem.transform.SetParent(sourceSlot);
        existingItem.transform.localPosition = Vector3.zero;
        existingItem.transform.localScale = Vector3.one;
        existingItem.originalSlotParent = sourceSlot;

        draggedItem.transform.SetParent(transform);
        draggedItem.transform.localPosition = Vector3.zero;
        draggedItem.transform.localScale = Vector3.one;
        draggedItem.originalSlotParent = transform;

        existingItem.SetBlocksRaycasts(true);
        draggedItem.SetBlocksRaycasts(true);

        if (sourceInventorySlot != null)
        {
            sourceInventorySlot.isOccupied = true;
            sourceInventorySlot.UpdateSlotText();
        }

        isOccupied = true;
        UpdateSlotText();
        UpdateAllSlots();

        Debug.Log($"✅ SWAP: {existingItem.itemType} <-> {draggedItem.itemType}");
    }

    public void UpdateSlotText()
    {
        TMP_Text text = GetComponentInChildren<TMP_Text>();
        InventoryItemMarker item = GetComponentInChildren<InventoryItemMarker>();

        if (text != null)
        {
            if (item != null && item.count > 1)
                text.text = item.count.ToString();
            else
                text.text = "";
        }
    }

    private void UpdateAllSlots()
    {
        InventoryUIManager manager = GetComponentInParent<InventoryUIManager>();
        if (manager != null)
            manager.UpdateAllSlots();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (isProcessingRightClick) return;

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            if (MenuManager.Instance != null && MenuManager.Instance.IsContextMenuOpen())
            {
                MenuManager.Instance.CloseContextMenu();
                return;
            }
        }

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            OnRightClick();
        }
    }

    public void HandleRightClickFromItem()
    {
        if (isProcessingRightClick) return;
        OnRightClick();
    }

    private void OnRightClick()
    {
        isProcessingRightClick = true;

        if (MenuManager.Instance != null && MenuManager.Instance.IsContextMenuOpen())
        {
            MenuManager.Instance.CloseContextMenu();
            isProcessingRightClick = false;
            return;
        }

        InventoryItemMarker item = GetComponentInChildren<InventoryItemMarker>();
        if (item == null)
        {
            isProcessingRightClick = false;
            return;
        }

        MenuManager.Instance?.OpenContextMenu(item, Input.mousePosition);

        isProcessingRightClick = false;
    }
}