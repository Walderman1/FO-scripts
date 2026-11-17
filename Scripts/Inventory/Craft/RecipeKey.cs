using System;
using System.Collections.Generic;
using System.Linq;

public struct RecipeKey : IEquatable<RecipeKey>
{
    private readonly (string id, int count)[] _entries;

    public RecipeKey(Dictionary<string, int> input)
    {
        _entries = input
            .Where(kv => !string.IsNullOrEmpty(kv.Key) && kv.Value > 0)
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToArray();
    }

    public RecipeKey(IEnumerable<Ingredient> ingredients)
    {
        var dict = new Dictionary<string, int>();
        foreach (var ing in ingredients)
        {
            if (ing == null) continue;
            string id = ing.GetId();
            if (string.IsNullOrEmpty(id) || ing.count <= 0) continue;

            dict[id] = dict.TryGetValue(id, out int c) ? c + ing.count : ing.count;
        }

        _entries = dict
            .OrderBy(kv => kv.Key, StringComparer.Ordinal)
            .Select(kv => (kv.Key, kv.Value))
            .ToArray();
    }

    public bool Equals(RecipeKey other)
    {
        if (_entries.Length != other._entries.Length) return false;
        for (int i = 0; i < _entries.Length; i++)
            if (_entries[i] != other._entries[i]) return false;
        return true;
    }

    public override bool Equals(object obj) => obj is RecipeKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            foreach (var e in _entries)
                hash = hash * 31 + (e.id?.GetHashCode() ?? 0) ^ e.count;
            return hash;
        }
    }

    public override string ToString() =>
        string.Join(", ", _entries.Select(e => $"{e.id}x{e.count}"));
}