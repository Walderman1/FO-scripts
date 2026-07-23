using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "MenuUIConfig", menuName = "Configs/Menu UI Config")]
public class MenuUIConfig : ScriptableObject
{
    [Header("📁 ПУТИ К ПРЕФАБАМ")]
    public string panelPrefabPath = "UI/Prefab/Panels/Panel";
    public string bigPanelPrefabPath = "UI/Prefab/Panels/BigPanel";
    public string buttonPrefabPath = "UI/Prefab/Buttons/MediumButton";
    public string closeButtonPrefabPath = "UI/Prefab/Buttons/CloseButton";
    public string backButtonPrefabPath = "UI/Prefab/Buttons/BackButton";
    public string trixieBackButtonPath = "UI/Prefab/Buttons/TrixieBackButton";
    public string trixieExitButtonPath = "UI/Prefab/Buttons/TrixieExitButton";
    public string sliderContainerPrefabPath = "UI/Prefab/Slider/SliderContainer";
    public string togglePrefabPath = "UI/Prefab/Toggle/Toggle";
    public string tabPrefabPath = "UI/Prefab/Buttons/Tab";
    public string defaultFontPath = "Fonts & Materials/LiberationSans SDF";
    public string backgroundsFolder = "UI/Prefab/Panels/Backgrounds";
    public string memoriesBackgroundPath = "UI/Prefab/Panels/Backgrounds/MemoriesBackground";

    [Header("🎨 ЦВЕТА")]
    public Color panelBgColor = new Color(0.1f, 0.1f, 0.1f, 0.9f);
    public Color settingsPanelColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color buttonNormalColor = new Color(0.2f, 0.2f, 0.2f);
    public Color buttonHighlightedColor = new Color(0.4f, 0.4f, 0.4f);
    public Color buttonPressedColor = new Color(0.1f, 0.1f, 0.1f);
    public Color buttonSelectedColor = new Color(0.3f, 0.3f, 0.3f);
    public Color tabNormalColor = new Color(0.2f, 0.2f, 0.2f);
    public Color tabSelectedColor = new Color(0.3f, 0.5f, 0.8f);
    public Color toggleOnColor = new Color(0.2f, 0.6f, 1f);
    public Color toggleOffColor = new Color(0.3f, 0.3f, 0.3f);
    public Color dropdownBgColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color dropdownItemColor = new Color(0.2f, 0.2f, 0.2f, 1f);
    public Color dropdownItemHoverColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    public Color overlayColor = new Color(0f, 0f, 0f, 0.5f);
    public Color placeholderTextColor = new Color(0.7f, 0.7f, 0.7f);
    public Color markTextColor = new Color(0.5f, 0.5f, 0.5f);
    public Color resetNotifyColor = Color.green;

    [Header("📐 РАЗМЕРЫ И ПОЗИЦИИ")]
    [Header("——— Панели ———")]
    public Vector2 mainPanelSize = new Vector2(400f, 200f);
    public Vector2 memoriesPanelSize = new Vector2(450f, 350f);
    public float panelPadding = 30f;
    public float rightOffset = 50f;
    public float verticalOffset = 0f;

    [Header("——— Кнопки ———")]
    public Vector2 buttonSize = new Vector2(320f, 60f);
    public Vector2 smallButtonSize = new Vector2(120f, 50f);
    public Vector2 exitButtonSize = new Vector2(350f, 60f);
    public float buttonFontSize = 30f;
    public float smallButtonFontSize = 28f;
    public float buttonSpacing = 20f;
    public float exitButtonSpacing = 40f;

    [Header("——— Вкладки ———")]
    public Vector2 tabSize = new Vector2(600f, 50f);
    public float tabFontSize = 24f;
    public float tabSpacing = 10f;

    [Header("——— Заголовки ———")]
    public float titleFontSize = 40f;
    public Vector2 titleSize = new Vector2(300f, 50f);
    public float titleYOffset = -30f;

    [Header("——— Слайдеры ———")]
    public Vector2 sliderSize = new Vector2(280f, 50f);
    public float sliderFontSize = 22f;

    [Header("——— Переключатели ———")]
    public Vector2 toggleSize = new Vector2(40f, 20f);
    public float toggleHandleOffset = 22f;

    [Header("——— Выпадающие списки ———")]
    public Vector2 dropdownSize = new Vector2(200f, 40f);
    public float dropdownFontSize = 20f;
    public float dropdownItemHeight = 28f;

    [Header("——— Метки ———")]
    public Vector2 labelSize = new Vector2(250f, 30f);
    public float labelFontSize = 22f;
    public float labelXOffset = 150f;

    [Header("——— Панель выхода ———")]
    public Vector2 exitPanelSize = new Vector2(500f, 300f);
    public float exitTitleYOffset = 150f;
    public float exitButtonsYOffset = -80f;

