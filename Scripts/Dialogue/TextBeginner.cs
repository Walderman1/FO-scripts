using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using DialogueSystem;
using UnityEngine.UI;
using VContainer;

namespace DialogueSystem
{
    public class TextBeginner : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private DialogueUI dialogueUI;
        [SerializeField] private DialogueFileManager fileManager;
        [SerializeField] private DialogueCharacterManager characterManager;
        [SerializeField] private RadialMenu radialMenu;
        [SerializeField] private Button skipButton;
        [SerializeField] private DialogueData dialogueConfig;

        [Header("Location")]
        [SerializeField] private GameObject currentLocation;
        [SerializeField] private GameObject currentRegion;

        [Header("Debug")]
        [SerializeField] private string newGameEventID = "event_novaya_igra";
        [SerializeField] private bool debugSpawnCharacterAfterEvent = true;

        [Header("Hold Skip (Ctrl)")]
        [Range(0.01f, 2f)]
        [SerializeField] private float holdSkipInterval = 0.15f;

        [Inject] private DialogueTracker _dialogueTracker;
        [Inject] private FlagManager _flagManager;
        [Inject] private QuestManager _questManager;
        [Inject] private EventStateManager _eventStateManager;
        [Inject] private SaveSystem _saveSystem;
        [Inject] private InventoryUIManager _inventoryUIManager;
        [Inject] private EventManager _eventManager;

        private DialogueNode[] currentDialogue;
        private DialogueNode currentNode;
        private bool isDialogueActive = false;
        private Coroutine typeCoroutine;
        private Coroutine skipCoroutine;
        private bool isSkipping = false;

        private Coroutine holdSkipCoroutine;
        private bool holdSkipRequested = false;

        private enum DialogueState { Idle, Typing, Showing, Choosing }
        private DialogueState state = DialogueState.Idle;

        public bool IsDialogueActive => isDialogueActive;
        public bool IsChoosing => state == DialogueState.Choosing;
        public bool IsInDialogue => isDialogueActive;
        public GameObject CurrentLocation => currentLocation;
        public GameObject CurrentRegion => currentRegion;

        private DialogueChoice[] currentChoices;
        private string currentDialogueFileName;

        private void Awake()
        {
            isDialogueActive = false;
            state = DialogueState.Idle;
        }

        private void Start()
        {
            FindAllManagers();

            if (dialogueUI != null) dialogueUI.HideDialoguePanel();

            UpdateCurrentLocation();
            FindSkipButton();

            if (skipButton == null) StartCoroutine(FindSkipButtonDelayed());

            if (debugSpawnCharacterAfterEvent && Application.isEditor)
                StartCoroutine(DebugSpawnCharacterRoutine());
        }

        // ★★★ ГЛАВНЫЙ ХЕЛПЕР: ищет ИМЕННО ChoiceRadialMenu, а не любой RadialMenu ★★★
        private RadialMenu FindChoiceRadialMenu()
        {
            var choice = GameReferences.Instance?.ChoiceRadialMenu;
            if (choice != null) return choice;

            try
            {
                var scope = SceneInjector.Instance?.CurrentSceneScope;
                if (scope != null)
                {
                    var registry = scope.Resolve<RadialMenuRegistry>();
                    if (registry != null && registry.ChoiceRadialMenu != null)
                        return registry.ChoiceRadialMenu;
                }
            }
            catch { }

            try
            {
                var byTag = GameObject.FindGameObjectWithTag("ChoicePanel");
                if (byTag != null)
                {
                    var menu = byTag.GetComponent<RadialMenu>();
                    if (menu != null)
                    {
                        Logger.LogVerbose(LogModule.Dialogue, $"ChoiceRadialMenu найден по тегу: {menu.name}");
                        return menu;
                    }
                }
            }
            catch (UnityException) { }

            var byName = GameObject.Find("ChoicePanel");
            if (byName != null)
            {
                var menu = byName.GetComponent<RadialMenu>();
                if (menu != null)
                {
                    Logger.LogVerbose(LogModule.Dialogue, $"ChoiceRadialMenu найден по имени: {menu.name}");
                    return menu;
                }
            }

            Logger.LogWarning(LogModule.Dialogue, "ChoiceRadialMenu не найден — варианты будут показаны через DialogueUI");
            return null;
        }

