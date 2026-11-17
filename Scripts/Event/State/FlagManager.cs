using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using VContainer;

[DefaultExecutionOrder(-9300)]
public class FlagManager : MonoBehaviour
{
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
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
        InitializeFlags();
    }

    private void InitializeFlags()
    {
        foreach (var entry in initialFlags)
        {
            if (entry == null || string.IsNullOrEmpty(entry.flagName)) continue;
            flags[entry.flagName] = entry.value;
        }
    }

    public void SetFlag(string flagName, bool value)
    {
        if (string.IsNullOrEmpty(flagName)) return;

        bool changed = !flags.TryGetValue(flagName, out bool old) || old != value;
        flags[flagName] = value;

        if (!changed) return;

        Logger.Log(LogModule.Core, $"Флаг {flagName} = {value}");

        var em = SceneInjector.Instance?.RootScope?.Resolve<EventManager>();
        em?.OnFlagChanged(flagName, value);
    }

    public bool GetFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return false;
        if (flags.TryGetValue(flagName, out bool value)) return value;
        return false;
    }

    public bool HasFlag(string flagName)
    {
        return !string.IsNullOrEmpty(flagName) && flags.ContainsKey(flagName);
    }

    public void ToggleFlag(string flagName)
    {
        bool current = GetFlag(flagName);
        SetFlag(flagName, !current);
    }

    public void RemoveFlag(string flagName)
    {
        if (string.IsNullOrEmpty(flagName)) return;
        flags.Remove(flagName);
    }

    public void ResetFlags()
    {
        flags.Clear();
        InitializeFlags();
    }

    public void ClearAllFlags()
    {
        flags.Clear();
    }

    public Dictionary<string, bool> GetAllFlags()
    {
        return new Dictionary<string, bool>(flags);
    }

    public string SerializeFlags()
    {
        return JsonConvert.SerializeObject(flags, Formatting.Indented);
    }

    public void DeserializeFlags(string json)
    {
        if (string.IsNullOrEmpty(json)) return;

        try
        {
            var loaded = JsonConvert.DeserializeObject<Dictionary<string, bool>>(json);
            if (loaded == null) return;

            flags.Clear();
            foreach (var kvp in loaded)
            {
                if (!string.IsNullOrEmpty(kvp.Key)) flags[kvp.Key] = kvp.Value;
            }
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.Core, $"Ошибка десериализации флагов: {e.Message}");
        }
    }
}