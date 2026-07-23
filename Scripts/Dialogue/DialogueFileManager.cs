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
        Logger.Log(LogModule.Dialogue, "DialogueFileManager создан");
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
            Logger.Log(LogModule.Dialogue, $"Загружено {filePaths.Count} путей из переданного списка");
        }
        else if (config != null && config.filePaths != null && config.filePaths.Count > 0)
        {
            foreach (string path in config.filePaths)
            {
                string fullPath = Application.dataPath + "/" + path;
                filePaths.Add(fullPath);
                Logger.Log(LogModule.Dialogue, $"Добавлен путь: {fullPath}");
            }
            Logger.Log(LogModule.Dialogue, $"Загружено {filePaths.Count} путей из конфига");
        }
        else
        {
            string defaultPath = Application.dataPath + "/Texts/ReplicProlouge.txt";
            filePaths.Add(defaultPath);
            Logger.LogWarning(LogModule.Dialogue, $"Используется путь по умолчанию: {defaultPath}");
        }
    }

    public bool LoadDialogueFromFile(int fileIndex)
    {
        if (fileIndex < 0 || fileIndex >= filePaths.Count)
        {
            string error = $"Неверный индекс файла: {fileIndex}. Доступно: 0-{filePaths.Count - 1}";
            Logger.LogError(LogModule.Dialogue, error);
            OnError?.Invoke(error);
            return false;
        }

        string filePath = filePaths[fileIndex];
        Logger.Log(LogModule.Dialogue, $"Загрузка файла {fileIndex}: {filePath}");
        return LoadDialogueFromPath(filePath, fileIndex);
    }

    public bool LoadDialogueFromPath(string filePath, int fileIndex = -1)
    {
        if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
        {
            string error = $"Файл не найден: {filePath}";
            Logger.LogError(LogModule.Dialogue, error);
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

            Logger.Log(LogModule.Dialogue, $"Загружено {dialogueLines.Count} строк из: {filePath}");

            if (config != null && config.enableDebugLogs && config.logLoadedLines)
            {
                for (int i = 0; i < Mathf.Min(dialogueLines.Count, 20); i++)
                {
                    Logger.Log(LogModule.Dialogue, $"Строка {i}: {dialogueLines[i]}");
                }
            }

            OnDialogueLoaded?.Invoke();
            return true;
        }
        catch (System.Exception e)
        {
            string error = $"Ошибка загрузки файла: {e.Message}";
            Logger.LogError(LogModule.Dialogue, error);
            OnError?.Invoke(error);
            return false;
        }
    }

    public string GetLine(int index)
    {
        if (index < 0 || index >= dialogueLines.Count)
        {
            Logger.LogWarning(LogModule.Dialogue, $"Запрос строки {index}, доступно {dialogueLines.Count}");
            return null;
        }
        return dialogueLines[index];
    }

    public void DeleteLines(int startIndex)
    {
        if (startIndex <= 0 || dialogueLines.Count == 0) return;
        int removeCount = Mathf.Min(startIndex, dialogueLines.Count);
        dialogueLines.RemoveRange(0, removeCount);
        Logger.Log(LogModule.Dialogue, $"Удалено {removeCount} строк из начала диалога");
    }

    public void Clear()
    {
        dialogueLines.Clear();
        filePaths.Clear();
        currentFilePath = null;
        currentFileIndex = -1;
        isFileLoaded = false;
        Logger.Log(LogModule.Dialogue, "DialogueFileManager очищен");
    }

    public List<string> GetFilePaths()
    {
        return new List<string>(filePaths);
    }

    public void LogFileInfo()
    {
        if (config != null && !config.enableDebugLogs) return;

        Logger.Log(LogModule.Dialogue, "=== DIALOGUE FILE MANAGER ===");
        Logger.Log(LogModule.Dialogue, $"Файл загружен: {isFileLoaded}");
        Logger.Log(LogModule.Dialogue, $"Текущий файл: {currentFilePath}");
        Logger.Log(LogModule.Dialogue, $"Индекс файла: {currentFileIndex}");
        Logger.Log(LogModule.Dialogue, $"Количество строк: {dialogueLines.Count}");
        Logger.Log(LogModule.Dialogue, $"Всего файлов: {filePaths.Count}");
        for (int i = 0; i < filePaths.Count; i++)
        {
            Logger.Log(LogModule.Dialogue, $"  {i}: {filePaths[i]}");
        }
        Logger.Log(LogModule.Dialogue, "=============================");
    }

    #endregion
}
