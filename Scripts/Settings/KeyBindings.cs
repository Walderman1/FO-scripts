using System;
using UnityEngine;

[Serializable]
public class KeyBindings
{
    public KeyCode openInventory = KeyCode.I;
    public KeyCode openEquipment = KeyCode.E;
    public KeyCode openQuests = KeyCode.J;
    public KeyCode closeAll = KeyCode.Escape;
    public KeyCode openRadialMenu = KeyCode.Mouse1;
    public KeyCode navigateLeft = KeyCode.LeftArrow;
    public KeyCode navigateRight = KeyCode.RightArrow;
    public KeyCode navigateUp = KeyCode.UpArrow;
    public KeyCode navigateDown = KeyCode.DownArrow;
    public KeyCode splitHalf = KeyCode.LeftShift;
    public KeyCode splitOne = KeyCode.LeftAlt;
    public KeyCode sortInventory = KeyCode.R;
    public KeyCode interactClick = KeyCode.Mouse0;
    public KeyCode useMagicLight = KeyCode.L;
    public KeyCode clairvoyance = KeyCode.F;
    public KeyCode skipDialogue = KeyCode.LeftControl;
    public KeyCode saveGame = KeyCode.F5;
    public KeyCode loadGame = KeyCode.F9;

    public KeyCode Get(string fieldName)
    {
        var f = typeof(KeyBindings).GetField(fieldName);
        return f != null ? (KeyCode)f.GetValue(this) : KeyCode.None;
    }

    public void Set(string fieldName, KeyCode key)
    {
        var f = typeof(KeyBindings).GetField(fieldName);
        if (f != null) f.SetValue(this, key);
    }

    public KeyBindings Clone() => (KeyBindings)MemberwiseClone();
}