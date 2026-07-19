// MenuUIConfigEditor.cs - КАСТОМНЫЙ РЕДАКТОР ДЛЯ КОНФИГА МЕНЮ UI
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MenuUIConfig))]
public class MenuUIConfigEditor : Editor
{
    private MenuUIConfig config;

    private bool showPaths = false;
    private bool showColors = false;
    private bool showSizes = false;
    private bool showTexts = false;
    private bool showAnimation = false;
    private bool showGameSettings = false;
    private bool showService = false;

    private SerializedProperty panelPrefabPath;
    private SerializedProperty bigPanelPrefabPath;
    private SerializedProperty buttonPrefabPath;
    private SerializedProperty closeButtonPrefabPath;
    private SerializedProperty backButtonPrefabPath;
    private SerializedProperty trixieBackButtonPath;
    private SerializedProperty trixieExitButtonPath;
    private SerializedProperty sliderContainerPrefabPath;
    private SerializedProperty togglePrefabPath;
    private SerializedProperty tabPrefabPath;
    private SerializedProperty defaultFontPath;
    private SerializedProperty backgroundsFolder;
    private SerializedProperty memoriesBackgroundPath;

    private SerializedProperty panelBgColor;
    private SerializedProperty settingsPanelColor;
    private SerializedProperty buttonNormalColor;
    private SerializedProperty buttonHighlightedColor;
    private SerializedProperty buttonPressedColor;
    private SerializedProperty buttonSelectedColor;
    private SerializedProperty tabNormalColor;
    private SerializedProperty tabSelectedColor;
    private SerializedProperty toggleOnColor;
    private SerializedProperty toggleOffColor;
    private SerializedProperty dropdownBgColor;
    private SerializedProperty dropdownItemColor;
    private SerializedProperty dropdownItemHoverColor;
    private SerializedProperty overlayColor;
    private SerializedProperty placeholderTextColor;
    private SerializedProperty markTextColor;
    private SerializedProperty resetNotifyColor;

    private SerializedProperty mainPanelSize;
    private SerializedProperty memoriesPanelSize;
    private SerializedProperty panelPadding;
    private SerializedProperty rightOffset;
    private SerializedProperty verticalOffset;
    private SerializedProperty buttonSize;
    private SerializedProperty smallButtonSize;
    private SerializedProperty exitButtonSize;
    private SerializedProperty buttonFontSize;
    private SerializedProperty smallButtonFontSize;
    private SerializedProperty buttonSpacing;
    private SerializedProperty exitButtonSpacing;
    private SerializedProperty tabSize;
    private SerializedProperty tabFontSize;
    private SerializedProperty tabSpacing;
    private SerializedProperty titleFontSize;
    private SerializedProperty titleSize;
    private SerializedProperty titleYOffset;
    private SerializedProperty sliderSize;
    private SerializedProperty sliderFontSize;
    private SerializedProperty toggleSize;
    private SerializedProperty toggleHandleOffset;
    private SerializedProperty dropdownSize;
    private SerializedProperty dropdownFontSize;
    private SerializedProperty dropdownItemHeight;
    private SerializedProperty labelSize;
    private SerializedProperty labelFontSize;
    private SerializedProperty labelXOffset;
    private SerializedProperty exitPanelSize;
    private SerializedProperty exitTitleYOffset;
    private SerializedProperty exitButtonsYOffset;
    private SerializedProperty placeholderSize;
    private SerializedProperty placeholderFontSize;
    private SerializedProperty resetNotifySize;
    private SerializedProperty resetNotifyFontSize;
    private SerializedProperty resetNotifyYOffset;
    private SerializedProperty resetNotifyDuration;

    private SerializedProperty newGameButtonText;
    private SerializedProperty continueButtonText;
    private SerializedProperty memoriesButtonText;
    private SerializedProperty settingsButtonText;
    private SerializedProperty exitButtonText;
    private SerializedProperty backButtonText;
    private SerializedProperty backToMenuText;
    private SerializedProperty quitButtonText;
    private SerializedProperty settingsTitleText;
    private SerializedProperty memoriesTitleText;
    private SerializedProperty exitConfirmTitleText;
    private SerializedProperty memoriesPlaceholderText;
    private SerializedProperty resetNotifyText;
    private SerializedProperty fullscreenLabel;
    private SerializedProperty resolutionLabel;
    private SerializedProperty qualityLabel;
    private SerializedProperty vsyncLabel;
    private SerializedProperty volumeLabel;
    private SerializedProperty musicLabel;
    private SerializedProperty sfxLabel;
    private SerializedProperty voiceLabel;
    private SerializedProperty sensitivityLabel;
    private SerializedProperty invertYLabel;
    private SerializedProperty controlSchemeLabel;
    private SerializedProperty vibrationLabel;
    private SerializedProperty languageLabel;
    private SerializedProperty difficultyLabel;
    private SerializedProperty autosaveLabel;
    private SerializedProperty resetProgressLabel;

