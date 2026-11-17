using UnityEngine;
using System;

public enum ColorBlindMode
{
    [InspectorName("Нет")] None,
    [InspectorName("Дейтеранопия")] Deuteranopia,
    [InspectorName("Протанопия")] Protanopia,
    [InspectorName("Тританопия")] Tritanopia
}

[Serializable]
public class SettingsData
{
    [Header("Графика")]
    public string resolution = "1920x1080";
    public bool fullscreen = true;
    public int qualityIndex = 2;
    public bool vsync = true;
    public int targetFps = 60;
    public bool showFps = false;
    public ColorBlindMode colorBlindMode = ColorBlindMode.None;
    public float uiScale = 1f;
    public float uiFontScale = 1f;

    [Header("Звук")]
    public float masterVolume = 0.8f;
    public float musicVolume = 0.8f;
    public float sfxVolume = 0.8f;
    public float voiceVolume = 0.8f;

    [Header("Управление")]
    public string controlScheme = "Стандартная";

    [Header("Клавиши")]
    public KeyBindings keyBindings = new KeyBindings();

    [Header("Игра")]
    public string language = "Русский";
    public string difficulty = "Средняя";
    public bool autosave = true;
    public float dialogueTextSpeed = 0.05f;
    public bool autoDialogue = false;
    public float autoDialogueDelay = 2f;
    public bool showTooltips = true;

    public SettingsData() { }
}