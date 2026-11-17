using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using VContainer;

public class TabManager : MonoBehaviour
{
    [Header("Префабы контента (если пусто — загрузятся из Resources)")]
    [SerializeField] private GameObject smallTitlePrefab;
    [SerializeField] private GameObject sliderFieldPrefab;
    [SerializeField] private GameObject toggleFieldPrefab;
    [SerializeField] private GameObject dropdownFieldPrefab;
    [SerializeField] private GameObject keyBindRowPrefab;

    [Header("Контейнеры (если пусто — найдутся в иерархии)")]
    [SerializeField] private Transform tabsButtonsContainer;
    [SerializeField] private Transform tabContentContainer;
    [SerializeField] private GameObject tabButtonPrefab;

    [Header("Вкладки")]
    [SerializeField] private string[] tabNames = { "Графика", "Звук", "Управление", "Клавиши", "Игра" };

    [Inject] private SettingsManager _settingsManager;

    private readonly Dictionary<string, GameObject> tabContents = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> tabButtons = new Dictionary<string, GameObject>();
    private string activeTab;

    public void Build()
    {
        EnsureReferences();

        if (tabsButtonsContainer == null || tabContentContainer == null)
        {
            Logger.LogError(LogModule.UI, "TabManager: не удалось найти или создать контейнеры.");
            return;
        }

        foreach (Transform c in tabsButtonsContainer) Destroy(c.gameObject);
        foreach (Transform c in tabContentContainer) Destroy(c.gameObject);

        tabContents.Clear();
        tabButtons.Clear();

        for (int i = 0; i < tabNames.Length; i++)
        {
            string name = tabNames[i];

            GameObject btn = Instantiate(tabButtonPrefab, tabsButtonsContainer);
            btn.name = $"Tab_{name}";
            btn.SetActive(true);
            var btnText = btn.GetComponentInChildren<TMP_Text>();
            if (btnText != null) btnText.text = name;

            int idx = i;
            var button = btn.GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => SwitchTab(tabNames[idx]));
            }

            GameObject content = new GameObject($"Content_{name}", typeof(RectTransform));
            content.transform.SetParent(tabContentContainer, false);

            var rt = content.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(16, 16, 12, 12);
            vlg.spacing = 15;
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childForceExpandWidth = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;

