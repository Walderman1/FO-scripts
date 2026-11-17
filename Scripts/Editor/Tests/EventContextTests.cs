using NUnit.Framework;
using UnityEngine;

public class EventContextTests
{
    [Test]
    public void WithContainer_ReturnsSameInstance()
    {
        var ctx = new EventContext();
        Assert.AreSame(ctx, ctx.WithContainer(null));
    }

    [Test]
    public void WithItem_SetsSOAndId()
    {
        var so = ScriptableObject.CreateInstance<ItemSO>();
        so.id = "apple";
        var ctx = new EventContext().WithItem(so);
        Assert.AreSame(so, ctx.ItemSO);
        Assert.AreEqual("apple", ctx.ItemId);
        Object.DestroyImmediate(so);
    }

    [Test]
    public void WithItem_NullSO_SetsEmptyId()
    {
        var ctx = new EventContext().WithItem((ItemSO)null);
        Assert.IsNull(ctx.ItemSO);
        Assert.AreEqual("", ctx.ItemId);
    }

    [Test]
    public void WithItemId_SetsId()
    {
        var ctx = new EventContext().WithItemId("sword");
        Assert.AreEqual("sword", ctx.ItemId);
    }

    [Test]
    public void WithLocation_StoresName()
    {
        var ctx = new EventContext().WithLocation("village");
        Assert.AreEqual("village", ctx.LocationName);
    }

    [Test]
    public void WithDialogue_StoresId()
    {
        var ctx = new EventContext().WithDialogue("prologue");
        Assert.AreEqual("prologue", ctx.DialogueID);
    }

    [Test]
    public void WithValue_Int_StoresIntValue()
    {
        var ctx = new EventContext().WithValue(42);
        Assert.AreEqual(42, ctx.IntValue);
    }

    [Test]
    public void WithValue_Float_StoresFloatValue()
    {
        var ctx = new EventContext().WithValue(3.14f);
        Assert.AreEqual(3.14f, ctx.FloatValue, 0.001f);
    }

    [Test]
    public void WithValue_String_StoresStringValue()
    {
        var ctx = new EventContext().WithValue("test");
        Assert.AreEqual("test", ctx.StringValue);
    }

    [Test]
    public void WithData_StoresCustomData()
    {
        var data = new object();
        var ctx = new EventContext().WithData(data);
        Assert.AreSame(data, ctx.CustomData);
    }

    [Test]
    public void WithFlag_StoresNameAndValue()
    {
        var ctx = new EventContext().WithFlag("test_flag", true);
        Assert.AreEqual("test_flag", ctx.FlagName);
        Assert.IsTrue(ctx.FlagValue);
    }

    [Test]
    public void Chaining_MultipleWith_AllStored()
    {
        var ctx = new EventContext()
            .WithLocation("loc")
            .WithDialogue("dlg")
            .WithValue(5)
            .WithFlag("f", true);

        Assert.AreEqual("loc", ctx.LocationName);
        Assert.AreEqual("dlg", ctx.DialogueID);
        Assert.AreEqual(5, ctx.IntValue);
        Assert.AreEqual("f", ctx.FlagName);
    }

    [Test]
    public void Constructor_SourceAndTarget_Stored()
    {
        var src = new GameObject();
        var tgt = new GameObject();
        var ctx = new EventContext(src, tgt);
        Assert.AreSame(src, ctx.SourceObject);
        Assert.AreSame(tgt, ctx.TargetObject);
        Object.DestroyImmediate(src);
        Object.DestroyImmediate(tgt);
    }
}