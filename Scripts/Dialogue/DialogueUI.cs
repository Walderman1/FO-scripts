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
        // Получаем CanvasGroup только для PanelDialogue
        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null)
            {
                panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
            }
        }

        // Изначально PanelDialogue скрыт
        SetPanelDialogueActive(false);
    }

    #region Panel Dialogue (CanvasGroup) - ТОЛЬКО ЭТОТ МЕТОД УПРАВЛЯЕТ ВИДИМОСТЬЮ

    public void SetPanelDialogueActive(bool active)
    {
        if (panelDialogueCG == null) return;

        if (active)
        {
            panelDialogueCG.alpha = 1f;
            panelDialogueCG.blocksRaycasts = true;
            panelDialogueCG.interactable = true;
        }
        else
        {
            panelDialogueCG.alpha = 0f;
            panelDialogueCG.blocksRaycasts = false;
            panelDialogueCG.interactable = false;
        }
    }

    public bool IsPanelDialogueActive()
    {
        return panelDialogueCG != null && panelDialogueCG.blocksRaycasts;
    }

    #endregion

    #region Dialogue Panel - НЕ ИСПОЛЬЗУЕТ SetActive

    public void ShowDialoguePanel(bool show)
    {
        // Ничего не делаем - управление через PanelDialogue CanvasGroup
        // Оставляем для обратной совместимости
    }

    public bool IsDialoguePanelVisible()
    {
        return IsPanelDialogueActive();
    }

    #endregion

    #region Choice Panel - НЕ ИСПОЛЬЗУЕТ SetActive

    public void ShowChoicePanel(bool show)
    {
        // Ничего не делаем - управление через PanelDialogue CanvasGroup
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
        if (speakerNameText != null)
            speakerNameText.text = speakerName;
    }

    public void ClearDialogueText()
    {
        if (dialogueText != null)
            dialogueText.text = "";
        if (speakerNameText != null)
            speakerNameText.text = "";
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
        }
    }

    #endregion

    #region Continue Button

    public void SetContinueButtonEnabled(bool enabled)
    {
        if (continueButton != null)
            continueButton.enabled = enabled;
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

        // Получаем CanvasGroup только для PanelDialogue
        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null)
            {
                panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
            }
        }

        // Скрываем PanelDialogue
        SetPanelDialogueActive(false);
    }

    #endregion
}