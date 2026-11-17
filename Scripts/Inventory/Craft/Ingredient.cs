using UnityEngine;

[System.Serializable]
public class Ingredient
{
    public ItemSO itemSO;

    [System.Obsolete("Используйте itemSO")]
    public ItemType itemType = ItemType.None;

    public int count = 1;

    public string GetId()
    {
        if (itemSO != null) return itemSO.id;

#pragma warning disable CS0618
        if (itemType != ItemType.None)
        {
            // ★ ScriptableObject → GameReferences.Instance оправдан
            itemSO = GameReferences.Instance?.ItemDatabase?.GetItemByLegacyType(itemType);
            return itemSO != null ? itemSO.id : "";
        }
#pragma warning restore CS0618

        return "";
    }

    public ItemSO ResolveItemSO()
    {
        if (itemSO != null) return itemSO;
        GetId();
        return itemSO;
    }

    public bool Matches(string id)
    {
        if (string.IsNullOrEmpty(id)) return false;
        return GetId() == id;
    }

    public string GetDisplayName()
    {
        var so = ResolveItemSO();
        if (so != null) return so.itemName;
        return "???";
    }

    public bool IsValid()
    {
        if (count <= 0) return false;
        return !string.IsNullOrEmpty(GetId());
    }
}