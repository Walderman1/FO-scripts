using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Отладочное окно внутри самой игры (аналог Editor-окна Tools/DevTools, но в билде).
///
/// Открывается/закрывается по F1. Ничего настраивать в сценах не нужно — окно создаётся
/// скриптом RuntimeItemLoader при запуске игры.
///
/// Возможности:
///  • Предметы: список предметов из папки ItemsInbox, выдача в инвентарь, спавн в мир,
///    пересборка папки, открытие папки в проводнике. Всё, что поставлено в мир через окно,
///    запоминается в списке «Заспавнено из DevTools» и удаляется одной кнопкой.
///  • Логи: те же фильтры по модулям, что и в Editor-версии + экспорт в файл,
///    размер шрифта списка (A−/A+/ползунок) и отключение разноцветной раскраски.
///  • Мир: телепорт по локациям.
///  • Прогресс: квесты (завершить/отменить) и флаги.
///  • Инфо: пути, версия, ошибки папки.
/// </summary>
[DefaultExecutionOrder(-9400)]
public class InGameDevTools : MonoBehaviour
{
    public static InGameDevTools Instance { get; private set; }

    /// <summary>Окно открыто.</summary>
    public static bool IsOpen { get; private set; }

    /// <summary>
    /// true, пока окно перехватывает управление: игровые хоткеи и клики по миру не работают.
    /// </summary>
    public static bool BlocksGameplayInput
    {
        get { return IsOpen || Time.frameCount == _closedFrame; }
    }

    public static KeyCode ToggleKey = KeyCode.F1;
    public static KeyCode CloseKey = KeyCode.Escape;

    private static int _closedFrame = -1;

    private enum Tab
    {
        Items = 0,
        Logs = 1,
        World = 2,
        Progress = 3,
        Info = 4
    }

    private const int WindowId = 9174231;
    private const int MaxLogRows = 400;

    // Вид логов: размер шрифта и режим раскраски. Настройки запоминаются в PlayerPrefs.
    private const float MinLogFontSize = 8f;
    private const float MaxLogFontSize = 22f;
    private const float DefaultLogFontSize = 11f;
    private const string LogFontSizeKey = "DevTools.LogFontSize";
    private const string LogColorsKey = "DevTools.LogColors";

    /// <summary>Цвет строк логов, когда «разноцветные» выключены: все строки одного оттенка.</summary>
    private static readonly Color MonoLogColor = new Color(0.86f, 0.86f, 0.86f);

    private Tab _tab = Tab.Items;
    private Rect _windowRect = new Rect(24f, 24f, 840f, 560f);
    private Vector2 _scroll;
    private Vector2 _logScroll;
    private string _itemSearch = "";
    private string _logSearch = "";
    private bool _onlyErrors;
    private bool _onlyWarnings;
    private bool _autoScroll = true;
    private bool _showModuleFilters;
    private bool _coloredLogs = true;
    private float _logFontSize = DefaultLogFontSize;
    private bool _logViewSettingsDirty;
    private string _newFlag = "";
    private string _status = "";
    private float _statusUntil;

    private GameObject _inputBlocker;
    private float _scale = 1f;

    private readonly List<RuntimeLogEntry> _logSnapshot = new List<RuntimeLogEntry>();
    private readonly List<RuntimeItemEntry> _entrySnapshot = new List<RuntimeItemEntry>();
    private readonly List<ItemSO> _databaseSnapshot = new List<ItemSO>();
    private readonly List<Action> _deferred = new List<Action>();
    private int _logSnapshotVersion = -1;

    /// <summary>
    /// Все объекты, поставленные в мир из DevTools («В мир»). Нужны, чтобы убрать за собой
    /// одной кнопкой: предметы, созданные отладкой, иначе остаются в мире и могут попасть
    /// в сохранение. Список статический, поэтому переживает пересоздание окна.
    /// </summary>
    private static readonly List<GameObject> _devSpawned = new List<GameObject>();

    /// <summary>Цвет рамки вокруг предметов, созданных отладкой.</summary>
    private static readonly Color DevFrameColor = new Color(1f, 0.15f, 0.15f, 0.9f);

    /// <summary>Белый пиксель 1x1: из него растягиваются четыре линии рамки.</summary>
    private static Sprite _devFrameSprite;

    private bool _stylesReady;
    private GUIStyle _sectionStyle;
    private GUIStyle _rowTitleStyle;
    private GUIStyle _miniStyle;
    private GUIStyle _wrapStyle;
    private GUIStyle _errorStyle;
    private GUIStyle _logStyle;

    public static void EnsureCreated()
    {
        if (Instance != null) return;

        var go = new GameObject("[InGameDevTools]");
        UnityEngine.Object.DontDestroyOnLoad(go);
        go.AddComponent<InGameDevTools>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        RuntimeLogBuffer.EnsureHooked();
        LoadLogViewSettings();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
            IsOpen = false;
            SetInputBlocker(false);
        }

