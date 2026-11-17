using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using VContainer;

public class MenuCloseHandler : MonoBehaviour
{
    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private TextBeginner _textBeginner;
    [Inject] private RadialMenuRegistry _registry;

    private void Update()
    {
        if (_radialMenuController == null) return;
        if (!_radialMenuController.IsAnyMenuOpen()) return;
        if (!Input.GetMouseButtonDown(0)) return;
        if (_textBeginner != null && _textBeginner.IsDialogueActive) return;
        if (InventoryItemMarker.IsAnyDragging) return;

        if (IsClickOnMenu()) return;
        _radialMenuController.CloseAllMenus();
    }

    private bool IsClickOnMenu()
    {
        EventSystem es = EventSystem.current;
        if (es == null)
        {
            var all = FindObjectsByType<EventSystem>(FindObjectsInactive.Exclude);
            if (all.Length > 0) es = all[0];
            if (es == null) return false;
        }

        var pointer = new PointerEventData(es) { position = Input.mousePosition };
        var results = new List<RaycastResult>();
        es.RaycastAll(pointer, results);

        if (_registry == null) return false;

        foreach (var result in results)
        {
            if (_registry.MainRadialMenu != null && ContainsTransform(_registry.MainRadialMenu.transform, result.gameObject))
                return true;
            if (_registry.ContextRadialMenu != null && ContainsTransform(_registry.ContextRadialMenu.transform, result.gameObject))
                return true;
            if (_registry.ChoiceRadialMenu != null && ContainsTransform(_registry.ChoiceRadialMenu.transform, result.gameObject))
                return true;
        }

        return false;
    }

    private static bool ContainsTransform(Transform root, GameObject go)
    {
        if (root == null || go == null) return false;
        return go == root.gameObject || go.transform.IsChildOf(root);
    }
}