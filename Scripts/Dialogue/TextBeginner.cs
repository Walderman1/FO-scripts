using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextBeginner : MonoBehaviour
{
    #region Fields and Serialization

    [Header("Configuration (Auto-loaded from Resources/Configs/DialogueConfig)")]
    [SerializeField] private DialogueData dialogueConfig;

    [Header("UI")]
    [SerializeField] private DialogueUI dialogueUI;

    [Header("Character Manager")]
    [SerializeField] private DialogueCharacterManager characterManager;

    [Header("Location")]
    [SerializeField] private GameObject currentLocation;
    [SerializeField] private GameObject currentRegion;
    [SerializeField] private Animator blackPanelAnimator;

    [Header("Start Settings")]
    [SerializeField] private bool startDialogueOnAwake = false;
    [SerializeField] private int startFileIndex = 0;

    // Dialogue state
    private bool isInDialogue;
    public bool isChoosing;
    private bool hasDeleted;
    private int currentLineIndex;
    private string currentCharacterName;

    // Dialogue data
    private DialogueFileManager fileManager;

    private Button skipButton;
    private GameObject skipButtonGameObject;
    private CanvasGroup skipButtonCanvasGroup;
    private bool isSkipping;

    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        LoadConfig();

        // Инициализация менеджера файлов
        fileManager = new DialogueFileManager(dialogueConfig);
        fileManager.OnDialogueLoaded += OnFileLoaded;
        fileManager.OnError += OnFileError;

        // Поиск UI
        if (dialogueUI == null)
        {
            dialogueUI = GetComponentInChildren<DialogueUI>();
            if (dialogueUI == null)
            {
                dialogueUI = gameObject.AddComponent<DialogueUI>();
            }
        }

        // Поиск Character Manager
        if (characterManager == null)
        {
            characterManager = GetComponentInChildren<DialogueCharacterManager>();
            if (characterManager == null)
            {
                characterManager = gameObject.AddComponent<DialogueCharacterManager>();
            }
        }

        AutoFindComponents();

        // Инициализация Character Manager - передаем найденные области
        if (characterManager != null)
        {
            characterManager.Initialize(dialogueConfig);
            characterManager.SetCharacterAreas(FirstCharactersArea, SecondCharactersArea);
        }

        // Скрываем PanelDialogue через CanvasGroup при старте
        if (dialogueUI != null)
        {
            dialogueUI.SetPanelDialogueActive(false);
            dialogueUI.ClearDialogueText();
        }
        isInDialogue = false;
    }

    private void Start()
    {
        // Инициализируем пути к файлам ДО загрузки
        fileManager.InitializeFilePaths();

        // Проверяем, что пути загружены
        if (fileManager.GetFilePaths().Count == 0)
        {
            Debug.LogError("❌ Нет путей к файлам диалогов! Проверьте DialogueConfig.");
            return;
        }

        LoadDialogueFromFile(startFileIndex);

        // Убеждаемся, что PanelDialogue скрыт
        if (dialogueUI != null)
        {
            dialogueUI.SetPanelDialogueActive(false);
            dialogueUI.ClearDialogueText();
        }
        isInDialogue = false;

        UpdateCurrentLocation();
        SetupSkipButton();

        // Запускаем диалог только если включен флаг
        if (startDialogueOnAwake)
        {
            StartDialogueWithFile(startFileIndex);
        }

        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            dialogueConfig.LogSettings();
            fileManager.LogFileInfo();
            Debug.Log($"Start dialogue on awake: {startDialogueOnAwake}");
        }
    }

    private void Update()
    {
        UpdateSkipButtonVisibility();
    }

    private void OnDestroy()
    {
        if (fileManager != null)
        {
            fileManager.OnDialogueLoaded -= OnFileLoaded;
            fileManager.OnError -= OnFileError;
        }
    }

    #endregion

    #region Event Handlers

    private void OnFileLoaded()
    {
        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log("Dialogue file loaded successfully!");
        }
    }

    private void OnFileError(string error)
    {
        Debug.LogError($"Dialogue file error: {error}");
    }

    #endregion

    #region Config Loading

    private void LoadConfig()
    {
        if (dialogueConfig != null)
        {
            Debug.Log("DialogueConfig already assigned in inspector.");
            return;
        }

        dialogueConfig = Resources.Load<DialogueData>("Configs/DialogueConfig");

        if (dialogueConfig != null)
        {
            Debug.Log("DialogueConfig loaded from Resources/Configs/DialogueConfig");
        }
        else
        {
            dialogueConfig = Resources.Load<DialogueData>("DialogueConfig");

            if (dialogueConfig != null)
            {
                Debug.Log("DialogueConfig loaded from Resources/DialogueConfig");
            }
            else
            {
                Debug.LogError("DialogueConfig not found in Resources/Configs/ or Resources/!");
            }
        }
    }

    #endregion

    #region Skip Button

    private void SetupSkipButton()
    {
        string buttonPanelName = dialogueConfig != null ? dialogueConfig.buttonPanelName : "ButtonPanel";
        string skipName = dialogueConfig != null ? dialogueConfig.skipButtonName : "SkipButton";

        GameObject buttonPanel = GameObject.Find(buttonPanelName);
        if (buttonPanel == null) return;

        Transform skipButtonTransform = buttonPanel.transform.Find(skipName);
        if (skipButtonTransform == null) return;

        skipButtonGameObject = skipButtonTransform.gameObject;
        skipButton = skipButtonTransform.GetComponent<Button>();
        if (skipButton == null) return;

        // Добавляем CanvasGroup для управления видимостью
        skipButtonCanvasGroup = skipButtonGameObject.GetComponent<CanvasGroup>();
        if (skipButtonCanvasGroup == null)
        {
            skipButtonCanvasGroup = skipButtonGameObject.AddComponent<CanvasGroup>();
        }

        skipButton.enabled = true;

        skipButton.onClick.RemoveAllListeners();
        skipButton.onClick.AddListener(StartSkip);

        // Скрываем через CanvasGroup, а не SetActive
        skipButtonCanvasGroup.alpha = 0f;
        skipButtonCanvasGroup.blocksRaycasts = false;
        skipButtonCanvasGroup.interactable = false;

        if (dialogueConfig != null && !string.IsNullOrEmpty(dialogueConfig.skipButtonText))
        {
            var text = skipButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = dialogueConfig.skipButtonText;
        }
    }

    private void UpdateSkipButtonVisibility()
    {
        if (skipButtonGameObject == null || skipButton == null || skipButtonCanvasGroup == null) return;

        bool shouldBeVisible = isInDialogue && !isChoosing && !isSkipping;

        if (dialogueConfig != null && !dialogueConfig.autoShowSkipButton)
        {
            shouldBeVisible = false;
        }

        // Управляем через CanvasGroup
        skipButtonCanvasGroup.alpha = shouldBeVisible ? 1f : 0f;
        skipButtonCanvasGroup.blocksRaycasts = shouldBeVisible;
        skipButtonCanvasGroup.interactable = shouldBeVisible;

        if (shouldBeVisible)
        {
            skipButton.enabled = true;
        }
    }

    private void StartSkip()
    {
        if (!isInDialogue || isChoosing || isSkipping) return;
        StartCoroutine(SkipDialogueCoroutine());
    }

    private IEnumerator SkipDialogueCoroutine()
    {
        isSkipping = true;

        // Скрываем кнопку через CanvasGroup во время пропуска
        if (skipButtonCanvasGroup != null)
        {
            skipButtonCanvasGroup.alpha = 0f;
            skipButtonCanvasGroup.blocksRaycasts = false;
            skipButtonCanvasGroup.interactable = false;
        }

        if (skipButton != null)
        {
            skipButton.enabled = true;
        }

        if (skipButton != null && dialogueConfig != null && !string.IsNullOrEmpty(dialogueConfig.skipButtonTextActive))
        {
            var text = skipButton.GetComponentInChildren<TMP_Text>();
            if (text != null) text.text = dialogueConfig.skipButtonTextActive;
        }

        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log($"=== SKIP START: currentLineIndex = {currentLineIndex} ===");
        }

        int maxIterations = dialogueConfig != null ? dialogueConfig.maxSkipIterations : 100;
        int iterations = 0;
        int lastIndex = -1;
        int stuckCounter = 0;

        while (isInDialogue && !isChoosing && iterations < maxIterations)
        {
            iterations++;

            if (currentLineIndex >= fileManager.LineCount)
            {
                EndDialogue();
                break;
            }

            if (currentLineIndex == lastIndex)
            {
                stuckCounter++;
                if (stuckCounter > 5)
                {
                    if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
                    {
                        Debug.LogWarning("Skip stuck, forcing end");
                    }
                    EndDialogue();
                    break;
                }
            }
            else
            {
                stuckCounter = 0;
                lastIndex = currentLineIndex;
            }

            int indexBefore = currentLineIndex;
            OnTap();

            if (isChoosing)
            {
                if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
                {
                    Debug.Log("SKIP: Choice menu appeared, stopping skip");
                }
                break;
            }

            if (!isInDialogue)
            {
                if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
                {
                    Debug.Log("SKIP: Dialogue ended");
                }
                break;
            }

            if (currentLineIndex == indexBefore && !isChoosing)
            {
                if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
                {
                    Debug.Log("SKIP: Index didn't change, ending dialogue");
                }
                EndDialogue();
                break;
            }

            float delay = dialogueConfig != null ? dialogueConfig.skipDelay : 0.05f;
            yield return new WaitForSeconds(delay);
        }

        // Восстановление состояния кнопки
        if (skipButton != null)
        {
            var text = skipButton.GetComponentInChildren<TMP_Text>();
            if (text != null && dialogueConfig != null && !string.IsNullOrEmpty(dialogueConfig.skipButtonText))
            {
                text.text = dialogueConfig.skipButtonText;
            }

            skipButton.enabled = true;
        }

        isSkipping = false;

        // Обновляем видимость кнопки
        UpdateSkipButtonVisibility();

        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log($"=== SKIP END: currentLineIndex = {currentLineIndex} ===");
        }
    }

    public void ResetSkipButtonState()
    {
        isSkipping = false;

        if (skipButton != null)
        {
            skipButton.enabled = true;

            var text = skipButton.GetComponentInChildren<TMP_Text>();
            if (text != null && dialogueConfig != null && !string.IsNullOrEmpty(dialogueConfig.skipButtonText))
            {
                text.text = dialogueConfig.skipButtonText;
            }

            // Скрываем через CanvasGroup
            if (skipButtonCanvasGroup != null)
            {
                skipButtonCanvasGroup.alpha = 0f;
                skipButtonCanvasGroup.blocksRaycasts = false;
                skipButtonCanvasGroup.interactable = false;
            }

            skipButton.onClick.RemoveAllListeners();
            skipButton.onClick.AddListener(StartSkip);
        }

        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log("Skip button state reset");
        }
    }

    #endregion

    #region Automatic Component Finding

    private GameObject FirstCharactersArea;
    private GameObject SecondCharactersArea;

    private void AutoFindComponents()
    {
        FindPanelComponents();
        ValidateComponents();
    }

    private void FindPanelComponents()
    {
        string dPanelTag = dialogueConfig != null ? dialogueConfig.dialoguePanelTag : "DialoguePanel";
        string cPanelTag = dialogueConfig != null ? dialogueConfig.choicePanelTag : "ChoicePanel";
        string sPanelTag = dialogueConfig != null ? dialogueConfig.speakerNamePanel : "SpeakerNamePanel";
        string fCharTag = dialogueConfig != null ? dialogueConfig.firstCharactersTag : "FirstCharactersTag";
        string sCharTag = dialogueConfig != null ? dialogueConfig.secondCharactersTag : "SecondCharactersTag";
        string pDialTag = dialogueConfig != null ? dialogueConfig.panelDialogueTag : "Dialogue";

        GameObject dialoguePanel = FindByTagOrName(dPanelTag, "DialoguePanel");
        GameObject choicePanel = FindByTagOrName(cPanelTag, "ChoicePanel");
        GameObject speakerPanel = FindByTagOrName(sPanelTag, "SpeakerNamePanel");
        FirstCharactersArea = FindByTagOrName(fCharTag, "FirstCharactersTag");
        SecondCharactersArea = FindByTagOrName(sCharTag, "SecondCharactersTag");
        GameObject PanelDialogue = FindByTagOrName(pDialTag, "Dialogue");

        TMP_Text dialogueText = null;
        TMP_Text speakerNameText = null;
        Button continueButton = null;
        RadialMenu radialMenu = null;

        if (dialoguePanel != null)
        {
            dialogueText = dialoguePanel.GetComponentInChildren<TMP_Text>();
            continueButton = dialoguePanel.GetComponentInChildren<Button>();
        }
        if (speakerPanel != null)
        {
            speakerNameText = speakerPanel.GetComponentInChildren<TMP_Text>();
        }
        if (choicePanel != null && choicePanel.TryGetComponent(out RadialMenu rm))
        {
            radialMenu = rm;
            radialMenu.textBeginner = this;
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetupReferences(
                dialogueText,
                speakerNameText,
                dialoguePanel,
                speakerPanel,
                choicePanel,
                PanelDialogue,
                continueButton,
                radialMenu
            );
        }

        if (blackPanelAnimator == null)
        {
            string blackTag = dialogueConfig != null ? dialogueConfig.blackPanelTag : "BlackPanel";
            var blackPanel = FindByTagOrName(blackTag, "BlackPanel");
            if (blackPanel != null)
            {
                blackPanelAnimator = blackPanel.GetComponent<Animator>();
            }
        }
    }

    private GameObject FindByTagOrName(string tag, string name)
    {
        GameObject obj = null;

        try
        {
            obj = GameObject.FindGameObjectWithTag(tag);
        }
        catch { }

        if (obj == null)
        {
            obj = GameObject.Find(name);
        }

        if (obj == null && dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.LogWarning($"Component not found: {tag}/{name}");
        }
        return obj;
    }

    private void ValidateComponents()
    {
        if (dialogueUI == null || dialogueUI.DialoguePanel == null)
        {
            Debug.LogError("DialoguePanel not found! Make sure it has tag 'DialoguePanel'");
        }
        if (dialogueUI == null || dialogueUI.DialogueText == null)
        {
            Debug.LogError("DialogueText not found inside DialoguePanel");
        }
        if (dialogueUI == null || dialogueUI.ChoicePanel == null)
        {
            Debug.LogError("ChoicePanel not found! Make sure it has tag 'ChoicePanel'");
        }
    }

    #endregion

    #region Initialization

    public void StartNewGame()
    {
        if (characterManager != null)
        {
            characterManager.ClearAllCharacters();
        }

        fileManager.InitializeFilePaths();
        LoadDialogueFromFile(0);
        UpdateCurrentLocation();
        isSkipping = false;
        ResetSkipButtonState();
    }

    #endregion

    #region Public Methods (UI Callbacks)

    public void OnTap()
    {
        if (isInDialogue)
        {
            if (currentCharacterName == null)
            {
                SetCurrentCharacter(characterManager != null && characterManager.Characters.Count > 0
                    ? characterManager.Characters[0].name
                    : "Trixie");
            }
            if (characterManager != null && !characterManager.IsCharacterSpawned)
            {
                characterManager.ActivateCharacter(currentCharacterName);
            }
        }
        CheckForFinishFlag();

        if (IsChoiceLine())
        {
            ShowChoiceMenu();
            return;
        }
        if (IsDialogueEnded())
        {
            EndDialogue();
            return;
        }
        DisplayNextDialogueLine();
    }

    public void StartDialogueWithFile(int fileIndex)
    {
        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log($"🔍 StartDialogueWithFile вызван с индексом: {fileIndex}");
        }

        ResetSkipButtonState();
        isSkipping = false;

        MoveCharacterToSpeakerArea();

        if (fileManager.GetFilePaths().Count == 0)
        {
            fileManager.InitializeFilePaths();
        }

        LoadDialogueFromFile(fileIndex);

        // ✅ ПРИНУДИТЕЛЬНАЯ АКТИВАЦИЯ PanelDialogue
        if (dialogueUI != null)
        {
            dialogueUI.SetPanelDialogueActive(true);

            // Дополнительно: принудительно включаем объект с тегом Dialogue
            GameObject panelDialogue = GameObject.FindGameObjectWithTag("Dialogue");
            if (panelDialogue != null)
            {
                CanvasGroup cg = panelDialogue.GetComponent<CanvasGroup>();
                if (cg != null)
                {
                    cg.alpha = 1f;
                    cg.blocksRaycasts = true;
                    cg.interactable = true;
                    Debug.Log("✅ PanelDialogue принудительно активирован через GameObject.Find");
                }
            }

            Debug.Log("✅ PanelDialogue activated via CanvasGroup");
        }

        currentLineIndex = 0;
        isInDialogue = true;
        //OnTap();
    }

    public void MakeChoice(int choiceIndex)
    {
        isChoosing = false;
        string targetChoice = GetMarkerChoice() + choiceIndex.ToString();

        for (int i = 0; i < fileManager.LineCount; i++)
        {
            if (fileManager.GetLine(i) == targetChoice)
            {
                currentLineIndex = i + 1;
                if (dialogueUI != null)
                {
                    dialogueUI.SetContinueButtonEnabled(true);
                }

                string line = fileManager.GetLine(currentLineIndex);
                if (!string.IsNullOrEmpty(line) && dialogueUI != null)
                {
                    string[] parts = line.Split(GetDialogueSeparator());
                    if (parts.Length > 0)
                    {
                        dialogueUI.SetDialogueText(parts[0], parts.Length > 1 ? parts[1] : "");
                    }
                }

                if (dialogueUI != null)
                {
                    dialogueUI.SetPanelDialogueActive(true);
                }
                break;
            }
        }
    }

    public void SetCurrentCharacter(string characterName)
    {
        currentCharacterName = characterName;
        if (characterManager != null)
        {
            characterManager.SetCurrentCharacter(characterName);
        }
    }

    public void ToggleDialogue(int mode)
    {
        if (mode != 2 && characterManager != null)
        {
            characterManager.ToggleInteraction(currentCharacterName);
        }
        if (mode == 0)
        {
            if (dialogueUI != null)
            {
                dialogueUI.ClearDialogueText();
                dialogueUI.SetPanelDialogueActive(false);
            }
        }
        isInDialogue = dialogueUI != null && dialogueUI.IsPanelDialogueActive();
    }

    public void LoadAndContinue()
    {
        if (!isInDialogue)
        {
            ToggleDialogue(1);
        }
        currentLineIndex--;
        if (isInDialogue)
        {
            OnTap();
        }
        if (blackPanelAnimator != null)
        {
            blackPanelAnimator.SetTrigger("1");
        }
    }

    public void LoadDialogueFromFile(int fileIndex)
    {
        currentLineIndex = 0;
        fileManager.LoadDialogueFromFile(fileIndex);
    }

    #endregion

    #region Proxy Methods for CharacterManager

    public void MoveCharacterToCurrentScene()
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToCurrentScene();
        }
    }

    public void MoveCharacterToActiveLocationReference()
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToActiveLocationReference();
        }
    }

    public void MoveCharacterToSpeakerArea()
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToSpeakerArea();
        }
    }

    public void MoveCharacterToLocation(GameObject location)
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToLocation(location);
        }
    }

    public void MoveCharacterToPosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToPosition(position, rotation, scale);
        }
    }

    public void MoveCharacterToCurrentLocationReference()
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToCurrentLocationReference();
        }
    }

    public void ClearAllCharacters()
    {
        if (characterManager != null)
        {
            characterManager.ClearAllCharacters();
        }
    }

    public GameObject GetCurrentCharacter()
    {
        return characterManager != null ? characterManager.CurrentCharacter : null;
    }

    public bool IsCharacterSpawned()
    {
        return characterManager != null && characterManager.IsCharacterSpawned;
    }

    #endregion

    #region Helper Methods

    private char GetDialogueSeparator()
    {
        return dialogueConfig != null ? dialogueConfig.GetDialogueSeparator() : DialogueData.Constants.DIALOGUE_SEPARATOR;
    }

    private string GetMarkerChoice()
    {
        return dialogueConfig != null ? dialogueConfig.GetMarkerChoice() : DialogueData.Constants.MARKER_CHOICE;
    }

    private string GetMarkerFinish()
    {
        return dialogueConfig != null ? dialogueConfig.GetMarkerFinish() : DialogueData.Constants.MARKER_FINISH;
    }

    private string GetMarkerContinue()
    {
        return dialogueConfig != null ? dialogueConfig.GetMarkerContinue() : DialogueData.Constants.MARKER_CONTINUE;
    }

    private string GetMarkerEnd()
    {
        return dialogueConfig != null ? dialogueConfig.GetMarkerEnd() : DialogueData.Constants.MARKER_END;
    }

    #endregion

    #region Dialogue Logic

    public void ForceResetDialogue()
    {
        isInDialogue = false;
        isChoosing = false;
        isSkipping = false;

        if (dialogueUI != null)
        {
            dialogueUI.SetPanelDialogueActive(false);
            dialogueUI.ClearDialogueText();
        }

        ResetSkipButtonState();

        if (dialogueConfig != null && dialogueConfig.enableDebugLogs)
        {
            Debug.Log("ForceResetDialogue called - isInDialogue set to false");
        }
    }

    private void CheckForFinishFlag()
    {
        if (currentLineIndex + 1 >= fileManager.LineCount)
        {
            return;
        }
        if (fileManager.GetLine(currentLineIndex + 1) == GetMarkerFinish())
        {
            FindAndMoveToContinueLine();
        }
    }

    private void FindAndMoveToContinueLine()
    {
        string markerContinue = GetMarkerContinue();
        for (int i = 0; i < fileManager.LineCount; i++)
        {
            if (fileManager.GetLine(i) == markerContinue)
            {
                currentLineIndex = i + 1;
                if (!hasDeleted && dialogueConfig != null && dialogueConfig.deleteReadLines)
                {
                    fileManager.DeleteLines(currentLineIndex);
                    currentLineIndex = 0;
                    hasDeleted = true;
                }
                break;
            }
        }
    }

    private bool IsChoiceLine()
    {
        if (currentLineIndex >= fileManager.LineCount)
        {
            return false;
        }
        string[] parts = fileManager.GetLine(currentLineIndex).Split(GetDialogueSeparator());
        return parts.Length >= 2 && parts[parts.Length - 2] == GetMarkerChoice();
    }

    private bool IsDialogueEnded()
    {
        if (currentLineIndex >= fileManager.LineCount)
        {
            return true;
        }
        string[] parts = fileManager.GetLine(currentLineIndex).Split(GetDialogueSeparator());
        return parts.Length >= 1 && parts[parts.Length - 1] == GetMarkerEnd();
    }

    private void DisplayNextDialogueLine()
    {
        if (currentLineIndex >= fileManager.LineCount)
        {
            return;
        }

        string line = fileManager.GetLine(currentLineIndex);
        if (!string.IsNullOrEmpty(line) && dialogueUI != null)
        {
            string[] parts = line.Split(GetDialogueSeparator());
            string text = parts.Length > 0 ? parts[0] : "";
            string speaker = parts.Length > 1 ? parts[1] : "";
            dialogueUI.SetDialogueText(text, speaker);
        }
        currentLineIndex++;
    }

    #endregion

    #region UI Management

    private void ShowChoiceMenu()
    {
        isChoosing = true;
        hasDeleted = false;

        if (dialogueUI != null)
        {
            string line = fileManager.GetLine(currentLineIndex);
            if (!string.IsNullOrEmpty(line))
            {
                string[] choices = line.Split(GetDialogueSeparator());
                var choicesList = new List<string>();
                string markerChoice = GetMarkerChoice();
                foreach (string choice in choices)
                {
                    if (choice != markerChoice && !string.IsNullOrEmpty(choice))
                    {
                        choicesList.Add(choice);
                    }
                }

                dialogueUI.SetupChoices(choicesList.ToArray(), (int index) =>
                {
                    MakeChoice(index + 1);
                });
            }

            dialogueUI.SetPanelDialogueActive(true);
            dialogueUI.SetContinueButtonEnabled(false);
        }
    }

    private void EndDialogue()
    {
        if (characterManager != null)
        {
            characterManager.MoveCharacterToCurrentLocationReference();
        }

        if (dialogueUI != null)
        {
            dialogueUI.SetPanelDialogueActive(false);
            dialogueUI.ClearDialogueText();
            dialogueUI.SetContinueButtonEnabled(false);
        }

        isInDialogue = false;
        isChoosing = false;
        isSkipping = false;
        currentLineIndex = 0;
        hasDeleted = false;

        ResetSkipButtonState();

        Debug.Log("Dialogue ended - UI hidden");

        EventManager.Instance?.TriggerEvent(EventTriggerType.DialogueEnd,
            new EventContext().WithDialogue(currentCharacterName ?? "unknown"));
    }

    #endregion

    #region Utility Methods

    public void UpdateCurrentLocation()
    {
        Debug.Log("=== UpdateCurrentLocation START ===");

        string worldTag = dialogueConfig != null ? dialogueConfig.worldTag : "World";
        GameObject world = GameObject.FindGameObjectWithTag(worldTag);

        if (world == null)
        {
            world = GameObject.Find("World");
            if (world == null)
            {
                Debug.LogError("World not found! Check tag or name.");
                return;
            }
        }
        Debug.Log($"World found: {world.name}");

        currentRegion = FindActiveChildByTagOrName(world, "Region", "Region");
        if (currentRegion == null)
        {
            foreach (Transform child in world.transform)
            {
                if (child.CompareTag("Region") && child.gameObject.activeInHierarchy)
                {
                    currentRegion = child.gameObject;
                    Debug.Log($"Region found by direct search: {currentRegion.name}");
                    break;
                }
            }

            if (currentRegion == null)
            {
                Debug.LogWarning("No active Region found in World!");
            }
        }
        else
        {
            Debug.Log($"Region found: {currentRegion.name}");
        }

        if (currentRegion != null)
        {
            currentLocation = FindActiveChildByTagOrName(currentRegion, "Location", "Location");
            if (currentLocation == null)
            {
                foreach (Transform child in currentRegion.transform)
                {
                    if (child.CompareTag("Location") && child.gameObject.activeInHierarchy)
                    {
                        currentLocation = child.gameObject;
                        Debug.Log($"Location found by direct search: {currentLocation.name}");
                        break;
                    }
                }

                if (currentLocation == null)
                {
                    Debug.LogWarning($"No active Location found in Region: {currentRegion.name}");
                }
            }
            else
            {
                Debug.Log($"Location found: {currentLocation.name}");
            }
        }

        PrintLocationHierarchy();
        Debug.Log("=== UpdateCurrentLocation END ===");
    }

    private GameObject FindActiveChildByTagOrName(GameObject parent, string tag, string name)
    {
        if (parent == null) return null;

        foreach (Transform child in parent.transform)
        {
            if (child.CompareTag(tag) && child.gameObject.activeInHierarchy)
            {
                return child.gameObject;
            }

            if (child.name == name && child.gameObject.activeInHierarchy)
            {
                return child.gameObject;
            }
        }
        return null;
    }

    private void PrintLocationHierarchy()
    {
        Debug.Log("=== Текущая иерархия локаций ===");
        Debug.Log($"World: World");

        if (currentRegion != null)
            Debug.Log($"Region: {currentRegion.name}");
        else
            Debug.Log("Region: None");

        if (currentLocation != null)
            Debug.Log($"Location: {currentLocation.name}");
        else
            Debug.Log("Location: None");

        Debug.Log("=================================");
    }

    #endregion

    #region Public Properties

    public bool IsInDialogue => isInDialogue;
    public bool IsChoosing => isChoosing;
    public string CurrentCharacter => currentCharacterName;
    public GameObject CurrentLocation => currentLocation;
    public GameObject CurrentRegion => currentRegion;

    #endregion
}