#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MenuUIConfig))]
public class MenuUIConfigEditor : Editor
{
    private bool showPaths, showColors, showSizes, showTexts, showAnimation, showService;

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("КОНФИГУРАЦИЯ МЕНЮ UI", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawSection("📁 ПУТИ К ПРЕФАБАМ", ref showPaths, new[]
        {
            "panelPrefabPath", "buttonPrefabPath", "closeButtonPrefabPath", "backButtonPrefabPath",
            "trixieBackButtonPath", "trixieExitButtonPath", "defaultFontPath",
            "backgroundsFolder", "memoriesBackgroundPath"
        });

        DrawSection("🎨 ЦВЕТА", ref showColors, new[]
        {
            "panelBgColor", "buttonNormalColor", "buttonHighlightedColor",
            "buttonPressedColor", "buttonSelectedColor",
            "overlayColor", "placeholderTextColor", "resetNotifyColor"
        });

        DrawSection("📐 РАЗМЕРЫ И ПОЗИЦИИ", ref showSizes, new[]
        {
            "mainPanelSize", "memoriesPanelSize", "panelPadding", "rightOffset", "verticalOffset",
            "buttonSize", "exitButtonSize", "buttonFontSize", "buttonSpacing",
            "titleFontSize", "titleSize", "titleYOffset",
            "exitPanelSize", "exitTitleYOffset", "exitButtonsYOffset",
            "placeholderSize", "placeholderFontSize",
            "resetNotifySize", "resetNotifyFontSize", "resetNotifyYOffset", "resetNotifyDuration"
        });

        DrawSection("🔔 ТЕКСТЫ", ref showTexts, new[]
        {
            "newGameButtonText", "continueButtonText", "memoriesButtonText",
            "settingsButtonText", "exitButtonText", "backButtonText",
            "backToMenuText", "quitButtonText",
            "memoriesTitleText", "exitConfirmTitleText",
            "memoriesPlaceholderText", "resetNotifyText"
        });

        DrawSection("⚙️ НАСТРОЙКИ АНИМАЦИИ", ref showAnimation, new[]
        {
            "backgroundTransitionDuration", "panelTransitionDuration",
            "backgroundOffset", "panelTransitionOffset",
            "trixieFadeInDuration", "trixieStartDelay",
            "trixieLightFadeInDuration", "trixieDefaultLightIntensity",
            "letterRevealDelay", "intensityMax", "flickerMin", "flickerMax",
            "flickerSpeed", "letterFadeDuration",
            "exitTransitionDuration", "exitTitleStartY", "exitTitleEndY",
            "exitButtonsStartY", "exitButtonsEndY"
        });

        DrawSection("🔧 СЛУЖЕБНОЕ", ref showService, new[]
        {
            "uiRootName", "canvasName", "exitOverlayName",
            "exitTitleName", "exitBackButtonName", "exitQuitButtonName"
        });

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawSection(string title, ref bool state, string[] fields)
    {
        state = EditorGUILayout.Foldout(state, title, true);
        if (!state) return;

        EditorGUILayout.BeginVertical("box");
        foreach (var f in fields)
        {
            var prop = serializedObject.FindProperty(f);
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }
        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(4);
    }
}
#endif