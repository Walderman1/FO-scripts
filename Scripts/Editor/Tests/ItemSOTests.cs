using NUnit.Framework;
using UnityEngine;

public class ItemSOTests
{
    private readonly System.Collections.Generic.List<ItemSO> _created = new System.Collections.Generic.List<ItemSO>();

    [TearDown]
    public void TearDown()
    {
        foreach (var so in _created) if (so != null) Object.DestroyImmediate(so);
        _created.Clear();
    }

    private ItemSO Make()
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        _created.Add(so);
        return so;
    }

    [Test]
    public void ToItemData_CopiesAllFields()
    {
        var so = Make();
        so.id = "test_id";
        so.itemName = "Test";
        so.description = "Desc";
        so.canStack = true;
        so.maxStack = 42;
        so.isEquippable = true;
        so.equipmentType = EquipmentType.Weapon;
        so.category = ItemCategory.Weapon;
        so.isUsable = true;
        so.useButtonText = "Use it";

        var data = so.ToItemData();

        Assert.AreEqual("test_id", data.id);
        Assert.AreEqual("Test", data.itemName);
        Assert.AreEqual("Desc", data.description);
        Assert.IsTrue(data.canStack);
        Assert.AreEqual(42, data.maxStack);
        Assert.IsTrue(data.isEquippable);
        Assert.AreEqual(EquipmentType.Weapon, data.equipmentType);
        Assert.AreEqual(ItemCategory.Weapon, data.category);
        Assert.IsTrue(data.isUsable);
        Assert.AreEqual("Use it", data.useButtonText);
    }

    [Test]
    public void ToItemData_DefaultValues_NotNull()
    {
        var so = Make();
        var data = so.ToItemData();
        Assert.IsNotNull(data);
    }
}