    [Header("——— Placeholder ———")]
    public Vector2 placeholderSize = new Vector2(300f, 100f);
    public float placeholderFontSize = 28f;

    [Header("——— Уведомление о сбросе ———")]
    public Vector2 resetNotifySize = new Vector2(300f, 50f);
    public float resetNotifyFontSize = 28f;
    public float resetNotifyYOffset = -150f;
    public float resetNotifyDuration = 1.5f;

    [Header("🔔 ТЕКСТЫ")]
    [Header("——— Кнопки ———")]
    public string newGameButtonText = "Новая игра";
    public string continueButtonText = "Продолжить";
    public string memoriesButtonText = "Воспоминания";
    public string settingsButtonText = "Настройки";
    public string exitButtonText = "Выход";
    public string backButtonText = "Назад";
    public string backToMenuText = "Вернуться";
    public string quitButtonText = "Выйти";

    [Header("——— Заголовки ———")]
    public string settingsTitleText = "Настройки";
    public string memoriesTitleText = "Воспоминания";
    public string exitConfirmTitleText = "Вы уверены, что хотите выйти?";

    [Header("——— Placeholder ———")]
    public string memoriesPlaceholderText = "Здесь будут ваши воспоминания...";

    [Header("——— Уведомления ———")]
    public string resetNotifyText = "Прогресс сброшен!";

    [Header("——— Настройки (лейблы) ———")]
    public string fullscreenLabel = "Полноэкранный режим";
    public string resolutionLabel = "Разрешение";
    public string qualityLabel = "Качество";
    public string vsyncLabel = "VSync";
    public string volumeLabel = "Громкость";
    public string musicLabel = "Музыка";
    public string sfxLabel = "Звуки";
    public string voiceLabel = "Голос";
    public string sensitivityLabel = "Чувствительность";
    public string invertYLabel = "Инвертировать Y";
    public string controlSchemeLabel = "Схема управления";
    public string vibrationLabel = "Вибрация";
    public string languageLabel = "Язык";
    public string difficultyLabel = "Сложность";
    public string autosaveLabel = "Автосохранение";
    public string resetProgressLabel = "Сброс прогресса";

    [Header("📑 НАЗВАНИЯ ВКЛАДОК")]
    public string[] tabNames = { "Графика", "Звук", "Управление", "Игра" };

    [Header("📋 ОПЦИИ ДЛЯ ВЫПАДАЮЩИХ СПИСКОВ")]
    public string[] resolutionOptions = { "1920x1080", "1600x900", "1280x720" };
    public string[] qualityOptions = { "Высокое", "Среднее", "Низкое" };
    public string[] controlOptions = { "Стандартная", "Альтернативная" };
    public string[] languageOptions = { "Русский", "English", "Deutsch" };
    public string[] difficultyOptions = { "Лёгкая", "Средняя", "Сложная" };

    [Header("⚙️ НАСТРОЙКИ АНИМАЦИИ")]
    public float backgroundTransitionDuration = 0.5f;
    public float panelTransitionDuration = 0.5f;
    public float backgroundOffset = 2000f;
    public float panelTransitionOffset = 2000f;

    [Header("——— Trixie ———")]
    public float trixieFadeInDuration = 1.0f;
    public float trixieStartDelay = 0.5f;
    public float trixieLightFadeInDuration = 0.8f;
    public float trixieDefaultLightIntensity = 2.0f;

    [Header("——— Finding Oneself ———")]
    public float letterRevealDelay = 0.15f;
    public float intensityMax = 4f;
    public float flickerMin = 2f;
    public float flickerMax = 4f;
    public float flickerSpeed = 0.2f;
    public float letterFadeDuration = 0.5f;

    [Header("——— Exit Panel ———")]
    public float exitTransitionDuration = 0.3f;
    public float exitTitleStartY = 800f;
    public float exitTitleEndY = 250f;
    public float exitButtonsStartY = 600f;
    public float exitButtonsEndY = 0f;

    [Header("🎮 НАСТРОЙКИ ИГРЫ")]
    public float volumeDefault = 1f;
    public float musicDefault = 0.8f;
    public float sfxDefault = 0.8f;
    public float voiceDefault = 0.8f;
    public float sensitivityDefault = 5f;
    public bool vsyncDefault = true;
    public bool vibrationDefault = true;
    public bool autosaveDefault = true;

    [Header("🔧 СЛУЖЕБНОЕ")]
    public string uiRootName = "UIRoot";
    public string canvasName = "MenuCanvas";
    public string exitOverlayName = "ExitOverlay";
    public string exitTitleName = "ExitTitle";
    public string exitBackButtonName = "ExitBackButton";
    public string exitQuitButtonName = "ExitQuitButton";

    public static MenuUIConfig GetDefault()
    {
        return Resources.Load<MenuUIConfig>("Configs/MenuUIConfig") ?? CreateInstance<MenuUIConfig>();
    }
}