        private IEnumerator FindSkipButtonDelayed()
        {
            yield return new WaitForSeconds(0.5f);
            FindSkipButton();

            if (skipButton == null)
            {
                yield return new WaitForSeconds(1f);
                FindSkipButton();
            }
        }

        private void FindSkipButton()
        {
            if (skipButton != null) return;

            string skipName = dialogueConfig != null ? dialogueConfig.SkipButtonName : "SkipButton";
            GameObject skipGO = GameObject.Find(skipName);
            if (skipGO != null)
            {
                skipButton = skipGO.GetComponent<Button>();
                if (skipButton != null)
                {
                    skipButton.onClick.RemoveAllListeners();
                    skipButton.onClick.AddListener(StartSkip);
                    skipButton.gameObject.SetActive(false);
                    return;
                }
            }

            Button[] allButtons = FindObjectsByType<Button>();
            foreach (Button btn in allButtons)
            {
                if (btn.name == skipName)
                {
                    skipButton = btn;
                    skipButton.onClick.RemoveAllListeners();
                    skipButton.onClick.AddListener(StartSkip);
                    skipButton.gameObject.SetActive(false);
                    return;
                }
            }
        }

        private void FindAllManagers()
        {
            if (dialogueUI == null) dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
            if (fileManager == null) fileManager = FindAnyObjectByType<DialogueFileManager>(FindObjectsInactive.Include);

            if (characterManager == null)
            {
                characterManager = FindAnyObjectByType<DialogueCharacterManager>(FindObjectsInactive.Include);
                if (characterManager == null) characterManager = gameObject.AddComponent<DialogueCharacterManager>();
            }

            if (radialMenu == null) radialMenu = FindChoiceRadialMenu();
            if (radialMenu != null) radialMenu.textBeginner = this;

            if (dialogueConfig == null) dialogueConfig = Resources.Load<DialogueData>("Configs/DialogueConfig");
        }

        private IEnumerator DebugSpawnCharacterRoutine()
        {
            yield return new WaitForSeconds(0.2f);

            if (isDialogueActive) yield break;
            if (characterManager == null) yield break;
            if (characterManager.CurrentCharacter != null) yield break;

            string defaultCharacter = dialogueConfig != null ? dialogueConfig.DefaultCharacterName : "";
            if (string.IsNullOrEmpty(defaultCharacter)) yield break;

            characterManager.ShowCharacter(defaultCharacter);
            characterManager.MoveCharacterToCurrentLocationReference();

            Logger.Log(LogModule.Dialogue, $"[DebugSpawn] Персонаж '{defaultCharacter}' заспавнен вручную");
        }

        private void Update()
        {
            if (!isDialogueActive) return;

            // Клики по внутриигровому окну DevTools не должны листать диалог.
            if (InGameDevTools.BlocksGameplayInput) return;

            if (state == DialogueState.Choosing) return;
            if (state == DialogueState.Idle) return;

            if (Input.GetMouseButtonDown(0)) HandleClick();

            if (skipButton != null)
            {
                bool shouldShow = isDialogueActive && state != DialogueState.Choosing && !isSkipping;
                if (skipButton.gameObject.activeSelf != shouldShow) skipButton.gameObject.SetActive(shouldShow);
            }
        }

