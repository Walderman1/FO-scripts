#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(InputManager))]
public class InputManagerEditor : Editor
{
    private SerializedProperty configProp;
    private SerializedProperty blockConflictingUIProp;
    private SerializedObject configObj;
    private Vector2 scroll;

    private void OnEnable()
    {
        configProp = serializedObject.FindProperty("config");
        blockConflictingUIProp = serializedObject.FindProperty("blockConflictingUI");
        RefreshConfigObject();
    }

    private void RefreshConfigObject()
    {
        InputManager manager = (InputManager)target;
        configObj = manager.Config != null ? new SerializedObject(manager.Config) : null;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        RefreshConfigObject();

        EditorGUILayout.Space(6);
        EditorGUILayout.PropertyField(configProp, new GUIContent("InputConfig"));
        if (blockConflictingUIProp != null)
            EditorGUILayout.PropertyField(blockConflictingUIProp, new GUIContent("Block Conflicting UI"));

        InputConfig cfg = ((InputManager)target).Config;

        if (cfg == null)
        {
            EditorGUILayout.HelpBox("InputConfig не назначен.", MessageType.Warning);
            if (GUILayout.Button("Найти в Resources"))
            {
                InputConfig found = Resources.Load<InputConfig>("Configs/InputConfig");
                if (found != null)
                {
                    configProp.objectReferenceValue = found;
                    serializedObject.ApplyModifiedProperties();
                    RefreshConfigObject();
                }
            }

            serializedObject.ApplyModifiedProperties();
            return;
        }

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField($"Редактирование: {cfg.name}", EditorStyles.boldLabel);

        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(500));
        configObj.Update();

        DrawCategory("Gameplay", "openInventory", "openEquipment", "openQuests", "closeAll");
        DrawCategory("Radial Menu", "openRadialMenu");
        DrawCategory("Navigation", "navigateLeft", "navigateRight", "navigateUp", "navigateDown");
        DrawCategory("Inventory Modifiers", "splitHalf", "splitOne");
        DrawCategory("Interaction", "interactClick");
        DrawCategory("Abilities", "useMagicLight", "clairvoyance");
        DrawCategory("Dialogue", "skipDialogue");
        DrawCategory("Save System", "saveGame", "loadGame");

        configObj.ApplyModifiedProperties();
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(6);

        if (GUILayout.Button("Открыть InputConfig"))
        {
            Selection.activeObject = cfg;
            EditorGUIUtility.PingObject(cfg);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawCategory(string title, params string[] fieldNames)
    {
        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        EditorGUI.indentLevel++;
        foreach (string f in fieldNames)
        {
            var prop = configObj.FindProperty(f);
            if (prop != null) EditorGUILayout.PropertyField(prop);
        }
        EditorGUI.indentLevel--;
    }
}
#endif