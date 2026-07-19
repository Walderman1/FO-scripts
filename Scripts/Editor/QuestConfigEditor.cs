// QuestConfigEditor.cs - КАСТОМНЫЙ РЕДАКТОР ДЛЯ КОНФИГА КВЕСТОВ
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(QuestConfigSO))]
public class QuestConfigEditor : Editor
{
    private QuestConfigSO config;
    private bool showPaths = false;
    private bool showColors = false;
    private bool showSizes = false;
    private bool showNotifications = false;
    private bool showIcons = false;
    private bool showTexts = false;
    private bool showTabs = false;
    private bool showOther = false;

    private SerializedProperty questItemPrefabPath;
    private SerializedProperty objectivePrefabPath;
    private SerializedProperty notificationPrefabPath;
    private SerializedProperty slotPrefabPath;
    private SerializedProperty questsFolder;
    private SerializedProperty notificationsTag;
    private SerializedProperty inventoryItemsPath;

    private SerializedProperty activeColor;
    private SerializedProperty completedColor;
    private SerializedProperty failedColor;
    private SerializedProperty trackingColor;
    private SerializedProperty startNotifyColor;
    private SerializedProperty updateNotifyColor;
    private SerializedProperty completeNotifyColor;
    private SerializedProperty objCompleteColor;
    private SerializedProperty objIncompleteColor;
    private SerializedProperty objFailedColor;
    private SerializedProperty detailBgColor;
    private SerializedProperty activeTabColor;
    private SerializedProperty inactiveTabColor;
    private SerializedProperty failedTabColor;

    private SerializedProperty fadeDuration;
    private SerializedProperty questItemHeight;
    private SerializedProperty fallbackItemHeight;
    private SerializedProperty slotSize;
    private SerializedProperty slotSpacing;
    private SerializedProperty titleFontSize;
    private SerializedProperty progressFontSize;
    private SerializedProperty emptyMsgFontSize;
    private SerializedProperty emptyMsgHeight;
    private SerializedProperty objectiveHeight;
    private SerializedProperty detailOffsetMin;
    private SerializedProperty detailOffsetMax;

    private SerializedProperty notifyDuration;
    private SerializedProperty notifyFade;
    private SerializedProperty notifySlide;
    private SerializedProperty maxNotifications;
    private SerializedProperty notifySpacing;
    private SerializedProperty notifyHeight;
    private SerializedProperty notifyBgAlpha;
    private SerializedProperty notifySlideOffset;

    private SerializedProperty startIcon;
    private SerializedProperty completeIcon;
    private SerializedProperty updateIcon;
    private SerializedProperty objectiveIcon;
    private SerializedProperty iconSize;

    private SerializedProperty trackBtnText;
    private SerializedProperty untrackBtnText;
    private SerializedProperty cancelBtnText;
    private SerializedProperty failedQuestText;
    private SerializedProperty questCompleteText;
    private SerializedProperty emptyActiveText;
    private SerializedProperty emptyCompletedText;
    private SerializedProperty emptyFailedText;
    private SerializedProperty progressDetailFormat;
    private SerializedProperty objectiveProgressFormat;

    private SerializedProperty activeTabName;
    private SerializedProperty completedTabName;
    private SerializedProperty failedTabName;

    private SerializedProperty enableLogging;
    private SerializedProperty autoLoadQuests;