        public void StartDialogue(string fileName)
        {
            UpdateCurrentLocation();

            if (skipButton == null)
            {
                FindSkipButton();
                if (skipButton == null) StartCoroutine(FindSkipButtonDelayed());
            }

            if (dialogueUI == null) dialogueUI = FindAnyObjectByType<DialogueUI>(FindObjectsInactive.Include);
            if (dialogueUI == null) { Logger.LogError(LogModule.Dialogue, "DialogueUI is null"); return; }

            if (fileManager == null) fileManager = FindAnyObjectByType<DialogueFileManager>(FindObjectsInactive.Include);
            if (fileManager == null) { Logger.LogError(LogModule.Dialogue, "DialogueFileManager is null"); return; }

            if (characterManager == null) characterManager = FindAnyObjectByType<DialogueCharacterManager>(FindObjectsInactive.Include);

            radialMenu = FindChoiceRadialMenu();
            if (radialMenu != null) radialMenu.textBeginner = this;

            if (characterManager != null)
            {
                string defaultCharacter = dialogueConfig != null ? dialogueConfig.DefaultCharacterName : "";
                if (!string.IsNullOrEmpty(defaultCharacter) && characterManager.CurrentCharacter == null)
                {
                    characterManager.ShowCharacter(defaultCharacter);
                    characterManager.SetPortrait(defaultCharacter);
                }
                characterManager.MoveCharacterToSpeakerArea();
            }

            currentDialogue = fileManager.LoadDialogue(fileName);
            currentDialogueFileName = fileName;
            if (currentDialogue == null) { Logger.LogError(LogModule.Dialogue, $"Failed to load dialogue: {fileName}"); return; }

            isDialogueActive = true;
            isSkipping = false;

            dialogueUI.ShowDialoguePanel();

            DialogueNode startNode = fileManager.GetNodeById(currentDialogue, "start");
            if (startNode == null) { EndDialogue(); return; }

            ShowNode(startNode);
        }

        private void ShowNode(DialogueNode node)
        {
            if (node == null) { EndDialogue(); return; }

            currentNode = node;
            state = DialogueState.Idle;

            if (dialogueUI != null) dialogueUI.ClearChoices();

            if (characterManager != null)
            {
                if (!string.IsNullOrEmpty(node.speaker))
                {
                    characterManager.ShowCharacter(node.speaker);
                    characterManager.SetPortrait(node.speaker);
                }
                characterManager.ProcessActions(node);
            }

            if (!string.IsNullOrEmpty(node.speaker) && dialogueUI != null)
                dialogueUI.UpdateSpeakerText(node.speaker);

            if (node.choices != null && node.choices.Length > 0)
            {
                state = DialogueState.Choosing;
                currentChoices = node.choices;
                ShowChoices(node.choices);
            }
            else if (!string.IsNullOrEmpty(node.text))
            {
                StartCoroutine(TypeText(node.text));
            }
            else if (!string.IsNullOrEmpty(node.nextNodeId))
            {
                DialogueNode nextNode = fileManager.GetNodeById(currentDialogue, node.nextNodeId);
                if (nextNode != null) ShowNode(nextNode);
                else EndDialogue();
            }
            else EndDialogue();
        }

        private void ShowChoices(DialogueChoice[] choices)
        {
            List<string> choiceTexts = new List<string>();
            foreach (DialogueChoice choice in choices)
            {
                if (!string.IsNullOrEmpty(choice.condition))
                    if (!CheckCondition(choice.condition)) continue;
                choiceTexts.Add(choice.text);
            }

            if (choiceTexts.Count == 0) { Logger.LogWarning(LogModule.Dialogue, "Нет доступных вариантов"); return; }

            // ★★★ ВСЕГДА берём ИМЕННО ChoicePanel через хелпер ★★★
            var choiceMenu = FindChoiceRadialMenu();
            if (choiceMenu != null)
            {
                radialMenu = choiceMenu;
                radialMenu.textBeginner = this;
                radialMenu.ClearButtons();
                radialMenu.SetChoiceMode(choiceTexts, OnChoiceMade);
                radialMenu.ShowAtCenter();

                CanvasGroup cg = radialMenu.GetComponent<CanvasGroup>();
                if (cg == null) cg = radialMenu.gameObject.AddComponent<CanvasGroup>();
                cg.alpha = 1f;
                cg.blocksRaycasts = true;
                cg.interactable = true;

                Logger.LogVerbose(LogModule.Dialogue, $"Выбор показан через ChoicePanel: {choiceMenu.name}");
            }
            else if (dialogueUI != null)
            {
                Logger.LogWarning(LogModule.Dialogue, "ChoiceRadialMenu не найден, fallback на DialogueUI.ShowChoices");
                dialogueUI.ShowChoices(choices, OnChoiceSelected);
            }
            else EndDialogue();
        }

