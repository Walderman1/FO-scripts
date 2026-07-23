using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class PanelManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private MenuUIConfig config;
    [SerializeField] private Transform canvasTransform;
    [SerializeField] private BackgroundManager backgroundManager;

    private GameObject panelPrefab;
    private GameObject bigPanelPrefab;
    private GameObject buttonPrefab;
    private TMP_FontAsset defaultFont;
    private UIBuilder uiBuilder;

    public GameObject MainPanel { get; private set; }
    public GameObject SettingsPanel { get; private set; }
    public GameObject MemoriesPanel { get; private set; }
    public GameObject CurrentPanel { get; private set; }

    public System.Action<GameObject> OnPanelChanged;
    public System.Action OnSettingsOpened;
    public System.Action OnMemoriesOpened;

    private bool isInitialized = false;

    public void Initialize(MenuUIConfig config, Transform canvasTransform, BackgroundManager backgroundManager,
                          GameObject panelPrefab, GameObject bigPanelPrefab, GameObject buttonPrefab,
                          TMP_FontAsset defaultFont, UIBuilder uiBuilder)
    {
        this.config = config;
        this.canvasTransform = canvasTransform;
        this.backgroundManager = backgroundManager;
        this.panelPrefab = panelPrefab;
        this.bigPanelPrefab = bigPanelPrefab;
        this.buttonPrefab = buttonPrefab;
        this.defaultFont = defaultFont;
        this.uiBuilder = uiBuilder;
        isInitialized = true;

        Logger.Log(LogModule.Menu, "PanelManager инициализирован");
    }

    public void CreateAllPanels()
    {
        if (!isInitialized)
        {
            Logger.LogError(LogModule.Menu, "PanelManager не инициализирован");
            return;
        }

        CreateMainPanel();
        CreateSettingsPanel();
        CreateMemoriesPanel();

        CurrentPanel = MainPanel;

        Logger.Log(LogModule.Menu, "Все панели созданы");
    }

    #region Create Panels

    private void CreateMainPanel()
    {
        if (panelPrefab == null)
        {
            Logger.LogWarning(LogModule.Menu, "panelPrefab не назначен, главная панель не создана");
            return;
        }

        MainPanel = Instantiate(panelPrefab, canvasTransform);
        MainPanel.name = "MainPanel";

        RectTransform rt = MainPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(1f, 0.5f);
            rt.anchorMax = new Vector2(1f, 0.5f);
            rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = new Vector2(-config.rightOffset, config.verticalOffset);
            rt.sizeDelta = config.mainPanelSize;
        }

        Image img = MainPanel.GetComponent<Image>();
        if (img != null)
        {
            img.color = config.panelBgColor;
            img.raycastTarget = false;
        }

        Logger.Log(LogModule.Menu, "Главная панель создана");
    }

    private void CreateSettingsPanel()
    {
        if (bigPanelPrefab == null)
        {
            Logger.LogWarning(LogModule.Menu, "bigPanelPrefab не назначен, панель настроек не создана");
            return;
        }

        SettingsPanel = Instantiate(bigPanelPrefab, canvasTransform);
        SettingsPanel.name = "SettingsPanel";
        SettingsPanel.SetActive(false);

        RectTransform rt = SettingsPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.125f, 0.125f);
            rt.anchorMax = new Vector2(0.875f, 0.875f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
        }

        Image img = SettingsPanel.GetComponent<Image>();
        if (img != null)
        {
            img.color = config.settingsPanelColor;
            img.raycastTarget = true;
        }

        Logger.Log(LogModule.Menu, "Панель настроек создана");
    }

    private void CreateMemoriesPanel()
    {
        if (panelPrefab == null)
        {
            Logger.LogWarning(LogModule.Menu, "panelPrefab не назначен, панель воспоминаний не создана");
            return;
        }

        MemoriesPanel = Instantiate(panelPrefab, canvasTransform);
        MemoriesPanel.name = "MemoriesPanel";
        MemoriesPanel.SetActive(false);

        RectTransform rt = MemoriesPanel.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = config.memoriesPanelSize;
            rt.anchoredPosition = Vector2.zero;
        }

        Image img = MemoriesPanel.GetComponent<Image>();
        if (img != null)
        {
            img.color = config.panelBgColor;
            img.raycastTarget = true;
        }

        Logger.Log(LogModule.Menu, "Панель воспоминаний создана");
    }

    #endregion

    #region Panel Management

    public void UpdateMainPanelSize()
    {
        if (MainPanel == null)
        {
            Logger.LogWarning(LogModule.Menu, "MainPanel отсутствует, обновление размера невозможно");
            return;
        }

        RectTransform rt = MainPanel.GetComponent<RectTransform>();
        if (rt == null) return;

        List<RectTransform> buttons = new List<RectTransform>();
        foreach (Transform child in MainPanel.transform)
        {
            Button btn = child.GetComponent<Button>();
            if (btn != null)
            {
                RectTransform btnRt = child.GetComponent<RectTransform>();
                if (btnRt != null) buttons.Add(btnRt);
            }
        }

        if (buttons.Count == 0)
        {
            Logger.Log(LogModule.Menu, "Нет кнопок для обновления размера панели");
            return;
        }

        float totalHeight = 0f;
        foreach (RectTransform btnRt in buttons) totalHeight += btnRt.sizeDelta.y;

        float spacing = (buttons.Count - 1) * config.buttonSpacing;
        float totalHeightWithSpacing = totalHeight + spacing;
        float panelHeightFinal = totalHeightWithSpacing + config.panelPadding * 2f;

        rt.sizeDelta = new Vector2(rt.sizeDelta.x, panelHeightFinal);

        float startY = totalHeightWithSpacing / 2f - config.panelPadding;
        for (int i = 0; i < buttons.Count; i++)
        {
            float y = startY - i * (buttons[i].sizeDelta.y + config.buttonSpacing);
            buttons[i].anchoredPosition = new Vector2(0, y);
        }

        Logger.Log(LogModule.Menu, $"Размер главной панели обновлён: {panelHeightFinal}");
    }

    public void SwitchPanel(GameObject targetPanel, System.Action onComplete = null)
    {
        if (targetPanel == null)
        {
            if (CurrentPanel != null)
            {
                CurrentPanel.SetActive(false);
                CurrentPanel = null;
                Logger.Log(LogModule.Menu, "Текущая панель скрыта");
            }
            onComplete?.Invoke();
            return;
        }

        if (CurrentPanel != null)
        {
            CurrentPanel.SetActive(false);
        }

        CurrentPanel = targetPanel;
        CurrentPanel.SetActive(true);
        OnPanelChanged?.Invoke(targetPanel);
        onComplete?.Invoke();

        Logger.Log(LogModule.Menu, $"Переключение на панель: {targetPanel.name}");
    }

    public void ShowPanel(GameObject panel)
    {
        if (panel == null)
        {
            Logger.LogWarning(LogModule.Menu, "Попытка показать null панель");
            return;
        }

        HideAllPanels();

        panel.SetActive(true);
        CurrentPanel = panel;
        OnPanelChanged?.Invoke(panel);

        Logger.Log(LogModule.Menu, $"Показана панель: {panel.name}");
    }

    public void HideAllPanels()
    {
        if (MainPanel != null) MainPanel.SetActive(false);
        if (SettingsPanel != null) SettingsPanel.SetActive(false);
        if (MemoriesPanel != null) MemoriesPanel.SetActive(false);

        Logger.Log(LogModule.Menu, "Все панели скрыты");
    }

    public void ResetPanelsPosition()
    {
        ResetPanelPosition(MainPanel);
        ResetPanelPosition(SettingsPanel);
        ResetPanelPosition(MemoriesPanel);

        Logger.Log(LogModule.Menu, "Позиции всех панелей сброшены");
    }

    private void ResetPanelPosition(GameObject panel)
    {
        if (panel == null) return;
        RectTransform rt = panel.GetComponent<RectTransform>();
        if (rt != null) rt.anchoredPosition = Vector2.zero;
    }

    #endregion

    #region Panel Content Setup

    public void SetupSettingsContent(System.Action<Transform> setupTabs, System.Action<Transform> setupBackButton)
    {
        if (SettingsPanel == null)
        {
            Logger.LogWarning(LogModule.Menu, "SettingsPanel отсутствует, настройка содержимого невозможна");
            return;
        }

        CreateTitle(SettingsPanel.transform, config.settingsTitleText);

        setupBackButton?.Invoke(SettingsPanel.transform);

        setupTabs?.Invoke(SettingsPanel.transform);

        Logger.Log(LogModule.Menu, "Настроено содержимое панели настроек");
    }

    public void SetupMemoriesContent(System.Action<Transform> setupBackButton)
    {
        if (MemoriesPanel == null)
        {
            Logger.LogWarning(LogModule.Menu, "MemoriesPanel отсутствует, настройка содержимого невозможна");
            return;
        }

        CreateTitle(MemoriesPanel.transform, config.memoriesTitleText);

        if (uiBuilder != null)
        {
            uiBuilder.CreatePlaceholderText(MemoriesPanel.transform, config.memoriesPlaceholderText);
        }

        setupBackButton?.Invoke(MemoriesPanel.transform);

        Logger.Log(LogModule.Menu, "Настроено содержимое панели воспоминаний");
    }

    private void CreateTitle(Transform parent, string text)
    {
        GameObject title = new GameObject("Title");
        title.transform.SetParent(parent, false);

        RectTransform rt = title.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 1f);
        rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = config.titleSize;
        rt.anchoredPosition = new Vector2(0, config.titleYOffset);

        TextMeshProUGUI tmp = title.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = config.titleFontSize;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        tmp.fontStyle = FontStyles.Bold;
        if (defaultFont != null) tmp.font = defaultFont;

        Logger.Log(LogModule.Menu, $"Создан заголовок: {text}");
    }

    #endregion

    public void Cleanup()
    {
        if (MainPanel != null) Destroy(MainPanel);
        if (SettingsPanel != null) Destroy(SettingsPanel);
        if (MemoriesPanel != null) Destroy(MemoriesPanel);

        Logger.Log(LogModule.Menu, "PanelManager очищен");
    }
}
