#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using DialogueSystem;

[CustomEditor(typeof(DialogueData))]
public class DialogueDataEditor : Editor
{
    private DialogueData config;
    private bool showFiles, showTags, showResources, showAnimations, showDialogues, showCharacters, showPositions, showActions;

    private void OnEnable()
    {
        config = (DialogueData)target;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("КОНФИГУРАЦИЯ ДИАЛОГОВ", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        DrawSection("📁 НАСТРОЙКИ ФАЙЛОВ", ref showFiles, new[]
        {
            "dialoguesFolder", "fileExtension", "validateOnLoad"
        });

        DrawSection("🏷️ ТЕГИ И ИМЕНА", ref showTags, new[]
        {
            "worldTag", "regionTag", "locationTag", "locationReferenceTag",
            "dialoguePanelTag", "choicePanelTag", "speakerNamePanel", "blackPanelTag",
            "panelDialogueTag", "firstCharactersTag", "secondCharactersTag", "characterTag",
            "charactersFolderName", "skipButtonName", "buttonPanelName", "worldObjectName"
        });

        DrawSection("📂 ПУТИ К РЕСУРСАМ", ref showResources, new[]
        {
            "charactersResourcesPath", "portraitsResourcesPath"
        });

        DrawSection("🎬 АНИМАЦИИ", ref showAnimations, new[]
        {
            "speakAnimation", "idleEyeAnimation", "fadeDuration", "moveDuration"
        });

        DrawSection("⚙️ НАСТРОЙКИ ДИАЛОГОВ", ref showDialogues, new[]
        {
            "defaultTextSpeed", "skipDelay", "maxSkipIterations", "choiceDelay",
            "speakerColor", "dialogueColor", "choiceColor"
        });

        DrawSection("📍 ПОЗИЦИИ ПЕРСОНАЖА", ref showPositions, new[]
        {
            "defaultLeftPosition", "defaultCenterPosition", "defaultRightPosition"
        });

        DrawSection("🔧 МАППИНГ ДЕЙСТВИЙ", ref showActions, new[]
        {
            "showCharacterAction", "hideCharacterAction", "moveCharacterAction", "setPortraitAction",
            "setFlagAction", "startQuestAction", "completeQuestAction",
            "addItemAction", "removeItemAction",
            "playSoundAction", "playAnimationAction",
            "completeDialogueAction", "unlockDialogueAction", "lockDialogueAction",
            "resetDialogueAction", "setNextDialogueAction", "checkDialogueStatusAction"
        });

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Открыть папку с диалогами"))
        {
            string path = "Assets/Resources/" + config.DialoguesFolder;
            if (System.IO.Directory.Exists(path)) EditorUtility.RevealInFinder(path);
            else
            {
                if (EditorUtility.DisplayDialog("Папка не найдена",
                    $"'{path}' не существует. Создать?", "Создать", "Отмена"))
                {
                    System.IO.Directory.CreateDirectory(path);
                    AssetDatabase.Refresh();
                }
            }
        }

        if (GUILayout.Button("Сбросить к дефолту"))
        {
            if (EditorUtility.DisplayDialog("Сброс", "Сбросить все настройки?", "Да", "Нет"))
            {
                config.ResetToDefaults();
                EditorUtility.SetDirty(config);
            }
        }

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