        public void MakeChoice(int choiceIndex)
        {
            if (currentChoices == null || choiceIndex < 0 || choiceIndex >= currentChoices.Length) return;
            OnChoiceSelected(currentChoices[choiceIndex]);
        }

        private void OnChoiceMade(int choiceIndex) => MakeChoice(choiceIndex);

        private void OnChoiceSelected(DialogueChoice choice)
        {
            if (choice == null) return;

            if (radialMenu != null && radialMenu.IsVisible()) radialMenu.Hide();

            ProcessActions(currentNode?.actions);

            DialogueNode nextNode = fileManager.GetNodeById(currentDialogue, choice.nextNodeId);
            ShowNode(nextNode);
        }

        private float GetTextSpeed()
        {
            var sm = SceneInjector.Instance?.RootScope?.Resolve<SettingsManager>();
            if (sm != null && sm.Settings != null)
                return Mathf.Clamp(sm.Settings.dialogueTextSpeed, 0.01f, 0.15f);
            return dialogueConfig != null ? dialogueConfig.DefaultTextSpeed : 0.05f;
        }

        private bool IsAutoDialogueEnabled()
        {
            var sm = SceneInjector.Instance?.RootScope?.Resolve<SettingsManager>();
            return sm != null && sm.Settings != null && sm.Settings.autoDialogue;
        }

        private float GetAutoDialogueDelay()
        {
            var sm = SceneInjector.Instance?.RootScope?.Resolve<SettingsManager>();
            if (sm != null && sm.Settings != null) return Mathf.Clamp(sm.Settings.autoDialogueDelay, 0.5f, 10f);
            return 2f;
        }

        private IEnumerator AutoAdvanceTimer()
        {
            yield return new WaitForSeconds(GetAutoDialogueDelay());
            if (state == DialogueState.Showing) HandleClick();
        }

        private IEnumerator TypeText(string text)
        {
            if (typeCoroutine != null) StopCoroutine(typeCoroutine);

            if (dialogueUI != null) dialogueUI.SetDialogueText("");
            else yield break;

            state = DialogueState.Typing;
            char[] characters = text.ToCharArray();

            for (int i = 0; i < characters.Length; i++)
            {
                if (state == DialogueState.Choosing) { typeCoroutine = null; yield break; }

                if (state == DialogueState.Showing)
                {
                    if (dialogueUI != null) dialogueUI.SetDialogueText(text);
                    state = DialogueState.Showing;
                    typeCoroutine = null;

                    if (IsAutoDialogueEnabled()) StartCoroutine(AutoAdvanceTimer());

                    yield return new WaitUntil(() => state != DialogueState.Showing);
                    if (state == DialogueState.Choosing) yield break;
                    if (!isDialogueActive) yield break;

                    if (!string.IsNullOrEmpty(currentNode?.nextNodeId))
                        ShowNode(fileManager.GetNodeById(currentDialogue, currentNode.nextNodeId));
                    else EndDialogue();
                    yield break;
                }

                if (dialogueUI != null) dialogueUI.AppendText(characters[i].ToString());
                yield return new WaitForSeconds(GetTextSpeed());
            }

            state = DialogueState.Showing;
            typeCoroutine = null;

            if (IsAutoDialogueEnabled()) StartCoroutine(AutoAdvanceTimer());

            yield return new WaitUntil(() => state != DialogueState.Showing);
            if (state == DialogueState.Choosing) yield break;
            if (!isDialogueActive) yield break;

            if (!string.IsNullOrEmpty(currentNode?.nextNodeId))
                ShowNode(fileManager.GetNodeById(currentDialogue, currentNode.nextNodeId));
            else EndDialogue();
        }

        private int GetNodeId(DialogueNode node)
        {
            if (node == null || currentDialogue == null) return -1;
            for (int i = 0; i < currentDialogue.Length; i++)
                if (currentDialogue[i] == node) return i;
            return -1;
        }

