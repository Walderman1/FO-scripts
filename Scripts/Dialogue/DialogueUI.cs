using DialogueSystem;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

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
    [SerializeField] private DialogueData dialogueConfig;

    [Header("Panel Dialogue Canvas Group")]
    [SerializeField] private CanvasGroup panelDialogueCG;

    [Header("Choice Button Prefab")]
    [SerializeField] private GameObject choiceButtonPrefab;
    private Stack<GameObject> choiceButtonPool = new Stack<GameObject>();
    [Inject] private DialogueData _injectedConfig;
    [Inject] private GameReferences _refs;
    [Inject] private RadialMenuRegistry _radialMenuRegistry;
    [Inject] private FlagManager _flagManager;
    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private void Awake()
    {
        if (dialogueConfig == null) dialogueConfig = _injectedConfig;

        AutoFindComponents();

        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null) panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
        }

        SetPanelDialogueActive(false);

        var refs = Refs;
        if (refs != null)
        {
            refs.OnSceneReferencesRefreshed -= HandleSceneRefreshed;
            refs.OnSceneReferencesRefreshed += HandleSceneRefreshed;
        }
    }

    private void AutoFindComponents()
    {
        if (panelDialogue == null)
        {
            panelDialogue = GameObject.FindGameObjectWithTag("Dialogue");
            if (panelDialogue == null)
                panelDialogue = GameObject.Find("Dialogue");
            if (panelDialogue != null)
                Logger.LogVerbose(LogModule.Dialogue, "PanelDialogue найден автоматически");
        }

        if (dialoguePanel == null)
        {
            dialoguePanel = GameObject.FindGameObjectWithTag("DialoguePanel");
            if (dialoguePanel == null)
                dialoguePanel = GameObject.Find("DialoguePanel");
            if (dialoguePanel != null)
                Logger.LogVerbose(LogModule.Dialogue, "DialoguePanel найден автоматически");
        }

        if (choicePanel == null)
        {
            choicePanel = GameObject.FindGameObjectWithTag("ChoicePanel");
            if (choicePanel == null)
                choicePanel = GameObject.Find("ChoicePanel");
            if (choicePanel == null && dialoguePanel != null)
            {
                Transform child = dialoguePanel.transform.Find("ChoicePanel");
                if (child != null)
                    choicePanel = child.gameObject;
            }
            if (choicePanel != null)
                Logger.LogVerbose(LogModule.Dialogue, "ChoicePanel найден автоматически");
        }

        if (choicePanel == null)
        {
            CreateChoicePanel();
        }

        if (speakerPanel == null)
        {
            speakerPanel = GameObject.FindGameObjectWithTag("SpeakerNamePanel");
            if (speakerPanel == null)
                speakerPanel = GameObject.Find("SpeakerNamePanel");
            if (speakerPanel != null)
                Logger.LogVerbose(LogModule.Dialogue, "SpeakerPanel найден автоматически");
        }

        if (dialogueText == null)
        {
            if (dialoguePanel != null)
                dialogueText = dialoguePanel.GetComponentInChildren<TMP_Text>();
            if (dialogueText == null)
                dialogueText = FindAnyObjectByType<TMP_Text>();
            if (dialogueText != null)
                Logger.LogVerbose(LogModule.Dialogue, "DialogueText найден автоматически");
        }

        if (speakerNameText == null)
        {
            if (speakerPanel != null)
                speakerNameText = speakerPanel.GetComponentInChildren<TMP_Text>();
            if (speakerNameText == null)
            {
                TMP_Text[] allTexts = FindObjectsByType<TMP_Text>();
                foreach (TMP_Text text in allTexts)
                {
                    if (text.gameObject.name == "SpeakerNameText" || text.gameObject.name == "SpeakerText")
                    {
                        speakerNameText = text;
                        break;
                    }
                }
            }
            if (speakerNameText != null)
                Logger.LogVerbose(LogModule.Dialogue, "SpeakerNameText найден автоматически");
        }

        if (continueButton == null)
        {
            if (dialoguePanel != null)
                continueButton = dialoguePanel.GetComponentInChildren<Button>();
            if (continueButton == null)
                continueButton = FindAnyObjectByType<Button>();
            if (continueButton != null)
                Logger.LogVerbose(LogModule.Dialogue, "ContinueButton найден автоматически");
        }

        if (radialMenu == null)
        {
            radialMenu = Refs?.ChoiceRadialMenu;
            if (radialMenu == null) radialMenu = FindChoiceRadialMenu();
        }

        if (choiceButtonPrefab == null)
        {
            choiceButtonPrefab = Resources.Load<GameObject>("UI/Prefab/Buttons/LongButton");
            if (choiceButtonPrefab == null)
                Logger.LogWarning(LogModule.Dialogue,
                    "ChoiceButtonPrefab не найден: Resources/UI/Prefab/Buttons/LongButton");
        }
    }

    private void HandleSceneRefreshed()
    {
        radialMenu = FindChoiceRadialMenu();
        if (radialMenu != null)
            Logger.LogVerbose(LogModule.Dialogue, $"HandleSceneRefreshed: ChoiceRadialMenu={radialMenu.name}");
        else
            Logger.LogWarning(LogModule.Dialogue, "HandleSceneRefreshed: ChoiceRadialMenu не найден");
    }

    // ★★★ Хелпер: ищет именно ChoicePanel, а не любой RadialMenu ★★★
    private RadialMenu FindChoiceRadialMenu()
    {
        if (_radialMenuRegistry != null && _radialMenuRegistry.ChoiceRadialMenu != null)
            return _radialMenuRegistry.ChoiceRadialMenu;

        try
        {
            var byTag = GameObject.FindGameObjectWithTag("ChoicePanel");
            if (byTag != null)
            {
                var menu = byTag.GetComponent<RadialMenu>();
                if (menu != null) return menu;
            }
        }
        catch (UnityException) { }

        var byName = GameObject.Find("ChoicePanel");
        if (byName != null)
        {
            var menu = byName.GetComponent<RadialMenu>();
            if (menu != null) return menu;
        }

        Logger.LogWarning(LogModule.Dialogue, "DialogueUI: ChoiceRadialMenu не найден — варианты будут показаны через DialogueUI.ShowChoices");
        return null;
    }

    private void OnDestroy()
    {
        var refs = Refs;
        if (refs != null) refs.OnSceneReferencesRefreshed -= HandleSceneRefreshed;
    }

    private void CreateChoicePanel()
    {
        GameObject parent = panelDialogue != null ? panelDialogue : (dialoguePanel != null ? dialoguePanel : gameObject);

        choicePanel = new GameObject("ChoicePanel");
        choicePanel.transform.SetParent(parent.transform);

        RectTransform rect = choicePanel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.1f, 0.05f);
        rect.anchorMax = new Vector2(0.9f, 0.4f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = choicePanel.AddComponent<VerticalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.spacing = 10f;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.childControlWidth = true;
        layout.childControlHeight = true;

        ContentSizeFitter fitter = choicePanel.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        CanvasGroup cg = choicePanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;

        choicePanel.SetActive(false);
        Logger.LogVerbose(LogModule.Dialogue, "ChoicePanel создан автоматически");
    }

    #region Panel Dialogue (CanvasGroup)

    public void SetPanelDialogueActive(bool active)
    {
        if (panelDialogueCG == null)
        {
            if (panelDialogue != null)
            {
                panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
                if (panelDialogueCG == null)
                    panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
            }
            else
            {
                panelDialogueCG = gameObject.GetComponent<CanvasGroup>();
                if (panelDialogueCG == null)
                    panelDialogueCG = gameObject.AddComponent<CanvasGroup>();
            }
        }

        if (panelDialogueCG == null) return;

        panelDialogueCG.alpha = active ? 1f : 0f;
        panelDialogueCG.blocksRaycasts = active;
        panelDialogueCG.interactable = active;
    }

    public bool IsPanelDialogueActive()
    {
        return panelDialogueCG != null && panelDialogueCG.blocksRaycasts;
    }

    #endregion

    #region Dialogue Panel

    public void ShowDialoguePanel() => SetPanelDialogueActive(true);

    public void HideDialoguePanel()
    {
        SetPanelDialogueActive(false);
        ClearDialogueText();
        ClearChoices();
    }

    public void ShowDialoguePanel(bool show) => SetPanelDialogueActive(show);

    public bool IsDialoguePanelVisible() => IsPanelDialogueActive();

    #endregion

    #region Choice Panel

    public void ShowChoicePanel(bool show)
    {
        if (choicePanel != null)
            choicePanel.SetActive(show);
    }

    public bool IsChoicePanelVisible()
    {
        return choicePanel != null && choicePanel.activeSelf;
    }

    #endregion

    #region Dialogue Text

    public void SetDialogueText(string text)
    {
        if (dialogueText != null)
            dialogueText.text = text;
    }

    public void SetDialogueText(string text, string speakerName)
    {
        if (dialogueText != null)
            dialogueText.text = text;
        if (speakerNameText != null)
            speakerNameText.text = speakerName;
    }

    public void UpdateSpeakerText(string speaker)
    {
        if (speakerNameText != null)
            speakerNameText.text = speaker;
    }

    public void AppendText(string text)
    {
        if (dialogueText != null)
            dialogueText.text += text;
    }

    public void ClearDialogueText()
    {
        if (dialogueText != null)
            dialogueText.text = "";
        if (speakerNameText != null)
            speakerNameText.text = "";
    }

    #endregion

    #region Choices

    public void ClearChoices()
    {
        if (choicePanel != null)
        {
            foreach (Transform child in choicePanel.transform)
            {
                if (child.gameObject.activeSelf)
                {
                    choiceButtonPool.Push(child.gameObject);
                    child.gameObject.SetActive(false);
                }
            }
            choicePanel.SetActive(false);
        }
    }

    public void ShowChoices(DialogueChoice[] choices, System.Action<DialogueChoice> onChoiceSelected)
    {
        if (choices == null || choices.Length == 0)
        {
            choicePanel?.SetActive(false);
            return;
        }

        if (choicePanel == null)
        {
            Logger.LogError(LogModule.Dialogue, "ChoicePanel is null, невозможно показать варианты выбора");
            return;
        }

        ClearChoices();
        EnsureChoicePanelReady();

        foreach (var choice in choices)
        {
            if (choice == null) continue;
            if (!string.IsNullOrEmpty(choice.condition) && !CheckCondition(choice.condition)) continue;

            var buttonObj = GetPooledChoiceButton();
            if (buttonObj == null) continue;

            buttonObj.transform.SetParent(choicePanel.transform, false);
            buttonObj.SetActive(true);

            var capturedChoice = choice;
            PrepareChoiceButton(buttonObj, choice.text, () =>
            {
                onChoiceSelected?.Invoke(capturedChoice);
                ClearChoices();
                choicePanel.SetActive(false);
            });
        }

        FinishChoices();
    }

    private GameObject GetPooledChoiceButton()
    {
        while (choiceButtonPool.Count > 0)
        {
            var pooled = choiceButtonPool.Pop();
            if (pooled != null)
            {
                pooled.SetActive(true);
                return pooled;
            }
        }

        if (choiceButtonPrefab != null)
            return Instantiate(choiceButtonPrefab, choicePanel.transform);

        return CreateDefaultChoiceButton();
    }

    private GameObject CreateDefaultChoiceButton()
    {
        GameObject buttonObj = new GameObject("ChoiceButton");

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(300, 45);

        Image image = buttonObj.AddComponent<Image>();
        image.color = new Color(0.2f, 0.2f, 0.3f, 0.9f);

        Button btn = buttonObj.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f, 0.9f);
        colors.highlightedColor = new Color(0.3f, 0.3f, 0.5f, 0.9f);
        colors.pressedColor = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        btn.colors = colors;

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform);
        TextMeshProUGUI tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = "Choice";
        tmp.fontSize = 24;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        RectTransform textRect = tmp.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        return buttonObj;
    }

    private void PrepareChoiceButton(GameObject buttonObj, string text, System.Action onClick)
    {
        if (buttonObj == null) return;

        Button btn = buttonObj.GetComponent<Button>();
        if (btn == null) btn = buttonObj.AddComponent<Button>();
        btn.onClick.RemoveAllListeners();
        btn.onClick.AddListener(() => onClick?.Invoke());

        TMP_Text tmp = buttonObj.GetComponentInChildren<TMP_Text>();
        if (tmp == null)
        {
            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(buttonObj.transform, false);
            tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 24;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            RectTransform textRect = tmp.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10, 5);
            textRect.offsetMax = new Vector2(-10, -5);
        }
        tmp.text = text;

        RectTransform btnRect = buttonObj.GetComponent<RectTransform>();
        if (btnRect == null) btnRect = buttonObj.AddComponent<RectTransform>();
        btnRect.sizeDelta = new Vector2(300, 45);
    }

    private void EnsureChoicePanelReady()
    {
        if (choicePanel == null) return;

        if (!choicePanel.activeSelf) choicePanel.SetActive(true);

        CanvasGroup cg = choicePanel.GetComponent<CanvasGroup>();
        if (cg == null) cg = choicePanel.AddComponent<CanvasGroup>();
        cg.alpha = 1f;
        cg.blocksRaycasts = true;
        cg.interactable = true;
    }

    private void FinishChoices()
    {
        if (choicePanel == null) return;
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicePanel.GetComponent<RectTransform>());
    }

    public void SetupChoices(string[] choices, System.Action<int> onChoiceSelected)
    {
        if (radialMenu != null)
        {
            var choicesList = new List<string>();
            foreach (var choice in choices)
            {
                if (!string.IsNullOrEmpty(choice)) choicesList.Add(choice);
            }
            radialMenu.SetChoiceMode(choicesList, onChoiceSelected);
            return;
        }

        if (choicePanel == null)
        {
            Logger.LogError(LogModule.Dialogue, "ChoicePanel is null, невозможно показать варианты выбора");
            return;
        }

        ClearChoices();

        if (choices == null || choices.Length == 0)
        {
            choicePanel.SetActive(false);
            return;
        }

        EnsureChoicePanelReady();

        for (int i = 0; i < choices.Length; i++)
        {
            if (string.IsNullOrEmpty(choices[i])) continue;

            var buttonObj = GetPooledChoiceButton();
            if (buttonObj == null) continue;

            buttonObj.transform.SetParent(choicePanel.transform, false);
            buttonObj.SetActive(true);

            int index = i;
            PrepareChoiceButton(buttonObj, choices[i], () =>
            {
                onChoiceSelected?.Invoke(index);
                ClearChoices();
                choicePanel.SetActive(false);
            });
        }

        FinishChoices();
    }

    private bool CheckCondition(string condition)
    {
        if (string.IsNullOrEmpty(condition)) return true;

        if (condition.StartsWith("Flag:"))
        {
            string flagName = condition.Substring(5);
            return _flagManager?.GetFlag(flagName) ?? false;
        }

        return true;
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

        if (panelDialogue != null)
        {
            panelDialogueCG = panelDialogue.GetComponent<CanvasGroup>();
            if (panelDialogueCG == null)
                panelDialogueCG = panelDialogue.AddComponent<CanvasGroup>();
        }

        SetPanelDialogueActive(false);
    }

    #endregion
}