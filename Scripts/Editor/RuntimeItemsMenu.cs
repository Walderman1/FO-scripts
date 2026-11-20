#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// Небольшие пункты меню, чтобы быстро найти папку художника из редактора Unity.
/// Сама логика предметов живёт в Assets/Scripts/RuntimeItems и работает в билде.
/// </summary>
public static class RuntimeItemsMenu
{
    [MenuItem("Tools/Предметы из картинок/Открыть папку ItemsInbox", false, 100)]
    public static void OpenInboxFolder()
    {
        RuntimeItemFolder.OpenPrimaryFolder();

        if (!string.IsNullOrEmpty(RuntimeItemFolder.PrimaryFolder))
            Debug.Log($"[ItemsInbox] Папка: {RuntimeItemFolder.PrimaryFolder}");
    }

    [MenuItem("Tools/Предметы из картинок/Показать пути папок", false, 101)]
    public static void LogFolders()
    {
        var folders = RuntimeItemFolder.Folders;
        if (folders.Count == 0)
        {
            Debug.Log("[ItemsInbox] Папки ещё не определены — запустите игру или откройте папку.");
            return;
        }

        for (int i = 0; i < folders.Count; i++)
            Debug.Log($"[ItemsInbox] Папка #{i + 1}: {folders[i]}");
    }
}
#endif