            var fitter = content.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            tabContents[name] = content;
            tabButtons[name] = btn;
        }

        PopulateGraphics(tabContents["Графика"].transform);
        PopulateAudio(tabContents["Звук"].transform);
        PopulateControls(tabContents["Управление"].transform);
        PopulateKeys(tabContents["Клавиши"].transform);
        PopulateGame(tabContents["Игра"].transform);

        SwitchTab(tabNames[0]);
    }

    private void EnsureReferences()
    {
        if (smallTitlePrefab == null) smallTitlePrefab = Resources.Load<GameObject>("UI/Prefab/SmallTitle");
        if (sliderFieldPrefab == null) sliderFieldPrefab = Resources.Load<GameObject>("UI/Prefab/Slider/VolumeSlider");
        if (toggleFieldPrefab == null) toggleFieldPrefab = Resources.Load<GameObject>("UI/Prefab/Toggle/Toggle");
        if (dropdownFieldPrefab == null) dropdownFieldPrefab = Resources.Load<GameObject>("UI/Prefab/ControlDropdown");
        if (keyBindRowPrefab == null) keyBindRowPrefab = Resources.Load<GameObject>("UI/Prefab/Panels/KeyButton");
        if (tabButtonPrefab == null) tabButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/TabsButton");

        if (tabsButtonsContainer == null) tabsButtonsContainer = FindChildRecursive(transform, "TabsContainer");

        if (tabContentContainer == null)
        {
            tabContentContainer = FindChildRecursive(transform, "ContentContainer");
            if (tabContentContainer == null) tabContentContainer = FindChildRecursive(transform, "Content");

            if (tabContentContainer == null)
            {
                GameObject go = new GameObject("TabContentContainer", typeof(RectTransform));
                go.transform.SetParent(transform, false);
                RectTransform rt = go.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0, 0);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(15, 80);
                rt.offsetMax = new Vector2(-15, -80);
                tabContentContainer = go.transform;
            }
        }
    }

    private Transform FindChildRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name.Contains(name)) return child;
            Transform result = FindChildRecursive(child, name);
            if (result != null) return result;
        }
        return null;
    }

    public void SwitchTab(string name)
    {
        foreach (var kvp in tabContents) kvp.Value.SetActive(kvp.Key == name);
        foreach (var kvp in tabButtons)
        {
            var img = kvp.Value.GetComponent<Image>();
            if (img != null) img.color = kvp.Key == name ? new Color(0.55f, 0.75f, 1f) : new Color(0.25f, 0.25f, 0.3f);
        }
        activeTab = name;
    }

    private Transform CreateRow(Transform parent, string label)
    {
        GameObject row = new GameObject($"Row_{label}", typeof(RectTransform));
        row.transform.SetParent(parent, false);

        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.MiddleCenter;
        hlg.spacing = 20;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var rowLe = row.AddComponent<LayoutElement>();
        rowLe.minHeight = 45f;
        rowLe.preferredHeight = 45f;

        if (smallTitlePrefab != null)
        {
            GameObject labelGo = Instantiate(smallTitlePrefab, row.transform);
            var labelTxt = labelGo.GetComponentInChildren<TMP_Text>();
            if (labelTxt != null) labelTxt.text = label;

            var labelLe = labelGo.GetComponent<LayoutElement>() ?? labelGo.AddComponent<LayoutElement>();
            labelLe.minWidth = 220f;
            labelLe.preferredWidth = 220f;
            labelLe.flexibleWidth = 0;
        }

        return row.transform;
    }

    private SliderField AddSlider(Transform parent, string label, float min, float max, float value, bool whole, System.Action<float> cb)
    {
        if (sliderFieldPrefab == null) return null;

        var row = CreateRow(parent, label);
        var go = Instantiate(sliderFieldPrefab, row);
        go.SetActive(true);

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = 400f;
        le.preferredWidth = 400f;
        le.flexibleWidth = 0;
        le.minHeight = 30f;
        le.preferredHeight = 30f;

        var field = go.GetComponent<SliderField>();
        if (field == null) field = go.AddComponent<SliderField>();

        field.Setup(label, min, max, value, whole, cb);
        return field;
    }

    private ToggleField AddToggle(Transform parent, string label, bool value, System.Action<bool> cb)
    {
        if (toggleFieldPrefab == null) return null;

        var row = CreateRow(parent, label);
        var go = Instantiate(toggleFieldPrefab, row);
        go.SetActive(true);

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = 60f;
        le.preferredWidth = 60f;
        le.flexibleWidth = 0;
        le.minHeight = 30f;
        le.preferredHeight = 30f;

        var field = go.GetComponent<ToggleField>();
        if (field == null) field = go.AddComponent<ToggleField>();

        field.Setup(label, value, cb);
        return field;
    }

    private DropdownField AddDropdown(Transform parent, string label, string[] options, int current, System.Action<int> cb)
    {
        if (dropdownFieldPrefab == null) return null;

        var row = CreateRow(parent, label);
        var go = Instantiate(dropdownFieldPrefab, row);
        go.SetActive(true);

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = 300f;
        le.preferredWidth = 300f;
        le.flexibleWidth = 0;
        le.minHeight = 30f;
        le.preferredHeight = 30f;

        var field = go.GetComponent<DropdownField>();
        if (field == null) field = go.AddComponent<DropdownField>();

        field.Setup(label, options, current, cb);
        return field;
    }

    private void PopulateGraphics(Transform parent)
    {
        if (_settingsManager == null) return;

        var s = _settingsManager.Settings;
        AddToggle(parent, "Полноэкранный режим", s.fullscreen, v => _settingsManager.SetFullscreen(v));
        string[] resolutions = { "1920x1080", "1600x900", "1280x720", "960x540", "800x450" };
        int resIndex = Mathf.Max(0, System.Array.IndexOf(resolutions, s.resolution));
        AddDropdown(parent, "Разрешение", resolutions, resIndex, v => _settingsManager.SetResolution(resolutions[v]));
        string[] qualities = { "Высокое", "Среднее", "Низкое" };
        AddDropdown(parent, "Качество", qualities, s.qualityIndex, v => _settingsManager.SetQuality(v));
        AddToggle(parent, "VSync", s.vsync, v => _settingsManager.SetVSync(v));
        string[] fpsLabels = { "30", "60", "90", "120", "144", "Без ограничения" };
        int[] fpsValues = { 30, 60, 90, 120, 144, -1 };
        int fpsIdx = System.Array.IndexOf(fpsValues, s.targetFps);
        if (fpsIdx < 0) fpsIdx = 1;
        AddDropdown(parent, "Лимит FPS", fpsLabels, fpsIdx, v => _settingsManager.SetTargetFps(fpsValues[v]));
        AddToggle(parent, "Показывать FPS", s.showFps, v => _settingsManager.SetShowFps(v));

        int fontSliderValue = Mathf.RoundToInt((s.uiFontScale - 0.5f) * 10f);
        AddSlider(parent, "Размер шрифта UI", 0f, 10f, fontSliderValue, true, v => _settingsManager.SetUIFontScale(0.5f + v * 0.1f));

        int scaleSliderValue = Mathf.RoundToInt((s.uiScale - 0.5f) * 10f);
        AddSlider(parent, "Масштаб интерфейса", 0f, 10f, scaleSliderValue, true, v => _settingsManager.SetUIScale(0.5f + v * 0.1f));
    }

    private void PopulateAudio(Transform parent)
    {
        if (_settingsManager == null) return;

        var s = _settingsManager.Settings;
        AddSlider(parent, "Общая громкость", 0f, 10f, s.masterVolume * 10f, true, v => _settingsManager.SetMasterVolume(v / 10f));
        AddSlider(parent, "Музыка", 0f, 10f, s.musicVolume * 10f, true, v => _settingsManager.SetMusicVolume(v / 10f));
        AddSlider(parent, "Звуки", 0f, 10f, s.sfxVolume * 10f, true, v => _settingsManager.SetSFXVolume(v / 10f));
        AddSlider(parent, "Голос", 0f, 10f, s.voiceVolume * 10f, true, v => _settingsManager.SetVoiceVolume(v / 10f));
    }

    private void PopulateControls(Transform parent)
    {
        if (_settingsManager == null) return;

        var s = _settingsManager.Settings;
        string[] options = { "Стандартная", "Альтернативная" };
        int idx = System.Array.IndexOf(options, s.controlScheme);
        if (idx < 0) idx = 0;
        AddDropdown(parent, "Схема управления", options, idx, v => _settingsManager.SetControlScheme(options[v]));
    }

    private void PopulateKeys(Transform parent)
    {
        if (_settingsManager == null) return;

        var keys = _settingsManager.Settings.keyBindings;
        var sm = _settingsManager;

        GameObject columnsContainer = new GameObject("KeysColumns", typeof(RectTransform));
        columnsContainer.transform.SetParent(parent, false);

        var hlg = columnsContainer.AddComponent<HorizontalLayoutGroup>();
        hlg.childAlignment = TextAnchor.UpperCenter;
        hlg.spacing = 40;
        hlg.childControlWidth = true;
        hlg.childControlHeight = true;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;

        var containerLe = columnsContainer.AddComponent<LayoutElement>();
        containerLe.minHeight = 500f;

        GameObject col1 = new GameObject("Column1", typeof(RectTransform));
        col1.transform.SetParent(columnsContainer.transform, false);
        var vlg1 = col1.AddComponent<VerticalLayoutGroup>();
        vlg1.spacing = 15;
        vlg1.childControlWidth = true;
        vlg1.childControlHeight = true;
        vlg1.childForceExpandWidth = false;

        GameObject col2 = new GameObject("Column2", typeof(RectTransform));
        col2.transform.SetParent(columnsContainer.transform, false);
        var vlg2 = col2.AddComponent<VerticalLayoutGroup>();
        vlg2.spacing = 15;
        vlg2.childControlWidth = true;
        vlg2.childControlHeight = true;
        vlg2.childForceExpandWidth = false;

        AddKeyRow(col1.transform, "Инвентарь", keys.openInventory, k => sm.SetKey("openInventory", k));
        AddKeyRow(col1.transform, "Экипировка", keys.openEquipment, k => sm.SetKey("openEquipment", k));
        AddKeyRow(col1.transform, "Квесты", keys.openQuests, k => sm.SetKey("openQuests", k));
        AddKeyRow(col1.transform, "Закрыть всё", keys.closeAll, k => sm.SetKey("closeAll", k));
        AddKeyRow(col1.transform, "Радиальное меню", keys.openRadialMenu, k => sm.SetKey("openRadialMenu", k));
        AddKeyRow(col1.transform, "Взаимодействие", keys.interactClick, k => sm.SetKey("interactClick", k));
        AddKeyRow(col1.transform, "Магический свет", keys.useMagicLight, k => sm.SetKey("useMagicLight", k));
        AddKeyRow(col1.transform, "Ясновидение", keys.clairvoyance, k => sm.SetKey("clairvoyance", k));

        AddKeyRow(col2.transform, "Пропуск диалога", keys.skipDialogue, k => sm.SetKey("skipDialogue", k));
        AddKeyRow(col2.transform, "Навигация влево", keys.navigateLeft, k => sm.SetKey("navigateLeft", k));
        AddKeyRow(col2.transform, "Навигация вправо", keys.navigateRight, k => sm.SetKey("navigateRight", k));
        AddKeyRow(col2.transform, "Навигация вверх", keys.navigateUp, k => sm.SetKey("navigateUp", k));
        AddKeyRow(col2.transform, "Навигация вниз", keys.navigateDown, k => sm.SetKey("navigateDown", k));
        AddKeyRow(col2.transform, "Разделить пополам", keys.splitHalf, k => sm.SetKey("splitHalf", k));
        AddKeyRow(col2.transform, "Разделить по одному", keys.splitOne, k => sm.SetKey("splitOne", k));
        AddKeyRow(col2.transform, "Сортировка инвентаря", keys.sortInventory, k => sm.SetKey("sortInventory", k));
        AddKeyRow(col2.transform, "Быстрое сохранение", keys.saveGame, k => sm.SetKey("saveGame", k));
        AddKeyRow(col2.transform, "Быстрая загрузка", keys.loadGame, k => sm.SetKey("loadGame", k));
    }

    private void AddKeyRow(Transform parent, string label, KeyCode currentKey, System.Action<KeyCode> onChanged)
    {
        if (keyBindRowPrefab == null) return;

        var row = CreateRow(parent, label);
        var go = Instantiate(keyBindRowPrefab, row);
        go.SetActive(true);
        go.name = $"Key_{label}";

        var le = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le.minWidth = 250f;
        le.preferredWidth = 250f;
        le.flexibleWidth = 0;
        le.minHeight = 30f;
        le.preferredHeight = 30f;

        var keyBtn = go.GetComponent<KeyBindButton>();
        if (keyBtn == null) keyBtn = go.GetComponentInChildren<KeyBindButton>();
        if (keyBtn == null) keyBtn = go.AddComponent<KeyBindButton>();

        var btn = keyBtn.GetComponent<Button>();
        if (btn == null) btn = keyBtn.GetComponentInChildren<Button>();

        var txt = btn != null ? btn.GetComponentInChildren<TextMeshProUGUI>() : keyBtn.GetComponentInChildren<TextMeshProUGUI>();

        if (btn != null && txt != null)
        {
            btn.interactable = true;
            txt.enableAutoSizing = true;
            txt.fontSizeMin = 10;
            txt.fontSizeMax = 24;

            var img = btn.GetComponent<Image>();
            keyBtn.Initialize(txt, btn, img, currentKey, onChanged);
        }
        else
        {
            Logger.LogError(LogModule.UI, $"AddKeyRow: не найден Button или Text в префабе KeyButton");
        }
    }

    private void PopulateGame(Transform parent)
    {
        if (_settingsManager == null) return;

        var s = _settingsManager.Settings;
        string[] languages = { "Русский", "English", "Deutsch" };
        int langIdx = System.Array.IndexOf(languages, s.language);
        if (langIdx < 0) langIdx = 0;
        AddDropdown(parent, "Язык", languages, langIdx, v => _settingsManager.SetLanguage(languages[v]));
        string[] difficulties = { "Лёгкая", "Средняя", "Сложная" };
        int diffIdx = System.Array.IndexOf(difficulties, s.difficulty);
        if (diffIdx < 0) diffIdx = 1;
        AddDropdown(parent, "Сложность", difficulties, diffIdx, v => _settingsManager.SetDifficulty(difficulties[v]));
        AddToggle(parent, "Автосохранение", s.autosave, v => _settingsManager.SetAutosave(v));

        int speedSliderValue = Mathf.RoundToInt(s.dialogueTextSpeed * 100f);
        AddSlider(parent, "Скорость текста", 1f, 15f, speedSliderValue, true, v => _settingsManager.SetDialogueTextSpeed(v / 100f));

        AddToggle(parent, "Авто-диалог", s.autoDialogue, v => _settingsManager.SetAutoDialogue(v));

        int delaySliderValue = Mathf.RoundToInt(s.autoDialogueDelay);
        AddSlider(parent, "Задержка авто-диалога", 1f, 10f, delaySliderValue, true, v => _settingsManager.SetAutoDialogueDelay(v));

        AddToggle(parent, "Показывать подсказки", s.showTooltips, v => _settingsManager.SetShowTooltips(v));
    }
}