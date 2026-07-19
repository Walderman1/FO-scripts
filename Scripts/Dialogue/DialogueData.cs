using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "DialogueConfig", menuName = "Dialogue/Dialogue Config")]
public class DialogueData : ScriptableObject
{
    [Header("📁 ПУТИ К ФАЙЛАМ")]
    public List<string> filePaths = new List<string>
    {
        "Texts/ReplicProlouge.txt",
        "Texts/Trixie.txt"
    };

    [Header("🏷️ ТЕГИ И ИМЕНА")]
    [Header("——— Панели ———")]
    public string dialoguePanelTag = "DialoguePanel";
    public string choicePanelTag = "ChoicePanel";
    public string speakerNamePanel = "SpeakerNamePanel";
    public string blackPanelTag = "BlackPanel";
    public string panelDialogueTag = "Dialogue";

    [Header("——— Персонажи ———")]
    public string firstCharactersTag = "FirstCharactersArea";
    public string secondCharactersTag = "SecondCharactersArea";
    public string characterTag = "Character";

    [Header("——— Локации ———")]
    public string worldTag = "World";
    public string locationReferenceTag = "LocationReference";

    [Header("——— Интерфейс ———")]
    public string buttonPanelName = "ButtonPanel";
    public string skipButtonName = "SkipButton";

    [Header("📌 МАРКЕРЫ ДИАЛОГА")]
    public string markerChoice = "choice";
    public string markerFinish = "finish";
    public string markerContinue = "continue";
    public string markerEnd = "end";
    public char dialogueSeparator = '#';

    [Header("👤 ПЕРСОНАЖИ")]
    public string charactersFolder = "Characters";
    public string charactersParentName = "Characters";

    [Header("⏭️ ПРОПУСК ДИАЛОГА")]
    public float skipDelay = 0.05f;
    public int maxSkipIterations = 100;
    public string skipButtonText = "Пропустить";
    public string skipButtonTextActive = "⏩";

    [Header("⚙️ ПОВЕДЕНИЕ")]
    public bool deleteReadLines = true;
    public bool autoShowSkipButton = true;

    [Header("🐞 ОТЛАДКА")]
    public bool enableDebugLogs = true;
    public bool logLoadedLines = false;

    // ============ МЕТОДЫ ============

    public string GetMarkerChoice() => markerChoice ?? "choice";
    public string GetMarkerFinish() => markerFinish ?? "finish";
    public string GetMarkerContinue() => markerContinue ?? "continue";
    public string GetMarkerEnd() => markerEnd ?? "end";
    public char GetDialogueSeparator() => dialogueSeparator != '\0' ? dialogueSeparator : '#';

    public bool IsValid()
    {
        if (string.IsNullOrEmpty(dialoguePanelTag))
        {
            Debug.LogError("DialoguePanelTag не может быть пустым!");
            return false;
        }
        if (string.IsNullOrEmpty(choicePanelTag))
        {
            Debug.LogError("ChoicePanelTag не может быть пустым!");
            return false;
        }
        return true;
    }

    public void ResetToDefaults()
    {
        markerChoice = "choice";
        markerFinish = "finish";
        markerContinue = "continue";
        markerEnd = "end";
        dialogueSeparator = '#';
        charactersFolder = "Characters";
        charactersParentName = "Characters";
        skipDelay = 0.05f;
        maxSkipIterations = 100;
        deleteReadLines = true;
        autoShowSkipButton = true;
        enableDebugLogs = true;
        skipButtonText = "Пропустить";
        skipButtonTextActive = "⏩";
    }

    public void LogSettings()
    {
        if (!enableDebugLogs) return;

        Debug.Log("=== НАСТРОЙКИ ДИАЛОГОВ ===");
        Debug.Log($"Файлов: {filePaths.Count}");
        Debug.Log($"Маркеры: choice='{markerChoice}', end='{markerEnd}'");
        Debug.Log($"Задержка пропуска: {skipDelay}с");
        Debug.Log($"Удалять прочитанные строки: {deleteReadLines}");
        Debug.Log("==========================");
    }

    // ============ КОНСТАНТЫ ============

    public static class Constants
    {
        public const string CHARACTERS_FOLDER = "Characters";
        public const string CHARACTERS_PARENT = "Characters";
        public const char DIALOGUE_SEPARATOR = '#';
        public const string MARKER_CHOICE = "choice";
        public const string MARKER_FINISH = "finish";
        public const string MARKER_CONTINUE = "continue";
        public const string MARKER_END = "end";
        public const string DEFAULT_SPEAKER = "???";
        public const string DEFAULT_BLACK_PANEL = "BlackPanel";
        public const string DEFAULT_DIALOGUE_PANEL = "DialoguePanel";
        public const string DEFAULT_CHOICE_PANEL = "ChoicePanel";
        public const string DEFAULT_SKIP_BUTTON_TEXT = "Пропустить";
        public const string DEFAULT_SKIP_BUTTON_ACTIVE = "⏩";
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (skipDelay < 0.01f) skipDelay = 0.01f;
        if (maxSkipIterations < 10) maxSkipIterations = 10;
        if (string.IsNullOrEmpty(markerChoice)) markerChoice = "choice";
        if (string.IsNullOrEmpty(markerEnd)) markerEnd = "end";
        if (string.IsNullOrEmpty(skipButtonText)) skipButtonText = "Пропустить";
    }
#endif
}