        public void SetHoldSkip(bool active)
        {
            holdSkipRequested = active;

            if (active)
            {
                if (holdSkipCoroutine == null && !isSkipping && isDialogueActive && state != DialogueState.Choosing)
                    holdSkipCoroutine = StartCoroutine(HoldSkipCoroutine());
            }
            else
            {
                if (holdSkipCoroutine != null) { StopCoroutine(holdSkipCoroutine); holdSkipCoroutine = null; }
            }
        }

        private IEnumerator HoldSkipCoroutine()
        {
            while (holdSkipRequested && isDialogueActive && !isSkipping)
            {
                if (state == DialogueState.Choosing) break;

                if (state == DialogueState.Typing) { SkipTyping(); yield return null; continue; }
                if (state != DialogueState.Showing) { yield return null; continue; }

                float elapsed = 0f;
                while (elapsed < holdSkipInterval)
                {
                    if (!holdSkipRequested || !isDialogueActive || state != DialogueState.Showing) break;
                    elapsed += Time.unscaledDeltaTime;
                    yield return null;
                }

                if (!holdSkipRequested || !isDialogueActive) break;
                if (state != DialogueState.Showing) continue;

                HandleClick();
                yield return null;
            }
            holdSkipCoroutine = null;
        }

        public void StartSkip()
        {
            if (!isDialogueActive) return;
            if (state == DialogueState.Choosing) return;
            if (isSkipping) return;

            if (skipCoroutine != null) StopCoroutine(skipCoroutine);
            skipCoroutine = StartCoroutine(SkipDialogueCoroutine());
        }

        private IEnumerator SkipDialogueCoroutine()
        {
            isSkipping = true;
            int iterations = 0;
            int lastNodeId = -1;

            if (typeCoroutine != null)
            {
                StopCoroutine(typeCoroutine);
                typeCoroutine = null;
                if (dialogueUI != null && currentNode != null) dialogueUI.SetDialogueText(currentNode.text);
                state = DialogueState.Showing;
            }

            int maxSkip = dialogueConfig != null ? dialogueConfig.MaxSkipIterations : 100;
            float skipDelay = dialogueConfig != null ? dialogueConfig.SkipDelay : 0.1f;

            while (isDialogueActive && iterations < maxSkip)
            {
                iterations++;

                if (state == DialogueState.Choosing) break;

                if (state == DialogueState.Typing)
                {
                    if (typeCoroutine != null) { StopCoroutine(typeCoroutine); typeCoroutine = null; }
                    if (dialogueUI != null && currentNode != null) dialogueUI.SetDialogueText(currentNode.text);
                    state = DialogueState.Showing;
                }

                if (state == DialogueState.Showing)
                {
                    yield return new WaitForSeconds(skipDelay);
                    if (state == DialogueState.Choosing) break;

                    state = DialogueState.Idle;

                    if (!string.IsNullOrEmpty(currentNode?.nextNodeId))
                    {
                        DialogueNode nextNode = fileManager.GetNodeById(currentDialogue, currentNode.nextNodeId);
                        if (nextNode != null)
                        {
                            int currentNodeId = GetNodeId(currentNode);
                            ShowNode(nextNode);
                            if (state == DialogueState.Choosing) break;

                            int nextNodeId = GetNodeId(nextNode);
                            if (currentNodeId == lastNodeId && currentNodeId == nextNodeId) break;
                            lastNodeId = currentNodeId;
                        }
                        else { EndDialogue(); break; }
                    }
                    else { EndDialogue(); break; }
                }
                else break;
            }

            isSkipping = false;
            skipCoroutine = null;

            if (skipButton != null) skipButton.gameObject.SetActive(isDialogueActive && state != DialogueState.Choosing);
        }

