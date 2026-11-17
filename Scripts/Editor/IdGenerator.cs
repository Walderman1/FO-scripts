using System.Collections.Generic;
using System.Text;
using UnityEngine;

public static class IdGenerator
{
    private static readonly Dictionary<char, string> Translit = new Dictionary<char, string>
    {
        {'а', "a"}, {'б', "b"}, {'в', "v"}, {'г', "g"}, {'д', "d"},
        {'е', "e"}, {'ё', "yo"}, {'ж', "zh"}, {'з', "z"}, {'и', "i"},
        {'й', "y"}, {'к', "k"}, {'л', "l"}, {'м', "m"}, {'н', "n"},
        {'о', "o"}, {'п', "p"}, {'р', "r"}, {'с', "s"}, {'т', "t"},
        {'у', "u"}, {'ф', "f"}, {'х', "kh"}, {'ц', "ts"}, {'ч', "ch"},
        {'ш', "sh"}, {'щ', "shch"}, {'ъ', ""}, {'ы', "y"}, {'ь', ""},
        {'э', "e"}, {'ю', "yu"}, {'я', "ya"}
    };

    public static string Slugify(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return "";

        source = source.ToLowerInvariant();
        var sb = new StringBuilder();
        bool lastUnderscore = false;

        foreach (char c in source)
        {
            if (Translit.TryGetValue(c, out var tr))
            {
                sb.Append(tr);
                lastUnderscore = false;
                continue;
            }

            if ((c >= 'a' && c <= 'z') || (c >= '0' && c <= '9'))
            {
                sb.Append(c);
                lastUnderscore = false;
                continue;
            }

            if (char.IsWhiteSpace(c) || c == '_' || c == '-')
            {
                if (sb.Length > 0 && !lastUnderscore)
                {
                    sb.Append('_');
                    lastUnderscore = true;
                }
            }
        }

        return sb.ToString().TrimEnd('_');
    }

    public static string Combine(string prefix, params string[] parts)
    {
        var sb = new StringBuilder();

        if (!string.IsNullOrEmpty(prefix))
        {
            var p = Slugify(prefix);
            if (!string.IsNullOrEmpty(p)) sb.Append(p);
        }

        if (parts != null)
        {
            foreach (var part in parts)
            {
                var slug = Slugify(part);
                if (string.IsNullOrEmpty(slug)) continue;
                if (sb.Length > 0) sb.Append('_');
                sb.Append(slug);
            }
        }

        return sb.ToString();
    }

    public static string FromAssetName(Object asset, string prefix = "")
    {
        if (asset == null) return "";
        return Combine(prefix, asset.name);
    }

    public static bool ContainsCyrillic(string text)
    {
        if (string.IsNullOrEmpty(text)) return false;
        foreach (char c in text)
        {
            if (c >= 'А' && c <= 'я') return true;
            if (c == 'ё' || c == 'Ё') return true;
        }
        return false;
    }
}