using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("Menus")]
    public RadialMenu mainRadialMenu;
    public RadialMenu contextMenu;
    public InventoryUIManager inventory;

    private InventoryItemMarker currentContextItem;
    private EquipmentSlot currentEquipmentSlot;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.RadialMenu, "MenuManager инициализирован");
        }
        else
        {
            Logger.Log(LogModule.RadialMenu, "Уничтожение дублирующего MenuManager");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        AutoFindComponents();
    }

    private void AutoFindComponents()
    {
        if (mainRadialMenu == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("RadialMenu");
            if (obj != null)
            {
                mainRadialMenu = obj.GetComponent<RadialMenu>();
                Logger.Log(LogModule.RadialMenu, "RadialMenu найден автоматически");
            }
        }

        if (contextMenu == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("ContextMenu");
            if (obj != null)
            {
                contextMenu = obj.GetComponent<RadialMenu>();
                Logger.Log(LogModule.RadialMenu, "ContextMenu найден автоматически");
            }
        }

        if (inventory == null)
        {
            GameObject obj = GameObject.FindGameObjectWithTag("InventoryPanel");
            if (obj != null)
            {
                inventory = obj.GetComponent<InventoryUIManager>();
                Logger.Log(LogModule.RadialMenu, "Inventory найден автоматически");
            }
        }
    }

    public void OpenMainMenu()
    {
        CloseAllMenus();
        if (mainRadialMenu != null)
        {
            mainRadialMenu.RestoreDefaultMenu();
            mainRadialMenu.ShowAtPosition(Input.mousePosition);
            Logger.Log(LogModule.RadialMenu, "Открыто главное меню");
        }
        else
        {
            Logger.LogWarning(LogModule.RadialMenu, "mainRadialMenu отсутствует");
        }
    }

    public void OpenChoiceMenu(List<string> choices, System.Action<int> onChoiceSelected)
    {
        CloseAllMenus();
        if (mainRadialMenu != null)
        {
            mainRadialMenu.SetChoiceMode(choices, onChoiceSelected);
            Logger.Log(LogModule.RadialMenu, $"Открыто меню выбора с {choices.Count} вариантами");
        }
        else
        {
            Logger.LogWarning(LogModule.RadialMenu, "mainRadialMenu отсутствует");
        }
    }

    public void OpenContextMenu(InventoryItemMarker item, Vector2 position)
    {
        CloseAllMenus();
        if (contextMenu != null && item != null)
        {
            currentContextItem = item;
            currentEquipmentSlot = null;

            List<string> buttons = GetContextButtons(item);
            List<RadialMenu.RadialAction> actions = GetContextActions(item);

            contextMenu.SetContextMenu(buttons, actions);
            contextMenu.SetCurrentItem(item);
            contextMenu.ShowAtPosition(position);

            Logger.Log(LogModule.RadialMenu, $"Открыто контекстное меню для предмета {item.itemType}");
        }
        else
        {
            Logger.LogWarning(LogModule.RadialMenu, "contextMenu отсутствует или item null");
        }
    }

    public void OpenContextMenuForEquipment(EquipmentSlot slot, Vector2 position)
    {
        if (slot == null || !slot.IsEquipped())
        {
            Logger.LogWarning(LogModule.RadialMenu, "Слот пуст или null");
            return;
        }

        InventoryItemMarker item = slot.GetCurrentItem();
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "В слоте нет предмета");
            return;
        }

        CloseAllMenus();
        if (contextMenu != null)
        {
            currentContextItem = item;
            currentEquipmentSlot = slot;

            List<string> buttons = GetEquipmentContextButtons(item);
            List<RadialMenu.RadialAction> actions = GetEquipmentContextActions(item);

            contextMenu.SetContextMenu(buttons, actions);
            contextMenu.SetCurrentItem(item);
            contextMenu.ShowAtPosition(position);

            Logger.Log(LogModule.RadialMenu, $"Открыто контекстное меню для экипировки {item.itemType} в слоте {slot.slotType}");
        }
    }

    public void OpenItemView(InventoryItemMarker item)
    {
        if (item != null)
        {
            ItemViewPanel.Instance?.ShowItem(item);
            Logger.Log(LogModule.RadialMenu, $"Открыт просмотр предмета {item.itemType}");
        }
        else
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null для просмотра");
        }
    }

    public void CloseAllMenus()
    {
        if (mainRadialMenu != null) mainRadialMenu.Hide();
        if (contextMenu != null) contextMenu.Hide();
        currentEquipmentSlot = null;
        Logger.Log(LogModule.RadialMenu, "Все меню закрыты");
    }

    public void CloseContextMenu()
    {
        if (contextMenu != null) contextMenu.Hide();
        currentEquipmentSlot = null;
        Logger.Log(LogModule.RadialMenu, "Контекстное меню закрыто");
    }

    public bool IsAnyMenuOpen()
    {
        bool mainOpen = mainRadialMenu != null && mainRadialMenu.IsVisible();
        bool contextOpen = contextMenu != null && contextMenu.IsVisible();
        return mainOpen || contextOpen;
    }

    public bool IsMainMenuOpen()
    {
        return mainRadialMenu != null && mainRadialMenu.IsVisible();
    }

    public bool IsContextMenuOpen()
    {
        return contextMenu != null && contextMenu.IsVisible();
    }

    public void CloseInventoryAndMenus()
    {
        if (inventory != null && inventory.IsInventoryOpen)
            inventory.CloseInventory();
        CloseAllMenus();
        Logger.Log(LogModule.RadialMenu, "Инвентарь и меню закрыты");
    }

    public InventoryItemMarker GetCurrentContextItem() => currentContextItem;

    private List<string> GetContextButtons(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при получении кнопок контекстного меню");
            return new List<string> { "Осмотреть", "Выбросить" };
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, $"Нет данных для предмета {item.itemType}");
            return new List<string> { "Осмотреть", "Выбросить" };
        }

        List<string> buttons = new List<string>();

        if (IsUsableItem(item.itemType))
        {
            buttons.Add(GetUseButtonText(item.itemType));
        }

        if (data.isEquippable)
        {
            bool isEquipped = IsItemEquipped(item);
            buttons.Add(isEquipped ? "Снять" : "Экипировать");
        }

        if (item.count > 1 && data.canStack)
        {
            buttons.Add("Разделить");
        }

        buttons.Add("Осмотреть");
        buttons.Add("Выбросить");

        return buttons;
    }

    private List<RadialMenu.RadialAction> GetContextActions(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при получении действий контекстного меню");
            return new List<RadialMenu.RadialAction> {
                RadialMenu.RadialAction.Examine,
                RadialMenu.RadialAction.Exit
            };
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, $"Нет данных для предмета {item.itemType}");
            return new List<RadialMenu.RadialAction> {
                RadialMenu.RadialAction.Examine,
                RadialMenu.RadialAction.Exit
            };
        }

        List<RadialMenu.RadialAction> actions = new List<RadialMenu.RadialAction>();

        if (IsUsableItem(item.itemType))
        {
            actions.Add(RadialMenu.RadialAction.Use);
        }

        if (data.isEquippable)
        {
            actions.Add(RadialMenu.RadialAction.Equip);
        }

        if (item.count > 1 && data.canStack)
        {
            actions.Add(RadialMenu.RadialAction.Split);
        }

        actions.Add(RadialMenu.RadialAction.Examine);
        actions.Add(RadialMenu.RadialAction.Exit);

        return actions;
    }

    private List<string> GetEquipmentContextButtons(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при получении кнопок для экипировки");
            return new List<string> { "Осмотреть", "Выбросить" };
        }

        List<string> buttons = new List<string>();
        buttons.Add("Снять");
        buttons.Add("Осмотреть");
        buttons.Add("Выбросить");

        return buttons;
    }

    private List<RadialMenu.RadialAction> GetEquipmentContextActions(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при получении действий для экипировки");
            return new List<RadialMenu.RadialAction> {
                RadialMenu.RadialAction.Examine,
                RadialMenu.RadialAction.Exit
            };
        }

        List<RadialMenu.RadialAction> actions = new List<RadialMenu.RadialAction>();
        actions.Add(RadialMenu.RadialAction.Examine);
        actions.Add(RadialMenu.RadialAction.Exit);

        return actions;
    }

    private bool IsUsableItem(ItemType itemType)
    {
        return itemType == ItemType.Apple ||
               itemType == ItemType.Potion ||
               itemType == ItemType.Key ||
               itemType == ItemType.GoldCoin;
    }

    private string GetUseButtonText(ItemType itemType)
    {
        switch (itemType)
        {
            case ItemType.Apple: return "Съесть";
            case ItemType.Potion: return "Выпить";
            case ItemType.Key: return "Использовать";
            case ItemType.GoldCoin: return "Использовать";
            default: return "Использовать";
        }
    }

    private bool IsItemEquipped(InventoryItemMarker item)
    {
        if (item == null) return false;

        EquipmentSystem eq = EquipmentSystem.Instance;
        if (eq == null) return false;

        return eq.IsEquipped(item.itemType);
    }

    public void HandleContextAction(InventoryItemMarker item, RadialMenu.RadialAction action)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при обработке действия контекстного меню");
            return;
        }

        Logger.Log(LogModule.RadialMenu, $"Обработка действия {action} для предмета {item.itemType}");

        switch (action)
        {
            case RadialMenu.RadialAction.Use:
                UseItem(item);
                break;
            case RadialMenu.RadialAction.Equip:
                ToggleEquipItem(item);
                break;
            case RadialMenu.RadialAction.Split:
                SplitItem(item);
                break;
            case RadialMenu.RadialAction.Examine:
                ExamineItem(item);
                break;
            case RadialMenu.RadialAction.Exit:
                DropItem(item);
                break;
            default:
                Logger.LogWarning(LogModule.RadialMenu, $"Неизвестное действие: {action}");
                break;
        }
    }

    private void UseItem(InventoryItemMarker item)
    {
        Logger.Log(LogModule.RadialMenu, $"Использован предмет: {item.itemType}");

        item.count--;
        if (item.count <= 0)
        {
            InventorySlot slot = item.GetComponentInParent<InventorySlot>();
            if (slot != null)
            {
                slot.isOccupied = false;
                slot.UpdateSlotText();
            }
            Destroy(item.gameObject);
        }
        else
        {
            item.UpdateUI();
            InventorySlot slot = item.GetComponentInParent<InventorySlot>();
            if (slot != null) slot.UpdateSlotText();
        }

        if (inventory != null) inventory.UpdateAllSlots();
        CloseContextMenu();
    }

    private void ToggleEquipItem(InventoryItemMarker item)
    {
        if (item == null)
        {
            Logger.LogWarning(LogModule.RadialMenu, "item null при экипировке");
            return;
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data == null || !data.isEquippable)
        {
            Logger.Log(LogModule.RadialMenu, $"Предмет {item.itemType} нельзя экипировать");
            CloseContextMenu();
            return;
        }

        bool isEquipped = IsItemEquipped(item);

        if (isEquipped)
        {
            EquipmentSlot[] slots = FindObjectsOfType<EquipmentSlot>();
            foreach (EquipmentSlot slot in slots)
            {
                if (slot.isOccupied && slot.slotType == data.equipmentType)
                {
                    slot.UnequipCurrent();
                    Logger.Log(LogModule.RadialMenu, $"Снят предмет: {item.itemType} из {data.equipmentType}");
                    break;
                }
            }
        }
        else
        {
            EquipmentSlot[] slots = FindObjectsOfType<EquipmentSlot>();
            EquipmentSlot targetSlot = null;

            foreach (EquipmentSlot slot in slots)
            {
                if (slot.slotType == data.equipmentType && !slot.isOccupied)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot != null)
            {
                item.DestroyDragClone();

                PointerEventData pointerData = new PointerEventData(EventSystem.current)
                {
                    pointerDrag = item.gameObject
                };
                targetSlot.OnDrop(pointerData);
                Logger.Log(LogModule.RadialMenu, $"Экипирован предмет: {item.itemType} в {data.equipmentType}");
            }
            else
            {
                Logger.LogWarning(LogModule.RadialMenu, $"Нет свободного слота для {data.equipmentType}");
            }
        }

        CloseContextMenu();
    }

    private void UnequipFromSlot(InventoryItemMarker item)
    {
        if (item == null) return;

        EquipmentSlot[] slots = FindObjectsOfType<EquipmentSlot>();
        foreach (EquipmentSlot slot in slots)
        {
            if (slot.IsEquipped() && slot.GetCurrentItem() == item)
            {
                slot.UnequipCurrent();
                Logger.Log(LogModule.RadialMenu, $"Снят предмет: {item.itemType} из {slot.slotType}");
                break;
            }
        }

        CloseContextMenu();
    }

    private void SplitItem(InventoryItemMarker item)
    {
        if (item == null || item.count <= 1)
        {
            Logger.LogWarning(LogModule.RadialMenu, "Невозможно разделить предмет");
            return;
        }

        int half = item.count / 2;
        if (half <= 0)
        {
            Logger.LogWarning(LogModule.RadialMenu, "Некорректное разделение");
            return;
        }

        Transform emptySlot = null;
        if (inventory != null && inventory.inventoryGrid != null)
        {
            foreach (Transform child in inventory.inventoryGrid.transform)
            {
                InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
                if (marker == null)
                {
                    emptySlot = child;
                    break;
                }
            }
        }

        if (emptySlot == null)
        {
            Logger.Log(LogModule.RadialMenu, "Нет свободного слота для разделения");
            CloseContextMenu();
            return;
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data != null && data.uiPrefab != null)
        {
            GameObject newItem = Instantiate(data.uiPrefab, emptySlot);
            newItem.transform.localPosition = Vector3.zero;
            newItem.transform.localScale = Vector3.one;

            InventoryItemMarker newMarker = newItem.GetComponent<InventoryItemMarker>();
            if (newMarker != null)
            {
                newMarker.itemType = item.itemType;
                newMarker.count = half;
                newMarker.UpdateUI();
            }

            item.count -= half;
            item.UpdateUI();

            InventorySlot originalSlot = item.GetComponentInParent<InventorySlot>();
            if (originalSlot != null)
            {
                originalSlot.UpdateSlotText();
                originalSlot.isOccupied = true;
            }

            InventorySlot newSlot = emptySlot.GetComponent<InventorySlot>();
            if (newSlot != null)
            {
                newSlot.UpdateSlotText();
                newSlot.isOccupied = true;
            }

            if (inventory != null) inventory.UpdateAllSlots();

            Logger.Log(LogModule.RadialMenu, $"Разделён предмет {item.itemType}: {half} в новом слоте");
        }

        CloseContextMenu();
    }

    private void ExamineItem(InventoryItemMarker item)
    {
        OpenItemView(item);
        CloseContextMenu();
        Logger.Log(LogModule.RadialMenu, $"Осмотрен предмет: {item.itemType}");
    }

    private void DropItem(InventoryItemMarker item)
    {
        if (item == null) return;

        bool isEquipped = IsItemEquipped(item);

        if (isEquipped)
        {
            EquipmentSlot[] slots = FindObjectsOfType<EquipmentSlot>();
            foreach (EquipmentSlot slot in slots)
            {
                if (slot.IsEquipped() && slot.GetCurrentItem() == item)
                {
                    slot.UnequipCurrent();
                    Logger.Log(LogModule.RadialMenu, $"Предмет снят перед выбросом: {item.itemType}");
                    break;
                }
            }
        }

        ItemData data = ItemDatabase.Instance?.GetItemData(item.itemType);
        if (data != null && data.worldPrefab != null)
        {
            Vector3 spawnPos = Camera.main.transform.position + Camera.main.transform.forward * 2f;
            GameObject droppedItem = Instantiate(data.worldPrefab, spawnPos, Quaternion.identity);

            PickupItem pickup = droppedItem.GetComponent<PickupItem>();
            if (pickup != null)
            {
                pickup.itemType = item.itemType;
            }

            Logger.Log(LogModule.RadialMenu, $"Предмет выброшен в мир: {item.itemType}");
        }

        if (!isEquipped)
        {
            InventorySlot slot = item.GetComponentInParent<InventorySlot>();
            if (slot != null)
            {
                slot.isOccupied = false;
                slot.UpdateSlotText();
            }
            Destroy(item.gameObject);
        }

        if (inventory != null) inventory.UpdateAllSlots();
        CloseContextMenu();
    }
}
