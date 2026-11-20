using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

/// <summary>Один предмет, найденный в папке предметов (или ошибка по конкретному файлу).</summary>
public class RuntimeItemEntry
{
    public string id;
    public string displayName;
    public string filePath;
    public string jsonPath;
    public string error;

    public ItemSO item;
    public Texture2D texture;
    public Sprite iconSprite;
    public Sprite worldSprite;

    public float worldSize;
    public float uiSize;

    public long fileLength;
    public DateTime fileWriteUtc;

    public bool missing;

    public bool HasError { get { return !string.IsNullOrEmpty(error); } }
}

/// <summary>
/// Главная точка входа для предметов из картинок.
///
/// Художник кладёт PNG (или JPG) в папку ItemsInbox — она лежит рядом с игрой
/// (в редакторе Unity — в корне проекта). Игра сама находит картинки, делает из них
/// полноценные ItemSO (иконка для UI + спрайт для мира) и регистрирует их в ItemDatabase.
///
/// Ничего создавать в редакторе Unity не нужно — всё работает в билде.
/// </summary>
public static class RuntimeItemFolder
{
    public const string FolderName = "ItemsInbox";

    /// <summary>
    /// Принудительный размер предмета в мире (в юнитах). 0 (по умолчанию) — размер как у обычных
    /// предметов: картинка / 100 ppu * 0.2 (зелье 206x341 → 0.41x0.68 юнита).
    /// </summary>
    public const float DefaultWorldSize = 0f;

    /// <summary>
    /// Принудительный размер иконки в UI. 0 (по умолчанию) — размер как у обычных предметов:
    /// иконка занимает столько же места, сколько картинка в пикселях, умноженная на 0.2.
    /// </summary>
    public const float DefaultUiSize = 0f;

    /// <summary>Как импортированы спрайты обычных предметов: 100 пикселей на юнит.</summary>
    public const float NativePixelsPerUnit = 100f;

    /// <summary>
    /// Предел размера картинки предмета: не больше 512x512 пикселей по каждой стороне.
    /// Файлы крупнее не загружаются (иконка и спрайт не создаются), а в DevTools появляется
    /// ошибка с фактическим размером — так в инвентарь не попадают огромные иконки.
    /// </summary>
    public const int MaxIconSide = 512;

    /// <summary>
    /// Верхняя граница веса файла картинки. 512x512 в 16 битах на канал весит около 2 МБ,
    /// поэтому файл тяжелее 4 МБ точно не пролезет по размеру — его отвергаем, не декодируя
    /// (декодирование 8K-картинки съело бы сотни мегабайт памяти).
    /// </summary>
    private const long MaxIconFileBytes = 4L * 1024 * 1024;

    private static readonly List<string> _folders = new List<string>();
    private static readonly List<RuntimeItemEntry> _entries = new List<RuntimeItemEntry>();
    private static readonly List<ItemSO> _items = new List<ItemSO>();
    private static readonly List<string> _globalErrors = new List<string>();

    private static string _signature = "";
    private static bool _scannedOnce;

    public static IReadOnlyList<string> Folders { get { return _folders; } }
    public static IReadOnlyList<RuntimeItemEntry> Entries { get { return _entries; } }
    public static IReadOnlyList<ItemSO> Items { get { return _items; } }
    public static IReadOnlyList<string> Errors { get { return _globalErrors; } }
    public static bool HasScanned { get { return _scannedOnce; } }
    public static int ItemCount { get { return _items.Count; } }

    /// <summary>Папка, которая считается основной (её открывает кнопка «Открыть папку»).</summary>
    public static string PrimaryFolder
    {
        get
        {
            for (int i = 0; i < _folders.Count; i++)
                if (Directory.Exists(_folders[i])) return _folders[i];

            return _folders.Count > 0 ? _folders[0] : "";
        }
    }

    /// <summary>
    /// Быстрая проверка: изменилось ли содержимое папки с прошлого сканирования.
    /// Возвращает true, если был выполнен полный пересбор.
    /// </summary>
    public static bool ScanIfChanged()
    {
        EnsureFolders();
        if (!_scannedOnce) return ScanInternal(true);

        string signature = ComputeSignature();
        if (signature == _signature) return false;

        return ScanInternal(true);
    }