        SaveLogViewSettings();
    }

    private void Update()
    {
        RunDeferred();

        if (Input.GetKeyDown(ToggleKey))
        {
            if (IsOpen) Close();
            else Open();
            return;
        }

        if (IsOpen && Input.GetKeyDown(CloseKey)) Close();
    }

    /// <summary>
    /// Отложенные действия: их нельзя выполнять прямо в OnGUI (создание/удаление объектов,
    /// загрузка базы, смена сцены) — иначе IMGUI-разметка ломается.
    /// </summary>
    private void Defer(Action action)
    {
        if (action == null) return;
        _deferred.Add(action);
    }

    private void RunDeferred()
    {
        if (_deferred.Count == 0) return;

        var actions = _deferred.ToArray();
        _deferred.Clear();

        for (int i = 0; i < actions.Length; i++)
        {
            try
            {
                actions[i]();
            }
            catch (Exception e)
            {
                Logger.LogWarning(LogModule.Debug, $"DevTools: отложенное действие упало: {e.Message}");
            }
        }
    }

    #region Открытие / закрытие

    public void Open()
    {
        if (IsOpen) return;

        IsOpen = true;
        _scroll = Vector2.zero;
        _logScroll = Vector2.zero;
        _status = "";
        _statusUntil = 0f;

        RuntimeLogBuffer.EnsureHooked();
        CloseGamePanels();
        SetInputBlocker(true);

        Logger.Log(LogModule.Debug, "DevTools открыты (F1)");
    }

    public void Close()
    {
        if (!IsOpen) return;

        IsOpen = false;
        _closedFrame = Time.frameCount;
        SetInputBlocker(false);
        SaveLogViewSettings();

        Logger.Log(LogModule.Debug, "DevTools закрыты");
    }

    private void CloseGamePanels()
    {
        var refs = GameReferences.Instance;
        if (refs == null) return;

        var inventory = refs.InventoryUIManager;
        if (inventory != null && inventory.IsInventoryOpen) inventory.CloseInventory();

        var crafting = refs.CraftingUI;
        if (crafting != null) crafting.SetVisible(false);

        var spells = refs.MagicSpellPanel;
        if (spells != null) spells.SetVisible(false);

        var questUi = refs.QuestUI;
        if (questUi != null && questUi.IsOpen) questUi.CloseQuestPanel();

        var itemView = refs.ItemViewPanel;
        if (itemView != null) itemView.HidePanel();

        var radial = refs.RadialMenuController;
        if (radial != null) radial.CloseAllMenus();

        var globalControl = refs.GlobalControl;
        if (globalControl != null)
        {
            if (globalControl.IsEquipmentOpen()) globalControl.CloseEquipment();
            if (globalControl.isMenuOpen) globalControl.CloseMenu();
        }
    }

    /// <summary>
    /// Пока окно открыто, весь ввод uGUI перекрывается невидимой картинкой на верхнем Canvas,
    /// чтобы кнопки игры под окном не нажимались.
    /// </summary>
    private void SetInputBlocker(bool active)
    {
        if (!active)
        {
            if (_inputBlocker != null)
            {
                Destroy(_inputBlocker);
                _inputBlocker = null;
            }

            return;
        }

        if (_inputBlocker != null) return;

        var canvasGo = new GameObject("DevToolsInputBlocker", typeof(Canvas), typeof(GraphicRaycaster));
        UnityEngine.Object.DontDestroyOnLoad(canvasGo);

        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32760;

        var blockerGo = new GameObject("Blocker", typeof(RectTransform), typeof(Image));
        blockerGo.transform.SetParent(canvasGo.transform, false);

        var rect = (RectTransform)blockerGo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = blockerGo.GetComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.004f);
        image.raycastTarget = true;

        _inputBlocker = canvasGo;
    }

    #endregion

    #region IMGUI

    private void OnGUI()
    {
        if (!IsOpen) return;

        _scale = Mathf.Clamp(Mathf.Floor(Screen.height / 900f), 1f, 2f);

        Matrix4x4 previous = GUI.matrix;
        GUI.matrix = Matrix4x4.Scale(new Vector3(_scale, _scale, 1f));

        try
        {
            EnsureStyles();
            _windowRect = GUILayout.Window(WindowId, _windowRect, DrawWindowContents, "DevTools  (F1 — закрыть)");

            float maxX = Mathf.Max(0f, Screen.width / _scale - _windowRect.width);
            float maxY = Mathf.Max(0f, Screen.height / _scale - _windowRect.height);
            _windowRect.x = Mathf.Clamp(_windowRect.x, 0f, maxX);
            _windowRect.y = Mathf.Clamp(_windowRect.y, 0f, maxY);
        }
        finally
        {
            GUI.matrix = previous;
        }
    }

    private void EnsureStyles()
    {
        if (_stylesReady) return;
        _stylesReady = true;

        _sectionStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            fontStyle = FontStyle.Bold,
            richText = true
        };

        _rowTitleStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 12,
            richText = true,
            wordWrap = true
        };

        _miniStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            richText = false,
            wordWrap = true,
            normal = { textColor = new Color(0.72f, 0.72f, 0.72f) }
        };

        _wrapStyle = new GUIStyle(_miniStyle) { wordWrap = true };

        _errorStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = 11,
            wordWrap = true,
            normal = { textColor = new Color(1f, 0.42f, 0.42f) }
        };

        _logStyle = new GUIStyle(GUI.skin.label)
        {
            fontSize = Mathf.RoundToInt(_logFontSize),
            richText = false,
            wordWrap = true
        };
    }

    #region Вид логов

    private void LoadLogViewSettings()
    {
        _logFontSize = Mathf.Clamp(
            Mathf.Round(PlayerPrefs.GetFloat(LogFontSizeKey, DefaultLogFontSize)),
            MinLogFontSize,
            MaxLogFontSize);

        _coloredLogs = PlayerPrefs.GetInt(LogColorsKey, 1) != 0;
        _logViewSettingsDirty = false;
    }

    private void SaveLogViewSettings()
    {
        if (!_logViewSettingsDirty) return;
        _logViewSettingsDirty = false;

        PlayerPrefs.SetFloat(LogFontSizeKey, _logFontSize);
        PlayerPrefs.SetInt(LogColorsKey, _coloredLogs ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void SetLogFontSize(float size)
    {
        float clamped = Mathf.Clamp(Mathf.Round(size), MinLogFontSize, MaxLogFontSize);
        if (Mathf.Approximately(clamped, _logFontSize)) return;

        _logFontSize = clamped;
        _logViewSettingsDirty = true;

        if (_logStyle != null) _logStyle.fontSize = Mathf.RoundToInt(_logFontSize);
    }

    private void SetColoredLogs(bool colored)
    {
        if (colored == _coloredLogs) return;

        _coloredLogs = colored;
        _logViewSettingsDirty = true;
    }

    #endregion

    private void DrawWindowContents(int id)
    {
        GUILayout.BeginHorizontal();
        DrawTabButton("Предметы", Tab.Items);
        DrawTabButton("Логи", Tab.Logs);
        DrawTabButton("Мир", Tab.World);
        DrawTabButton("Прогресс", Tab.Progress);
        DrawTabButton("Инфо", Tab.Info);
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("×", GUILayout.Width(26))) Close();
        GUILayout.EndHorizontal();

        switch (_tab)
        {
            case Tab.Items: DrawItemsTab(); break;
            case Tab.Logs: DrawLogsTab(); break;
            case Tab.World: DrawWorldTab(); break;
            case Tab.Progress: DrawProgressTab(); break;
            case Tab.Info: DrawInfoTab(); break;
        }

        DrawStatusBar();
    }

    private void DrawTabButton(string label, Tab tab)
    {
        Color previous = GUI.backgroundColor;
        if (_tab == tab) GUI.backgroundColor = new Color(0.45f, 0.7f, 1f);

        if (GUILayout.Button(label, GUILayout.Width(96)))
        {
            _tab = tab;
            _scroll = Vector2.zero;
        }

        GUI.backgroundColor = previous;
    }

    private void DrawStatusBar()
    {
        GUILayout.Space(2);

        if (!string.IsNullOrEmpty(_status) && Time.unscaledTime < _statusUntil)
        {
            GUILayout.Label("● " + _status, _rowTitleStyle);
            return;
        }

        int fps = Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime));
        GUILayout.Label(
            $"Предметов из папки: {RuntimeItemFolder.ItemCount}   |   Ошибок: {RuntimeItemFolder.Errors.Count}   |   " +
            $"Логов: {RuntimeLogBuffer.Count}   |   FPS: {fps}",
            _miniStyle);
    }

    private void SetStatus(string message)
    {
        _status = message;
        _statusUntil = Time.unscaledTime + 4f;
        Logger.Log(LogModule.Debug, "DevTools: " + message);
    }

    #endregion

    #region Предметы

    private void DrawItemsTab()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Открыть папку", GUILayout.Width(130))) RuntimeItemFolder.OpenPrimaryFolder();
        if (GUILayout.Button("Пересканировать", GUILayout.Width(140)))
        {
            Defer(() =>
            {
                RuntimeItemLoader.RescanNow();
                SetStatus("Папка перечитана");
            });
        }
        if (GUILayout.Button("Перезагрузить базу", GUILayout.Width(160)))
        {
            Defer(() =>
            {
                var database = FindDatabase();
                if (database == null) return;

                database.LoadDatabase();
                if (RuntimeItemLoader.Instance != null) RuntimeItemLoader.Instance.BindDatabase();
                SetStatus("База предметов перезагружена");
            });
        }

        GUILayout.FlexibleSpace();
        GUILayout.Label("Поиск:", GUILayout.Width(50));
        _itemSearch = GUILayout.TextField(_itemSearch ?? "", GUILayout.Width(180));
        if (GUILayout.Button("X", GUILayout.Width(24))) _itemSearch = "";
        GUILayout.EndHorizontal();

        GUILayout.Label("Папка: " + RuntimeItemFolder.PrimaryFolder, _miniStyle);

        DrawDevSpawnToolbar();

        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        DrawRuntimeItemsSection();
        GUILayout.Space(8);
        DrawDatabaseItemsSection();
        GUILayout.Space(8);
        DrawSpawnedSection();

        GUILayout.EndScrollView();
    }

    #region Спавн из DevTools

    /// <summary>
    /// Строка «сколько поставлено в мир» + «Удалить все». Видна всегда и имеет постоянную высоту,
    /// чтобы списки предметов не съезжали под курсором после каждого спавна.
    /// </summary>
    private void DrawDevSpawnToolbar()
    {
        PruneDevSpawns();

        GUILayout.BeginHorizontal();
        GUILayout.Label($"Заспавнено из DevTools: {_devSpawned.Count}", _miniStyle, GUILayout.Width(230));
        if (GUILayout.Button($"Удалить все ({_devSpawned.Count})", GUILayout.Width(170)))
            Defer(DeleteAllDevSpawns);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();
    }

    /// <summary>
    /// Подробный список того, что поставлено в мир кнопкой «В мир»: по нему видно, что именно
    /// осталось в мире, и каждый предмет можно убрать отдельно. Лежит внизу вкладки, чтобы
    /// растущий список не сдвигал кнопки спавна.
    /// </summary>
    private void DrawSpawnedSection()
    {
        PruneDevSpawns();
        if (_devSpawned.Count == 0) return;

        GUILayout.Label($"ЗАСПАВНЕНО ИЗ DEVTOOLS ({_devSpawned.Count})", _sectionStyle);

        for (int i = 0; i < _devSpawned.Count; i++)
        {
            var spawned = _devSpawned[i];
            if (spawned == null) continue;

            GUILayout.BeginHorizontal("box");
            GUILayout.Label(DescribeDevSpawn(spawned), _miniStyle);
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Удалить", GUILayout.Width(90)))
            {
                var target = spawned;
                Defer(() => DeleteDevSpawn(target));
            }

            GUILayout.EndHorizontal();
        }
    }

    /// <summary>Выкидывает из списка объекты, которых уже нет (подобрали, сменили сцену и т.п.).</summary>
    private static void PruneDevSpawns()
    {
        for (int i = _devSpawned.Count - 1; i >= 0; i--)
            if (_devSpawned[i] == null) _devSpawned.RemoveAt(i);
    }

    private void DeleteAllDevSpawns()
    {
        PruneDevSpawns();

        int removed = 0;
        for (int i = 0; i < _devSpawned.Count; i++)
        {
            var spawned = _devSpawned[i];
            if (spawned == null) continue;

            Destroy(spawned);
            removed++;
        }

        _devSpawned.Clear();
        SetStatus(removed > 0 ? $"Удалено заспавненных предметов: {removed}" : "Заспавненных предметов нет");
    }

    private void DeleteDevSpawn(GameObject spawned)
    {
        if (spawned == null)
        {
            PruneDevSpawns();
            return;
        }

        string description = DescribeDevSpawn(spawned);
        _devSpawned.Remove(spawned);
        Destroy(spawned);
        SetStatus("Удалён: " + description);
    }

    private static string DescribeDevSpawn(GameObject spawned)
    {
        if (spawned == null) return "";

        var pickup = spawned.GetComponent<PickupItem>();
        string title = pickup != null && pickup.itemSO != null ? pickup.itemSO.itemName : spawned.name;
        int count = pickup != null ? pickup.Count : 1;

        Vector3 position = spawned.transform.position;
        string countText = count > 1 ? $" x{count}" : "";

        return $"{title}{countText}   ({position.x:0.0}, {position.y:0.0})";
    }

    #endregion

    private void DrawRuntimeItemsSection()
    {
        _entrySnapshot.Clear();
        var entries = RuntimeItemFolder.Entries;
        for (int i = 0; i < entries.Count; i++) _entrySnapshot.Add(entries[i]);

        GUILayout.Label($"ПРЕДМЕТЫ ИЗ ПАПКИ ({_entrySnapshot.Count})", _sectionStyle);

        if (_entrySnapshot.Count == 0)
        {
            GUILayout.Label(
                "Пока пусто. Положите .png (или .jpg) в папку выше — предмет появится автоматически.\n" +
                "Подробная инструкция лежит внутри папки: «КАК ДОБАВИТЬ ПРЕДМЕТ.txt».",
                _miniStyle);
            return;
        }

        for (int i = 0; i < _entrySnapshot.Count; i++)
        {
            var entry = _entrySnapshot[i];
            if (entry == null) continue;
            if (!MatchesSearch(entry)) continue;
            DrawRuntimeEntryRow(entry);
        }
    }

    private bool MatchesSearch(RuntimeItemEntry entry)
    {
        string search = _itemSearch;
        if (string.IsNullOrEmpty(search)) return true;

        string name = entry.item != null ? entry.item.itemName : "";
        string id = entry.id ?? "";
        string file = entry.filePath != null ? Path.GetFileName(entry.filePath) : "";

        return Contains(name, search) || Contains(id, search) || Contains(file, search);
    }

    private bool MatchesSearch(ItemSO item)
    {
        string search = _itemSearch;
        if (string.IsNullOrEmpty(search)) return true;
        if (item == null) return false;

        return Contains(item.itemName, search) || Contains(item.id, search);
    }

    private static bool Contains(string source, string pattern)
    {
        return !string.IsNullOrEmpty(source) &&
               source.IndexOf(pattern, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private void DrawRuntimeEntryRow(RuntimeItemEntry entry)
    {
        GUILayout.BeginHorizontal("box");

        DrawItemIcon(entry.item != null ? entry.item.icon : null);

        GUILayout.BeginVertical();
        string title = entry.item != null ? entry.item.itemName : Path.GetFileName(entry.filePath);
        GUILayout.Label($"<b>{title}</b>   <size=10><color=#909090>id: {entry.id}</color></size>", _rowTitleStyle);
        GUILayout.Label(DescribeEntry(entry), entry.HasError ? _errorStyle : _miniStyle);
        GUILayout.EndVertical();

        if (entry.item != null)
        {
            var item = entry.item;
            if (GUILayout.Button("+1", GUILayout.Width(38), GUILayout.Height(24))) Defer(() => GiveItem(item, 1));
            if (GUILayout.Button("+10", GUILayout.Width(44), GUILayout.Height(24))) Defer(() => GiveItem(item, 10));
            if (GUILayout.Button("В мир", GUILayout.Width(70), GUILayout.Height(24))) Defer(() => SpawnItemInWorld(item, 1));
        }

        GUILayout.EndHorizontal();
    }

    private static string DescribeEntry(RuntimeItemEntry entry)
    {
        if (entry.HasError) return "ОШИБКА: " + entry.error;
        if (entry.missing) return "файл удалён — предмет остался в базе";

        var so = entry.item;
        if (so == null) return "предмет не создан";

        var sb = new StringBuilder();
        sb.Append(so.category);

        if (so.canStack) sb.Append(", стопка до ").Append(so.maxStack);
        else sb.Append(", не складывается");

        if (so.isEquippable) sb.Append(", экипировка: ").Append(so.equipmentType);
        if (so.isUsable) sb.Append(", используется");

        if (entry.filePath != null) sb.Append(", файл: ").Append(Path.GetFileName(entry.filePath));
        if (!string.IsNullOrEmpty(entry.jsonPath)) sb.Append(" (+json)");

        return sb.ToString();
    }

    private void DrawDatabaseItemsSection()
    {
        var database = FindDatabase();

        _databaseSnapshot.Clear();
        if (database != null)
        {
            var all = database.GetAllItems();
            if (all != null)
            {
                for (int i = 0; i < all.Count; i++)
                {
                    var so = all[i];
                    if (so == null) continue;
                    if (IsRuntimeItem(so)) continue;
                    _databaseSnapshot.Add(so);
                }
            }
        }

        GUILayout.Label($"ПРЕДМЕТЫ ИГРЫ (Resources, {_databaseSnapshot.Count})", _sectionStyle);

        if (database == null)
        {
            GUILayout.Label("ItemDatabase не найден.", _miniStyle);
            return;
        }

        bool any = false;
        for (int i = 0; i < _databaseSnapshot.Count; i++)
        {
            var so = _databaseSnapshot[i];
            if (!MatchesSearch(so)) continue;
            any = true;

            GUILayout.BeginHorizontal("box");
            DrawItemIcon(so.icon);

            GUILayout.BeginVertical();
            GUILayout.Label($"<b>{so.itemName}</b>   <size=10><color=#909090>id: {so.id}</color></size>", _rowTitleStyle);
            GUILayout.Label(so.category + (so.isEquippable ? ", " + so.equipmentType.ToString() : ""), _miniStyle);
            GUILayout.EndVertical();

            var item = so;
            if (GUILayout.Button("+1", GUILayout.Width(38), GUILayout.Height(24))) Defer(() => GiveItem(item, 1));
            if (GUILayout.Button("+10", GUILayout.Width(44), GUILayout.Height(24))) Defer(() => GiveItem(item, 10));
            if (GUILayout.Button("В мир", GUILayout.Width(70), GUILayout.Height(24))) Defer(() => SpawnItemInWorld(item, 1));

            GUILayout.EndHorizontal();
        }

        if (!any) GUILayout.Label("Ничего не найдено.", _miniStyle);
    }

    private static bool IsRuntimeItem(ItemSO item)
    {
        var items = RuntimeItemFolder.Items;
        for (int i = 0; i < items.Count; i++)
            if (items[i] == item) return true;

        return false;
    }

    private void DrawItemIcon(Sprite icon)
    {
        if (icon != null && icon.texture != null)
        {
            GUILayout.Label(icon.texture, GUILayout.Width(30), GUILayout.Height(30));
            return;
        }

        GUILayout.Box("?", GUILayout.Width(30), GUILayout.Height(30));
    }

    private void GiveItem(ItemSO so, int count)
    {
        if (so == null) return;

        var refs = GameReferences.Instance;
        var inventory = refs != null ? refs.InventoryUIManager : null;

        if (inventory == null)
        {
            SetStatus("Инвентарь недоступен — нужно быть в игровой сцене");
            return;
        }

        int added = inventory.AddItem(so, count);

        if (added > 0) SetStatus($"Выдано: {so.itemName} x{added}");
        else SetStatus($"Не удалось выдать {so.itemName} (инвентарь полон?)");
    }

    private void SpawnItemInWorld(ItemSO so, int count)
    {
        if (so == null) return;

        if (so.worldPrefab == null)
        {
            SetStatus($"У предмета «{so.itemName}» нет worldPrefab");
            return;
        }

        var refs = GameReferences.Instance;
        if (refs != null && refs.IsDialogueActive())
        {
            SetStatus("Нельзя: идёт диалог");
            return;
        }

        Vector3 position = GetSpawnPosition(refs);

        GameObject spawned;
        try
        {
            spawned = Instantiate(so.worldPrefab, position, Quaternion.identity);
        }
        catch (Exception e)
        {
            SetStatus("Ошибка спавна: " + e.Message);
            return;
        }

        if (spawned == null) return;

        spawned.name = so.id + "_DevSpawn";

        var location = refs != null && refs.TextBeginner != null ? refs.TextBeginner.CurrentLocation : null;
        if (location != null) spawned.transform.SetParent(location.transform, true);

        var renderers = spawned.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++) renderers[i].sortingOrder = 50;

        SceneInjector.InjectInto(spawned);

        var pickup = spawned.GetComponent<PickupItem>();
        if (pickup != null)
        {
            pickup.itemSO = so;
            pickup.SetCount(Mathf.Max(1, count));
            pickup.GenerateNewID();
        }

        // Запоминаем объект: вкладка «Предметы» покажет его в списке и даст убрать разом.
        _devSpawned.Add(spawned);

        SetStatus($"Создан в мире: {so.itemName} x{Mathf.Max(1, count)}");
        Logger.Log(LogModule.Debug, $"DevTools: спавн предмета {so.id} x{count} в {position}");
    }

    /// <summary>
    /// Куда поставить предмет из DevTools: под ноги персонажу (как игра делает при выбрасывании),
    /// иначе — центр экрана, иначе — центр текущей локации.
    /// </summary>
    private static Vector3 GetSpawnPosition(GameReferences refs)
    {
        var characterManager = refs != null ? refs.DialogueCharacterManager : null;
        var character = characterManager != null ? characterManager.CurrentCharacter : null;

        if (character != null)
        {
            var sprite = character.GetComponentInChildren<SpriteRenderer>();

            Vector3 basePosition = sprite != null
                ? new Vector3(character.transform.position.x, sprite.bounds.min.y, 0f)
                : new Vector3(character.transform.position.x, character.transform.position.y - 1f, 0f);

            return basePosition + new Vector3(UnityEngine.Random.Range(-0.6f, 0.6f), 0.1f, 0f);
        }

        var camera = Camera.main;
        if (camera == null) camera = FindAnyObjectByType<Camera>();

        if (camera != null)
        {
            Vector3 position = camera.ScreenToWorldPoint(
                new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f));
            position.z = 0f;
            return position;
        }

        var location = refs != null && refs.TextBeginner != null ? refs.TextBeginner.CurrentLocation : null;
        if (location != null) return location.transform.position;

        return Vector3.zero;
    }

    private static ItemDatabase FindDatabase()
    {
        if (RuntimeItemLoader.Instance != null && RuntimeItemLoader.Instance.Database != null)
            return RuntimeItemLoader.Instance.Database;

        return FindAnyObjectByType<ItemDatabase>(FindObjectsInactive.Include);
    }

    #endregion

    #region Логи

    private void DrawLogsTab()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Очистить", GUILayout.Width(90))) RuntimeLogBuffer.Clear();
        _showModuleFilters = GUILayout.Toggle(_showModuleFilters, "Модули", GUILayout.Width(80));

        bool errorsOnly = GUILayout.Toggle(_onlyErrors, "Ошибки", GUILayout.Width(80));
        if (errorsOnly != _onlyErrors)
        {
            _onlyErrors = errorsOnly;
            if (errorsOnly) _onlyWarnings = false;
        }

        bool warningsOnly = GUILayout.Toggle(_onlyWarnings, "Предупр.", GUILayout.Width(90));
        if (warningsOnly != _onlyWarnings)
        {
            _onlyWarnings = warningsOnly;
            if (warningsOnly) _onlyErrors = false;
        }

        _autoScroll = GUILayout.Toggle(_autoScroll, "Автоскролл", GUILayout.Width(100));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Экспорт", GUILayout.Width(90))) Defer(ExportLogs);
        if (GUILayout.Button(Logger.VerboseEnabled ? "Verbose: вкл" : "Verbose: выкл", GUILayout.Width(120)))
            Logger.SetVerbose(!Logger.VerboseEnabled);
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Поиск:", GUILayout.Width(50));
        _logSearch = GUILayout.TextField(_logSearch ?? "", GUILayout.Width(240));
        if (GUILayout.Button("X", GUILayout.Width(24))) _logSearch = "";
        GUILayout.FlexibleSpace();
        GUILayout.Label($"Всего: {RuntimeLogBuffer.Count}", _miniStyle, GUILayout.Width(110));
        GUILayout.EndHorizontal();

        GUILayout.BeginHorizontal();
        GUILayout.Label("Шрифт:", GUILayout.Width(52));
        if (GUILayout.Button("A−", GUILayout.Width(30))) SetLogFontSize(_logFontSize - 1f);
        SetLogFontSize(GUILayout.HorizontalSlider(
            _logFontSize, MinLogFontSize, MaxLogFontSize, GUILayout.Width(130)));
        if (GUILayout.Button("A+", GUILayout.Width(30))) SetLogFontSize(_logFontSize + 1f);
        GUILayout.Label(Mathf.RoundToInt(_logFontSize) + " px", _miniStyle, GUILayout.Width(44));

        GUILayout.FlexibleSpace();

        bool colored = GUILayout.Toggle(_coloredLogs, "Разноцветные", GUILayout.Width(120));
        SetColoredLogs(colored);

        GUILayout.EndHorizontal();

        if (_showModuleFilters) DrawModuleFilters();

        RefreshLogSnapshot();

        _logScroll = GUILayout.BeginScrollView(_logScroll, GUILayout.ExpandHeight(true));

        int shown = 0;
        int start = Mathf.Max(0, _logSnapshot.Count - MaxLogRows);

        for (int i = start; i < _logSnapshot.Count; i++)
        {
            var entry = _logSnapshot[i];
            if (entry == null || !MatchesLogFilter(entry)) continue;

            shown++;
            Color previous = _logStyle.normal.textColor;
            _logStyle.normal.textColor = _coloredLogs
                ? RuntimeLogBuffer.GetColor(entry.module, entry.type)
                : MonoLogColor;
            GUILayout.Label($"[{entry.time:F1}] [{entry.module}] {entry.message}", _logStyle);
            _logStyle.normal.textColor = previous;
        }

        if (shown == 0) GUILayout.Label("Нет логов под текущие фильтры.", _miniStyle);

        GUILayout.EndScrollView();

        if (_autoScroll) _logScroll.y = float.MaxValue;
    }

    private void DrawModuleFilters()
    {
        GUILayout.BeginVertical("box");
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Все", GUILayout.Width(60))) RuntimeLogBuffer.SetAllModules(true);
        if (GUILayout.Button("Ничего", GUILayout.Width(70))) RuntimeLogBuffer.SetAllModules(false);
        GUILayout.FlexibleSpace();
        GUILayout.EndHorizontal();

        var modules = RuntimeLogBuffer.Modules;
        int inRow = 0;

        GUILayout.BeginHorizontal();
        for (int i = 0; i < modules.Count; i++)
        {
            string module = modules[i];
            bool enabled = RuntimeLogBuffer.IsModuleEnabled(module);
            bool next = GUILayout.Toggle(enabled, module, GUILayout.Width(120));

            if (next != enabled) RuntimeLogBuffer.SetModuleEnabled(module, next);

            inRow++;
            if (inRow % 5 == 0)
            {
                GUILayout.EndHorizontal();
                GUILayout.BeginHorizontal();
            }
        }

        GUILayout.EndHorizontal();
        GUILayout.EndVertical();
    }

    private void RefreshLogSnapshot()
    {
        if (_logSnapshotVersion == RuntimeLogBuffer.Version) return;

        _logSnapshotVersion = RuntimeLogBuffer.Version;
        RuntimeLogBuffer.CopyTo(_logSnapshot);
    }

    private bool MatchesLogFilter(RuntimeLogEntry entry)
    {
        if (entry == null) return false;

        bool isError = entry.type == LogType.Error || entry.type == LogType.Exception;
        bool isWarning = entry.type == LogType.Warning;

        if (_onlyErrors && !isError) return false;
        if (_onlyWarnings && !isWarning) return false;
        if (!RuntimeLogBuffer.IsModuleEnabled(entry.module)) return false;

        if (!string.IsNullOrEmpty(_logSearch) && !Contains(entry.message, _logSearch)) return false;

        return true;
    }

    private void ExportLogs()
    {
        try
        {
            RefreshLogSnapshot();

            var sb = new StringBuilder();
            sb.AppendLine("DevTools logs — " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
            sb.AppendLine("Unity " + Application.unityVersion + ", сцена: " + SceneManager.GetActiveScene().name);
            sb.AppendLine("--------------------------------------------------");

            for (int i = 0; i < _logSnapshot.Count; i++)
            {
                var entry = _logSnapshot[i];
                if (entry == null || !MatchesLogFilter(entry)) continue;
                sb.AppendLine($"[{entry.time:F2}] [{entry.module}] {entry.message}");
            }

            string path = Path.Combine(Application.persistentDataPath,
                "devtools_logs_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".txt");

            File.WriteAllText(path, sb.ToString(), new UTF8Encoding(true));
            SetStatus("Логи сохранены: " + path);
        }
        catch (Exception e)
        {
            SetStatus("Ошибка экспорта: " + e.Message);
        }
    }

    #endregion

    #region Мир

    private void DrawWorldTab()
    {
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        GUILayout.Label("ТЕКУЩАЯ СЦЕНА", _sectionStyle);
        GUILayout.Label(SceneManager.GetActiveScene().name, _rowTitleStyle);

        GUILayout.Space(6);
        GUILayout.Label("ТЕЛЕПОРТ ПО ЛОКАЦИЯМ", _sectionStyle);

        var refs = GameReferences.Instance;
        var slide = refs != null ? refs.SceneSlideTransition : null;

        if (slide == null)
        {
            GUILayout.Label("SceneSlideTransition не найден (нужно быть в игровой сцене).", _miniStyle);
        }
        else
        {
            var scenes = slide.GetAllScenes();

            if (scenes == null || scenes.Count == 0)
            {
                GUILayout.Label("Список локаций пуст.", _miniStyle);
            }
            else
            {
                for (int i = 0; i < scenes.Count; i++)
                {
                    var sceneGo = scenes[i];
                    if (sceneGo == null) continue;

                    string sceneName = sceneGo.name;

                    GUILayout.BeginHorizontal();
                    if (GUILayout.Button("→ " + sceneName, GUILayout.Width(260)))
                    {
                        Defer(() =>
                        {
                            Close();
                            slide.SwitchToScene(sceneName);
                        });
                    }
                    GUILayout.EndHorizontal();
                }
            }
        }

        GUILayout.EndScrollView();
    }

    #endregion

    #region Прогресс

    private void DrawProgressTab()
    {
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        var refs = GameReferences.Instance;

        GUILayout.Label("АКТИВНЫЕ КВЕСТЫ", _sectionStyle);

        if (refs == null) GUILayout.Label("GameReferences не найден.", _miniStyle);
        else
        {
            var questManager = refs.QuestManager;
            if (questManager == null) GUILayout.Label("QuestManager не найден.", _miniStyle);
            else
            {
                var active = questManager.GetActiveQuests();
                if (active == null || active.Count == 0) GUILayout.Label("Нет активных квестов.", _miniStyle);
                else
                {
                    var copy = new List<QuestInstance>(active);
                    for (int i = 0; i < copy.Count; i++)
                    {
                        var quest = copy[i];
                        if (quest == null) continue;

                        GUILayout.BeginHorizontal("box");
                        GUILayout.Label($"{quest.QuestName} ({quest.GetProgressText()})", _rowTitleStyle);

                        if (GUILayout.Button("Завершить", GUILayout.Width(100)))
                        {
                            string questId = quest.QuestID;
                            string questName = quest.QuestName;
                            Defer(() =>
                            {
                                questManager.CompleteQuest(questId);
                                SetStatus("Квест завершён: " + questName);
                            });
                        }

                        if (GUILayout.Button("Отменить", GUILayout.Width(90)))
                        {
                            string questId = quest.QuestID;
                            string questName = quest.QuestName;
                            Defer(() =>
                            {
                                questManager.CancelQuest(questId);
                                SetStatus("Квест отменён: " + questName);
                            });
                        }

                        GUILayout.EndHorizontal();
                    }
                }
            }
        }

        GUILayout.Space(8);
        GUILayout.Label("ФЛАГИ", _sectionStyle);

        if (refs != null)
        {
            var flagManager = refs.FlagManager;
            if (flagManager != null)
            {
                var flags = flagManager.GetAllFlags();
                if (flags == null || flags.Count == 0) GUILayout.Label("Нет активных флагов.", _miniStyle);
                else
                {
                    foreach (var pair in flags)
                    {
                        bool next = GUILayout.Toggle(pair.Value, " " + pair.Key);
                        if (next != pair.Value)
                        {
                            string flagName = pair.Key;
                            bool flagValue = next;
                            Defer(() => flagManager.SetFlag(flagName, flagValue));
                        }
                    }
                }

                GUILayout.Space(4);
                GUILayout.BeginHorizontal();
                _newFlag = GUILayout.TextField(_newFlag ?? "", GUILayout.Width(220));
                if (GUILayout.Button("Добавить флаг", GUILayout.Width(140)) && !string.IsNullOrWhiteSpace(_newFlag))
                {
                    string flagName = _newFlag.Trim();
                    Defer(() =>
                    {
                        flagManager.SetFlag(flagName, true);
                        SetStatus("Флаг добавлен: " + flagName);
                    });
                    _newFlag = "";
                }
                GUILayout.EndHorizontal();
            }
            else GUILayout.Label("FlagManager не найден.", _miniStyle);
        }

        GUILayout.EndScrollView();
    }

    #endregion

    #region Инфо

    private void DrawInfoTab()
    {
        _scroll = GUILayout.BeginScrollView(_scroll, GUILayout.ExpandHeight(true));

        GUILayout.Label("ПРЕДМЕТЫ ИЗ КАРТИНОК", _sectionStyle);

        string primary = RuntimeItemFolder.PrimaryFolder;
        GUILayout.Label("Основная папка: " + (string.IsNullOrEmpty(primary) ? "(не найдена)" : primary), _wrapStyle);

        var folders = RuntimeItemFolder.Folders;
        for (int i = 0; i < folders.Count; i++)
        {
            bool exists = false;
            try { exists = Directory.Exists(folders[i]); } catch { }
            GUILayout.Label($"   • {folders[i]}{(exists ? "" : "   (нет)")}", _miniStyle);
        }

        GUILayout.Label($"Предметов из папки: {RuntimeItemFolder.ItemCount}", _rowTitleStyle);
        GUILayout.Label($"Зарегистрировано в базе: " +
                        (RuntimeItemLoader.Instance != null ? RuntimeItemLoader.Instance.RegisteredCount : 0), _miniStyle);
        GUILayout.Label($"Предел размера картинки: {RuntimeItemFolder.MaxIconSide}x{RuntimeItemFolder.MaxIconSide} px", _miniStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Открыть папку", GUILayout.Width(140))) RuntimeItemFolder.OpenPrimaryFolder();
        if (GUILayout.Button("Копировать путь", GUILayout.Width(150)))
        {
            GUIUtility.systemCopyBuffer = primary;
            SetStatus("Путь скопирован");
        }
        if (GUILayout.Button("Пересканировать", GUILayout.Width(150)))
        {
            Defer(() =>
            {
                RuntimeItemLoader.RescanNow();
                SetStatus("Папка перечитана");
            });
        }
        GUILayout.EndHorizontal();

        GUILayout.Space(8);
        GUILayout.Label("ОШИБКИ ПАПКИ", _sectionStyle);

        var errors = RuntimeItemFolder.Errors;
        if (errors.Count == 0) GUILayout.Label("Ошибок нет.", _miniStyle);
        else
        {
            for (int i = 0; i < errors.Count; i++) GUILayout.Label("• " + errors[i], _errorStyle);
        }

        GUILayout.Space(8);
        GUILayout.Label("ИГРА", _sectionStyle);
        GUILayout.Label("Unity: " + Application.unityVersion, _miniStyle);
        GUILayout.Label("Платформа: " + Application.platform, _miniStyle);
        GUILayout.Label("Версия игры: " + Application.version, _miniStyle);
        GUILayout.Label("Сцена: " + SceneManager.GetActiveScene().name, _miniStyle);
        GUILayout.Label("dataPath: " + Application.dataPath, _miniStyle);
        GUILayout.Label("persistentDataPath: " + Application.persistentDataPath, _miniStyle);
        GUILayout.Label($"FPS: {Mathf.RoundToInt(1f / Mathf.Max(0.0001f, Time.unscaledDeltaTime))}", _miniStyle);

        var database = FindDatabase();
        GUILayout.Label("Предметов в базе игры: " + (database != null ? database.GetAllItems().Count : 0), _miniStyle);

        GUILayout.Space(8);
        GUILayout.Label("ПРОЧЕЕ", _sectionStyle);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button(Logger.VerboseEnabled ? "Подробные логи: вкл" : "Подробные логи: выкл", GUILayout.Width(220)))
            Logger.SetVerbose(!Logger.VerboseEnabled);
        if (GUILayout.Button("Экспорт логов", GUILayout.Width(160))) Defer(ExportLogs);
        GUILayout.EndHorizontal();

        GUILayout.EndScrollView();
    }

    #endregion
}
