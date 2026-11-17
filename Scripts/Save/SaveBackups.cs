using System;
using System.IO;
using UnityEngine;

public static class SaveBackups
{
    public const int MaxBackups = 10;
    public const string BackupsFolderName = "Backups";

    public static string GetBackupsFolder(string mainSavePath)
    {
        if (string.IsNullOrEmpty(mainSavePath)) return null;
        string dir = Path.GetDirectoryName(mainSavePath);
        if (string.IsNullOrEmpty(dir)) return null;
        return Path.Combine(dir, BackupsFolderName);
    }

    public static string CreateBackup(string mainSavePath, string reason = "")
    {
        if (string.IsNullOrEmpty(mainSavePath) || !File.Exists(mainSavePath))
            return null;

        try
        {
            string folder = GetBackupsFolder(mainSavePath);
            if (string.IsNullOrEmpty(folder)) return null;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string baseName = Path.GetFileNameWithoutExtension(mainSavePath);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string suffix = string.IsNullOrEmpty(reason) ? "" : "_" + reason;
            string backupPath = Path.Combine(folder, $"{baseName}_{timestamp}{suffix}.sv");

            File.Copy(mainSavePath, backupPath, true);
            CleanupOldBackups(mainSavePath);
            return backupPath;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка создания бэкапа: {e.Message}");
            return null;
        }
    }

    public static string CreateSnapshot(string mainSavePath, string json, string reason = "")
    {
        if (string.IsNullOrEmpty(json)) return null;

        try
        {
            string folder = GetBackupsFolder(mainSavePath);
            if (string.IsNullOrEmpty(folder)) return null;
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);

            string baseName = Path.GetFileNameWithoutExtension(mainSavePath);
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string suffix = string.IsNullOrEmpty(reason) ? "" : "_" + reason;
            string backupPath = Path.Combine(folder, $"{baseName}_{timestamp}{suffix}.sv");

            File.WriteAllText(backupPath, json);
            CleanupOldBackups(mainSavePath);
            return backupPath;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка создания snapshot: {e.Message}");
            return null;
        }
    }

    public static string[] GetBackupFiles(string mainSavePath)
    {
        string folder = GetBackupsFolder(mainSavePath);
        if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return Array.Empty<string>();
        var files = Directory.GetFiles(folder, "*.sv");
        Array.Sort(files, (a, b) => File.GetCreationTime(b).CompareTo(File.GetCreationTime(a)));
        return files;
    }

    public static bool RestoreFromBackup(string mainSavePath, string backupPath)
    {
        if (string.IsNullOrEmpty(backupPath) || !File.Exists(backupPath)) return false;
        if (string.IsNullOrEmpty(mainSavePath)) return false;

        try
        {
            CreateBackup(mainSavePath, "pre_restore");
            File.Copy(backupPath, mainSavePath, true);
            Logger.Log(LogModule.Save, $"Восстановлено из бэкапа: {Path.GetFileName(backupPath)}");
            return true;
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Save, $"Ошибка восстановления из бэкапа: {e.Message}");
            return false;
        }
    }

    private static void CleanupOldBackups(string mainSavePath)
    {
        try
        {
            string folder = GetBackupsFolder(mainSavePath);
            if (string.IsNullOrEmpty(folder) || !Directory.Exists(folder)) return;

            var files = Directory.GetFiles(folder, "*.sv");
            if (files.Length <= MaxBackups) return;

            Array.Sort(files, (a, b) => File.GetCreationTime(a).CompareTo(File.GetCreationTime(b)));
            int toDelete = files.Length - MaxBackups;
            for (int i = 0; i < toDelete; i++) File.Delete(files[i]);
        }
        catch { }
    }
}