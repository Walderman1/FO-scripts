using UnityEngine;
using UnityEngine.EventSystems;

public class MenuCloseHandler : MonoBehaviour
{
    private void Update()
    {
        // ✅ Проверяем, открыто ли меню
        bool isOpen = MenuManager.Instance.IsAnyMenuOpen();

        if (!isOpen) return;

        if (!Input.GetMouseButtonDown(0)) return;

        bool isDragging = InventoryItemMarker.IsAnyDragging;
        bool onMenu = IsClickOnMenu();


        if (isDragging) return;
        if (onMenu) return;

        MenuManager.Instance.CloseAllMenus();
    }

    private bool IsClickOnMenu()
    {
        if (EventSystem.current == null) return false;

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
                    return true;
                }
            }

            if (MenuManager.Instance.contextMenu != null)
            {
                GameObject menu = MenuManager.Instance.contextMenu.gameObject;
                if (result.gameObject == menu || result.gameObject.transform.IsChildOf(menu.transform))
                {
                    return true;
                }
            }
        }

        return false;
    }
}