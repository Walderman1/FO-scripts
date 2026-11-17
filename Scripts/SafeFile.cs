using System;
using System.IO;

public static class SafeFile
{
    public static string Read(string path, out bool loadedFromBackup)
    {
        loadedFromBackup = false;

        if (string.IsNullOrEmpty(path))
        {
            Logger.LogError(LogModule.Save, "SafeFile.Read: path is null or empty");
            return null;
        }

        if (File.Exists(path))
        {
            try
            {
                string content = File.ReadAllText(path);
                if (!string.IsNullOrEmpty(content)) return content;
            }
            catch (Exception e)
            {
                Logger.LogWarning(LogModule.Save, $"SafeFile.Read failed for '{path}': {e.Message}");
            }
        }

        string backupPath = path + ".bak";
        if (File.Exists(backupPath))
        {
            try
            {
                string content = File.ReadAllText(backupPath);
                if (!string.IsNullOrEmpty(content))
                {
                    Logger.LogWarning(LogModule.Save, $"Загружен резервный файл: {backupPath}");
                    loadedFromBackup = true;
                    return content;
                }
            }
            catch (Exception e)
            {
                Logger.LogWarning(LogModule.Save, $"SafeFile.Read failed for backup '{backupPath}': {e.Message}");
            }
        }

        return null;
    }

    public static bool Write(string path, string content)
    {
        if (string.IsNullOrEmpty(path))
        {
            Logger.LogError(LogModule.Save, "SafeFile.Write: path is null or empty");
            return false;
        }

        string directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            Directory.CreateDirectory(directory);

        string tempPath = path + ".tmp";
        string backupPath = path + ".bak";

        try
        {
            File.WriteAllText(tempPath, content);
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"SafeFile.Write temp failed for '{path}': {e.Message}");
            TryDelete(tempPath);
            return false;
        }

        if (File.Exists(path))
        {
            try
            {
                TryDelete(backupPath);
                File.Move(path, backupPath);
            }
            catch (Exception e)
            {
                Logger.LogWarning(LogModule.Save, $"SafeFile.Write backup move failed for '{path}': {e.Message}");
                TryDelete(tempPath);
                return false;
            }
        }

        try
        {
            File.Move(tempPath, path);
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"SafeFile.Write finalize failed for '{path}': {e.Message}");
            TryDelete(tempPath);

            if (File.Exists(backupPath) && !File.Exists(path))
            {
                try { File.Move(backupPath, path); }
                catch { }
            }

            return false;
        }
    }

    private static void TryDelete(string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        try { if (File.Exists(path)) File.Delete(path); }
        catch { }
    }
}