    private SerializedProperty tabNames;
    private SerializedProperty resolutionOptions;
    private SerializedProperty qualityOptions;
    private SerializedProperty controlOptions;
    private SerializedProperty languageOptions;
    private SerializedProperty difficultyOptions;

    private SerializedProperty backgroundTransitionDuration;
    private SerializedProperty panelTransitionDuration;
    private SerializedProperty backgroundOffset;
    private SerializedProperty panelTransitionOffset;
    private SerializedProperty trixieFadeInDuration;
    private SerializedProperty trixieStartDelay;
    private SerializedProperty trixieLightFadeInDuration;
    private SerializedProperty trixieDefaultLightIntensity;
    private SerializedProperty letterRevealDelay;
    private SerializedProperty intensityMax;
    private SerializedProperty flickerMin;
    private SerializedProperty flickerMax;
    private SerializedProperty flickerSpeed;
    private SerializedProperty letterFadeDuration;
    private SerializedProperty exitTransitionDuration;
    private SerializedProperty exitTitleStartY;
    private SerializedProperty exitTitleEndY;
    private SerializedProperty exitButtonsStartY;
    private SerializedProperty exitButtonsEndY;

    private SerializedProperty volumeDefault;
    private SerializedProperty musicDefault;
    private SerializedProperty sfxDefault;
    private SerializedProperty voiceDefault;
    private SerializedProperty sensitivityDefault;
    private SerializedProperty vsyncDefault;
    private SerializedProperty vibrationDefault;
    private SerializedProperty autosaveDefault;

    private SerializedProperty uiRootName;
    private SerializedProperty canvasName;
    private SerializedProperty exitOverlayName;
    private SerializedProperty exitTitleName;
    private SerializedProperty exitBackButtonName;
    private SerializedProperty exitQuitButtonName;

