#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public class DebuggerWindow : EditorWindow
{
    [MenuItem("Tools/Личное Окно Отладки")]
    public static void ShowWindow()
    {
        var w = GetWindow<DebuggerWindow>("Отладчик");
        w.minSize = new Vector2(320, 300);
    }

    private void OnEnable() => DebuggerCore.Register(this);
    private void OnDisable() => DebuggerCore.Unregister(this);

    private void OnGUI() => DebuggerCore.Draw(position.width);
}
#endif