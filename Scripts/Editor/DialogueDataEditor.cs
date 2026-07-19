// DialogueDataEditor.cs - КАСТОМНЫЙ РЕДАКТОР ДЛЯ КОНФИГА ДИАЛОГОВ
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    private DialogueData config;

    private bool showTags = false;
    private bool showMarkers = false;
    private bool showCharacters = false;
    private bool showSkip = false;
    private bool showBehavior = false;
    private bool showDebug = false;

    private SerializedProperty filePaths;
    private SerializedProperty dialoguePanelTag;
    private SerializedProperty choicePanelTag;
    private SerializedProperty speakerNamePanel;
    private SerializedProperty blackPanelTag;
    private SerializedProperty panelDialogueTag;
    private SerializedProperty firstCharactersTag;
    private SerializedProperty secondCharactersTag;
    private SerializedProperty characterTag;
    private SerializedProperty worldTag;
    private SerializedProperty locationReferenceTag;
    private SerializedProperty buttonPanelName;
    private SerializedProperty skipButtonName;
    private SerializedProperty markerChoice;
    private SerializedProperty markerFinish;
    private SerializedProperty markerContinue;
    private SerializedProperty markerEnd;
    private SerializedProperty dialogueSeparator;
    private SerializedProperty charactersFolder;
    private SerializedProperty charactersParentName;
    private SerializedProperty skipDelay;
    private SerializedProperty maxSkipIterations;
    private SerializedProperty skipButtonText;
    private SerializedProperty skipButtonTextActive;
    private SerializedProperty deleteReadLines;
    private SerializedProperty autoShowSkipButton;
    private SerializedProperty enableDebugLogs;
    private SerializedProperty logLoadedLines;

    private void OnEnable()
    {
        config = (DialogueData)target;

        // Пути к файлам
        filePaths = serializedObject.FindProperty("filePaths");

        // Теги и имена - Панели
        dialoguePanelTag = serializedObject.FindProperty("dialoguePanelTag");
        choicePanelTag = serializedObject.FindProperty("choicePanelTag");
        speakerNamePanel = serializedObject.FindProperty("speakerNamePanel");
        blackPanelTag = serializedObject.FindProperty("blackPanelTag");
        panelDialogueTag = serializedObject.FindProperty("panelDialogueTag");

        // Теги и имена - Персонажи
        firstCharactersTag = serializedObject.FindProperty("firstCharactersTag");
        secondCharactersTag = serializedObject.FindProperty("secondCharactersTag");
        characterTag = serializedObject.FindProperty("characterTag");

        // Теги и имена - Локации
        worldTag = serializedObject.FindProperty("worldTag");
        locationReferenceTag = serializedObject.FindProperty("locationReferenceTag");

        // Теги и имена - Интерфейс
        buttonPanelName = serializedObject.FindProperty("buttonPanelName");
        skipButtonName = serializedObject.FindProperty("skipButtonName");

        // Маркеры
        markerChoice = serializedObject.FindProperty("markerChoice");
        markerFinish = serializedObject.FindProperty("markerFinish");
        markerContinue = serializedObject.FindProperty("markerContinue");
        markerEnd = serializedObject.FindProperty("markerEnd");
        dialogueSeparator = serializedObject.FindProperty("dialogueSeparator");

        // Персонажи
        charactersFolder = serializedObject.FindProperty("charactersFolder");
        charactersParentName = serializedObject.FindProperty("charactersParentName");

        // Пропуск
        skipDelay = serializedObject.FindProperty("skipDelay");
        maxSkipIterations = serializedObject.FindProperty("maxSkipIterations");
        skipButtonText = serializedObject.FindProperty("skipButtonText");
        skipButtonTextActive = serializedObject.FindProperty("skipButtonTextActive");

        // Поведение
        deleteReadLines = serializedObject.FindProperty("deleteReadLines");
        autoShowSkipButton = serializedObject.FindProperty("autoShowSkipButton");

        // Отладка
        enableDebugLogs = serializedObject.FindProperty("enableDebugLogs");
        logLoadedLines = serializedObject.FindProperty("logLoadedLines");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.Space(10);

        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUILayout.Label("💬 НАСТРОЙКИ ДИАЛОГОВОЙ СИСТЕМЫ", EditorStyles.boldLabel);
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(5);

        // ВСЕ СЕКЦИИ КРОМЕ ПУТЕЙ
        DrawTagsSection();
        DrawMarkersSection();
        DrawCharactersSection();
        DrawSkipSection();
        DrawBehaviorSection();
        DrawDebugSection();

        EditorGUILayout.Space(10);

        // Разделительная линия
        Rect rect = EditorGUILayout.GetControlRect(false, 1);
        rect.height = 1;
        EditorGUI.DrawRect(rect, new Color(0.5f, 0.5f, 0.5f, 0.5f));
        EditorGUILayout.Space(5);

        // СЕКЦИЯ ПУТЕЙ В САМОМ НИЗУ (всегда видима)
        DrawPathsSection();

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawPathsSection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("📁 ПУТИ К ФАЙЛАМ", EditorStyles.boldLabel);
        EditorGUILayout.Space(3);

        EditorGUILayout.LabelField("Файлы диалогов", EditorStyles.miniLabel);
        EditorGUILayout.PropertyField(filePaths, GUIContent.none);

        EditorGUILayout.Space(3);

        if (GUILayout.Button("Открыть папку с текстами", GUILayout.Height(25)))
        {
#if UNITY_EDITOR
            string path = Application.dataPath + "/Resources/Texts/";
            if (System.IO.Directory.Exists(path))
            {
                EditorUtility.RevealInFinder(path);
            }
            else
            {
                Debug.LogWarning($"Папка не найдена: {path}");
            }
#endif
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawTagsSection()
    {
        showTags = EditorGUILayout.BeginFoldoutHeaderGroup(showTags, "🏷️ ТЕГИ И ИМЕНА");
        if (showTags)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.LabelField("Панели", EditorStyles.boldLabel);
            DrawProperty(dialoguePanelTag, "Панель диалога", "Тег панели диалога");
            DrawProperty(choicePanelTag, "Панель выбора", "Тег панели выбора");
            DrawProperty(speakerNamePanel, "Панель говорящего", "Тег панели с именем говорящего");
            DrawProperty(blackPanelTag, "Черная панель", "Тег черной панели для переходов");
            DrawProperty(panelDialogueTag, "Панель диалога (CanvasGroup)", "Тег панели диалога для CanvasGroup");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Персонажи", EditorStyles.boldLabel);
            DrawProperty(firstCharactersTag, "Первая область персонажей", "Тег первой области с персонажами");
            DrawProperty(secondCharactersTag, "Вторая область персонажей", "Тег второй области с персонажами");
            DrawProperty(characterTag, "Тег персонажа", "Тег объекта персонажа");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Локации", EditorStyles.boldLabel);
            DrawProperty(worldTag, "Мир", "Тег корневого объекта мира");
            DrawProperty(locationReferenceTag, "Ссылка на локацию", "Тег ссылки на локацию");

            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("Интерфейс", EditorStyles.boldLabel);
            DrawProperty(buttonPanelName, "Панель кнопок", "Имя панели с кнопками");
            DrawProperty(skipButtonName, "Кнопка пропуска", "Имя кнопки пропуска");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawMarkersSection()
    {
        showMarkers = EditorGUILayout.BeginFoldoutHeaderGroup(showMarkers, "📌 МАРКЕРЫ ДИАЛОГА");
        if (showMarkers)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(markerChoice, "Маркер выбора", "Маркер начала списка выбора");
            DrawProperty(markerFinish, "Маркер завершения", "Маркер завершения диалога");
            DrawProperty(markerContinue, "Маркер продолжения", "Маркер перехода к следующему блоку");
            DrawProperty(markerEnd, "Маркер конца", "Маркер конца диалога");
            DrawProperty(dialogueSeparator, "Разделитель", "Символ разделения строки диалога");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawCharactersSection()
    {
        showCharacters = EditorGUILayout.BeginFoldoutHeaderGroup(showCharacters, "👤 ПЕРСОНАЖИ");
        if (showCharacters)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(charactersFolder, "Папка персонажей", "Имя папки с персонажами в Resources");
            DrawProperty(charactersParentName, "Родительский объект", "Имя родительского объекта для персонажей");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawSkipSection()
    {
        showSkip = EditorGUILayout.BeginFoldoutHeaderGroup(showSkip, "⏭️ ПРОПУСК ДИАЛОГА");
        if (showSkip)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(skipDelay, "Задержка", "Задержка между пропускаемыми строками (сек)");
            DrawProperty(maxSkipIterations, "Макс. итераций", "Максимальное количество пропускаемых строк");
            DrawProperty(skipButtonText, "Текст кнопки", "Текст на кнопке пропуска");
            DrawProperty(skipButtonTextActive, "Текст при пропуске", "Текст на кнопке пропуска во время пропуска");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawBehaviorSection()
    {
        showBehavior = EditorGUILayout.BeginFoldoutHeaderGroup(showBehavior, "⚙️ ПОВЕДЕНИЕ");
        if (showBehavior)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(deleteReadLines, "Удалять прочитанные", "Удалять ли строки после прочтения");
            DrawProperty(autoShowSkipButton, "Показывать кнопку", "Автоматически показывать кнопку пропуска");

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawDebugSection()
    {
        showDebug = EditorGUILayout.BeginFoldoutHeaderGroup(showDebug, "🐞 ОТЛАДКА");
        if (showDebug)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            DrawProperty(enableDebugLogs, "Включить логи", "Включить детальное логирование");
            DrawProperty(logLoadedLines, "Логировать строки", "Показывать загруженные строки в консоли");

            EditorGUILayout.Space(5);

            if (GUILayout.Button("Сбросить к значениям по умолчанию", GUILayout.Height(30)))
            {
                config.ResetToDefaults();
                EditorUtility.SetDirty(config);
                Debug.Log("Настройки диалогов сброшены к значениям по умолчанию");
            }

            EditorGUILayout.EndVertical();
        }
        EditorGUILayout.EndFoldoutHeaderGroup();
        EditorGUILayout.Space(5);
    }

    private void DrawProperty(SerializedProperty prop, string label, string tooltip)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(new GUIContent(label, tooltip), GUILayout.Width(160));
        EditorGUILayout.PropertyField(prop, GUIContent.none);
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.Space(2);
    }
}