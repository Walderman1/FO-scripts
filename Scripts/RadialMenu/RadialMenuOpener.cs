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
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(openKey))
        {
            // Проверяем, не клик ли по инвентарю
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
                        return;
                    }
                }
            }

            // Открываем или закрываем основное меню
            if (MenuManager.Instance.IsMainMenuOpen())
            {
                MenuManager.Instance.CloseAllMenus();
            }
            else
            {
                MenuManager.Instance.OpenMainMenu();
            }
        }
    }
}