        public void UpdateCurrentLocation()
        {
            string worldTag = dialogueConfig != null ? dialogueConfig.WorldTag : "World";
            GameObject world = GameObject.FindGameObjectWithTag(worldTag);

            if (world == null)
            {
                world = GameObject.Find(dialogueConfig != null ? dialogueConfig.WorldObjectName : "World");
                if (world == null) return;
            }

            currentRegion = FindActiveChildByTagOrName(world, dialogueConfig != null ? dialogueConfig.RegionTag : "Region", "Region");
            if (currentRegion == null)
            {
                foreach (Transform child in world.transform)
                {
                    if (child.CompareTag(dialogueConfig != null ? dialogueConfig.RegionTag : "Region") && child.gameObject.activeInHierarchy)
                    { currentRegion = child.gameObject; break; }
                }
            }

            if (currentRegion != null)
            {
                currentLocation = FindActiveChildByTagOrName(currentRegion, dialogueConfig != null ? dialogueConfig.LocationTag : "Location", "Location");
                if (currentLocation == null)
                {
                    foreach (Transform child in currentRegion.transform)
                    {
                        if (child.CompareTag(dialogueConfig != null ? dialogueConfig.LocationTag : "Location") && child.gameObject.activeInHierarchy)
                        { currentLocation = child.gameObject; break; }
                    }
                }
            }
        }

        private GameObject FindActiveChildByTagOrName(GameObject parent, string tag, string name)
        {
            if (parent == null) return null;
            foreach (Transform child in parent.transform)
            {
                if (child.CompareTag(tag) && child.gameObject.activeInHierarchy) return child.gameObject;
                if (child.name == name && child.gameObject.activeInHierarchy) return child.gameObject;
            }
            return null;
        }

        private void ProcessActions(DialogueAction[] actions)
        {
            if (actions == null) return;

            foreach (DialogueAction action in actions)
            {
                switch (action.type)
                {
                    case "SetFlag": _flagManager?.SetFlag(action.target, bool.Parse(action.value)); break;
                    case "StartQuest": _questManager?.StartQuest(action.target); break;
                    case "AddItem": AddItemByName(action.target); break;
                    case "CompleteQuest": _questManager?.CompleteQuest(action.target); break;
                    case "CompleteDialogue": _dialogueTracker?.MarkDialogueCompleted(action.target); break;
                    case "UnlockDialogue": _dialogueTracker?.SetDialogueLock(action.target, false); break;
                    case "LockDialogue": _dialogueTracker?.SetDialogueLock(action.target, true); break;
                    case "ResetDialogue": _dialogueTracker?.ResetDialogue(action.target); break;
                    case "SetNextDialogue": _dialogueTracker?.SetNextDialogue(action.target, action.value); break;
                    case "CheckDialogueStatus":
                        bool isCompleted = _dialogueTracker?.IsDialogueCompleted(action.target) ?? false;
                        if (!string.IsNullOrEmpty(action.value)) _flagManager?.SetFlag(action.value, isCompleted);
                        break;
                    default:
                        Logger.LogWarning(LogModule.Dialogue, $"Неизвестный тип действия: {action.type}");
                        break;
                }
            }
        }

        private void AddItemByName(string itemName)
        {
            if (string.IsNullOrEmpty(itemName)) return;
            if (_inventoryUIManager == null) return;

            var db = SceneInjector.Instance?.RootScope?.Resolve<ItemDatabase>();
            if (db == null) return;

            ItemSO so = db.GetItem(itemName);
            if (so == null) so = db.GetItemByName(itemName);

            if (so == null)
            {
                try
                {
#pragma warning disable CS0618
                    ItemType legacyType = (ItemType)System.Enum.Parse(typeof(ItemType), itemName, true);
                    so = db.GetItemByLegacyType(legacyType);
#pragma warning restore CS0618
                }
                catch (System.ArgumentException) { }
            }

            if (so == null) return;

            _inventoryUIManager.AddItem(so, 1);
        }

