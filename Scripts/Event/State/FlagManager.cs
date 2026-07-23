using UnityEngine;
using System.Collections.Generic;

public class FlagManager : MonoBehaviour
{
    public static FlagManager Instance;

    [System.Serializable]
    public class FlagEntry
    {
        public string flagName;
        public bool value;
    }

    [Header("Initial Flags")]
    [SerializeField] private List<FlagEntry> initialFlags = new List<FlagEntry>();

    private Dictionary<string, bool> flags = new Dictionary<string, bool>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeFlags();
        }
        else
        {
            Logger.Log(LogModule.Core, "Уничтожение дублирующего FlagManager");
            Destroy(gameObject);
        }
    }

    private void InitializeFlags()
    {
        foreach (var entry in initialFlags)
        {
            flags[entry.flagName] = entry.value;
        }
        Logger.Log(LogModule.Core, $"FlagManager инициализирован с {initialFlags.Count} флагами");
    }

    public void SetFlag(string flagName, bool value)
    {
        flags[flagName] = value;
        Logger.Log(LogModule.Core, $"Флаг {flagName} = {value}");

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnFlagChanged(flagName, value);
        }
    }

    public bool GetFlag(string flagName)
    {
        if (flags.TryGetValue(flagName, out bool value))
            return value;
        return false;
    }

    public bool HasFlag(string flagName)
    {
        return flags.ContainsKey(flagName);
    }

    public void ToggleFlag(string flagName)
    {
        bool currentValue = GetFlag(flagName);
        SetFlag(flagName, !currentValue);
        Logger.Log(LogModule.Core, $"Флаг {flagName} переключен на {!currentValue}");
    }

    public void RemoveFlag(string flagName)
    {
        if (flags.Remove(flagName))
        {
            Logger.Log(LogModule.Core, $"Флаг {flagName} удалён");
        }
    }

    public void ClearAllFlags()
    {
        flags.Clear();
        Logger.Log(LogModule.Core, "Все флаги очищены");
    }

    public Dictionary<string, bool> GetAllFlags()
    {
        return new Dictionary<string, bool>(flags);
    }

    public void PrintAllFlags()
    {
        Logger.Log(LogModule.Core, "=== ВСЕ ФЛАГИ ===");
        foreach (var kvp in flags)
        {
            Logger.Log(LogModule.Core, $"  {kvp.Key} = {kvp.Value}");
        }
    }

    public string SerializeFlags()
    {
        return JsonUtility.ToJson(new FlagSaveData { flags = flags });
    }

    public void DeserializeFlags(string json)
    {
        var data = JsonUtility.FromJson<FlagSaveData>(json);
        if (data != null)
        {
            flags = data.flags;
            Logger.Log(LogModule.Core, "Флаги восстановлены из сохранения");
        }
    }

    [System.Serializable]
    private class FlagSaveData
    {
        public Dictionary<string, bool> flags;
    }

#if UNITY_EDITOR
    [ContextMenu("Print All Flags")]
    public void PrintAllFlagsEditor()
    {
        PrintAllFlags();
    }

    [ContextMenu("Clear All Flags")]
    public void ClearAllFlagsEditor()
    {
        ClearAllFlags();
    }
#endif
}
