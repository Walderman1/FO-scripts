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

        Logger.Log(LogModule.Inventory, $"Слот экипировки {slotType} инициализирован");
    }

    public void OnDrop(PointerEventData eventData)
    {
        GameObject dropped = eventData.pointerDrag;
        if (dropped == null)
        {
            Logger.LogWarning(LogModule.Inventory, "Попытка бросить null объект в слот экипировки");
            return;
        }

        InventoryItemMarker draggedItem = dropped.GetComponent<InventoryItemMarker>();
        if (draggedItem == null)
        {
            Logger.LogWarning(LogModule.Inventory, "Брошенный объект не является предметом инвентаря");
            return;
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(draggedItem.itemType);
        if (data == null || !data.isEquippable)
        {
            Logger.Log(LogModule.Inventory, $"Предмет {draggedItem.itemType} нельзя экипировать");
            return;
        }

        if (data.equipmentType != slotType)
        {
            Logger.Log(LogModule.Inventory, $"Предмет {draggedItem.itemType} ({data.equipmentType}) не подходит для слота {slotType}");
            return;
        }

        if (isOccupied)
        {
            Logger.Log(LogModule.Inventory, $"Слот {slotType} занят, снятие текущего предмета");
            UnequipCurrent();
        }

        EquipItem(draggedItem);
    }

    private void EquipItem(InventoryItemMarker item)
    {
        item.DestroyDragClone();

        currentItem = item;

        item.transform.SetParent(transform);
        item.transform.localPosition = Vector3.zero;
        item.transform.localScale = Vector3.one;

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

        EquipmentSystem.Instance?.EquipItem(item);

        Logger.Log(LogModule.Inventory, $"Экипирован предмет: {item.itemType} в слот {slotType}");
    }

    public void UnequipCurrent()
    {
        if (!isOccupied || currentItem == null)
        {
            Logger.LogWarning(LogModule.Inventory, $"Попытка снять предмет с пустого слота {slotType}");
            return;
        }

        ItemType itemType = currentItem.itemType;

        EquipmentSystem.Instance?.UnequipItem(slotType);

        InventoryUIManager uiManager = FindFirstObjectByType<InventoryUIManager>();
        if (uiManager != null)
        {
            uiManager.AddItem(itemType);
            Logger.Log(LogModule.Inventory, $"Предмет {itemType} возвращён в инвентарь");
        }
        else
        {
            Logger.LogWarning(LogModule.Inventory, "InventoryUIManager не найден, предмет не возвращён в инвентарь");
        }

        Destroy(currentItem.gameObject);

        isOccupied = false;
        currentItem = null;

        Logger.Log(LogModule.Inventory, $"Предмет снят со слота {slotType}");
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!isOccupied) return;

        if (eventData.button == PointerEventData.InputButton.Right)
        {
            if (currentItem != null)
            {
                MenuManager.Instance?.OpenContextMenu(currentItem, Input.mousePosition);
                Logger.Log(LogModule.Inventory, $"Открыто контекстное меню для предмета в слоте {slotType}");
            }
            return;
        }

        if (eventData.button == PointerEventData.InputButton.Left)
        {
            float timeSinceLastClick = Time.time - lastClickTime;
            lastClickTime = Time.time;

            if (timeSinceLastClick < DOUBLE_CLICK_DELAY)
            {
                UnequipCurrent();
                Logger.Log(LogModule.Inventory, $"Двойной клик по слоту {slotType} - предмет снят");
            }
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

    public bool IsEquipped()
    {
        return isOccupied;
    }

    public ItemType GetEquippedItemType()
    {
        return currentItem != null ? currentItem.itemType : ItemType.None;
    }
}