    /// <summary>Полное сканирование папок и пересбор предметов.</summary>
    public static void Scan(bool force)
    {
        ScanInternal(force);
    }

    private static bool ScanInternal(bool force)
    {
        _globalErrors.Clear();
        EnsureFolders();

        string signature = ComputeSignature();
        if (!force && _scannedOnce && signature == _signature) return false;

        _signature = signature;
        _scannedOnce = true;

        var files = new List<string>();
        for (int i = 0; i < _folders.Count; i++)
        {
            string folder = _folders[i];
            if (!Directory.Exists(folder)) continue;

            string[] found;
            try
            {
                found = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
            }
            catch (Exception e)
            {
                _globalErrors.Add($"Не удалось прочитать папку '{folder}': {e.Message}");
                continue;
            }

            for (int f = 0; f < found.Length; f++)
                if (RuntimeItemNaming.IsImageFile(found[f])) files.Add(found[f]);
        }

        files.Sort(StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (int i = 0; i < files.Count; i++)
        {
            string file = files[i];
            if (seen.Contains(file)) continue;
            seen.Add(file);

            try
            {
                ProcessFile(file);
            }
            catch (Exception e)
            {
                RecordFileError(file, e.Message);
                Logger.LogWarning(LogModule.Inventory, $"Предмет из '{file}' не создан: {e.Message}");
            }
        }

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || string.IsNullOrEmpty(entry.filePath)) continue;
            entry.missing = !seen.Contains(entry.filePath);
        }

        RebuildItemsList();