    private void OnEnable()
    {
        config = (QuestConfigSO)target;

        // Пути
        questItemPrefabPath = serializedObject.FindProperty("questItemPrefabPath");
        objectivePrefabPath = serializedObject.FindProperty("objectivePrefabPath");
        notificationPrefabPath = serializedObject.FindProperty("notificationPrefabPath");
        slotPrefabPath = serializedObject.FindProperty("slotPrefabPath");
        questsFolder = serializedObject.FindProperty("questsFolder");
        notificationsTag = serializedObject.FindProperty("notificationsTag");
        inventoryItemsPath = serializedObject.FindProperty("inventoryItemsPath");

        // Цвета
        activeColor = serializedObject.FindProperty("activeColor");
        completedColor = serializedObject.FindProperty("completedColor");
        failedColor = serializedObject.FindProperty("failedColor");
        trackingColor = serializedObject.FindProperty("trackingColor");
        startNotifyColor = serializedObject.FindProperty("startNotifyColor");
        updateNotifyColor = serializedObject.FindProperty("updateNotifyColor");
        completeNotifyColor = serializedObject.FindProperty("completeNotifyColor");
        objCompleteColor = serializedObject.FindProperty("objCompleteColor");
        objIncompleteColor = serializedObject.FindProperty("objIncompleteColor");
        objFailedColor = serializedObject.FindProperty("objFailedColor");
        detailBgColor = serializedObject.FindProperty("detailBgColor");
        activeTabColor = serializedObject.FindProperty("activeTabColor");
        inactiveTabColor = serializedObject.FindProperty("inactiveTabColor");
        failedTabColor = serializedObject.FindProperty("failedTabColor");

        // Размеры
        fadeDuration = serializedObject.FindProperty("fadeDuration");
        questItemHeight = serializedObject.FindProperty("questItemHeight");
        fallbackItemHeight = serializedObject.FindProperty("fallbackItemHeight");
        slotSize = serializedObject.FindProperty("slotSize");
        slotSpacing = serializedObject.FindProperty("slotSpacing");
        titleFontSize = serializedObject.FindProperty("titleFontSize");
        progressFontSize = serializedObject.FindProperty("progressFontSize");
        emptyMsgFontSize = serializedObject.FindProperty("emptyMsgFontSize");
        emptyMsgHeight = serializedObject.FindProperty("emptyMsgHeight");
        objectiveHeight = serializedObject.FindProperty("objectiveHeight");
        detailOffsetMin = serializedObject.FindProperty("detailOffsetMin");
        detailOffsetMax = serializedObject.FindProperty("detailOffsetMax");

        // Уведомления
        notifyDuration = serializedObject.FindProperty("notifyDuration");
        notifyFade = serializedObject.FindProperty("notifyFade");
        notifySlide = serializedObject.FindProperty("notifySlide");
        maxNotifications = serializedObject.FindProperty("maxNotifications");
        notifySpacing = serializedObject.FindProperty("notifySpacing");
        notifyHeight = serializedObject.FindProperty("notifyHeight");
        notifyBgAlpha = serializedObject.FindProperty("notifyBgAlpha");
        notifySlideOffset = serializedObject.FindProperty("notifySlideOffset");

        // Иконки
        startIcon = serializedObject.FindProperty("startIcon");
        completeIcon = serializedObject.FindProperty("completeIcon");
        updateIcon = serializedObject.FindProperty("updateIcon");
        objectiveIcon = serializedObject.FindProperty("objectiveIcon");
        iconSize = serializedObject.FindProperty("iconSize");

        // Тексты
        trackBtnText = serializedObject.FindProperty("trackBtnText");
        untrackBtnText = serializedObject.FindProperty("untrackBtnText");
        cancelBtnText = serializedObject.FindProperty("cancelBtnText");
        failedQuestText = serializedObject.FindProperty("failedQuestText");
        questCompleteText = serializedObject.FindProperty("questCompleteText");
        emptyActiveText = serializedObject.FindProperty("emptyActiveText");
        emptyCompletedText = serializedObject.FindProperty("emptyCompletedText");
        emptyFailedText = serializedObject.FindProperty("emptyFailedText");
        progressDetailFormat = serializedObject.FindProperty("progressDetailFormat");
        objectiveProgressFormat = serializedObject.FindProperty("objectiveProgressFormat");

        // Вкладки
        activeTabName = serializedObject.FindProperty("activeTabName");
        completedTabName = serializedObject.FindProperty("completedTabName");
        failedTabName = serializedObject.FindProperty("failedTabName");

        // Прочее
        enableLogging = serializedObject.FindProperty("enableLogging");
        autoLoadQuests = serializedObject.FindProperty("autoLoadQuests");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);

