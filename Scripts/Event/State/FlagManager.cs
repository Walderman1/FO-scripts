// FlagManager.cs - ОБЪЕДИНЕННЫЙ МЕНЕДЖЕР ФЛАГОВ
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
            Destroy(gameObject);
        }
    }

    private void InitializeFlags()
    {
        foreach (var entry in initialFlags)
        {
            flags[entry.flagName] = entry.value;
        }
        Debug.Log($"🚩 FlagManager initialized with {initialFlags.Count} flags");
    }

    // ============ ОСНОВНЫЕ МЕТОДЫ ============

    public void SetFlag(string flagName, bool value)
    {
        flags[flagName] = value;
        Debug.Log($"🚩 Flag {flagName} = {value}");

        // Оповещаем EventManager об изменении флага
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

    // ============ ДОПОЛНИТЕЛЬНЫЕ МЕТОДЫ ============

    public void ToggleFlag(string flagName)
    {
        bool currentValue = GetFlag(flagName);
        SetFlag(flagName, !currentValue);
        Debug.Log($"🔄 Flag {flagName} toggled to {!currentValue}");
    }

    public void RemoveFlag(string flagName)
    {
        if (flags.Remove(flagName))
        {
            Debug.Log($"🗑️ Flag {flagName} removed");
        }
    }

    public void ClearAllFlags()
    {
        flags.Clear();
        Debug.Log($"🗑️ All flags cleared");
    }

    public Dictionary<string, bool> GetAllFlags()
    {
        return new Dictionary<string, bool>(flags);
    }

    public void PrintAllFlags()
    {
        Debug.Log("=== ALL FLAGS ===");
        foreach (var kvp in flags)
        {
            Debug.Log($"  {kvp.Key} = {kvp.Value}");
        }
    }

    // ============ СОХРАНЕНИЕ/ЗАГРУЗКА ============

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
            Debug.Log($"📂 Flags restored from save");
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