using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;

public class RadialMenuOpener : MonoBehaviour
{
    [SerializeField] private GameObject radialMenu;
    [SerializeField] private bool blockDuringDialogue = true;

    [Inject] private InputManager _inputManager;
    [Inject] private TextBeginner _textBeginner;
    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private RadialMenuRegistry _registry;

    private RadialMenu cachedRadialMenu;
    private float nextCheckTime = 0f;
    private const float CHECK_INTERVAL = 2f;

    private void Start()
    {
        ResolveRadialMenu();
        if (cachedRadialMenu != null)
        {
            cachedRadialMenu.Hide();
            EnsureCanvasTop(cachedRadialMenu);
        }
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextCheckTime)
        {
            nextCheckTime = Time.unscaledTime + CHECK_INTERVAL;
            if (cachedRadialMenu == null) ResolveRadialMenu();
        }

        if (_inputManager == null) return;
        var cfg = _inputManager.Config;
        if (cfg == null) return;
        if (!Input.GetKeyDown(cfg.openRadialMenu)) return;

        if (SceneManager.GetActiveScene().buildIndex == 0) return;

        if (blockDuringDialogue)
        {
            if (_textBeginner != null && _textBeginner.IsDialogueActive) return;
        }

        if (IsPointerOverContextSource()) return;

        ToggleRadialMenu();
    }

    private bool IsPointerOverContextSource()
    {
        if (EventSystem.current == null) return false;

        var pointer = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(pointer, results);

        foreach (var result in results)
        {
            var go = result.gameObject;
            if (go.GetComponent<InventorySlot>() != null) return true;
            if (go.GetComponent<InventoryItemMarker>() != null) return true;
            if (go.GetComponent<EquipmentSlot>() != null) return true;
        }
        return false;
    }

    private void ToggleRadialMenu()
    {
        if (_radialMenuController == null) return;

        if (_radialMenuController.IsMainMenuOpen())
        {
            _radialMenuController.CloseAllMenus();
        }
        else
        {
            if (cachedRadialMenu == null) ResolveRadialMenu();
            if (cachedRadialMenu != null) EnsureCanvasTop(cachedRadialMenu);
            _radialMenuController.OpenMainMenu();
        }
    }

    private void ResolveRadialMenu()
    {
        cachedRadialMenu = _registry?.MainRadialMenu;

        if (cachedRadialMenu == null && radialMenu != null)
            cachedRadialMenu = radialMenu.GetComponent<RadialMenu>();

        if (cachedRadialMenu == null)
        {
            var all = FindObjectsByType<RadialMenu>(FindObjectsInactive.Include);
            foreach (var m in all)
            {
                if (m == null) continue;
                try { if (m.CompareTag("RadialMenu")) { cachedRadialMenu = m; break; } }
                catch (UnityException) { }
            }
        }

        if (cachedRadialMenu != null) radialMenu = cachedRadialMenu.gameObject;
    }

    private void EnsureCanvasTop(RadialMenu menu)
    {
        if (menu == null) return;

        Canvas canvas = menu.GetComponent<Canvas>();
        if (canvas == null)
        {
            canvas = menu.gameObject.AddComponent<Canvas>();
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
            if (menu.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
                menu.gameObject.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }
        else
        {
            canvas.overrideSorting = true;
            canvas.sortingOrder = 10000;
        }
    }
}