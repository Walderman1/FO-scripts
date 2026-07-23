using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TabManager : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private MenuUIConfig config;
    [SerializeField] private UIBuilder uiBuilder;

    private Dictionary<string, GameObject> tabContent = new Dictionary<string, GameObject>();
    private Dictionary<string, GameObject> tabButtons = new Dictionary<string, GameObject>();
    private string activeTab = "";

    public System.Action<string> OnTabSwitched;

    private bool isFullscreen = true;
    private System.Action<bool> onFullscreenToggle;
    private System.Action onResetProgress;

    private bool isInitialized = false;

    public void Initialize(MenuUIConfig config, UIBuilder uiBuilder,
                          bool isFullscreen, System.Action<bool> onFullscreenToggle,
                          System.Action onResetProgress)
    {
        this.config = config;
        this.uiBuilder = uiBuilder;
        this.isFullscreen = isFullscreen;
        this.onFullscreenToggle = onFullscreenToggle;
        this.onResetProgress = onResetProgress;
        isInitialized = true;

        Logger.Log(LogModule.Menu, "TabManager инициализирован");
    }

    public void CreateTabs(Transform parent)
    {
        if (!isInitialized || uiBuilder == null)
        {
            Logger.LogWarning(LogModule.Menu, "TabManager не инициализирован или uiBuilder отсутствует");
            return;
        }

        GameObject container = uiBuilder.CreateTabContainer(parent, config.tabSize, new Vector2(0, -80));

        foreach (string name in config.tabNames)
        {
            GameObject tab = uiBuilder.CreateTabButton(container.transform, name, () => SwitchTab(name));
            tabButtons[name] = tab;
            Logger.Log(LogModule.Menu, $"Создана вкладка: {name}");
        }

        GameObject contentContainer = uiBuilder.CreateTabContentContainer(parent);

        CreateGraphicsTab(contentContainer.transform);
        CreateAudioTab(contentContainer.transform);
        CreateControlsTab(contentContainer.transform);
        CreateGameTab(contentContainer.transform);

        if (config.tabNames.Length > 0)
        {
            SwitchTab(config.tabNames[0]);
        }

        Logger.Log(LogModule.Menu, $"Создано {config.tabNames.Length} вкладок");
    }

    public void SwitchTab(string name)
    {
        if (!isInitialized)
        {
            Logger.LogWarning(LogModule.Menu, "TabManager не инициализирован, переключение вкладки невозможно");
            return;
        }

        foreach (var kvp in tabContent) kvp.Value.SetActive(false);
        if (tabContent.TryGetValue(name, out GameObject tab))
        {
            tab.SetActive(true);
            activeTab = name;
            Logger.Log(LogModule.Menu, $"Переключение на вкладку: {name}");
        }
        else
        {
            Logger.LogWarning(LogModule.Menu, $"Вкладка '{name}' не найдена");
        }
        UpdateTabVisuals(name);
        OnTabSwitched?.Invoke(name);
    }

    private void UpdateTabVisuals(string active)
    {
        foreach (var kvp in tabButtons)
        {
            Image img = kvp.Value?.GetComponent<Image>();
            if (img != null)
                img.color = kvp.Key == active ? config.tabSelectedColor : config.tabNormalColor;
        }
    }

    #region Tab Content Creation

    private void CreateGraphicsTab(Transform parent)
    {
        GameObject tab = uiBuilder.CreateTabContent(parent, "GraphicsTab");
        tabContent["Графика"] = tab;

        float labelX = -config.labelXOffset;
        float toggleX = config.labelXOffset;

        uiBuilder.CreateSettingLabel(tab.transform, config.fullscreenLabel, new Vector2(labelX, 80));
        GameObject fullscreenToggle = uiBuilder.CreateToggle(tab.transform, new Vector2(toggleX, 80), isFullscreen,
            (value) => onFullscreenToggle?.Invoke(value));
        fullscreenToggle.name = "FullscreenToggle";

        uiBuilder.CreateSettingLabel(tab.transform, config.resolutionLabel, new Vector2(labelX, 20));
        GameObject resolutionDropdown = uiBuilder.CreateDropdown(tab.transform, new Vector2(toggleX, 20), config.resolutionOptions);
        resolutionDropdown.name = "ResolutionDropdown";

        uiBuilder.CreateSettingLabel(tab.transform, config.qualityLabel, new Vector2(labelX, -40));
        GameObject qualityDropdown = uiBuilder.CreateDropdown(tab.transform, new Vector2(toggleX, -40), config.qualityOptions);
        qualityDropdown.name = "QualityDropdown";

        uiBuilder.CreateSettingLabel(tab.transform, config.vsyncLabel, new Vector2(labelX, -100));
        GameObject vsyncToggle = uiBuilder.CreateToggle(tab.transform, new Vector2(toggleX, -100), config.vsyncDefault);
        vsyncToggle.name = "VSyncToggle";

        Logger.Log(LogModule.Menu, "Создана вкладка 'Графика'");
    }

    private void CreateAudioTab(Transform parent)
    {
        GameObject tab = uiBuilder.CreateTabContent(parent, "AudioTab");
        tabContent["Звук"] = tab;

        float labelX = -config.labelXOffset;
        float sliderX = config.labelXOffset;

        uiBuilder.CreateSettingLabel(tab.transform, config.volumeLabel, new Vector2(labelX, 80));
        uiBuilder.CreateVolumeSlider(tab.transform, new Vector2(sliderX, 80));

        uiBuilder.CreateSettingLabel(tab.transform, config.musicLabel, new Vector2(labelX, 10));
        GameObject musicSlider = uiBuilder.CreateSlider(tab.transform, new Vector2(sliderX, 10), 0f, 10f, config.musicDefault * 10f);
        musicSlider.name = "MusicSlider";

        uiBuilder.CreateSettingLabel(tab.transform, config.sfxLabel, new Vector2(labelX, -60));
        GameObject sfxSlider = uiBuilder.CreateSlider(tab.transform, new Vector2(sliderX, -60), 0f, 10f, config.sfxDefault * 10f);
        sfxSlider.name = "SFXSlider";

        uiBuilder.CreateSettingLabel(tab.transform, config.voiceLabel, new Vector2(labelX, -130));
        GameObject voiceSlider = uiBuilder.CreateSlider(tab.transform, new Vector2(sliderX, -130), 0f, 10f, config.voiceDefault * 10f);
        voiceSlider.name = "VoiceSlider";

        Logger.Log(LogModule.Menu, "Создана вкладка 'Звук'");
    }

    private void CreateControlsTab(Transform parent)
    {
        GameObject tab = uiBuilder.CreateTabContent(parent, "ControlsTab");
        tabContent["Управление"] = tab;

        float labelX = -config.labelXOffset;
        float controlX = config.labelXOffset;

        uiBuilder.CreateSettingLabel(tab.transform, config.sensitivityLabel, new Vector2(labelX, 80));
        GameObject sensitivitySlider = uiBuilder.CreateSlider(tab.transform, new Vector2(controlX, 80), 0f, 10f, config.sensitivityDefault);
        sensitivitySlider.name = "SensitivitySlider";

        uiBuilder.CreateSettingLabel(tab.transform, config.invertYLabel, new Vector2(labelX, 20));
        GameObject invertToggle = uiBuilder.CreateToggle(tab.transform, new Vector2(controlX, 20), false);
        invertToggle.name = "InvertYToggle";

        uiBuilder.CreateSettingLabel(tab.transform, config.controlSchemeLabel, new Vector2(labelX, -40));
        GameObject controlDropdown = uiBuilder.CreateDropdown(tab.transform, new Vector2(controlX, -40), config.controlOptions);
        controlDropdown.name = "ControlDropdown";

        uiBuilder.CreateSettingLabel(tab.transform, config.vibrationLabel, new Vector2(labelX, -100));
        GameObject vibrationToggle = uiBuilder.CreateToggle(tab.transform, new Vector2(controlX, -100), config.vibrationDefault);
        vibrationToggle.name = "VibrationToggle";

        Logger.Log(LogModule.Menu, "Создана вкладка 'Управление'");
    }

    private void CreateGameTab(Transform parent)
    {
        GameObject tab = uiBuilder.CreateTabContent(parent, "GameTab");
        tabContent["Игра"] = tab;

        float labelX = -config.labelXOffset;
        float gameX = config.labelXOffset;

        uiBuilder.CreateSettingLabel(tab.transform, config.languageLabel, new Vector2(labelX, 80));
        GameObject languageDropdown = uiBuilder.CreateDropdown(tab.transform, new Vector2(gameX, 80), config.languageOptions);
        languageDropdown.name = "LanguageDropdown";

        uiBuilder.CreateSettingLabel(tab.transform, config.difficultyLabel, new Vector2(labelX, 20));
        GameObject difficultyDropdown = uiBuilder.CreateDropdown(tab.transform, new Vector2(gameX, 20), config.difficultyOptions);
        difficultyDropdown.name = "DifficultyDropdown";

        uiBuilder.CreateSettingLabel(tab.transform, config.autosaveLabel, new Vector2(labelX, -40));
        GameObject autosaveToggle = uiBuilder.CreateToggle(tab.transform, new Vector2(gameX, -40), config.autosaveDefault);
        autosaveToggle.name = "AutosaveToggle";

        GameObject resetBtn = uiBuilder.CreateSmallButton(tab.transform, config.resetProgressLabel,
            () => onResetProgress?.Invoke());
        RectTransform resetRt = resetBtn.GetComponent<RectTransform>();
        if (resetRt != null)
        {
            resetRt.anchoredPosition = new Vector2(0, -130);
            resetRt.sizeDelta = new Vector2(250, 45);
        }

        Logger.Log(LogModule.Menu, "Создана вкладка 'Игра'");
    }

    #endregion

    public void Cleanup()
    {
        tabContent.Clear();
        tabButtons.Clear();
        Logger.Log(LogModule.Menu, "TabManager очищен");
    }
}
