using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private TMP_Text speakerNameText;
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private GameObject speakerPanel;
    [SerializeField] private GameObject choicePanel;
    [SerializeField] private GameObject panelDialogue;
    [SerializeField] private Button continueButton;
    [SerializeField] private RadialMenu radialMenu;

    [Header("Panel Dialogue Canvas Group")]
    [SerializeField] private CanvasGroup panelDialogueCG;

    private void Awake()
    {
        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null)
            {
                panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
                Logger.Log(LogModule.Dialogue, "CanvasGroup добавлен на PanelDialogue");
            }
        }

        SetPanelDialogueActive(false);
        Logger.Log(LogModule.Dialogue, "DialogueUI инициализирован");
    }

    #region Panel Dialogue (CanvasGroup)

    public void SetPanelDialogueActive(bool active)
    {
        if (panelDialogueCG == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "panelDialogueCG не найден");
            return;
        }

        if (active)
        {
            panelDialogueCG.alpha = 1f;
            panelDialogueCG.blocksRaycasts = true;
            panelDialogueCG.interactable = true;
            Logger.Log(LogModule.Dialogue, "Панель диалога показана");
        }
        else
        {
            panelDialogueCG.alpha = 0f;
            panelDialogueCG.blocksRaycasts = false;
            panelDialogueCG.interactable = false;
            Logger.Log(LogModule.Dialogue, "Панель диалога скрыта");
        }
    }

    public bool IsPanelDialogueActive()
    {
        return panelDialogueCG != null && panelDialogueCG.blocksRaycasts;
    }

    #endregion

    #region Dialogue Panel

    public void ShowDialoguePanel(bool show)
    {
        // Управление через PanelDialogue CanvasGroup
        // Оставляем для обратной совместимости
    }

    public bool IsDialoguePanelVisible()
    {
        return IsPanelDialogueActive();
    }

    #endregion

    #region Choice Panel

    public void ShowChoicePanel(bool show)
    {
        // Управление через PanelDialogue CanvasGroup
        // Оставляем для обратной совместимости
    }

    public bool IsChoicePanelVisible()
    {
        return IsPanelDialogueActive();
    }

    #endregion

    #region Dialogue Text

    public void SetDialogueText(string text, string speakerName)
    {
        if (dialogueText != null)
            dialogueText.text = text;
        else
            Logger.LogWarning(LogModule.Dialogue, "dialogueText не назначен");

        if (speakerNameText != null)
            speakerNameText.text = speakerName;
        else
            Logger.LogWarning(LogModule.Dialogue, "speakerNameText не назначен");

        Logger.Log(LogModule.Dialogue, $"Установлен текст диалога: {text?.Length ?? 0} символов, спикер: {speakerName}");
    }

    public void ClearDialogueText()
    {
        if (dialogueText != null)
            dialogueText.text = "";
        if (speakerNameText != null)
            speakerNameText.text = "";
        Logger.Log(LogModule.Dialogue, "Текст диалога очищен");
    }

    #endregion

    #region Choice Menu

    public void SetupChoices(string[] choices, System.Action<int> onChoiceSelected)
    {
        if (radialMenu != null)
        {
            var choicesList = new List<string>();
            foreach (string choice in choices)
            {
                if (!string.IsNullOrEmpty(choice))
                {
                    choicesList.Add(choice);
                }
            }
            radialMenu.SetChoiceMode(choicesList, onChoiceSelected);
            Logger.Log(LogModule.Dialogue, $"Настроено {choicesList.Count} вариантов выбора");
        }
        else
        {
            Logger.LogWarning(LogModule.Dialogue, "RadialMenu не назначен, настройка выбора невозможна");
        }
    }

    #endregion

    #region Continue Button

    public void SetContinueButtonEnabled(bool enabled)
    {
        if (continueButton != null)
        {
            continueButton.enabled = enabled;
            Logger.Log(LogModule.Dialogue, $"Кнопка продолжения установлена в состояние: {(enabled ? "включена" : "отключена")}");
        }
        else
        {
            Logger.LogWarning(LogModule.Dialogue, "continueButton не назначен");
        }
    }

    #endregion

    #region Getters

    public TMP_Text DialogueText => dialogueText;
    public TMP_Text SpeakerNameText => speakerNameText;
    public GameObject DialoguePanel => dialoguePanel;
    public GameObject SpeakerPanel => speakerPanel;
    public GameObject ChoicePanel => choicePanel;
    public GameObject PanelDialogue => panelDialogue;
    public Button ContinueButton => continueButton;
    public RadialMenu RadialMenu => radialMenu;

    #endregion

    #region Setup

    public void SetupReferences(
        TMP_Text dialogText,
        TMP_Text speakerText,
        GameObject dialogPanel,
        GameObject speakerPanel,
        GameObject choicePanel,
        GameObject panelDialogue,
        Button continueBtn,
        RadialMenu radialMenu)
    {
        this.dialogueText = dialogText;
        this.speakerNameText = speakerText;
        this.dialoguePanel = dialogPanel;
        this.speakerPanel = speakerPanel;
        this.choicePanel = choicePanel;
        this.panelDialogue = panelDialogue;
        this.continueButton = continueBtn;
        this.radialMenu = radialMenu;

        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null)
            {
                panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
            }
        }

        SetPanelDialogueActive(false);
        Logger.Log(LogModule.Dialogue, "DialogueUI настроен через SetupReferences");
    }

    #endregion
}
