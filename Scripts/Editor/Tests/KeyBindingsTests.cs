using NUnit.Framework;
using UnityEngine;

public class KeyBindingsTests
{
    [Test]
    public void DefaultValues_AreExpected()
    {
        var kb = new KeyBindings();
        Assert.AreEqual(KeyCode.I, kb.openInventory);
        Assert.AreEqual(KeyCode.E, kb.openEquipment);
        Assert.AreEqual(KeyCode.J, kb.openQuests);
        Assert.AreEqual(KeyCode.Escape, kb.closeAll);
        Assert.AreEqual(KeyCode.Mouse1, kb.openRadialMenu);
        Assert.AreEqual(KeyCode.F5, kb.saveGame);
        Assert.AreEqual(KeyCode.F9, kb.loadGame);
    }

    [Test]
    public void Get_ExistingField_ReturnsValue()
    {
        var kb = new KeyBindings { openInventory = KeyCode.K };
        Assert.AreEqual(KeyCode.K, kb.Get("openInventory"));
    }

    [Test]
    public void Get_UnknownField_ReturnsNone()
    {
        var kb = new KeyBindings();
        Assert.AreEqual(KeyCode.None, kb.Get("nonexistent"));
    }

    [Test]
    public void Set_ExistingField_UpdatesValue()
    {
        var kb = new KeyBindings();
        kb.Set("openInventory", KeyCode.Q);
        Assert.AreEqual(KeyCode.Q, kb.openInventory);
    }

    [Test]
    public void Set_UnknownField_DoesNotThrow()
    {
        var kb = new KeyBindings();
        Assert.DoesNotThrow(() => kb.Set("nonexistent", KeyCode.Q));
    }

    [Test]
    public void Clone_IsIndependent()
    {
        var original = new KeyBindings { openInventory = KeyCode.K };
        var clone = original.Clone();
        Assert.AreEqual(KeyCode.K, clone.openInventory);

        clone.openInventory = KeyCode.L;
        Assert.AreEqual(KeyCode.K, original.openInventory);
        Assert.AreEqual(KeyCode.L, clone.openInventory);
    }
}