using UnityEngine;
using UnityEngine.EventSystems;

public class RadialMenuOpener : MonoBehaviour
{
    [SerializeField] private GameObject radialMenu;
    [SerializeField] private KeyCode openKey = KeyCode.Mouse1;

    private RadialMenu cachedRadialMenu;

    private void Start()
    {
        if (radialMenu != null)
        {
            cachedRadialMenu = radialMenu.GetComponent<RadialMenu>();
            if (cachedRadialMenu != null)
            {
                cachedRadialMenu.Hide();
                Logger.Log(LogModule.RadialMenu, "RadialMenuOpener: RadialMenu скрыт при старте");
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            if (EventSystem.current != null)
            {
                var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
                var results = new System.Collections.Generic.List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, results);

                foreach (var result in results)
                {
                    if (result.gameObject.GetComponent<InventorySlot>() != null ||
                        result.gameObject.GetComponent<InventoryItemMarker>() != null ||
                        result.gameObject.CompareTag("InventoryPanel"))
                    {
                        Logger.Log(LogModule.RadialMenu, "Клик по инвентарю, открытие меню заблокировано");
                        return;
                    }
                }
            }

            if (MenuManager.Instance.IsMainMenuOpen())
            {
                MenuManager.Instance.CloseAllMenus();
                Logger.Log(LogModule.RadialMenu, "Главное меню закрыто по повторному нажатию");
            }
            else
            {
                MenuManager.Instance.OpenMainMenu();
                Logger.Log(LogModule.RadialMenu, "Главное меню открыто");
            }
        }
    }
}
