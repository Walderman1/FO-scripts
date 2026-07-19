using UnityEngine;
using System.IO;
using System.Collections.Generic;

public class DialogueFileManager
{
    #region Fields

    private List<string> dialogueLines = new List<string>();
    private List<string> filePaths = new List<string>();
    private DialogueData config;

    private string currentFilePath;
    private int currentFileIndex = -1;
    private bool isFileLoaded;

    public System.Action OnDialogueLoaded;
    public System.Action<string> OnError;

    #endregion

    #region Properties

    public List<string> DialogueLines => dialogueLines;
    public string CurrentFilePath => currentFilePath;
    public int CurrentFileIndex => currentFileIndex;
    public bool IsFileLoaded => isFileLoaded;
    public int LineCount => dialogueLines.Count;

    #endregion

    #region Constructor

    public DialogueFileManager(DialogueData config = null)
    {
        this.config = config;
    }

    #endregion

    #region Public Methods

    public void InitializeFilePaths(List<string> paths = null)
    {
        filePaths.Clear();

        if (paths != null && paths.Count > 0)
        {
            foreach (string path in paths)
            {
                filePaths.Add(path);
            }
            Debug.Log($"✅ Загружено {filePaths.Count} путей из переданного списка");
        }
        else if (config != null && config.filePaths != null && config.filePaths.Count > 0)
        {
            foreach (string path in config.filePaths)
            {
                string fullPath = Application.dataPath + "/" + path;
                filePaths.Add(fullPath);
                Debug.Log($"📂 Добавлен путь: {fullPath}");
            }
            Debug.Log($"✅ Загружено {filePaths.Count} путей из конфига");
        }
        else
        {
            string defaultPath = Application.dataPath + "/Texts/ReplicProlouge.txt";
            filePaths.Add(defaultPath);
            Debug.LogWarning($"⚠️ Используется путь по умолчанию: {defaultPath}");
        }
    }

    public bool LoadDialogueFromFile(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= filePaths.Count)
        {
            string error = $"Invalid file index: {fileIndex}. Available: 0-{filePaths.Count - 1}";
            Debug.LogError(error);
            OnError?.Invoke(error);
            return false;
        }

        string filePath = filePaths[fileIndex];
        Debug.Log($"📂 Загрузка файла [{fileIndex}]: {filePath}");
        return LoadDialogueFromPath(filePath, fileIndex);
    }

    public bool LoadDialogueFromPath(string filePath, int fileIndex = -1)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            string error = $"File not found: {filePath}";
            Debug.LogError(error);
            OnError?.Invoke(error);
            return false;
        }

        try
        {
            dialogueLines.Clear();

            using (StreamReader reader = new StreamReader(filePath))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (!string.IsNullOrEmpty(line) && line != "-")
                    {
                        dialogueLines.Add(line);
                    }
                }
            }

            currentFilePath = filePath;
            currentFileIndex = fileIndex >= 0 ? fileIndex : filePaths.IndexOf(filePath);
            isFileLoaded = true;

            Debug.Log($"✅ Загружено {dialogueLines.Count} строк из: {filePath}");

            if (config != null && config.enableDebugLogs && config.logLoadedLines)
            {
                for (int i = 0; i < Mathf.Min(dialogueLines.Count, 20); i++)
                {
                    Debug.Log($"Line {i}: {dialogueLines[i]}");
                }
            }

            OnDialogueLoaded?.Invoke();
            return true;
        }
        catch (System.Exception e)
        {
            string error = $"Error loading file: {e.Message}";
            Debug.LogError(error);
            OnError?.Invoke(error);
            return false;
        }
    }

    public string GetLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Count)
        {
            return null;
        }
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

    public List<string> GetFilePaths()
    {
        return new List<string>(filePaths);
    }

    public void LogFileInfo()
    {
        if (config != null && !config.enableDebugLogs) return;

        Debug.Log("=== DIALOGUE FILE MANAGER ===");
        Debug.Log($"File loaded: {isFileLoaded}");
        Debug.Log($"Current file: {currentFilePath}");
        Debug.Log($"File index: {currentFileIndex}");
        Debug.Log($"Lines count: {dialogueLines.Count}");
        Debug.Log($"Total files: {filePaths.Count}");
        for (int i = 0; i < filePaths.Count; i++)
        {
            Debug.Log($"  [{i}] {filePaths[i]}");
        }
        Debug.Log("=============================");
    }

    #endregion
}