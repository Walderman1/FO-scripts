using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class EquipmentSlot : MonoBehaviour, IDropHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    public EquipmentType slotType;
    public bool isOccupied = false;

    private Image slotImage;
    private Color originalColor;
    private InventoryItemMarker currentItem;

    // Для двойного клика
    private float lastClickTime = 0f;
    private const float DOUBLE_CLICK_DELAY = 0.3f;

    private void Awake()
    {
        slotImage = GetComponent<Image>();
        if (slotImage == null)
            slotImage = gameObject.AddComponent<Image>();

        originalColor = slotImage.color;

        Color color = slotImage.color;
        color.a = 0.1f;
        slotImage.color = color;
        slotImage.raycastTarget = true;
    }

    // ========== DROP ==========
    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null) return;

        InventoryItemMarker draggedItem = dropped.GetComponent<InventoryItemMarker>();
        if (draggedItem == null) return;

        ItemData data = ItemDatabase.Instance?.GetItemData(draggedItem.itemType);
        if (data == null || !data.isEquippable)
        {
            Debug.Log($"Item {draggedItem.itemType} is not equippable!");
            return;
        }

        // Проверяем соответствие типа
        if (data.equipmentType != slotType)
        {
            Debug.Log($"Item {draggedItem.itemType} ({data.equipmentType}) doesn't match slot {slotType}!");
            return;
        }

        // Если слот занят - снимаем
        if (isOccupied)
        {
            UnequipCurrent();
        }

        // Экипируем новый предмет
        EquipItem(draggedItem);
    }

    // ========== ЭКИПИРОВКА ==========
    private void EquipItem(InventoryItemMarker item)
    {
        // ✅ Удаляем dragVisual у предмета
        item.DestroyDragClone();

        // Сохраняем ссылку
        currentItem = item;

        // Перемещаем предмет в слот
        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = Vector3.one;

        // Отключаем взаимодействие с предметом
        CanvasGroup cg = item.GetComponent<CanvasGroup>();
        if (cg == null) cg = item.gameObject.AddComponent<CanvasGroup>();
        cg.blocksRaycasts = false;
        cg.interactable = false;

        // Отключаем скрипт InventoryItemMarker
        item.enabled = false;

        isOccupied = true;

        // Удаляем из инвентаря (освобождаем слот)
        InventorySlot originalSlot = item.GetComponentInParent<InventorySlot>();
        if (originalSlot != null)
        {
            originalSlot.isOccupied = false;
            originalSlot.UpdateSlotText();
        }

        // Создаем предмет в мире через EquipmentSystem
        EquipmentSystem.Instance?.EquipItem(item);

        Debug.Log($"✅ Equipped: {item.itemType} in {slotType}");
    }

    // ========== СНЯТИЕ ==========
    public void UnequipCurrent()
    {
        if (!isOccupied || currentItem == null) return;

        ItemType itemType = currentItem.itemType;

        // Снимаем с мира
        EquipmentSystem.Instance?.UnequipItem(slotType);

        // Возвращаем предмет в инвентарь
        InventoryUIManager uiManager = FindFirstObjectByType<InventoryUIManager>();
        if (uiManager != null)
        {
            uiManager.AddItem(itemType);
        }

        // Удаляем предмет из слота
        Destroy(currentItem.gameObject);

        isOccupied = false;
        currentItem = null;

        Debug.Log($"Unequipped from {slotType}");
    }

    // ========== КЛИК ==========
    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOccupied) return;

        // ✅ ПКМ - открываем контекстное меню
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            // Создаем временный InventoryItemMarker для контекстного меню
            // Используем currentItem как основу
            if (currentItem != null)
            {
                // Открываем контекстное меню для этого предмета
                MenuManager.Instance?.OpenContextMenu(currentItem, Input.mousePosition);
            }
            return;
        }

        // ЛКМ - проверяем двойной клик
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < DOUBLE_CLICK_DELAY)
            {
                // ДВОЙНОЙ КЛИК - СНИМАЕМ ПРЕДМЕТ
                UnequipCurrent();
                Debug.Log($"Double click on {slotType} - unequipped");
            }
        }
    }

    // ========== HOVER ЭФФЕКТЫ ==========
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.3f;
            slotImage.color = color;
        }

        // Показываем тултип с названием предмета
        if (isOccupied && currentItem != null)
        {
            ItemData data = ItemDatabase.Instance?.GetItemData(currentItem.itemType);
            if (data != null)
            {
                string tooltip = $"<b>{data.itemName}</b>\n{data.description}";
                TooltipManager.Instance?.ShowTooltip(tooltip, Input.mousePosition);
            }
        }
    }

    public InventoryItemMarker GetCurrentItem()
    {
        return currentItem;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (slotImage != null)
        {
            Color color = slotImage.color;
            color.a = 0.1f;
            slotImage.color = color;
        }

        TooltipManager.Instance?.HideTooltip();
    }

    // ========== ВСПОМОГАТЕЛЬНЫЕ МЕТОДЫ ==========
    public bool IsEquipped()
    {
        return isOccupied;
    }

    public ItemType GetEquippedItemType()
    {
        return currentItem != null ? currentItem.itemType : ItemType.None;
    }
}