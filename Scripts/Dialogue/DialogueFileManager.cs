using DialogueSystem;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

// ★ Восстановленные DTO-классы (были в этом же файле)
[System.Serializable]
public class DialogueAction
{
    public string type;
    public string target;
    public string value;
}

[System.Serializable]
public class DialogueChoice
{
    public string text;
    public string nextNodeId;
    public string condition;
}

[System.Serializable]
public class DialogueNode
{
    public string id;
    public string speaker;
    public string text;
    public string nextNodeId;
    public DialogueChoice[] choices;
    public DialogueAction[] actions;
}

[System.Serializable]
public class DialogueWrapper
{
    public DialogueNode[] nodes;
}

public class DialogueFileManager : MonoBehaviour
{
    [SerializeField] private DialogueData dialogueConfig;

    [Inject] private DialogueData _injectedConfig;
    [Inject] private SettingsManager _settingsManager;
    [Inject] private GameReferences _refs;

    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private List<string> dialogueLines = new List<string>();
    private List<string> filePaths = new List<string>();
    private string currentFilePath;
    private int currentFileIndex = -1;
    private bool isFileLoaded;

    public System.Action OnDialogueLoaded;
    public System.Action<string> OnError;

    public List<string> DialogueLines => dialogueLines;
    public string CurrentFilePath => currentFilePath;
    public int CurrentFileIndex => currentFileIndex;
    public bool IsFileLoaded => isFileLoaded;
    public int LineCount => dialogueLines.Count;

    private DialogueData Config
    {
        get
        {
            if (dialogueConfig == null) dialogueConfig = _injectedConfig;
            return dialogueConfig;
        }
    }

    private void Awake()
    {
        if (dialogueConfig == null) dialogueConfig = _injectedConfig;
    }

    private string GetLanguageCode()
    {
        if (_settingsManager != null && _settingsManager.Settings != null)
            return _settingsManager.GetLanguageCode();

        var sm = Refs?.SettingsManager;
        if (sm != null) return sm.GetLanguageCode();

        return "ru";
    }

    public void InitializeFilePaths(List<string> paths = null)
    {
        filePaths.Clear();

        if (paths != null && paths.Count > 0)
        {
            foreach (string path in paths) filePaths.Add(path);
            return;
        }

        if (Config == null) return;

        string folder = Config.DialoguesFolder;
        TextAsset[] assets = Resources.LoadAll<TextAsset>(folder);

        foreach (var asset in assets)
        {
            if (asset == null) continue;
            string cleanName = asset.name;
            if (cleanName.EndsWith(".ru") || cleanName.EndsWith(".en") || cleanName.EndsWith(".de"))
                cleanName = cleanName.Substring(0, cleanName.Length - 3);

            if (!filePaths.Contains(cleanName)) filePaths.Add(cleanName);
        }
    }

    public DialogueNode[] LoadDialogue(string fileName)
    {
        if (Config == null) return null;

        string langCode = GetLanguageCode();
        string resourcePath = $"{Config.DialoguesFolder}/{fileName}.{langCode}";
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);

        if (asset == null && langCode != "ru")
        {
            resourcePath = $"{Config.DialoguesFolder}/{fileName}.ru";
            asset = Resources.Load<TextAsset>(resourcePath);
        }

        if (asset == null)
        {
            resourcePath = $"{Config.DialoguesFolder}/{fileName}";
            asset = Resources.Load<TextAsset>(resourcePath);
        }

        if (asset == null)
        {
            Logger.LogError(LogModule.Dialogue, $"Диалог не найден: {resourcePath}");
            return null;
        }

        try
        {
            DialogueWrapper wrapper = JsonUtility.FromJson<DialogueWrapper>(asset.text);
            if (wrapper?.nodes == null || wrapper.nodes.Length == 0) return null;

            isFileLoaded = true;
            currentFilePath = $"Resources/{resourcePath}";
            OnDialogueLoaded?.Invoke();
            return wrapper.nodes;
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.Dialogue, $"Ошибка загрузки диалога {fileName}: {e.Message}");
            OnError?.Invoke(e.Message);
            return null;
        }
    }

    public DialogueNode GetNodeById(DialogueNode[] nodes, string id)
    {
        if (string.IsNullOrEmpty(id) || nodes == null) return null;
        foreach (DialogueNode node in nodes)
            if (node.id == id) return node;
        return null;
    }

    public string GetLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Count) return null;
        return dialogueLines[index];
    }

    public void DeleteLines(int startIndex)
    {
        if (startIndex <= 0 || dialogueLines.Count == 0) return;
        int removeCount = Mathf.Min(startIndex, dialogueLines.Count);
        dialogueLines.RemoveRange(0, removeCount);
    }

    public void Clear()
    {
        dialogueLines.Clear();
        filePaths.Clear();
        currentFilePath = null;
        currentFileIndex = -1;
        isFileLoaded = false;
    }

    public List<string> GetFilePaths() => new List<string>(filePaths);

    public void LogFileInfo()
    {
        Logger.LogVerbose(LogModule.Dialogue, "=== DIALOGUE FILE MANAGER ===");
        Logger.LogVerbose(LogModule.Dialogue, $"File loaded: {isFileLoaded}");
        Logger.LogVerbose(LogModule.Dialogue, $"Current file: {currentFilePath}");
        Logger.LogVerbose(LogModule.Dialogue, $"Lines count: {dialogueLines.Count}");
        Logger.LogVerbose(LogModule.Dialogue, $"Total files: {filePaths.Count}");
    }
}