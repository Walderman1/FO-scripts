using UnityEngine;
using System.IO;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Newtonsoft.Json;
using VContainer;

[DefaultExecutionOrder(-9500)]
public class SettingsManager : MonoBehaviour
{
    private SettingsData settings = new SettingsData();
    private string settingsPath;

    [Inject] private FpsCounter _fpsCounter;
    [Inject] private ColorBlindFilter _colorBlindFilter;

    public event Action<KeyBindings> OnKeyBindingsChanged;

    private Dictionary<TMP_Text, float> baseFontSizes = new Dictionary<TMP_Text, float>();
    private Dictionary<CanvasScaler, Vector2> baseScalerSizes = new Dictionary<CanvasScaler, Vector2>();

    private readonly List<TMP_Text> registeredTexts = new List<TMP_Text>();
    private readonly List<CanvasScaler> registeredScalers = new List<CanvasScaler>();
    private bool registryDirty = true;

    public SettingsData Settings => settings;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);

        settingsPath = GamePaths.SettingsFile;
        LoadSettings();
    }

    private void Start() => ApplySettings();

    private void OnEnable() { SceneManager.sceneLoaded -= OnSceneLoaded; SceneManager.sceneLoaded += OnSceneLoaded; }
    private void OnDisable() { SceneManager.sceneLoaded -= OnSceneLoaded; }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { registryDirty = true; }

    public void RegisterText(TMP_Text text)
    {
        if (text == null) return;
        if (registeredTexts.Contains(text)) return;

        registeredTexts.Add(text);
        if (!baseFontSizes.ContainsKey(text)) baseFontSizes[text] = text.fontSize;
        text.fontSize = baseFontSizes[text] * settings.uiFontScale;
    }

    public void UnregisterText(TMP_Text text)
    {
        if (text == null) return;
        registeredTexts.Remove(text);
        baseFontSizes.Remove(text);
    }

    public void RegisterScaler(CanvasScaler scaler)
    {
        if (scaler == null) return;
        if (registeredScalers.Contains(scaler)) return;

        registeredScalers.Add(scaler);
        if (!baseScalerSizes.ContainsKey(scaler)) baseScalerSizes[scaler] = scaler.referenceResolution;

        Vector2 baseRes = baseScalerSizes[scaler];
        scaler.referenceResolution = new Vector2(baseRes.x / settings.uiScale, baseRes.y / settings.uiScale);
    }

    public void UnregisterScaler(CanvasScaler scaler)
    {
        if (scaler == null) return;
        registeredScalers.Remove(scaler);
        baseScalerSizes.Remove(scaler);
    }

    public void SaveSettings()
    {
        try
        {
            string directory = Path.GetDirectoryName(settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            SafeFile.Write(settingsPath, json);
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Menu, $"Ошибка сохранения настроек: {e.Message}");
        }
    }

    public void LoadSettings()
    {
        string json = SafeFile.Read(settingsPath, out bool fromBackup);

        SettingsData loadedData = null;
        if (!string.IsNullOrEmpty(json))
        {
            try { loadedData = JsonConvert.DeserializeObject<SettingsData>(json); }
            catch (Exception e) { Logger.LogWarning(LogModule.Menu, $"Ошибка разбора JSON настроек: {e.Message}"); }
        }

        if (loadedData != null)
        {
            settings = loadedData;
            if (settings.keyBindings == null) settings.keyBindings = new KeyBindings();
            if (fromBackup) SaveSettings();
        }
        else
        {
            settings = new SettingsData();
            SaveSettings();
        }
    }

    public void ApplySettings()
    {
        if (settings == null) return;

        try
        {
            if (!string.IsNullOrEmpty(settings.resolution))
            {
                string[] parts = settings.resolution.Split('x');
                if (parts.Length == 2 && int.TryParse(parts[0], out int width) && int.TryParse(parts[1], out int height))
                    Screen.SetResolution(width, height, settings.fullscreen);
            }

            Screen.fullScreen = settings.fullscreen;
            AudioListener.volume = Mathf.Clamp01(settings.masterVolume);

            int maxQuality = QualitySettings.names.Length - 1;
            settings.qualityIndex = Mathf.Clamp(settings.qualityIndex, 0, maxQuality);
            QualitySettings.SetQualityLevel(settings.qualityIndex);

            QualitySettings.vSyncCount = settings.vsync ? 1 : 0;
            Application.targetFrameRate = settings.vsync ? -1 : (settings.targetFps > 0 ? settings.targetFps : -1);

            ApplyKeyBindings();
            ApplyUIFontScale();
            ApplyUIScale();
            ApplyColorBlind();
            ApplyFpsSetting();
        }
        catch (Exception e)
        {
            Logger.LogError(LogModule.Menu, $"Ошибка применения настроек: {e.Message}");
        }
    }

    public void ApplyFpsSetting()
    {
        if (_fpsCounter != null && settings != null)
            _fpsCounter.SetVisible(settings.showFps);
    }

    public void ResetToDefaults()
    {
        settings = new SettingsData();
        SaveSettings();
        ApplySettings();
    }

    public void SetResolution(string resolution) { settings.resolution = resolution; SaveSettings(); ApplySettings(); }
    public void SetFullscreen(bool fullscreen) { settings.fullscreen = fullscreen; SaveSettings(); ApplySettings(); }
    public void SetQuality(int qualityIndex) { settings.qualityIndex = qualityIndex; SaveSettings(); ApplySettings(); }
    public void SetVSync(bool vsync) { settings.vsync = vsync; SaveSettings(); ApplySettings(); }
    public void SetTargetFps(int fps) { settings.targetFps = fps; SaveSettings(); ApplySettings(); }

    public void SetShowFps(bool show)
    {
        settings.showFps = show;
        SaveSettings();
        ApplyFpsSetting();
    }

    public void SetColorBlindMode(int modeIndex)
    {
        settings.colorBlindMode = (ColorBlindMode)Mathf.Clamp(modeIndex, 0, 3);
        SaveSettings();
        ApplyColorBlind();
    }

    public void SetUIScale(float scale) { settings.uiScale = Mathf.Clamp(scale, 0.5f, 1.5f); SaveSettings(); ApplyUIScale(); }
    public void SetUIFontScale(float scale) { settings.uiFontScale = Mathf.Clamp(scale, 0.5f, 1.5f); SaveSettings(); ApplyUIFontScale(); }
    public void SetMasterVolume(float volume) { settings.masterVolume = Mathf.Clamp01(volume); SaveSettings(); ApplySettings(); }
    public void SetMusicVolume(float volume) { settings.musicVolume = Mathf.Clamp01(volume); SaveSettings(); }
    public void SetSFXVolume(float volume) { settings.sfxVolume = Mathf.Clamp01(volume); SaveSettings(); }
    public void SetVoiceVolume(float volume) { settings.voiceVolume = Mathf.Clamp01(volume); SaveSettings(); }
    public void SetControlScheme(string controlScheme) { settings.controlScheme = controlScheme; SaveSettings(); }

    public void SetKey(string fieldName, KeyCode key)
    {
        settings.keyBindings.Set(fieldName, key);
        SaveSettings();
        ApplyKeyBindings();
    }

    public void ResetKeyBindings()
    {
        var template = Resources.Load<InputConfig>("Configs/InputConfig");
        if (template == null) template = Resources.Load<InputConfig>("InputConfig");

        if (template != null) settings.keyBindings = template.ToKeyBindings();
        else settings.keyBindings = new KeyBindings();

        SaveSettings();
        ApplyKeyBindings();
    }

    public void ApplyKeyBindings()
    {
        if (settings.keyBindings != null)
            OnKeyBindingsChanged?.Invoke(settings.keyBindings);
    }

    public void SetLanguage(string language) { settings.language = language; SaveSettings(); }
    public void SetDifficulty(string difficulty) { settings.difficulty = difficulty; SaveSettings(); }
    public void SetAutosave(bool autosave) { settings.autosave = autosave; SaveSettings(); }
    public void SetDialogueTextSpeed(float speed) { settings.dialogueTextSpeed = Mathf.Clamp(speed, 0.01f, 0.15f); SaveSettings(); }
    public void SetAutoDialogue(bool value) { settings.autoDialogue = value; SaveSettings(); }
    public void SetAutoDialogueDelay(float delay) { settings.autoDialogueDelay = Mathf.Clamp(delay, 0.5f, 10f); SaveSettings(); }
    public void SetShowTooltips(bool value) { settings.showTooltips = value; SaveSettings(); }

    public string GetLanguageCode()
    {
        if (settings == null || string.IsNullOrEmpty(settings.language)) return "ru";

        switch (settings.language.ToLowerInvariant())
        {
            case "english": return "en";
            case "deutsch": return "de";
            case "русский": return "ru";
            default: return "ru";
        }
    }

    private void RebuildRegistry()
    {
        registeredTexts.Clear();
        registeredScalers.Clear();
        baseFontSizes.Clear();
        baseScalerSizes.Clear();

        var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include);
        foreach (var t in texts)
        {
            if (t == null) continue;
            registeredTexts.Add(t);
            baseFontSizes[t] = t.fontSize;
        }

        var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include);
        foreach (var s in scalers)
        {
            if (s == null) continue;
            registeredScalers.Add(s);
            baseScalerSizes[s] = s.referenceResolution;
        }

        registryDirty = false;
    }

    private void ApplyUIFontScale()
    {
        if (registryDirty || registeredTexts.Count == 0) RebuildRegistry();

        for (int i = registeredTexts.Count - 1; i >= 0; i--)
        {
            var t = registeredTexts[i];
            if (t == null) { registeredTexts.RemoveAt(i); continue; }
            if (!baseFontSizes.ContainsKey(t)) baseFontSizes[t] = t.fontSize;
            t.fontSize = baseFontSizes[t] * settings.uiFontScale;
        }
    }

    private void ApplyUIScale()
    {
        if (registryDirty || registeredScalers.Count == 0) RebuildRegistry();

        for (int i = registeredScalers.Count - 1; i >= 0; i--)
        {
            var s = registeredScalers[i];
            if (s == null) { registeredScalers.RemoveAt(i); continue; }
            if (!baseScalerSizes.ContainsKey(s)) baseScalerSizes[s] = s.referenceResolution;

            Vector2 baseRes = baseScalerSizes[s];
            s.referenceResolution = new Vector2(baseRes.x / settings.uiScale, baseRes.y / settings.uiScale);
        }
    }

    private void ApplyColorBlind()
    {
        if (_colorBlindFilter != null) _colorBlindFilter.SetMode(settings.colorBlindMode);
    }

    [ContextMenu("Open Settings Folder")]
    public void OpenSettingsFolder()
    {
        string folder = Path.GetDirectoryName(settingsPath);
        if (!string.IsNullOrEmpty(folder) && Directory.Exists(folder))
        {
#if UNITY_EDITOR
            UnityEditor.EditorUtility.RevealInFinder(folder);
#else
            System.Diagnostics.Process.Start(folder);
#endif
        }
    }

    [ContextMenu("Delete Settings File")]
    public void DeleteSettingsFile()
    {
        if (File.Exists(settingsPath))
        {
            File.Delete(settingsPath);
            settings = new SettingsData();
            SaveSettings();
        }
    }
}