using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Вспомогательные функции для предметов, которые создаются из картинок прямо во время игры.
/// Здесь нет ничего, что зависело бы от редактора Unity — класс работает и в билде.
/// </summary>
public static class RuntimeItemNaming
{
    /// <summary>Транслитерация кириллицы в латиницу (для стабильного id из имени файла).</summary>
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

    /// <summary>Поддерживаемые расширения картинок.</summary>
    public static bool IsImageFile(string path)
    {
        if (string.IsNullOrEmpty(path)) return false;

        string ext = System.IO.Path.GetExtension(path);
        if (string.IsNullOrEmpty(ext)) return false;

        ext = ext.ToLowerInvariant();
        return ext == ".png" || ext == ".jpg" || ext == ".jpeg";
    }

    /// <summary>
    /// Приводит произвольную строку к безопасному id: "Красный цветок.png" -> "krasnyy_tsvetok".
    /// </summary>
    public static string Slugify(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return "";

        source = source.ToLowerInvariant();

        var sb = new StringBuilder(source.Length);
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

        string result = sb.ToString().TrimEnd('_');

        // id не должен начинаться с цифры — так удобнее читать в сохранениях и логах.
        if (result.Length > 0 && result[0] >= '0' && result[0] <= '9')
            result = "item_" + result;

        return result;
    }

    /// <summary>Имя предмета по имени файла: "Красный_цветок" -> "Красный цветок".</summary>
    public static string DisplayNameFromFile(string fileNameWithoutExtension)
    {
        if (string.IsNullOrEmpty(fileNameWithoutExtension)) return "Предмет";

        string name = fileNameWithoutExtension.Replace('_', ' ').Trim();
        if (name.Length == 0) return "Предмет";

        return char.ToUpperInvariant(name[0]) + name.Substring(1);
    }

    /// <summary>Делает id уникальным внутри набора уже занятых id.</summary>
    public static string MakeUnique(string baseId, ICollection<string> usedIds)
    {
        if (string.IsNullOrEmpty(baseId)) baseId = "item";
        if (usedIds == null || !usedIds.Contains(baseId)) return baseId;

        for (int i = 2; i < 10000; i++)
        {
            string candidate = baseId + "_" + i;
            if (!usedIds.Contains(candidate)) return candidate;
        }

        return baseId + "_" + Mathf.Abs(baseId.GetHashCode()).ToString();
    }
}
