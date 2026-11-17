using VContainer;

public enum UIKind
{
    Inventory,
    Equipment,
    Craft,
    Spells,
    Quests,
    Settings
}

public static class UICoordinator
{
    public static void BeforeOpen(UIKind kind)
    {
        var refs = GameReferences.Instance;
        var sceneScope = SceneInjector.Instance?.CurrentSceneScope;
        var rootScope = GameLifetimeScope.Current?.Container;

        bool isCharacterPanel = kind == UIKind.Inventory
                             || kind == UIKind.Equipment
                             || kind == UIKind.Craft
                             || kind == UIKind.Spells;

        if (isCharacterPanel)
        {
            if (kind != UIKind.Craft)
            {
                var craft = Resolve<CraftingUI>(sceneScope) ?? refs?.CraftingUI;
                craft?.SetVisible(false);
            }

            if (kind != UIKind.Spells)
            {
                var spell = Resolve<MagicSpellPanel>(sceneScope) ?? refs?.MagicSpellPanel;
                spell?.SetVisible(false);
            }
        }
        else
        {
            var inv = Resolve<InventoryUIManager>(sceneScope) ?? refs?.InventoryUIManager;
            inv?.CloseInventory();

            var craft = Resolve<CraftingUI>(sceneScope) ?? refs?.CraftingUI;
            craft?.SetVisible(false);

            var spell = Resolve<MagicSpellPanel>(sceneScope) ?? refs?.MagicSpellPanel;
            spell?.SetVisible(false);

            var gcEquip = Resolve<GlobalControl>(rootScope) ?? refs?.GlobalControl;
            gcEquip?.CloseEquipment();
        }

        var quests = Resolve<QuestUI>(sceneScope) ?? refs?.QuestUI;
        quests?.CloseQuestPanel();

        var itemView = Resolve<ItemViewPanel>(sceneScope) ?? refs?.ItemViewPanel;
        itemView?.HidePanel();

        var rmc = Resolve<RadialMenuController>(sceneScope) ?? refs?.RadialMenuController;
        rmc?.CloseAllMenus();

        var gc = Resolve<GlobalControl>(rootScope) ?? refs?.GlobalControl;
        if (gc != null && kind != UIKind.Settings)
            gc.CloseMenu();
    }

    private static T Resolve<T>(IObjectResolver scope) where T : class
    {
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }
}