        Logger.Log(LogModule.Inventory,
            $"Папка предметов обработана: картинок {files.Count}, предметов {_items.Count}, ошибок {_globalErrors.Count}");
        return true;
    }

    private static void ProcessFile(string path)
    {
        string baseName = Path.GetFileNameWithoutExtension(path);
        string jsonPath = Path.ChangeExtension(path, ".json");

        string json = null;
        if (File.Exists(jsonPath))
        {
            try { json = File.ReadAllText(jsonPath); }
            catch (Exception e)
            {
                Logger.LogWarning(LogModule.Inventory, $"Не удалось прочитать '{jsonPath}': {e.Message}");
            }
        }

        var entry = FindEntry(path);

        string id = entry != null && !string.IsNullOrEmpty(entry.id)
            ? entry.id
            : BuildId(baseName);

        // Новый предмет или уже существующий (у него просто перерисовали картинку).
        bool isNewItem = entry == null || entry.item == null;

        ItemSO so;
        if (!isNewItem)
        {
            so = entry.item;
        }
        else
        {
            so = ScriptableObject.CreateInstance<ItemSO>();
            so.hideFlags = HideFlags.DontSave;
            so.name = "RT_" + id;
        }

        if (entry == null)
        {
            entry = new RuntimeItemEntry();
            _entries.Add(entry);
        }

        so.id = id;

        float worldSize;
        float uiSize;
        ApplyDefinition(so, json, RuntimeItemNaming.DisplayNameFromFile(baseName),
            CategoryFromFolder(path), out worldSize, out uiSize);

        // Страховка от гигантских файлов: 512x512 даже в 16 битах на канал весит ~2 МБ,
        // поэтому более тяжёлый файл заведомо не пролезет — не тратим память на декодирование.
        long fileBytes = 0;
        try { fileBytes = new FileInfo(path).Length; } catch { }

        if (fileBytes > MaxIconFileBytes)
        {
            RejectIcon(path, entry, so, id, isNewItem,
                $"файл весит {fileBytes / (1024f * 1024f):0.#} МБ — это больше предела " +
                $"{MaxIconSide}x{MaxIconSide} px, иконка не загружена: уменьшите картинку",
                null);
            return;
        }

        Texture2D texture = LoadTexture(path);
        if (texture == null)
        {
            RecordFileError(path, "не удалось прочитать картинку (повреждённый или неподдерживаемый файл)");
            entry.item = so;
            entry.id = id;
            return;
        }

        // Картинку больше 512 px не принимаем вовсе: огромная иконка ломает слоты инвентаря.
        if (texture.width > MaxIconSide || texture.height > MaxIconSide)
        {
            RejectIcon(path, entry, so, id, isNewItem,
                $"картинка {texture.width}x{texture.height} больше предела " +
                $"{MaxIconSide}x{MaxIconSide} px — иконка не загружена: уменьшите картинку",
                texture);
            return;
        }

        Sprite iconSprite = CreateSprite(texture, id + "_icon", NativePixelsPerUnit);

        // Мир: по умолчанию как у обычных предметов — 100 ppu и корень scale 0.2.
        // Если задан worldSize — вписываем предмет в этот размер (по большей стороне).
        float maxSide = Mathf.Max(texture.width, texture.height);
        float worldPixelsPerUnit = worldSize > 0.01f
            ? maxSide * RuntimeItemVisuals.WorldRootScale / worldSize
            : NativePixelsPerUnit;

        Sprite worldSprite = CreateSprite(texture, id + "_world", worldPixelsPerUnit);

        // Освобождаем предыдущую текстуру этого же предмета (перерисовали картинку).
        if (entry.texture != null && entry.texture != texture)
            UnityEngine.Object.Destroy(entry.texture);

        entry.id = id;
        entry.filePath = path;
        entry.jsonPath = File.Exists(jsonPath) ? jsonPath : "";
        entry.displayName = so.itemName;
        entry.item = so;
        entry.texture = texture;
        entry.iconSprite = iconSprite;
        entry.worldSprite = worldSprite;
        entry.worldSize = worldSize;
        entry.uiSize = uiSize;
        entry.error = null;
        entry.missing = false;

        try
        {
            var info = new FileInfo(path);
            entry.fileLength = info.Length;
            entry.fileWriteUtc = info.LastWriteTimeUtc;
        }
        catch
        {
            // Размер файла нужен только для отладки — не критично.
        }

        so.icon = iconSprite;

        RuntimeItemVisuals.Rebuild(so, iconSprite, uiSize, worldSprite, worldSize);

        string iconInfo = uiSize > 0.01f
            ? $"иконка {uiSize:0.#} px (принудительно)"
            : $"иконка {texture.width * RuntimeItemVisuals.UIRootScale:0.#}x{texture.height * RuntimeItemVisuals.UIRootScale:0.#} px";

        string worldInfo = worldSize > 0.01f
            ? $"мир {worldSize:0.##} юнита (принудительно)"
            : $"мир {worldSprite.bounds.size.x * RuntimeItemVisuals.WorldRootScale:0.##}x{worldSprite.bounds.size.y * RuntimeItemVisuals.WorldRootScale:0.##} юнита";

        Logger.Log(LogModule.Inventory,
            $"Предмет из папки «{so.itemName}» ({id}): картинка {texture.width}x{texture.height}, {iconInfo}, {worldInfo}");
    }

    /// <summary>
    /// Картинка не прошла по размеру: иконка и спрайт не создаются, в DevTools пишется причина.
    ///
    /// Если предмет новый — он не появляется в игре вообще (иначе в инвентаре был бы пустой слот),
    /// а в DevTools видна строка с ошибкой. Если предмет уже существовал — он остаётся работать
    /// с прежней иконкой, но в списке предметов и в «Ошибках папки» показывается ошибка.
    /// </summary>
    private static void RejectIcon(string path, RuntimeItemEntry entry, ItemSO so, string id,
        bool isNewItem, string message, Texture2D texture)
    {
        if (texture != null) UnityEngine.Object.Destroy(texture);

        // filePath выставляем до RecordFileError, чтобы ошибка попала в эту же запись, а не в дубль.
        entry.filePath = path;
        RecordFileError(path, message);
        entry.id = id;
        entry.missing = false;

        Logger.LogWarning(LogModule.Inventory,
            $"Предмет из '{Path.GetFileName(path)}' не создан: {message}");

        if (!isNewItem) return;

        UnityEngine.Object.Destroy(so);
        entry.item = null;
    }

    private static void ApplyDefinition(ItemSO so, string json, string displayName,
        ItemCategory folderCategory, out float worldSize, out float uiSize)
    {
        worldSize = DefaultWorldSize;
        uiSize = DefaultUiSize;

        so.itemName = displayName;
        so.description = "";
        so.category = folderCategory;
        so.canStack = true;
        so.maxStack = 99;
        so.isUsable = false;
        so.useButtonText = "Использовать";
        so.isEquippable = false;
        so.equipmentType = EquipmentType.None;
        so.highlightColor = Color.yellow;
        so.highlightScale = 1.15f;
        so.animationSpeed = 4f;
        so.hoverEffectPath = "Particles/HoverEffect";
        so.pickupEffectPath = "Particles/PickupEffect";
        so.pickupSound = null;
        so.healthRestore = 0;
        so.staminaRestore = 0;
        so.damageBonus = 0;
        so.defenseBonus = 0;
        so.customTags = new string[0];

        if (string.IsNullOrWhiteSpace(json)) return;

        var def = RuntimeItemDefinition.Parse(json, so.id + ".json");
        if (def == null) return;

        if (RuntimeItemDefinition.HasKey(json, "name") && !string.IsNullOrWhiteSpace(def.name))
            so.itemName = def.name.Trim();

        if (RuntimeItemDefinition.HasKey(json, "description"))
            so.description = def.description ?? "";

        if (RuntimeItemDefinition.HasKey(json, "category") &&
            TryParseEnum<ItemCategory>(def.category, out var category))
            so.category = category;

        if (RuntimeItemDefinition.HasKey(json, "canStack"))
            so.canStack = def.canStack;

        if (RuntimeItemDefinition.HasKey(json, "maxStack"))
            so.maxStack = Mathf.Clamp(def.maxStack, 1, 9999);

        if (RuntimeItemDefinition.HasKey(json, "usable"))
            so.isUsable = def.usable;

        if (RuntimeItemDefinition.HasKey(json, "useButtonText") && !string.IsNullOrWhiteSpace(def.useButtonText))
            so.useButtonText = def.useButtonText;

        if (RuntimeItemDefinition.HasKey(json, "equippable"))
            so.isEquippable = def.equippable;

        if (RuntimeItemDefinition.HasKey(json, "equipmentType") &&
            TryParseEnum<EquipmentType>(def.equipmentType, out var equipmentType))
            so.equipmentType = equipmentType;

        if (RuntimeItemDefinition.HasKey(json, "worldSize") && def.worldSize > 0.01f)
            worldSize = def.worldSize;

        if (RuntimeItemDefinition.HasKey(json, "uiSize") && def.uiSize > 1f)
            uiSize = def.uiSize;

        if (RuntimeItemDefinition.HasKey(json, "highlightColor") &&
            !string.IsNullOrWhiteSpace(def.highlightColor) &&
            ColorUtility.TryParseHtmlString(def.highlightColor, out var highlight))
            so.highlightColor = highlight;

        if (RuntimeItemDefinition.HasKey(json, "highlightScale") && def.highlightScale > 0f)
            so.highlightScale = Mathf.Clamp(def.highlightScale, 1f, 2f);

        if (RuntimeItemDefinition.HasKey(json, "animationSpeed") && def.animationSpeed > 0f)
            so.animationSpeed = def.animationSpeed;

        if (RuntimeItemDefinition.HasKey(json, "hoverEffectPath"))
            so.hoverEffectPath = def.hoverEffectPath ?? "";

        if (RuntimeItemDefinition.HasKey(json, "pickupEffectPath"))
            so.pickupEffectPath = def.pickupEffectPath ?? "";

        if (RuntimeItemDefinition.HasKey(json, "tags") && def.tags != null)
            so.customTags = def.tags;

        if (RuntimeItemDefinition.HasKey(json, "healthRestore")) so.healthRestore = def.healthRestore;
        if (RuntimeItemDefinition.HasKey(json, "staminaRestore")) so.staminaRestore = def.staminaRestore;
        if (RuntimeItemDefinition.HasKey(json, "damageBonus")) so.damageBonus = def.damageBonus;
        if (RuntimeItemDefinition.HasKey(json, "defenseBonus")) so.defenseBonus = def.defenseBonus;

        // Если предмет не складывается в стопку — у него всегда 1 в слоте.
        if (!so.canStack) so.maxStack = 1;

        // Категория по типу экипировки, если художник её не указал.
        if (so.category == ItemCategory.None && so.isEquippable && so.equipmentType != EquipmentType.None)
        {
            if (so.equipmentType == EquipmentType.Weapon || so.equipmentType == EquipmentType.Offhand)
                so.category = ItemCategory.Weapon;
            else if (so.equipmentType == EquipmentType.Lantern)
                so.category = ItemCategory.Tool;
            else
                so.category = ItemCategory.Armor;
        }
    }

    /// <summary>
    /// Разбор enum по имени без generic-ограничений (Enum.TryParse&lt;T&gt; требует более жёстких
    /// ограничений, чем "where T : struct").
    /// </summary>
    private static bool TryParseEnum<T>(string value, out T result) where T : struct
    {
        result = default(T);
        if (string.IsNullOrWhiteSpace(value)) return false;

        try
        {
            object parsed = Enum.Parse(typeof(T), value.Trim(), true);
            if (parsed == null) return false;
            if (!Enum.IsDefined(typeof(T), parsed)) return false;

            result = (T)parsed;
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string BuildId(string baseName)
    {
        string baseId = RuntimeItemNaming.Slugify(baseName);
        if (string.IsNullOrEmpty(baseId)) baseId = "item";

        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e != null && !string.IsNullOrEmpty(e.id)) used.Add(e.id);
        }

        return RuntimeItemNaming.MakeUnique(baseId, used);
    }

    private static ItemCategory CategoryFromFolder(string filePath)
    {
        string folder = Path.GetDirectoryName(filePath);
        string primary = PrimaryFolder;

        if (string.IsNullOrEmpty(folder) || string.IsNullOrEmpty(primary)) return ItemCategory.None;
        if (!folder.StartsWith(primary, StringComparison.OrdinalIgnoreCase)) return ItemCategory.None;

        string relative = folder.Substring(primary.Length)
            .Trim(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        if (string.IsNullOrEmpty(relative)) return ItemCategory.None;

        string first = relative.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)[0];
        return TryParseEnum<ItemCategory>(first, out var category) ? category : ItemCategory.None;
    }

    private static RuntimeItemEntry FindEntry(string path)
    {
        for (int i = 0; i < _entries.Count; i++)
        {
            var e = _entries[i];
            if (e == null || string.IsNullOrEmpty(e.filePath)) continue;
            if (string.Equals(e.filePath, path, StringComparison.OrdinalIgnoreCase)) return e;
        }

        return null;
    }

    private static void RecordFileError(string path, string message)
    {
        var entry = FindEntry(path);
        if (entry == null)
        {
            entry = new RuntimeItemEntry();
            _entries.Add(entry);
        }

        entry.filePath = path;
        entry.error = message;
        _globalErrors.Add($"{Path.GetFileName(path)}: {message}");
    }

    private static void RebuildItemsList()
    {
        _items.Clear();

        for (int i = 0; i < _entries.Count; i++)
        {
            var entry = _entries[i];
            if (entry == null || entry.item == null) continue;
            if (_items.Contains(entry.item)) continue;
            _items.Add(entry.item);
        }
    }

    private static Texture2D LoadTexture(string path)
    {
        byte[] bytes = File.ReadAllBytes(path);
        if (bytes == null || bytes.Length == 0) return null;

        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        if (!texture.LoadImage(bytes))
        {
            UnityEngine.Object.Destroy(texture);
            return null;
        }

        texture.wrapMode = TextureWrapMode.Clamp;
        texture.filterMode = FilterMode.Bilinear;
        texture.name = "rt_" + Path.GetFileNameWithoutExtension(path);
        return texture;
    }

    private static Sprite CreateSprite(Texture2D texture, string name, float pixelsPerUnit)
    {
        var sprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            Mathf.Max(1f, pixelsPerUnit),
            0,
            SpriteMeshType.FullRect);

        sprite.name = name;
        return sprite;
    }

    #region Папки

    private static void EnsureFolders()
    {
        if (_folders.Count > 0) return;

        _folders.AddRange(ResolveFolders());
        if (_folders.Count == 0) return;
        if (_folders.Exists(Directory.Exists)) return;

        // Папки нет ни одной — создаём первую доступную для записи.
        for (int i = 0; i < _folders.Count; i++)
            if (TryCreateFolder(_folders[i])) break;
    }

    private static List<string> ResolveFolders()
    {
        var result = new List<string>();

        for (int i = 0; i < 4; i++)
        {
            string candidate = null;
            try
            {
                switch (i)
                {
                    case 0: candidate = Path.Combine(GetGameRootFolder(), FolderName); break;
                    case 1: candidate = Path.Combine(Application.dataPath, FolderName); break;
                    case 2: candidate = Path.Combine(Application.persistentDataPath, FolderName); break;
                    case 3: candidate = Path.Combine(Application.streamingAssetsPath, FolderName); break;
                }
            }
            catch
            {
                candidate = null;
            }

            if (string.IsNullOrEmpty(candidate)) continue;

            string full;
            try { full = Path.GetFullPath(candidate); }
            catch { continue; }

            if (result.Contains(full)) continue;
            result.Add(full);
        }

        return result;
    }

    /// <summary>
    /// Папка с игрой: в билде — рядом с .exe, в редакторе Unity — корень проекта
    /// (там же, где папка Assets). На macOS — папка, в которой лежит .app.
    /// </summary>
    private static string GetGameRootFolder()
    {
        var dataDir = new DirectoryInfo(Application.dataPath);
        var parent = dataDir.Parent;
        if (parent == null) return Application.dataPath;

        var current = parent;
        while (current != null &&
               (string.Equals(current.Name, "Contents", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(current.Extension, ".app", StringComparison.OrdinalIgnoreCase)))
        {
            current = current.Parent;
        }

        return current != null ? current.FullName : parent.FullName;
    }

    private static bool TryCreateFolder(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            WriteReadme(path);
            Logger.Log(LogModule.Inventory, $"Создана папка для предметов: {path}");
            return true;
        }
        catch (Exception e)
        {
            string message = $"Не удалось создать папку '{path}': {e.Message}";
            _globalErrors.Add(message);
            Logger.LogWarning(LogModule.Inventory, message);
            return false;
        }
    }

    /// <summary>Открывает основную папку предметов в проводнике.</summary>
    public static void OpenPrimaryFolder()
    {
        EnsureFolders();
        string folder = PrimaryFolder;
        if (string.IsNullOrEmpty(folder)) return;

        try
        {
            Directory.CreateDirectory(folder);
            Application.OpenURL(new Uri(folder).AbsoluteUri);
        }
        catch (Exception e)
        {
            Logger.LogWarning(LogModule.Inventory, $"Не удалось открыть папку '{folder}': {e.Message}");
        }
    }

    private static void WriteReadme(string folder)
    {
        try
        {
            string readmePath = Path.Combine(folder, "КАК ДОБАВИТЬ ПРЕДМЕТ.txt");
            if (File.Exists(readmePath)) return;
            File.WriteAllText(readmePath, ReadmeText, new UTF8Encoding(true));
        }
        catch
        {
            // Инструкция — приятный бонус, её отсутствие не должно ломать игру.
        }
    }

    private const string ReadmeText =
        "КАК ДОБАВИТЬ ПРЕДМЕТ В ИГРУ\r\n" +
        "==================================================\r\n" +
        "\r\n" +
        "1. Положите картинку (.png, .jpg, .jpeg) в эту папку.\r\n" +
        "   Имя файла становится названием предмета.\r\n" +
        "   Пример: \"Красный цветок.png\" -> предмет \"Красный цветок\",\r\n" +
        "   внутренний id: krasnyy_tsvetok.\r\n" +
        "\r\n" +
        "2. Запустите игру. Предмет появится автоматически (через 1-2 секунды,\r\n" +
        "   если игра уже запущена).\r\n" +
        "\r\n" +
        "3. В самой игре нажмите F1 — откроется окно DevTools:\r\n" +
        "   там можно выдать предмет в инвентарь, поставить его в мир,\r\n" +
        "   посмотреть логи и пересобрать предметы после правок.\r\n" +
        "\r\n" +
        "4. Чтобы обновить картинку — просто перезапишите файл\r\n" +
        "   (или нажмите «Пересканировать» в DevTools).\r\n" +
        "\r\n" +
        "--------------------------------------------------\r\n" +
        "НАСТРОЙКИ (необязательно)\r\n" +
        "--------------------------------------------------\r\n" +
        "Рядом с картинкой можно положить файл с тем же именем и расширением .json.\r\n" +
        "Например \"Красный цветок.json\":\r\n" +
        "\r\n" +
        "{\r\n" +
        "  \"name\": \"Красный цветок\",\r\n" +
        "  \"description\": \"Пахнет болотом.\",\r\n" +
        "  \"category\": \"Ingredient\",\r\n" +
        "  \"canStack\": true,\r\n" +
        "  \"maxStack\": 99,\r\n" +
        "  \"usable\": false,\r\n" +
        "  \"equippable\": false,\r\n" +
        "  \"equipmentType\": \"None\",\r\n" +
        "  \"highlightColor\": \"#FFFF00\",\r\n" +
        "  \"tags\": [\"flower\"]\r\n" +
        "}\r\n" +
        "\r\n" +
        "Все поля необязательны. Любое пропущенное поле = значение по умолчанию.\r\n" +
        "\r\n" +
        "category        : None, Ingredient, Consumable, Weapon, Armor, Accessory,\r\n" +
        "                  Tool, Quest, Currency, Misc\r\n" +
        "equipmentType   : None, Head, Face, Neck, Chest, Waist, Legs, Boots,\r\n" +
        "                  Weapon, Offhand, Ring, Lantern\r\n" +
        "worldSize       : (необязательно) принудительный размер предмета в мире.\r\n" +
        "                  По умолчанию — как у обычных предметов игры\r\n" +
        "                  (картинка / 100 * 0.2, зелье 206x341 = 0.41x0.68 юнита)\r\n" +
        "uiSize          : (необязательно) принудительный размер иконки в пикселях.\r\n" +
        "                  По умолчанию иконка такая же, как у обычных предметов\r\n" +
        "                  (размер картинки * 0.2), поэтому делать ничего не нужно\r\n" +
        "highlightColor  : цвет подсветки при наведении, формат #RRGGBB\r\n" +
        "\r\n" +
        "--------------------------------------------------\r\n" +
        "СОВЕТЫ\r\n" +
        "--------------------------------------------------\r\n" +
        "- Максимальный размер картинки — 512x512 px по каждой стороне.\r\n" +
        "  Файлы крупнее игра не загружает: предмет не создаётся, а причина\r\n" +
        "  видна в DevTools (F1) в «Ошибках папки» или в строке предмета.\r\n" +
        "- Делайте картинки с прозрачным фоном.\r\n" +
        "- Размер иконки в инвентаре = размер картинки * 0.2 — так же, как у обычных\r\n" +
        "  предметов игры (например зелье 206x341 даёт иконку 41x68).\r\n" +
        "  Чтобы иконка аккуратно заполняла слот (~70 px), делайте картинку примерно\r\n" +
        "  250-350 px по большей стороне.\r\n" +
        "- Размер предмета на земле тоже берётся из картинки (как у обычных предметов),\r\n" +
        "  поэтому одна и та же картинка выглядит одинаково везде.\r\n" +
        "- Можно раскладывать картинки по подпапкам с именем категории,\r\n" +
        "  например ItemsInbox\\Ingredient\\... — категория подставится сама.\r\n" +
        "- Не переименовывайте файл после того, как предмет уже попал в сохранение:\r\n" +
        "  id предмета берётся из имени файла.\r\n";

    #endregion

    private static string ComputeSignature()
    {
        var sb = new StringBuilder();

        for (int i = 0; i < _folders.Count; i++)
        {
            string folder = _folders[i];
            sb.Append(folder).Append('|');

            if (!Directory.Exists(folder))
            {
                sb.Append("-").Append('\n');
                continue;
            }

            try
            {
                string[] files = Directory.GetFiles(folder, "*", SearchOption.AllDirectories);
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);

                for (int f = 0; f < files.Length; f++)
                {
                    if (!RuntimeItemNaming.IsImageFile(files[f])) continue;
                    var info = new FileInfo(files[f]);
                    sb.Append(files[f]).Append(';')
                      .Append(info.Length).Append(';')
                      .Append(info.LastWriteTimeUtc.Ticks).Append('\n');
                }
            }
            catch
            {
                sb.Append("err").Append('\n');
            }
        }

        return sb.ToString();
    }
}
