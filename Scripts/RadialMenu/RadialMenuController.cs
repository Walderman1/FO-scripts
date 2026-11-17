using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class RadialMenuController : MonoBehaviour
{
    [Inject] private RadialMenuRegistry _radialMenuRegistry;
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private EquipmentSystem _equipmentSystem;
    [Inject] private ItemViewPanel _itemViewPanel;
    [Inject] private TextBeginner _textBeginner;
    [Inject] private DialogueCharacterManager _dialogueCharacterManager;
    [Inject] private ItemDatabase _itemDatabase;
    [Inject] WorldItemTracker _worldItemTracker;

    public RadialMenu mainRadialMenu;
    public RadialMenu contextMenu;
    public InventoryUIManager inventory;

    private InventoryItemMarker currentContextItem;
    private EquipmentSlot currentEquipmentSlot;
    private Camera cachedCamera;

    private void Start()
    {
        cachedCamera = Camera.main;
        AutoFindComponents();
    }

    private void AutoFindComponents()
    {
        var registry = SceneInjector.Instance?.CurrentSceneScope?.Resolve<RadialMenuRegistry>();

        if (registry != null)
        {
            mainRadialMenu = registry.MainRadialMenu;
            contextMenu = registry.ContextRadialMenu;
            Logger.LogVerbose(LogModule.RadialMenu, $"AutoFindComponents: registry main={(mainRadialMenu != null ? mainRadialMenu.name : "null")} ctx={(contextMenu != null ? contextMenu.name : "null")}");
        }
        else
        {
            Logger.LogWarning(LogModule.RadialMenu, "AutoFindComponents: registry null");
        }

        inventory = SceneInjector.Instance?.CurrentSceneScope?.Resolve<InventoryUIManager>();
    }

    private RadialMenu GetChoiceMenu() => _radialMenuRegistry?.ChoiceRadialMenu;

    public void OpenMainMenu()
    {
        Logger.LogVerbose(LogModule.RadialMenu, $"OpenMainMenu enter. main={(mainRadialMenu != null ? mainRadialMenu.name : "null")}");
        CloseAllMenus();

        if (mainRadialMenu == null) AutoFindComponents();

        if (mainRadialMenu != null)
        {
            mainRadialMenu.RestoreDefaultMenu();
            mainRadialMenu.ShowAtPosition(Input.mousePosition);
            Logger.LogVerbose(LogModule.RadialMenu, $"OpenMainMenu: показано menu={mainRadialMenu.name} buttons={mainRadialMenu.buttonCount}");
        }
        else
        {
            Logger.LogError(LogModule.RadialMenu, "OpenMainMenu: меню не найдено!");
        }
    }

    public void OpenChoiceMenu(List<string> choices, System.Action<int> onChoiceSelected)
    {
        CloseAllMenus();
        var choiceMenu = GetChoiceMenu();
        if (choiceMenu != null) choiceMenu.SetChoiceMode(choices, onChoiceSelected);
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
        }
    }

    public void OpenContextMenuForEquipment(EquipmentSlot slot, Vector2 position)
    {
        if (slot == null || !slot.IsEquipped()) return;

        InventoryItemMarker item = slot.GetCurrentItem();
        if (item == null) return;

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
        }
    }

    public void OpenItemView(InventoryItemMarker item)
    {
        if (item != null) _itemViewPanel?.ShowItem(item);
    }

    public void CloseAllMenus()
    {
        Logger.LogVerbose(LogModule.RadialMenu, "CloseAllMenus");
        if (mainRadialMenu != null) mainRadialMenu.Hide();
        if (contextMenu != null) contextMenu.Hide();

        var choiceMenu = GetChoiceMenu();
        if (choiceMenu != null && choiceMenu.IsVisible()) choiceMenu.Hide();

        currentEquipmentSlot = null;
    }

    public void CloseContextMenu()
    {
        if (contextMenu != null) contextMenu.Hide();
        currentEquipmentSlot = null;
    }

    public bool IsAnyMenuOpen()
    {
        bool mainOpen = mainRadialMenu != null && mainRadialMenu.IsVisible();
        bool contextOpen = contextMenu != null && contextMenu.IsVisible();
        var choiceMenu = GetChoiceMenu();
        bool choiceOpen = choiceMenu != null && choiceMenu.IsVisible();
        return mainOpen || contextOpen || choiceOpen;
    }

    public bool IsMainMenuOpen() => mainRadialMenu != null && mainRadialMenu.IsVisible();
    public bool IsContextMenuOpen() => contextMenu != null && contextMenu.IsVisible();

    public void CloseInventoryAndMenus()
    {
        if (_inventoryUIManager != null && _inventoryUIManager.IsInventoryOpen)
            _inventoryUIManager.CloseInventory();
        CloseAllMenus();
    }

    public InventoryItemMarker GetCurrentContextItem() => currentContextItem;

    private List<string> GetContextButtons(InventoryItemMarker item)
    {
        if (item == null) return new List<string> { "Осмотреть", "Выбросить" };

        var so = item.ItemSO;
        if (so == null) return new List<string> { "Осмотреть", "Выбросить" };

        List<string> buttons = new List<string>();

        if (so.isUsable)
        {
            string useText = string.IsNullOrEmpty(so.useButtonText) ? "Использовать" : so.useButtonText;
            buttons.Add(useText);
        }

        if (so.isEquippable)
        {
            bool isEquipped = IsItemEquipped(item);
            buttons.Add(isEquipped ? "Снять" : "Экипировать");
        }

        if (item.count > 1 && so.canStack) buttons.Add("Разделить");

        buttons.Add("Осмотреть");
        buttons.Add("Выбросить");

        return buttons;
    }

    private List<RadialMenu.RadialAction> GetContextActions(InventoryItemMarker item)
    {
        if (item == null) return new List<RadialMenu.RadialAction>
        {
            RadialMenu.RadialAction.Examine,
            RadialMenu.RadialAction.Exit
        };

        var so = item.ItemSO;
        if (so == null) return new List<RadialMenu.RadialAction>
        {
            RadialMenu.RadialAction.Examine,
            RadialMenu.RadialAction.Exit
        };

        List<RadialMenu.RadialAction> actions = new List<RadialMenu.RadialAction>();

        if (so.isUsable) actions.Add(RadialMenu.RadialAction.Use);
        if (so.isEquippable) actions.Add(RadialMenu.RadialAction.Equip);
        if (item.count > 1 && so.canStack) actions.Add(RadialMenu.RadialAction.Split);

        actions.Add(RadialMenu.RadialAction.Examine);
        actions.Add(RadialMenu.RadialAction.Exit);

        return actions;
    }

    private List<string> GetEquipmentContextButtons(InventoryItemMarker item)
    {
        if (item == null) return new List<string> { "Осмотреть", "Выбросить" };
        return new List<string> { "Снять", "Осмотреть", "Выбросить" };
    }

    private List<RadialMenu.RadialAction> GetEquipmentContextActions(InventoryItemMarker item)
    {
        if (item == null) return new List<RadialMenu.RadialAction>
        {
            RadialMenu.RadialAction.Examine,
            RadialMenu.RadialAction.Exit
        };

        return new List<RadialMenu.RadialAction>
        {
            RadialMenu.RadialAction.Examine,
            RadialMenu.RadialAction.Exit
        };
    }

    private bool IsItemEquipped(InventoryItemMarker item)
    {
        if (item == null) return false;
        var so = item.ItemSO;
        if (so == null || !so.isEquippable) return false;
        return _equipmentSystem != null && _equipmentSystem.IsEquipped(so.equipmentType);
    }

    public void HandleContextAction(InventoryItemMarker item, RadialMenu.RadialAction action)
    {
        if (item == null) return;

        switch (action)
        {
            case RadialMenu.RadialAction.Use: UseItem(item); break;
            case RadialMenu.RadialAction.Equip: ToggleEquipItem(item); break;
            case RadialMenu.RadialAction.Split: SplitItem(item); break;
            case RadialMenu.RadialAction.Examine: ExamineItem(item); break;
            case RadialMenu.RadialAction.Exit: DropItem(item); break;
        }
    }

    private void UseItem(InventoryItemMarker item)
    {
        if (item == null) return;

        item.count--;
        InventorySlot slot = item.GetComponentInParent<InventorySlot>();

        if (item.count <= 0)
        {
            if (slot != null)
            {
                slot.isOccupied = false;
                slot.UpdateSlotText();
            }
            item.gameObject.SetActive(false);
            Destroy(item.gameObject);
        }
        else
        {
            item.UpdateUI();
            if (slot != null) slot.UpdateSlotText();
        }

        if (_inventoryUIManager != null) _inventoryUIManager.UpdateAllSlots();
        CloseContextMenu();
    }

    private void ToggleEquipItem(InventoryItemMarker item)
    {
        if (item == null) return;

        var so = item.ItemSO;
        if (so == null || !so.isEquippable)
        {
            CloseContextMenu();
            return;
        }

        bool isEquipped = IsItemEquipped(item);

        if (isEquipped)
        {
            EquipmentSlot[] slots = FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Exclude);
            foreach (EquipmentSlot slot in slots)
            {
                if (slot.isOccupied && slot.slotType == so.equipmentType)
                {
                    slot.UnequipCurrent();
                    break;
                }
            }
        }
        else
        {
            EquipmentSlot[] slots = FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Exclude);
            EquipmentSlot targetSlot = null;

            foreach (EquipmentSlot slot in slots)
            {
                if (slot.slotType == so.equipmentType && !slot.isOccupied)
                {
                    targetSlot = slot;
                    break;
                }
            }

            if (targetSlot != null)
            {
                item.DestroyDragClone();
                PointerEventData pointerData = new PointerEventData(EventSystem.current) { pointerDrag = item.gameObject };
                targetSlot.OnDrop(pointerData);
            }
        }

        CloseContextMenu();
    }

    private void SplitItem(InventoryItemMarker item)
    {
        if (item == null || item.count <= 1)
        {
            CloseContextMenu();
            return;
        }

        var so = item.ItemSO;
        if (so == null) { CloseContextMenu(); return; }

        int half = item.count / 2;
        if (half <= 0) { CloseContextMenu(); return; }

        Transform emptySlot = null;
        if (_inventoryUIManager != null && _inventoryUIManager.inventoryGrid != null)
        {
            foreach (Transform child in _inventoryUIManager.inventoryGrid.transform)
            {
                InventoryItemMarker marker = child.GetComponentInChildren<InventoryItemMarker>();
                if (marker == null) { emptySlot = child; break; }
            }
        }

        if (emptySlot == null) { CloseContextMenu(); return; }

        if (so.uiPrefab != null)
        {
            GameObject newItem = Instantiate(so.uiPrefab, emptySlot);
            newItem.transform.localPosition = Vector3.zero;
            SceneInjector.InjectInto(newItem);

            InventoryItemMarker newMarker = newItem.GetComponent<InventoryItemMarker>();
            if (newMarker != null)
            {
                newMarker.itemSO = so;
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

            if (_inventoryUIManager != null) _inventoryUIManager.UpdateAllSlots();
        }

        CloseContextMenu();
    }

    private void ExamineItem(InventoryItemMarker item)
    {
        OpenItemView(item);
        CloseContextMenu();
    }

    private void DropItem(InventoryItemMarker item)
    {
        if (item == null) return;

        bool isEquipped = IsItemEquipped(item);

        if (isEquipped)
        {
            EquipmentSlot[] slots = FindObjectsByType<EquipmentSlot>(FindObjectsInactive.Exclude);
            foreach (EquipmentSlot slot in slots)
            {
                if (slot.IsEquipped() && slot.GetCurrentItem() == item)
                {
                    slot.UnequipCurrent();
                    break;
                }
            }
        }

        int dropCount = item.count;
        var so = item.ItemSO;

        if (so != null && so.worldPrefab != null)
        {
            var currentLocation = _textBeginner?.CurrentLocation;

            Vector3 spawnPos = GetDropPosition();
            Vector2 randomOffset = new Vector2(Random.Range(-1.0f, 1.0f), Random.Range(-0.5f, 0.1f));
            Vector3 targetWorldPos = spawnPos + (Vector3)randomOffset;

            GameObject droppedItem = Instantiate(so.worldPrefab, spawnPos, Quaternion.identity);

            if (currentLocation != null)
            {
                droppedItem.transform.SetParent(currentLocation.transform);
                droppedItem.transform.SetAsFirstSibling();
            }

            SpriteRenderer[] renderers = droppedItem.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in renderers)
                sr.sortingOrder = 50;

            SceneInjector.InjectInto(droppedItem);

            PickupItem pickup = droppedItem.GetComponent<PickupItem>();
            if (pickup != null)
            {
                pickup.itemSO = so;
                pickup.SetCount(dropCount);
                pickup.GenerateNewID();

                pickup.AnimateDrop(targetWorldPos, () =>
                {
                    string locName = currentLocation != null ? currentLocation.name : "";
                    Vector3 localPos = currentLocation != null
                        ? currentLocation.transform.InverseTransformPoint(targetWorldPos)
                        : targetWorldPos;

                    _worldItemTracker?.RegisterDroppedItem(so, locName, localPos, dropCount);
                });
            }
        }

        if (!isEquipped)
        {
            InventorySlot slot = item.GetComponentInParent<InventorySlot>();
            if (slot != null)
            {
                item.gameObject.SetActive(false);
                slot.isOccupied = false;
                slot.UpdateSlotText();
            }
            Destroy(item.gameObject);
        }

        if (_inventoryUIManager != null) _inventoryUIManager.UpdateAllSlots();
        CloseContextMenu();
    }

    private Vector3 GetDropPosition()
    {
        if (cachedCamera == null) cachedCamera = Camera.main;

        var character = _dialogueCharacterManager?.CurrentCharacter;
        if (character != null)
        {
            SpriteRenderer sr = character.GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                return new Vector3(character.transform.position.x, sr.bounds.min.y, 0f);

            Vector3 fallbackPos = character.transform.position;
            fallbackPos.y -= 1f;
            fallbackPos.z = 0f;
            return fallbackPos;
        }

        Vector3 fallback = cachedCamera != null ? cachedCamera.transform.position : Vector3.zero;
        fallback.z = 0f;
        return fallback;
    }
}