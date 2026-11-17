#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(QuestConfigSO))]
public class QuestConfigEditor : Editor
{
    private QuestConfigSO config;

    private bool showPaths, showColors, showSizes, showNotifications, showIcons, showTexts, showTabs, showOther;

    private void OnEnable()
    {
        config = (QuestConfigSO)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("КОНФИГУРАЦИЯ КВЕСТОВОЙ СИСТЕМЫ", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawSection("📁 ПУТИ К ПРЕФАБАМ", ref showPaths, new[]
        {
            "questItemPrefabPath", "objectivePrefabPath", "notificationPrefabPath",
            "slotPrefabPath", "questsFolder", "notificationsTag", "inventoryItemsPath",
            "rewardSlotPrefabPath"
        });

        DrawSection("🎨 ЦВЕТА", ref showColors, new[]
        {
            "activeColor", "completedColor", "failedColor", "trackingColor",
            "startNotifyColor", "updateNotifyColor", "completeNotifyColor",
            "objCompleteColor", "objIncompleteColor", "objFailedColor",
            "detailBgColor", "activeTabColor", "inactiveTabColor", "failedTabColor",
            "rewardBgColor", "rewardBorderColor"
        });

        DrawSection("📐 РАЗМЕРЫ", ref showSizes, new[]
        {
            "fadeDuration", "questItemHeight", "fallbackItemHeight",
            "slotSize", "slotSpacing", "titleFontSize", "progressFontSize",
            "emptyMsgFontSize", "emptyMsgHeight", "objectiveHeight",
            "detailOffsetMin", "detailOffsetMax",
            "rewardSlotSize", "rewardSlotSpacing"
        });

        DrawSection("🔔 УВЕДОМЛЕНИЯ", ref showNotifications, new[]
        {
            "notifyDuration", "notifyFade", "notifySlide",
            "maxNotifications", "notifySpacing", "notifyHeight",
            "notifyBgAlpha", "notifySlideOffset"
        });

        DrawSection("🖼️ ИКОНКИ", ref showIcons, new[]
        {
            "startIcon", "completeIcon", "updateIcon", "objectiveIcon", "iconSize"
        });

        DrawSection("📝 ТЕКСТЫ", ref showTexts, new[]
        {
            "trackBtnText", "untrackBtnText", "cancelBtnText",
            "failedQuestText", "questCompleteText",
            "emptyActiveText", "emptyCompletedText", "emptyFailedText",
            "progressDetailFormat", "objectiveProgressFormat",
            "rewardsLabelText", "noRewardsText"
        });

        DrawSection("📑 ВКЛАДКИ", ref showTabs, new[]
        {
            "activeTabName", "completedTabName", "failedTabName"
        });

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("⚙️ ПРОЧЕЕ", EditorStyles.boldLabel);
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(serializedObject.FindProperty("enableLogging"), new GUIContent("Логирование"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("autoLoadQuests"), new GUIContent("Автозагрузка"));
        EditorGUILayout.EndVertical();

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