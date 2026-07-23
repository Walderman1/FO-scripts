using UnityEngine;
using UnityEngine.EventSystems;

public class MenuCloseHandler : MonoBehaviour
{
    private void Update()
    {
        bool isOpen = MenuManager.Instance.IsAnyMenuOpen();

        if (!isOpen) return;

        if (!Input.GetMouseButtonDown(0)) return;

        bool isDragging = InventoryItemMarker.IsAnyDragging;
        bool onMenu = IsClickOnMenu();

        if (isDragging)
        {
            Logger.Log(LogModule.Inventory, "Клик заблокирован - идёт перетаскивание предмета");
            return;
        }

        if (onMenu)
        {
            Logger.Log(LogModule.Inventory, "Клик по элементу меню - закрытие отменено");
            return;
        }

        Logger.Log(LogModule.Inventory, "Клик вне меню - закрытие всех меню");
        MenuManager.Instance.CloseAllMenus();
    }

    private bool IsClickOnMenu()
    {
        if (EventSystem.current == null)
        {
            Logger.LogWarning(LogModule.Inventory, "EventSystem.current отсутствует");
            return false;
        }

        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new System.Collections.Generic.List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var result in results)
        {
            if (MenuManager.Instance.mainRadialMenu != null)
            {
                GameObject menu = MenuManager.Instance.mainRadialMenu.gameObject;
                if (result.gameObject == menu || result.gameObject.transform.IsChildOf(menu.transform))
                {
                    Logger.Log(LogModule.Inventory, $"Клик по радиальному меню: {result.gameObject.name}");
                    return true;
                }
            }

            if (MenuManager.Instance.contextMenu != null)
            {
                GameObject menu = MenuManager.Instance.contextMenu.gameObject;
                if (result.gameObject == menu || result.gameObject.transform.IsChildOf(menu.transform))
                {
                    Logger.Log(LogModule.Inventory, $"Клик по контекстному меню: {result.gameObject.name}");
                    return true;
                }
            }
        }

        return false;
    }
}