        private bool CheckCondition(string condition)
        {
            if (string.IsNullOrEmpty(condition)) return true;
            var tracker = _dialogueTracker;

            if (condition.StartsWith("Flag:"))
            {
                string flagName = condition.Substring(5);
                return _flagManager?.GetFlag(flagName) ?? false;
            }

            if (condition.StartsWith("DialogueCompleted:"))
                return tracker?.IsDialogueCompleted(condition.Substring("DialogueCompleted:".Length)) ?? false;

            if (condition.StartsWith("DialogueNotCompleted:"))
                return !(tracker?.IsDialogueCompleted(condition.Substring("DialogueNotCompleted:".Length)) ?? false);

            if (condition.StartsWith("DialogueUnlocked:"))
                return !(tracker?.IsDialogueLocked(condition.Substring("DialogueUnlocked:".Length)) ?? true);

            if (condition.StartsWith("DialoguePlayCount:"))
            {
                string[] parts = condition.Substring("DialoguePlayCount:".Length).Split('>');
                if (parts.Length == 2)
                {
                    string dialogueId = parts[0];
                    if (int.TryParse(parts[1], out int minCount))
                        return (tracker?.GetDialoguePlayCount(dialogueId) ?? 0) >= minCount;
                }
                return false;
            }

            return true;
        }

        public void EndDialogue()
        {
            if (!isDialogueActive && state == DialogueState.Idle) return;

            if (typeCoroutine != null) { StopCoroutine(typeCoroutine); typeCoroutine = null; }
            if (skipCoroutine != null) { StopCoroutine(skipCoroutine); skipCoroutine = null; }
            if (holdSkipCoroutine != null) { StopCoroutine(holdSkipCoroutine); holdSkipCoroutine = null; }

            holdSkipRequested = false;
            isSkipping = false;

            if (characterManager != null)
                characterManager.MoveCharacterToCurrentLocationReference();

            if (radialMenu != null && radialMenu.IsVisible()) radialMenu.Hide();

            string finishedDialogue = currentDialogueFileName;

            isDialogueActive = false;
            state = DialogueState.Idle;
            currentDialogue = null;
            currentNode = null;
            currentChoices = null;
            currentDialogueFileName = null;

            if (dialogueUI != null) dialogueUI.HideDialoguePanel();

            UpdateCurrentLocation();

            if (skipButton != null) skipButton.gameObject.SetActive(false);

            if (!string.IsNullOrEmpty(finishedDialogue))
            {
                _eventManager?.TriggerEvent(
                    EventTriggerType.DialogueEnd,
                    new EventContext().WithDialogue(finishedDialogue));
            }

            _saveSystem?.RequestAutoSave();
        }

        public void HandleClick()
        {
            switch (state)
            {
                case DialogueState.Typing: state = DialogueState.Showing; break;
                case DialogueState.Showing: state = DialogueState.Idle; break;
            }
        }

        public void SetCurrentCharacter(string characterName)
        {
            characterManager?.ShowCharacter(characterName);
        }

        public void SkipTyping()
        {
            if (state == DialogueState.Typing) state = DialogueState.Showing;
        }

        public void StartDialogueWithFile(int fileIndex)
        {
            StartDialogue(GetDialogueFileName(fileIndex));
        }

        private string GetDialogueFileName(int index)
        {
            switch (index)
            {
                case 0: return "Prologue";
                case 1: return "Trixie";
                default: return $"Dialogue_{index}";
            }
        }

        public bool IsDialogueCompleted(string dialogueId) => _dialogueTracker?.IsDialogueCompleted(dialogueId) ?? false;
        public int GetDialoguePlayCount(string dialogueId) => _dialogueTracker?.GetDialoguePlayCount(dialogueId) ?? 0;
        public void MarkDialogueCompleted(string dialogueId) => _dialogueTracker?.MarkDialogueCompleted(dialogueId);
        public void SetDialogueLock(string dialogueId, bool locked) => _dialogueTracker?.SetDialogueLock(dialogueId, locked);
        public void ResetDialogueStatus(string dialogueId) => _dialogueTracker?.ResetDialogue(dialogueId);
        public void SaveDialogueStatuses() => _dialogueTracker?.SaveStatuses();
        public void LoadDialogueStatuses() => _dialogueTracker?.LoadStatuses();
        public void DebugDialogueStatuses() => _dialogueTracker?.DebugLogAllStatuses();
    }
}