    private void OnEnable()
    {
        config = (MenuUIConfig)target;

        // Пути
        panelPrefabPath = serializedObject.FindProperty("panelPrefabPath");
        bigPanelPrefabPath = serializedObject.FindProperty("bigPanelPrefabPath");
        buttonPrefabPath = serializedObject.FindProperty("buttonPrefabPath");
        closeButtonPrefabPath = serializedObject.FindProperty("closeButtonPrefabPath");
        backButtonPrefabPath = serializedObject.FindProperty("backButtonPrefabPath");
        trixieBackButtonPath = serializedObject.FindProperty("trixieBackButtonPath");
        trixieExitButtonPath = serializedObject.FindProperty("trixieExitButtonPath");
        sliderContainerPrefabPath = serializedObject.FindProperty("sliderContainerPrefabPath");
        togglePrefabPath = serializedObject.FindProperty("togglePrefabPath");
        tabPrefabPath = serializedObject.FindProperty("tabPrefabPath");
        defaultFontPath = serializedObject.FindProperty("defaultFontPath");
        backgroundsFolder = serializedObject.FindProperty("backgroundsFolder");
        memoriesBackgroundPath = serializedObject.FindProperty("memoriesBackgroundPath");

        // Цвета
        panelBgColor = serializedObject.FindProperty("panelBgColor");
        settingsPanelColor = serializedObject.FindProperty("settingsPanelColor");
        buttonNormalColor = serializedObject.FindProperty("buttonNormalColor");
        buttonHighlightedColor = serializedObject.FindProperty("buttonHighlightedColor");
        buttonPressedColor = serializedObject.FindProperty("buttonPressedColor");
        buttonSelectedColor = serializedObject.FindProperty("buttonSelectedColor");
        tabNormalColor = serializedObject.FindProperty("tabNormalColor");
        tabSelectedColor = serializedObject.FindProperty("tabSelectedColor");
        toggleOnColor = serializedObject.FindProperty("toggleOnColor");
        toggleOffColor = serializedObject.FindProperty("toggleOffColor");
        dropdownBgColor = serializedObject.FindProperty("dropdownBgColor");
        dropdownItemColor = serializedObject.FindProperty("dropdownItemColor");
        dropdownItemHoverColor = serializedObject.FindProperty("dropdownItemHoverColor");
        overlayColor = serializedObject.FindProperty("overlayColor");
        placeholderTextColor = serializedObject.FindProperty("placeholderTextColor");
        markTextColor = serializedObject.FindProperty("markTextColor");
        resetNotifyColor = serializedObject.FindProperty("resetNotifyColor");

        // Размеры
        mainPanelSize = serializedObject.FindProperty("mainPanelSize");
        memoriesPanelSize = serializedObject.FindProperty("memoriesPanelSize");
        panelPadding = serializedObject.FindProperty("panelPadding");
        rightOffset = serializedObject.FindProperty("rightOffset");
        verticalOffset = serializedObject.FindProperty("verticalOffset");
        buttonSize = serializedObject.FindProperty("buttonSize");
        smallButtonSize = serializedObject.FindProperty("smallButtonSize");
        exitButtonSize = serializedObject.FindProperty("exitButtonSize");
        buttonFontSize = serializedObject.FindProperty("buttonFontSize");
        smallButtonFontSize = serializedObject.FindProperty("smallButtonFontSize");
        buttonSpacing = serializedObject.FindProperty("buttonSpacing");
        exitButtonSpacing = serializedObject.FindProperty("exitButtonSpacing");
        tabSize = serializedObject.FindProperty("tabSize");
        tabFontSize = serializedObject.FindProperty("tabFontSize");
        tabSpacing = serializedObject.FindProperty("tabSpacing");
        titleFontSize = serializedObject.FindProperty("titleFontSize");
        titleSize = serializedObject.FindProperty("titleSize");
        titleYOffset = serializedObject.FindProperty("titleYOffset");
        sliderSize = serializedObject.FindProperty("sliderSize");
        sliderFontSize = serializedObject.FindProperty("sliderFontSize");
        toggleSize = serializedObject.FindProperty("toggleSize");
        toggleHandleOffset = serializedObject.FindProperty("toggleHandleOffset");
        dropdownSize = serializedObject.FindProperty("dropdownSize");
        dropdownFontSize = serializedObject.FindProperty("dropdownFontSize");
        dropdownItemHeight = serializedObject.FindProperty("dropdownItemHeight");
        labelSize = serializedObject.FindProperty("labelSize");
        labelFontSize = serializedObject.FindProperty("labelFontSize");
        labelXOffset = serializedObject.FindProperty("labelXOffset");
        exitPanelSize = serializedObject.FindProperty("exitPanelSize");
        exitTitleYOffset = serializedObject.FindProperty("exitTitleYOffset");
        exitButtonsYOffset = serializedObject.FindProperty("exitButtonsYOffset");
        placeholderSize = serializedObject.FindProperty("placeholderSize");
        placeholderFontSize = serializedObject.FindProperty("placeholderFontSize");
        resetNotifySize = serializedObject.FindProperty("resetNotifySize");
        resetNotifyFontSize = serializedObject.FindProperty("resetNotifyFontSize");
        resetNotifyYOffset = serializedObject.FindProperty("resetNotifyYOffset");
        resetNotifyDuration = serializedObject.FindProperty("resetNotifyDuration");

        // Тексты
        newGameButtonText = serializedObject.FindProperty("newGameButtonText");
        continueButtonText = serializedObject.FindProperty("continueButtonText");
        memoriesButtonText = serializedObject.FindProperty("memoriesButtonText");
        settingsButtonText = serializedObject.FindProperty("settingsButtonText");
        exitButtonText = serializedObject.FindProperty("exitButtonText");
        backButtonText = serializedObject.FindProperty("backButtonText");
        backToMenuText = serializedObject.FindProperty("backToMenuText");
        quitButtonText = serializedObject.FindProperty("quitButtonText");
        settingsTitleText = serializedObject.FindProperty("settingsTitleText");
        memoriesTitleText = serializedObject.FindProperty("memoriesTitleText");
        exitConfirmTitleText = serializedObject.FindProperty("exitConfirmTitleText");
        memoriesPlaceholderText = serializedObject.FindProperty("memoriesPlaceholderText");
        resetNotifyText = serializedObject.FindProperty("resetNotifyText");
        fullscreenLabel = serializedObject.FindProperty("fullscreenLabel");
        resolutionLabel = serializedObject.FindProperty("resolutionLabel");
        qualityLabel = serializedObject.FindProperty("qualityLabel");
        vsyncLabel = serializedObject.FindProperty("vsyncLabel");
        volumeLabel = serializedObject.FindProperty("volumeLabel");
        musicLabel = serializedObject.FindProperty("musicLabel");
        sfxLabel = serializedObject.FindProperty("sfxLabel");
        voiceLabel = serializedObject.FindProperty("voiceLabel");
        sensitivityLabel = serializedObject.FindProperty("sensitivityLabel");
        invertYLabel = serializedObject.FindProperty("invertYLabel");
        controlSchemeLabel = serializedObject.FindProperty("controlSchemeLabel");
        vibrationLabel = serializedObject.FindProperty("vibrationLabel");
        languageLabel = serializedObject.FindProperty("languageLabel");
        difficultyLabel = serializedObject.FindProperty("difficultyLabel");
        autosaveLabel = serializedObject.FindProperty("autosaveLabel");
        resetProgressLabel = serializedObject.FindProperty("resetProgressLabel");

        // Вкладки и опции
        tabNames = serializedObject.FindProperty("tabNames");
        resolutionOptions = serializedObject.FindProperty("resolutionOptions");
        qualityOptions = serializedObject.FindProperty("qualityOptions");
        controlOptions = serializedObject.FindProperty("controlOptions");
        languageOptions = serializedObject.FindProperty("languageOptions");
        difficultyOptions = serializedObject.FindProperty("difficultyOptions");

        // Анимация
        backgroundTransitionDuration = serializedObject.FindProperty("backgroundTransitionDuration");
        panelTransitionDuration = serializedObject.FindProperty("panelTransitionDuration");
        backgroundOffset = serializedObject.FindProperty("backgroundOffset");
        panelTransitionOffset = serializedObject.FindProperty("panelTransitionOffset");
        trixieFadeInDuration = serializedObject.FindProperty("trixieFadeInDuration");
        trixieStartDelay = serializedObject.FindProperty("trixieStartDelay");
        trixieLightFadeInDuration = serializedObject.FindProperty("trixieLightFadeInDuration");
        trixieDefaultLightIntensity = serializedObject.FindProperty("trixieDefaultLightIntensity");
        letterRevealDelay = serializedObject.FindProperty("letterRevealDelay");
        intensityMax = serializedObject.FindProperty("intensityMax");
        flickerMin = serializedObject.FindProperty("flickerMin");
        flickerMax = serializedObject.FindProperty("flickerMax");
        flickerSpeed = serializedObject.FindProperty("flickerSpeed");
        letterFadeDuration = serializedObject.FindProperty("letterFadeDuration");
        exitTransitionDuration = serializedObject.FindProperty("exitTransitionDuration");
        exitTitleStartY = serializedObject.FindProperty("exitTitleStartY");
        exitTitleEndY = serializedObject.FindProperty("exitTitleEndY");
        exitButtonsStartY = serializedObject.FindProperty("exitButtonsStartY");
        exitButtonsEndY = serializedObject.FindProperty("exitButtonsEndY");

        // Настройки игры
        volumeDefault = serializedObject.FindProperty("volumeDefault");
        musicDefault = serializedObject.FindProperty("musicDefault");
        sfxDefault = serializedObject.FindProperty("sfxDefault");
        voiceDefault = serializedObject.FindProperty("voiceDefault");
        sensitivityDefault = serializedObject.FindProperty("sensitivityDefault");
        vsyncDefault = serializedObject.FindProperty("vsyncDefault");
        vibrationDefault = serializedObject.FindProperty("vibrationDefault");
        autosaveDefault = serializedObject.FindProperty("autosaveDefault");

        // Служебное
        uiRootName = serializedObject.FindProperty("uiRootName");
        canvasName = serializedObject.FindProperty("canvasName");
        exitOverlayName = serializedObject.FindProperty("exitOverlayName");
        exitTitleName = serializedObject.FindProperty("exitTitleName");
        exitBackButtonName = serializedObject.FindProperty("exitBackButtonName");
        exitQuitButtonName = serializedObject.FindProperty("exitQuitButtonName");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("⚙️ КОНФИГУРАЦИЯ МЕНЮ UI", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // Сворачиваемые секции (основные)
        DrawPathsSection();
        DrawColorsSection();
        DrawSizesSection();
        DrawTextsSection();
        DrawAnimationSection();
        DrawGameSettingsSection();
        DrawServiceSection();

        // Разделитель
        EditorGUILayout.Space(10);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("━━━ ДОПОЛНИТЕЛЬНЫЕ НАСТРОЙКИ ━━━", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);

        // Несворачиваемые секции (внизу)
        DrawTabsSection();
        DrawOptionsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPathsSection()
    {
        showPaths = EditorGUILayout.BeginFoldoutHeaderGroup(showPaths, "📁 ПУТИ К ПРЕФАБАМ");
        if (showPaths)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Панели", EditorStyles.boldLabel);
            DrawProperty(panelPrefabPath, "Панель", "Путь к префабу обычной панели");
            DrawProperty(bigPanelPrefabPath, "Большая панель", "Путь к префабу большой панели");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Кнопки", EditorStyles.boldLabel);
            DrawProperty(buttonPrefabPath, "Кнопка", "Путь к префабу кнопки");
            DrawProperty(closeButtonPrefabPath, "Закрыть", "Путь к префабу кнопки закрытия");
            DrawProperty(backButtonPrefabPath, "Назад", "Путь к префабу кнопки назад");
            DrawProperty(trixieBackButtonPath, "Trixie Назад", "Путь к префабу кнопки назад в стиле Trixie");
            DrawProperty(trixieExitButtonPath, "Trixie Выход", "Путь к префабу кнопки выхода в стиле Trixie");
            DrawProperty(tabPrefabPath, "Вкладка", "Путь к префабу вкладки");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Элементы управления", EditorStyles.boldLabel);
            DrawProperty(sliderContainerPrefabPath, "Слайдер", "Путь к префабу контейнера слайдера");
            DrawProperty(togglePrefabPath, "Переключатель", "Путь к префабу переключателя");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Ресурсы", EditorStyles.boldLabel);
            DrawProperty(defaultFontPath, "Шрифт", "Путь к шрифту по умолчанию");
            DrawProperty(backgroundsFolder, "Фоны", "Папка с фонами");
            DrawProperty(memoriesBackgroundPath, "Фон воспоминаний", "Путь к фону панели воспоминаний");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawColorsSection()
    {
        showColors = EditorGUILayout.BeginFoldoutHeaderGroup(showColors, "🎨 ЦВЕТА");
        if (showColors)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Панели", EditorStyles.boldLabel);
            DrawProperty(panelBgColor, "Фон панели", "Цвет фона обычной панели");
            DrawProperty(settingsPanelColor, "Фон настроек", "Цвет фона панели настроек");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Кнопки", EditorStyles.boldLabel);
            DrawProperty(buttonNormalColor, "Обычная", "Цвет кнопки в обычном состоянии");
            DrawProperty(buttonHighlightedColor, "Подсвеченная", "Цвет кнопки при наведении");
            DrawProperty(buttonPressedColor, "Нажатая", "Цвет кнопки при нажатии");
            DrawProperty(buttonSelectedColor, "Выбранная", "Цвет выбранной кнопки");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Вкладки", EditorStyles.boldLabel);
            DrawProperty(tabNormalColor, "Обычная", "Цвет неактивной вкладки");
            DrawProperty(tabSelectedColor, "Выбранная", "Цвет активной вкладки");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Переключатели", EditorStyles.boldLabel);
            DrawProperty(toggleOnColor, "Включен", "Цвет переключателя во включенном состоянии");
            DrawProperty(toggleOffColor, "Выключен", "Цвет переключателя в выключенном состоянии");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Выпадающие списки", EditorStyles.boldLabel);
            DrawProperty(dropdownBgColor, "Фон", "Цвет фона выпадающего списка");
            DrawProperty(dropdownItemColor, "Элемент", "Цвет элемента списка");
            DrawProperty(dropdownItemHoverColor, "Элемент (наведение)", "Цвет элемента при наведении");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Прочее", EditorStyles.boldLabel);
            DrawProperty(overlayColor, "Затемнение", "Цвет затемнения фона");
            DrawProperty(placeholderTextColor, "Placeholder", "Цвет текста-заглушки");
            DrawProperty(markTextColor, "Метки слайдера", "Цвет меток на слайдере");
            DrawProperty(resetNotifyColor, "Уведомление о сбросе", "Цвет уведомления о сбросе прогресса");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawSizesSection()
    {
        showSizes = EditorGUILayout.BeginFoldoutHeaderGroup(showSizes, "📐 РАЗМЕРЫ И ПОЗИЦИИ");
        if (showSizes)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Панели", EditorStyles.boldLabel);
            DrawProperty(mainPanelSize, "Размер главной панели", "Размер главной панели меню");
            DrawProperty(memoriesPanelSize, "Размер панели воспоминаний", "Размер панели воспоминаний");
            DrawProperty(panelPadding, "Отступ панели", "Внутренний отступ панели");
            DrawProperty(rightOffset, "Отступ справа", "Отступ главной панели от правого края");
            DrawProperty(verticalOffset, "Вертикальный отступ", "Вертикальный отступ главной панели");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Кнопки", EditorStyles.boldLabel);
            DrawProperty(buttonSize, "Размер кнопки", "Размер обычной кнопки");
            DrawProperty(smallButtonSize, "Размер маленькой кнопки", "Размер маленькой кнопки");
            DrawProperty(exitButtonSize, "Размер кнопки выхода", "Размер кнопок в панели выхода");
            DrawProperty(buttonFontSize, "Размер шрифта кнопки", "Размер шрифта на кнопке");
            DrawProperty(smallButtonFontSize, "Размер шрифта (маленькая)", "Размер шрифта на маленькой кнопке");
            DrawProperty(buttonSpacing, "Отступ между кнопками", "Отступ между кнопками в главном меню");
            DrawProperty(exitButtonSpacing, "Отступ кнопок выхода", "Отступ между кнопками в панели выхода");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Вкладки", EditorStyles.boldLabel);
            DrawProperty(tabSize, "Размер вкладок", "Размер контейнера вкладок");
            DrawProperty(tabFontSize, "Размер шрифта вкладок", "Размер шрифта названий вкладок");
            DrawProperty(tabSpacing, "Отступ между вкладками", "Отступ между вкладками");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Заголовки", EditorStyles.boldLabel);
            DrawProperty(titleFontSize, "Размер шрифта заголовка", "Размер шрифта заголовка панели");
            DrawProperty(titleSize, "Размер заголовка", "Размер заголовка панели");
            DrawProperty(titleYOffset, "Отступ заголовка", "Вертикальный отступ заголовка");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Слайдеры", EditorStyles.boldLabel);
            DrawProperty(sliderSize, "Размер слайдера", "Размер контейнера слайдера");
            DrawProperty(sliderFontSize, "Размер шрифта слайдера", "Размер шрифта значений слайдера");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Переключатели", EditorStyles.boldLabel);
            DrawProperty(toggleSize, "Размер переключателя", "Размер переключателя");
            DrawProperty(toggleHandleOffset, "Смещение ручки", "Смещение ручки переключателя");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Выпадающие списки", EditorStyles.boldLabel);
            DrawProperty(dropdownSize, "Размер списка", "Размер выпадающего списка");
            DrawProperty(dropdownFontSize, "Размер шрифта списка", "Размер шрифта в выпадающем списке");
            DrawProperty(dropdownItemHeight, "Высота элемента списка", "Высота элемента в выпадающем списке");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Метки", EditorStyles.boldLabel);
            DrawProperty(labelSize, "Размер метки", "Размер метки настройки");
            DrawProperty(labelFontSize, "Размер шрифта метки", "Размер шрифта метки настройки");
            DrawProperty(labelXOffset, "Смещение метки", "Смещение метки от центра");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Панель выхода", EditorStyles.boldLabel);
            DrawProperty(exitPanelSize, "Размер панели", "Размер панели выхода");
            DrawProperty(exitTitleYOffset, "Отступ заголовка", "Вертикальный отступ заголовка панели выхода");
            DrawProperty(exitButtonsYOffset, "Отступ кнопок", "Вертикальный отступ кнопок панели выхода");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Placeholder", EditorStyles.boldLabel);
            DrawProperty(placeholderSize, "Размер placeholder", "Размер текста-заглушки");
            DrawProperty(placeholderFontSize, "Размер шрифта placeholder", "Размер шрифта текста-заглушки");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Уведомление о сбросе", EditorStyles.boldLabel);
            DrawProperty(resetNotifySize, "Размер уведомления", "Размер уведомления о сбросе прогресса");
            DrawProperty(resetNotifyFontSize, "Размер шрифта уведомления", "Размер шрифта уведомления о сбросе");
            DrawProperty(resetNotifyYOffset, "Отступ уведомления", "Вертикальный отступ уведомления");
            DrawProperty(resetNotifyDuration, "Длительность уведомления", "Длительность показа уведомления (сек)");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawTextsSection()
    {
        showTexts = EditorGUILayout.BeginFoldoutHeaderGroup(showTexts, "🔔 ТЕКСТЫ");
        if (showTexts)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Кнопки", EditorStyles.boldLabel);
            DrawProperty(newGameButtonText, "Новая игра", "Текст кнопки новой игры");
            DrawProperty(continueButtonText, "Продолжить", "Текст кнопки продолжения");
            DrawProperty(memoriesButtonText, "Воспоминания", "Текст кнопки воспоминаний");
            DrawProperty(settingsButtonText, "Настройки", "Текст кнопки настроек");
            DrawProperty(exitButtonText, "Выход", "Текст кнопки выхода");
            DrawProperty(backButtonText, "Назад", "Текст кнопки назад");
            DrawProperty(backToMenuText, "Вернуться", "Текст кнопки возврата в меню");
            DrawProperty(quitButtonText, "Выйти", "Текст кнопки выхода из игры");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Заголовки", EditorStyles.boldLabel);
            DrawProperty(settingsTitleText, "Настройки", "Заголовок панели настроек");
            DrawProperty(memoriesTitleText, "Воспоминания", "Заголовок панели воспоминаний");
            DrawProperty(exitConfirmTitleText, "Подтверждение выхода", "Заголовок панели подтверждения выхода");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Placeholder", EditorStyles.boldLabel);
            DrawProperty(memoriesPlaceholderText, "Воспоминания", "Текст-заглушка на панели воспоминаний");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Уведомления", EditorStyles.boldLabel);
            DrawProperty(resetNotifyText, "Сброс прогресса", "Текст уведомления о сбросе прогресса");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Настройки (лейблы)", EditorStyles.boldLabel);
            DrawProperty(fullscreenLabel, "Полноэкранный режим", "Подпись переключателя полноэкранного режима");
            DrawProperty(resolutionLabel, "Разрешение", "Подпись выпадающего списка разрешений");
            DrawProperty(qualityLabel, "Качество", "Подпись выпадающего списка качества");
            DrawProperty(vsyncLabel, "VSync", "Подпись переключателя VSync");
            DrawProperty(volumeLabel, "Громкость", "Подпись слайдера громкости");
            DrawProperty(musicLabel, "Музыка", "Подпись слайдера музыки");
            DrawProperty(sfxLabel, "Звуки", "Подпись слайдера звуков");
            DrawProperty(voiceLabel, "Голос", "Подпись слайдера голоса");
            DrawProperty(sensitivityLabel, "Чувствительность", "Подпись слайдера чувствительности");
            DrawProperty(invertYLabel, "Инвертировать Y", "Подпись переключателя инверсии Y");
            DrawProperty(controlSchemeLabel, "Схема управления", "Подпись выпадающего списка схем управления");
            DrawProperty(vibrationLabel, "Вибрация", "Подпись переключателя вибрации");
            DrawProperty(languageLabel, "Язык", "Подпись выпадающего списка языков");
            DrawProperty(difficultyLabel, "Сложность", "Подпись выпадающего списка сложности");
            DrawProperty(autosaveLabel, "Автосохранение", "Подпись переключателя автосохранения");
            DrawProperty(resetProgressLabel, "Сброс прогресса", "Текст кнопки сброса прогресса");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawTabsSection()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("📑 НАЗВАНИЯ ВКЛАДОК", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Названия вкладок в панели настроек", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(tabNames, new GUIContent(" "), true);
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawOptionsSection()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("📋 ОПЦИИ ДЛЯ ВЫПАДАЮЩИХ СПИСКОВ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);

        EditorGUILayout.LabelField("Разрешения", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(resolutionOptions, new GUIContent(" "), true);

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Качество", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(qualityOptions, new GUIContent(" "), true);

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Схемы управления", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(controlOptions, new GUIContent(" "), true);

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Языки", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(languageOptions, new GUIContent(" "), true);

        EditorGUILayout.Space(3);
        EditorGUILayout.LabelField("Сложность", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(difficultyOptions, new GUIContent(" "), true);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(5);
    }

    private void DrawAnimationSection()
    {
        showAnimation = EditorGUILayout.BeginFoldoutHeaderGroup(showAnimation, "⚙️ НАСТРОЙКИ АНИМАЦИИ");
        if (showAnimation)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Переходы", EditorStyles.boldLabel);
            DrawProperty(backgroundTransitionDuration, "Длительность фона", "Длительность анимации перехода фона (сек)");
            DrawProperty(panelTransitionDuration, "Длительность панели", "Длительность анимации перехода панели (сек)");
            DrawProperty(backgroundOffset, "Смещение фона", "Смещение фона при переходе");
            DrawProperty(panelTransitionOffset, "Смещение панели", "Смещение панели при переходе");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Trixie", EditorStyles.boldLabel);
            DrawProperty(trixieFadeInDuration, "Появление", "Длительность появления Trixie (сек)");
            DrawProperty(trixieStartDelay, "Задержка старта", "Задержка перед появлением Trixie (сек)");
            DrawProperty(trixieLightFadeInDuration, "Появление света", "Длительность появления света Trixie (сек)");
            DrawProperty(trixieDefaultLightIntensity, "Интенсивность света", "Интенсивность света Trixie по умолчанию");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Finding Oneself", EditorStyles.boldLabel);
            DrawProperty(letterRevealDelay, "Задержка букв", "Задержка между появлением букв (сек)");
            DrawProperty(intensityMax, "Макс. интенсивность", "Максимальная интенсивность света букв");
            DrawProperty(flickerMin, "Мерцание (мин)", "Минимальная интенсивность мерцания");
            DrawProperty(flickerMax, "Мерцание (макс)", "Максимальная интенсивность мерцания");
            DrawProperty(flickerSpeed, "Скорость мерцания", "Скорость изменения интенсивности мерцания");
            DrawProperty(letterFadeDuration, "Появление буквы", "Длительность появления одной буквы (сек)");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Панель выхода", EditorStyles.boldLabel);
            DrawProperty(exitTransitionDuration, "Длительность перехода", "Длительность анимации панели выхода (сек)");
            DrawProperty(exitTitleStartY, "Старт заголовка", "Начальная позиция заголовка по Y");
            DrawProperty(exitTitleEndY, "Финиш заголовка", "Конечная позиция заголовка по Y");
            DrawProperty(exitButtonsStartY, "Старт кнопок", "Начальная позиция кнопок по Y");
            DrawProperty(exitButtonsEndY, "Финиш кнопок", "Конечная позиция кнопок по Y");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawGameSettingsSection()
    {
        showGameSettings = EditorGUILayout.BeginFoldoutHeaderGroup(showGameSettings, "🎮 НАСТРОЙКИ ИГРЫ");
        if (showGameSettings)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(volumeDefault, "Громкость", "Громкость звука по умолчанию");
            DrawProperty(musicDefault, "Музыка", "Громкость музыки по умолчанию");
            DrawProperty(sfxDefault, "Звуки", "Громкость звуков по умолчанию");
            DrawProperty(voiceDefault, "Голос", "Громкость голоса по умолчанию");
            DrawProperty(sensitivityDefault, "Чувствительность", "Чувствительность управления по умолчанию");
            DrawProperty(vsyncDefault, "VSync", "VSync включен по умолчанию");
            DrawProperty(vibrationDefault, "Вибрация", "Вибрация включена по умолчанию");
            DrawProperty(autosaveDefault, "Автосохранение", "Автосохранение включено по умолчанию");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawServiceSection()
    {
        showService = EditorGUILayout.BeginFoldoutHeaderGroup(showService, "🔧 СЛУЖЕБНОЕ");
        if (showService)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(uiRootName, "Имя UIRoot", "Имя корневого объекта UI");
            DrawProperty(canvasName, "Имя Canvas", "Имя объекта Canvas");
            DrawProperty(exitOverlayName, "Имя затемнения", "Имя объекта затемнения панели выхода");
            DrawProperty(exitTitleName, "Имя заголовка выхода", "Имя объекта заголовка панели выхода");
            DrawProperty(exitBackButtonName, "Имя кнопки назад", "Имя объекта кнопки возврата");
            DrawProperty(exitQuitButtonName, "Имя кнопки выхода", "Имя объекта кнопки выхода");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawProperty(SerializedProperty prop, string label, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(180));
        EditorGUILayout.PropertyField(prop, GUIContent.none);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }
}