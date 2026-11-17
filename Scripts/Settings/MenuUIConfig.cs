using UnityEngine;

[CreateAssetMenu(fileName = "MenuUIConfig", menuName = "Configs/Menu UI Config")]
public class MenuUIConfig : ScriptableObject
{
    [Header("📁 ПУТИ К ПРЕФАБАМ")]
    public string panelPrefabPath = "UI/Prefab/Panels/Panel";
    public string buttonPrefabPath = "UI/Prefab/Buttons/MediumButton";
    public string closeButtonPrefabPath = "UI/Prefab/Buttons/CloseButton";
    public string backButtonPrefabPath = "UI/Prefab/Buttons/BackButton";
    public string trixieBackButtonPath = "UI/Prefab/Buttons/TrixieBackButton";
    public string trixieExitButtonPath = "UI/Prefab/Buttons/TrixieExitButton";
    public string defaultFontPath = "Fonts & Materials/LiberationSans SDF";
    public string backgroundsFolder = "UI/Prefab/Panels/Backgrounds";
    public string memoriesBackgroundPath = "UI/Prefab/Panels/Backgrounds/MemoriesBackground";

    [Header("🎨 ЦВЕТА")]
    public Color panelBgColor = Color.white;
    public Color buttonNormalColor = Color.white;
    public Color buttonHighlightedColor = new Color(0.9f, 0.9f, 0.9f);
    public Color buttonPressedColor = new Color(0.7f, 0.7f, 0.7f);
    public Color buttonSelectedColor = new Color(0.85f, 0.85f, 0.85f);
    public Color overlayColor = new Color(0f, 0f, 0f, 0.5f);
    public Color placeholderTextColor = new Color(0.3f, 0.3f, 0.3f);
    public Color resetNotifyColor = Color.green;

    [Header("📐 РАЗМЕРЫ И ПОЗИЦИИ")]
    public Vector2 mainPanelSize = new Vector2(400f, 200f);
    public Vector2 memoriesPanelSize = new Vector2(450f, 350f);
    public float panelPadding = 30f;
    public float rightOffset = 50f;
    public float verticalOffset = 0f;

    public Vector2 buttonSize = new Vector2(320f, 60f);
    public Vector2 exitButtonSize = new Vector2(350f, 60f);
    public float buttonFontSize = 30f;
    public float buttonSpacing = 20f;
    public float exitButtonSpacing = 40f;

    public float titleFontSize = 40f;
    public Vector2 titleSize = new Vector2(300f, 50f);
    public float titleYOffset = -30f;

    public Vector2 exitPanelSize = new Vector2(500f, 300f);
    public float exitTitleYOffset = 150f;
    public float exitButtonsYOffset = -80f;

    public Vector2 placeholderSize = new Vector2(300f, 100f);
    public float placeholderFontSize = 28f;

    public Vector2 resetNotifySize = new Vector2(300f, 50f);
    public float resetNotifyFontSize = 28f;
    public float resetNotifyYOffset = -150f;
    public float resetNotifyDuration = 1.5f;

    [Header("🔔 ТЕКСТЫ")]
    public string newGameButtonText = "Новая игра";
    public string continueButtonText = "Продолжить";
    public string loadButtonText = "Загрузить";
    public string memoriesButtonText = "Воспоминания";
    public string settingsButtonText = "Настройки";
    public string exitButtonText = "Выход";
    public string backButtonText = "Назад";
    public string backToMenuText = "Вернуться";
    public string quitButtonText = "Выйти";

    public string memoriesTitleText = "Воспоминания";
    public string exitConfirmTitleText = "Вы уверены, что хотите выйти?";
    public string memoriesPlaceholderText = "Здесь будут ваши воспоминания...";
    public string resetNotifyText = "Прогресс сброшен!";

    [Header("⚙️ НАСТРОЙКИ АНИМАЦИИ")]
    public float backgroundTransitionDuration = 0.5f;
    public float panelTransitionDuration = 0.5f;
    public float backgroundOffset = 2000f;
    public float panelTransitionOffset = 2000f;

    public float trixieFadeInDuration = 1.0f;
    public float trixieStartDelay = 0.5f;
    public float trixieLightFadeInDuration = 0.8f;
    public float trixieDefaultLightIntensity = 2.0f;

    public float letterRevealDelay = 0.15f;
    public float intensityMax = 4f;
    public float flickerMin = 2f;
    public float flickerMax = 4f;
    public float flickerSpeed = 0.2f;
    public float letterFadeDuration = 0.5f;

    public float exitTransitionDuration = 0.3f;
    public float exitTitleStartY = 800f;
    public float exitTitleEndY = 250f;
    public float exitButtonsStartY = 600f;
    public float exitButtonsEndY = 0f;

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