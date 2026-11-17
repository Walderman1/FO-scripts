#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class LocationEditorHelper
{
    public static void DrawLocationDropdown(
        string label,
        string currentValue,
        Action<string> setter,
        float labelWidth = 180f,
        bool focusOnChange = true)
    {
        var locations = new List<string>(GetLocationNames());
        currentValue ??= "";

        if (locations.Count > 0)
        {
            if (!locations.Contains(currentValue) && !string.IsNullOrEmpty(currentValue))
                locations.Insert(0, currentValue);
            else if (string.IsNullOrEmpty(currentValue) && !locations.Contains("(Не выбрано)"))
                locations.Insert(0, "(Не выбрано)");

            int currentIndex = locations.IndexOf(currentValue);
            if (currentIndex < 0) currentIndex = 0;

            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));

            int newIndex = EditorGUILayout.Popup(currentIndex, locations.ToArray());
            if (newIndex != currentIndex)
            {
                string selected = locations[newIndex];
                if (selected == "(Не выбрано)") selected = "";
                setter?.Invoke(selected);

                if (focusOnChange && !string.IsNullOrEmpty(selected))
                    FocusOnLocationByName(selected);
            }

            if (GUILayout.Button(new GUIContent("🎯", "Перейти к локации в Scene View"), GUILayout.Width(25)))
            {
                if (!string.IsNullOrEmpty(currentValue))
                    FocusOnLocationByName(currentValue);
            }

            if (GUILayout.Button(new GUIContent("↻", "Обновить список"), GUILayout.Width(25)))
            {
                GUI.changed = true;
            }

            EditorGUILayout.EndHorizontal();
        }
        else
        {
            EditorGUILayout.BeginHorizontal();
            if (!string.IsNullOrEmpty(label))
                EditorGUILayout.LabelField(label, GUILayout.Width(labelWidth));

            string newValue = EditorGUILayout.TextField(currentValue);
            if (newValue != currentValue) setter?.Invoke(newValue);

            EditorGUILayout.EndHorizontal();
        }
    }

    public static void FocusOnLocationByName(string locationName)
    {
        if (string.IsNullOrEmpty(locationName)) return;

        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        GameObject targetObj = null;

        foreach (var go in allObjects)
        {
            if (go == null || !go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (go.CompareTag("Location") && go.name == locationName)
            {
                targetObj = go;
                break;
            }
        }

        if (targetObj != null)
        {
            var sceneView = SceneView.lastActiveSceneView;
            if (sceneView != null)
            {
                sceneView.LookAt(targetObj.transform.position, sceneView.rotation, 10f, true);
                sceneView.Repaint();
            }
        }
    }

    public static string[] GetLocationNames()
    {
        var locations = new List<string>();
        var allObjects = Resources.FindObjectsOfTypeAll<GameObject>();

        foreach (var go in allObjects)
        {
            if (go == null) continue;
            if (!go.scene.IsValid() || EditorUtility.IsPersistent(go)) continue;
            if (go.CompareTag("Location"))
            {
                if (!locations.Contains(go.name))
                    locations.Add(go.name);
            }
        }

        locations.Sort();
        return locations.ToArray();
    }
}
#endif