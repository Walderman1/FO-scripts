using UnityEngine;
using UnityEngine.SceneManagement;

public class RadialMenuRegistry : MonoBehaviour
{
    /// <summary>
    /// Быстрое меню панелей (I — инвентарь, E — экипировка, J — квесты, настройки).
    /// Открывается по ПКМ, содержит кнопки, привязанные к <c>RadialAction</c>.
    /// Тег объекта: <c>RadialMenu</c>. Роль в <c>RadialMenu.role</c>: <c>Main</c>.
    /// </summary>
    public RadialMenu MainRadialMenu { get; private set; }
    /// <summary>
    /// Меню выбора в диалоге. Заполняется из <c>TextBeginner.ShowChoices</c>
    /// через <c>SetChoiceMode</c>. Не открывается вручную.
    /// Тег объекта: <c>ChoicePanel</c>. Роль: <c>Choice</c>.
    /// </summary>
    public RadialMenu ChoiceRadialMenu { get; private set; }
    /// <summary>
    /// Меню действий над предметом (использовать, экипировать, разделить, осмотреть, выбросить).
    /// Открывается по ПКМ по слоту инвентаря/экипировки через <c>RadialMenuController.OpenContextMenu</c>.
    /// Тег объекта: <c>ContextMenu</c>. Роль: <c>Context</c>.
    /// </summary>
    public RadialMenu ContextRadialMenu { get; private set; }

    public void Refresh(Scene scene)
    {
        MainRadialMenu = null;
        ChoiceRadialMenu = null;
        ContextRadialMenu = null;

        var roots = scene.GetRootGameObjects();
        int len = roots.Length;

        for (int i = 0; i < len; i++)
        {
            var root = roots[i];
            if (root == null) continue;

            var menus = root.GetComponentsInChildren<RadialMenu>(true);
            int menuLen = menus.Length;

            for (int j = 0; j < menuLen; j++)
            {
                var menu = menus[j];
                if (menu == null) continue;

                string tag = "?";
                try { tag = menu.tag; } catch (UnityException) { }

                Logger.LogVerbose(LogModule.DI, $"candidate name={menu.name} tag={tag} activeSelf={menu.gameObject.activeSelf} activeInHierarchy={menu.gameObject.activeInHierarchy}");

                if (MainRadialMenu == null && tag == "RadialMenu") { MainRadialMenu = menu; Logger.LogVerbose(LogModule.DI, "→ MAIN"); continue; }
                if (ChoiceRadialMenu == null && tag == "ChoicePanel") { ChoiceRadialMenu = menu; Logger.LogVerbose(LogModule.DI, "→ CHOICE"); continue; }
                if (ContextRadialMenu == null && tag == "ContextMenu") { ContextRadialMenu = menu; Logger.LogVerbose(LogModule.DI, "→ CONTEXT"); continue; }
            }
        }

        if (MainRadialMenu == null)
        {
            for (int i = 0; i < len; i++)
            {
                var root = roots[i];
                if (root == null) continue;
                var menus = root.GetComponentsInChildren<RadialMenu>(true);
                for (int j = 0; j < menus.Length; j++)
                {
                    var menu = menus[j];
                    if (menu == null) continue;
                    if (menu.name.IndexOf("Main", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        MainRadialMenu = menu;
                        Logger.LogVerbose(LogModule.DI, $"fallback by name: Main={menu.name}");
                        break;
                    }
                }
                if (MainRadialMenu != null) break;
            }
        }

        Logger.LogVerbose(LogModule.DI, $"RESULT scene={scene.name} Main={(MainRadialMenu != null ? MainRadialMenu.name : "null")} Choice={(ChoiceRadialMenu != null ? ChoiceRadialMenu.name : "null")} Context={(ContextRadialMenu != null ? ContextRadialMenu.name : "null")}");
    }
}