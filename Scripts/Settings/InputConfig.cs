using UnityEngine;

[CreateAssetMenu(fileName = "КонфигIO", menuName = "|Конфиги|/Конфиг IO")]
public class InputConfig : ScriptableObject
{
    [Header("Gameplay")]
    public KeyCode openInventory = KeyCode.I;
    public KeyCode openEquipment = KeyCode.E;
    public KeyCode openQuests = KeyCode.J;
    public KeyCode closeAll = KeyCode.Escape;

    [Header("Radial Menu")]
    public KeyCode openRadialMenu = KeyCode.Mouse1;

    [Header("Navigation")]
    public KeyCode navigateLeft = KeyCode.LeftArrow;
    public KeyCode navigateRight = KeyCode.RightArrow;
    public KeyCode navigateUp = KeyCode.UpArrow;
    public KeyCode navigateDown = KeyCode.DownArrow;

    [Header("Inventory Modifiers")]
    public KeyCode splitHalf = KeyCode.LeftShift;
    public KeyCode splitOne = KeyCode.LeftAlt;
    public KeyCode sortInventory = KeyCode.R;

    [Header("Interaction")]
    public KeyCode interactClick = KeyCode.Mouse0;

    [Header("Abilities")]
    public KeyCode useMagicLight = KeyCode.L;
    public KeyCode clairvoyance = KeyCode.F;

    [Header("Dialogue")]
    public KeyCode skipDialogue = KeyCode.LeftControl;

    [Header("Save System")]
    public KeyCode saveGame = KeyCode.F5;
    public KeyCode loadGame = KeyCode.F9;

    public bool SplitHalfPressed()
    {
        return Input.GetKey(splitHalf) || Input.GetKey(KeyCode.RightShift);
    }

    public bool SplitOnePressed()
    {
        return Input.GetKey(splitOne) || Input.GetKey(KeyCode.RightAlt);
    }

    public bool SortInventoryPressed() => Input.GetKeyDown(sortInventory);

    public bool NavigateLeftPressed() => Input.GetKeyDown(navigateLeft);
    public bool NavigateRightPressed() => Input.GetKeyDown(navigateRight);
    public bool NavigateUpPressed() => Input.GetKeyDown(navigateUp);
    public bool NavigateDownPressed() => Input.GetKeyDown(navigateDown);

    public bool OpenInventoryPressed() => Input.GetKeyDown(openInventory);
    public bool OpenEquipmentPressed() => Input.GetKeyDown(openEquipment);
    public bool OpenQuestsPressed() => Input.GetKeyDown(openQuests);
    public bool CloseAllPressed() => Input.GetKeyDown(closeAll);
    public bool OpenRadialMenuPressed() => Input.GetKeyDown(openRadialMenu);
    public bool InteractClickPressed() => Input.GetKeyDown(interactClick);
    public bool UseMagicLightPressed() => Input.GetKeyDown(useMagicLight);
    public bool ClairvoyancePressed() => Input.GetKeyDown(clairvoyance);
    public bool SkipDialogueHeld() => Input.GetKey(skipDialogue);
    public bool SaveGamePressed() => Input.GetKeyDown(saveGame);
    public bool LoadGamePressed() => Input.GetKeyDown(loadGame);

    public void ApplyBindings(KeyBindings b)
    {
        if (b == null) return;
        openInventory = b.openInventory;
        openEquipment = b.openEquipment;
        openQuests = b.openQuests;
        closeAll = b.closeAll;
        openRadialMenu = b.openRadialMenu;
        navigateLeft = b.navigateLeft;
        navigateRight = b.navigateRight;
        navigateUp = b.navigateUp;
        navigateDown = b.navigateDown;
        splitHalf = b.splitHalf;
        splitOne = b.splitOne;
        sortInventory = b.sortInventory;
        interactClick = b.interactClick;
        useMagicLight = b.useMagicLight;
        clairvoyance = b.clairvoyance;
        skipDialogue = b.skipDialogue;
        saveGame = b.saveGame;
        loadGame = b.loadGame;
    }

    public KeyBindings ToKeyBindings()
    {
        return new KeyBindings
        {
            openInventory = openInventory,
            openEquipment = openEquipment,
            openQuests = openQuests,
            closeAll = closeAll,
            openRadialMenu = openRadialMenu,
            navigateLeft = navigateLeft,
            navigateRight = navigateRight,
            navigateUp = navigateUp,
            navigateDown = navigateDown,
            splitHalf = splitHalf,
            splitOne = splitOne,
            sortInventory = sortInventory,
            interactClick = interactClick,
            useMagicLight = useMagicLight,
            clairvoyance = clairvoyance,
            skipDialogue = skipDialogue,
            saveGame = saveGame,
            loadGame = loadGame,
        };
    }
}