        // Заголовок
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("⚙️ КОНФИГУРАЦИЯ КВЕСТОВОЙ СИСТЕМЫ", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        DrawPathsSection();
        DrawColorsSection();
        DrawSizesSection();
        DrawNotificationsSection();
        DrawIconsSection();
        DrawTextsSection();
        DrawTabsSection();
        DrawOtherSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPathsSection()
    {
        showPaths = EditorGUILayout.BeginFoldoutHeaderGroup(showPaths, "📁 ПУТИ К ПРЕФАБАМ");
        if (showPaths)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Элементы интерфейса", EditorStyles.boldLabel);
            DrawProperty(questItemPrefabPath, "Элемент квеста в списке", "Путь к префабу элемента квеста в списке");
            DrawProperty(objectivePrefabPath, "Элемент цели", "Путь к префабу цели квеста");
            DrawProperty(notificationPrefabPath, "Уведомление", "Путь к префабу уведомления");
            DrawProperty(slotPrefabPath, "Слот предмета", "Путь к префабу слота для предметов");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Папки", EditorStyles.boldLabel);
            DrawProperty(questsFolder, "Папка с квестами", "Папка в Resources где хранятся квесты");
            DrawProperty(notificationsTag, "Тег уведомлений", "Тег объекта-контейнера для уведомлений");
            DrawProperty(inventoryItemsPath, "Папка с предметами", "Папка в Resources где хранятся предметы инвентаря");

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

            EditorGUILayout.LabelField("Квесты", EditorStyles.boldLabel);
            DrawProperty(activeColor, "Активный", "Цвет для активных квестов (синий)");
            DrawProperty(completedColor, "Выполненный", "Цвет для выполненных квестов (зеленый)");
            DrawProperty(failedColor, "Проваленный", "Цвет для проваленных квестов (красный)");
            DrawProperty(trackingColor, "Отслеживаемый", "Цвет для отслеживаемого квеста (ярко-синий)");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Уведомления", EditorStyles.boldLabel);
            DrawProperty(startNotifyColor, "Старт", "Цвет уведомления о старте квеста");
            DrawProperty(updateNotifyColor, "Обновление", "Цвет уведомления об обновлении квеста");
            DrawProperty(completeNotifyColor, "Завершение", "Цвет уведомления о завершении квеста");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Цели", EditorStyles.boldLabel);
            DrawProperty(objCompleteColor, "Выполнена", "Цвет выполненной цели");
            DrawProperty(objIncompleteColor, "Не выполнена", "Цвет невыполненной цели");
            DrawProperty(objFailedColor, "Провалена", "Цвет проваленной цели");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Интерфейс", EditorStyles.boldLabel);
            DrawProperty(detailBgColor, "Фон деталей", "Цвет фона панели деталей квеста");
            DrawProperty(activeTabColor, "Активная вкладка", "Цвет активной вкладки");
            DrawProperty(inactiveTabColor, "Неактивная вкладка", "Цвет неактивной вкладки");
            DrawProperty(failedTabColor, "Вкладка проваленных", "Цвет вкладки проваленных квестов");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawSizesSection()
    {
        showSizes = EditorGUILayout.BeginFoldoutHeaderGroup(showSizes, "📐 РАЗМЕРЫ UI");
        if (showSizes)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Панель квестов", EditorStyles.boldLabel);
            DrawProperty(fadeDuration, "Длительность появления", "Длительность появления/исчезновения панели (сек)");
            DrawProperty(questItemHeight, "Высота элемента квеста", "Высота элемента квеста в списке");
            DrawProperty(fallbackItemHeight, "Высота (запасной вариант)", "Высота элемента квеста если префаб не найден");
            DrawProperty(titleFontSize, "Размер шрифта заголовка", "Размер шрифта для заголовка квеста");
            DrawProperty(progressFontSize, "Размер шрифта прогресса", "Размер шрифта для отображения прогресса");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Слоты предметов", EditorStyles.boldLabel);
            DrawProperty(slotSize, "Размер слота", "Размер слота для предмета");
            DrawProperty(slotSpacing, "Отступ между слотами", "Отступ между слотами");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Пустые сообщения", EditorStyles.boldLabel);
            DrawProperty(emptyMsgFontSize, "Размер шрифта", "Размер шрифта для пустого сообщения");
            DrawProperty(emptyMsgHeight, "Высота", "Высота элемента пустого сообщения");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Цели", EditorStyles.boldLabel);
            DrawProperty(objectiveHeight, "Высота цели", "Высота цели квеста (запасной вариант)");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Панель деталей", EditorStyles.boldLabel);
            DrawProperty(detailOffsetMin, "Отступ (мин)", "Минимальные отступы для панели деталей");
            DrawProperty(detailOffsetMax, "Отступ (макс)", "Максимальные отступы для панели деталей");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawNotificationsSection()
    {
        showNotifications = EditorGUILayout.BeginFoldoutHeaderGroup(showNotifications, "🔔 УВЕДОМЛЕНИЯ");
        if (showNotifications)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Время", EditorStyles.boldLabel);
            DrawProperty(notifyDuration, "Время отображения", "Время отображения уведомления (сек)");
            DrawProperty(notifyFade, "Время затухания", "Время затухания уведомления (сек)");
            DrawProperty(notifySlide, "Время появления", "Время появления уведомления (сек)");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Расположение", EditorStyles.boldLabel);
            DrawProperty(maxNotifications, "Максимум", "Максимальное количество уведомлений на экране");
            DrawProperty(notifySpacing, "Отступ", "Отступ между уведомлениями");
            DrawProperty(notifyHeight, "Высота", "Высота уведомления");
            DrawProperty(notifyBgAlpha, "Прозрачность фона", "Прозрачность фона уведомления");
            DrawProperty(notifySlideOffset, "Смещение при появлении", "Смещение уведомления при появлении");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawIconsSection()
    {
        showIcons = EditorGUILayout.BeginFoldoutHeaderGroup(showIcons, "🖼️ ИКОНКИ");
        if (showIcons)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(startIcon, "Старт", "Иконка для старта квеста");
            DrawProperty(completeIcon, "Завершение", "Иконка для завершения квеста");
            DrawProperty(updateIcon, "Обновление", "Иконка для обновления квеста");
            DrawProperty(objectiveIcon, "Цель", "Иконка для выполнения цели");
            DrawProperty(iconSize, "Размер иконки", "Размер иконки в уведомлении");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawTextsSection()
    {
        showTexts = EditorGUILayout.BeginFoldoutHeaderGroup(showTexts, "📝 ТЕКСТЫ");
        if (showTexts)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Кнопки", EditorStyles.boldLabel);
            DrawProperty(trackBtnText, "Отслеживать", "Текст для кнопки отслеживания");
            DrawProperty(untrackBtnText, "Отменить отслеживание", "Текст для кнопки отмены отслеживания");
            DrawProperty(cancelBtnText, "Отменить квест", "Текст для кнопки отмены квеста");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Сообщения", EditorStyles.boldLabel);
            DrawProperty(failedQuestText, "Провален", "Текст для проваленного квеста");
            DrawProperty(questCompleteText, "Завершен", "Текст для завершенного квеста в HUD");
            DrawProperty(emptyActiveText, "Нет активных", "Текст для пустого списка активных квестов");
            DrawProperty(emptyCompletedText, "Нет выполненных", "Текст для пустого списка выполненных квестов");
            DrawProperty(emptyFailedText, "Нет проваленных", "Текст для пустого списка проваленных квестов");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Форматы", EditorStyles.boldLabel);
            DrawProperty(progressDetailFormat, "Прогресс в деталях", "Формат прогресса в деталях квеста");
            DrawProperty(objectiveProgressFormat, "Прогресс цели", "Формат прогресса цели квеста");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawTabsSection()
    {
        showTabs = EditorGUILayout.BeginFoldoutHeaderGroup(showTabs, "📑 ВКЛАДКИ");
        if (showTabs)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(activeTabName, "Активные", "Название вкладки активных квестов");
            DrawProperty(completedTabName, "Выполненные", "Название вкладки выполненных квестов");
            DrawProperty(failedTabName, "Проваленные", "Название вкладки проваленных квестов");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawOtherSection()
    {
        showOther = EditorGUILayout.BeginFoldoutHeaderGroup(showOther, "⚙️ ПРОЧЕЕ");
        if (showOther)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(enableLogging, "Включить логи", "Включить логи квестовой системы");
            DrawProperty(autoLoadQuests, "Автозагрузка квестов", "Автоматически загружать квесты при старте");

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