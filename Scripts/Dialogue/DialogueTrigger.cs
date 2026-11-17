using UnityEngine;
using DialogueSystem;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using VContainer;

public class DialogueTrigger : MonoBehaviour
{
    [Header("Dialogue Settings")]
    [SerializeField] private string dialogueFileName = "Prologue";
    [SerializeField] private string alternativeDialogue = "";
    [SerializeField] private bool triggerOnStart = false;
    [SerializeField] private bool triggerOnCollision = false;
    [SerializeField] private bool triggerOnClick = false;
    [SerializeField] private bool destroyAfterTrigger = false;

    [Header("Dialogue Progression")]
    [SerializeField] private bool useDialogueChain = false;
    [SerializeField] private string nextDialogueOnComplete = "";
    [SerializeField] private bool markCompletedOnTrigger = true;

    [Header("Status Conditions")]
    [SerializeField] private bool onlyIfNotCompleted = false;
    [SerializeField] private bool onlyIfCompleted = false;
    [SerializeField] private bool allowRepeat = false;

    [Header("Conditions")]
    [SerializeField] private string requiredFlag = "";
    [SerializeField] private bool requiredFlagValue = true;

    [Header("Dialogue Pool")]
    [SerializeField] private bool useDialoguePool = false;
    [SerializeField] private string[] dialoguePool;
    [SerializeField] private bool randomFromPool = true;
    [SerializeField] private bool removeUsedFromPool = false;

    [Header("References")]
    [SerializeField] private TextBeginner textBeginner;
    [SerializeField] private DialogueData dialogueConfig;

    [Inject] private DialogueTracker _dialogueTracker;
    [Inject] private FlagManager _flagManager;
    [Inject] private TextBeginner _injectedTextBeginner;
    [Inject] private RadialMenuController _radialMenuController;
    [Inject] private QuestUI _questUI;
    [Inject] private InventoryUIManager _inventoryUIManager;
    [Inject] private CraftingUI _craftingUI;
    [Inject] private GlobalControl _globalControl;
    [Inject] private GameReferences _refs;

    private DialogueTracker Tracker => _dialogueTracker;
    private FlagManager Flags => _flagManager;

    private TextBeginner TB
    {
        get
        {
            if (textBeginner != null) return textBeginner;
            if (_injectedTextBeginner != null) return _injectedTextBeginner;

            var refs = GameReferences.Instance;
            if (refs != null && refs.TextBeginner != null)
                textBeginner = refs.TextBeginner;

            return textBeginner;
        }
    }

    public string DialogueFileName => dialogueFileName;
    public string AlternativeDialogue => alternativeDialogue;
    public bool UseDialogueChain => useDialogueChain;
    public bool OnlyIfNotCompleted => onlyIfNotCompleted;
    public bool OnlyIfCompleted => onlyIfCompleted;
    public bool AllowRepeat => allowRepeat;
    public bool HasTriggered => hasTriggered;

    private bool hasTriggered = false;
    private bool wasCompletedOnStart = false;
    private bool wasLockedOnStart = false;

    private void Start()
    {
        if (SceneManager.GetActiveScene().buildIndex == 0) return;

        if (dialogueConfig == null && _refs != null) dialogueConfig = _refs.DialogueConfig;

        if (!string.IsNullOrEmpty(dialogueFileName))
            Tracker?.RegisterDialogue(dialogueFileName);

        CheckDialogueStatusOnStart();

        if (triggerOnStart) TryTriggerDialogue();
    }

    private void CheckDialogueStatusOnStart()
    {
        if (string.IsNullOrEmpty(dialogueFileName)) return;
        var tracker = Tracker;
        if (tracker == null) return;

        wasCompletedOnStart = tracker.IsDialogueCompleted(dialogueFileName);
        wasLockedOnStart = tracker.IsDialogueLocked(dialogueFileName);

        if (wasCompletedOnStart && !allowRepeat && !onlyIfCompleted)
        {
            bool hasAlternative = !string.IsNullOrEmpty(alternativeDialogue);
            bool hasChain = useDialogueChain && !string.IsNullOrEmpty(tracker.GetNextDialogue(dialogueFileName));
            bool hasPool = useDialoguePool && dialoguePool != null && dialoguePool.Length > 0;
            if (!hasAlternative && !hasChain && !hasPool) hasTriggered = true;
        }

        if (wasLockedOnStart) hasTriggered = true;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!triggerOnCollision) return;
        if (collision.gameObject.CompareTag("Player")) TryTriggerDialogue();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!triggerOnCollision) return;
        if (other.CompareTag("Player")) TryTriggerDialogue();
    }

    private void OnMouseDown()
    {
        if (!triggerOnClick) return;
        if (IsBlockedByUI()) return;
        TryTriggerDialogue();
    }

    private bool IsBlockedByUI()
    {
        if (_injectedTextBeginner != null && _injectedTextBeginner.IsDialogueActive) return true;
        if (_radialMenuController != null && _radialMenuController.IsAnyMenuOpen()) return true;
        if (_questUI != null && _questUI.IsOpen) return true;
        if (_globalControl != null && _globalControl.isMenuOpen) return true;
        if (_craftingUI != null && _craftingUI.IsOpen) return true;
        if (_inventoryUIManager != null && _inventoryUIManager.IsInventoryOpen) return true;
        if (_refs != null && _refs.IsPointerOverAnyOpenPanel(Input.mousePosition)) return true;
        return false;
    }

    public void TriggerDialogue() => TryTriggerDialogue();

    private void TryTriggerDialogue()
    {
        if (hasTriggered) return;

        var tb = TB;
        if (tb == null)
        {
            Logger.LogVerbose(LogModule.Dialogue, $"DialogueTrigger '{gameObject.name}': TextBeginner не найден, триггер пропущен");
            return;
        }
        if (tb.IsDialogueActive) return;

        if (!string.IsNullOrEmpty(requiredFlag))
        {
            bool flagValue = Flags?.GetFlag(requiredFlag) ?? false;
            if (flagValue != requiredFlagValue) return;
        }

        var tracker = Tracker;
        if (tracker == null) return;

        bool isCompleted = tracker.IsDialogueCompleted(dialogueFileName);
        bool isLocked = tracker.IsDialogueLocked(dialogueFileName);

        if (isLocked) return;
        if (onlyIfNotCompleted && isCompleted) return;
        if (onlyIfCompleted && !isCompleted) return;

        if (isCompleted && !allowRepeat)
        {
            bool hasAlternative = !string.IsNullOrEmpty(alternativeDialogue);
            bool hasChain = useDialogueChain && !string.IsNullOrEmpty(tracker.GetNextDialogue(dialogueFileName));
            bool hasPool = useDialoguePool && dialoguePool != null && dialoguePool.Length > 0;
            if (!hasAlternative && !hasChain && !hasPool) { hasTriggered = true; return; }
        }

        string finalDialogue = GetDialogueToPlay();
        if (string.IsNullOrEmpty(finalDialogue)) return;

        tb.StartDialogue(finalDialogue);
        hasTriggered = true;

        if (markCompletedOnTrigger && !string.IsNullOrEmpty(dialogueFileName))
        {
            tracker.MarkDialogueCompleted(dialogueFileName);
            if (!string.IsNullOrEmpty(nextDialogueOnComplete))
                tracker.SetNextDialogue(dialogueFileName, nextDialogueOnComplete);
        }

        if (destroyAfterTrigger) Destroy(gameObject);
    }

    private string GetDialogueToPlay()
    {
        var tracker = Tracker;
        if (tracker == null) return dialogueFileName;

        if (useDialogueChain)
        {
            string next = tracker.GetNextDialogue(dialogueFileName);
            if (!string.IsNullOrEmpty(next) && !tracker.IsDialogueLocked(next)) return next;
        }

        bool mainCompleted = tracker.IsDialogueCompleted(dialogueFileName);
        if (mainCompleted && !string.IsNullOrEmpty(alternativeDialogue))
            if (!tracker.IsDialogueLocked(alternativeDialogue)) return alternativeDialogue;

        if (useDialoguePool && dialoguePool != null && dialoguePool.Length > 0)
        {
            List<string> available = new List<string>();
            foreach (string dlg in dialoguePool)
            {
                if (string.IsNullOrEmpty(dlg)) continue;
                if (tracker.IsDialogueLocked(dlg)) continue;
                if (!allowRepeat && tracker.IsDialogueCompleted(dlg)) continue;
                available.Add(dlg);
            }

            if (available.Count > 0)
            {
                string selected = randomFromPool ? available[Random.Range(0, available.Count)] : available[0];
                if (removeUsedFromPool) tracker.SetDialogueLock(selected, true);
                return selected;
            }
        }

        return dialogueFileName;
    }

    public void ResetTrigger()
    {
        hasTriggered = false;
        wasCompletedOnStart = false;
        wasLockedOnStart = false;
        CheckDialogueStatusOnStart();
    }

    public void SetDialogue(string newDialogue)
    {
        dialogueFileName = newDialogue;
        Tracker?.RegisterDialogue(newDialogue);
    }

    public bool IsDialogueAvailable()
    {
        var tracker = Tracker;
        if (!string.IsNullOrEmpty(requiredFlag))
        {
            bool flagValue = Flags?.GetFlag(requiredFlag) ?? false;
            if (flagValue != requiredFlagValue) return false;
        }
        if (tracker == null) return false;
        bool isCompleted = tracker.IsDialogueCompleted(dialogueFileName);
        if (onlyIfNotCompleted && isCompleted) return false;
        if (onlyIfCompleted && !isCompleted) return false;
        if (tracker.IsDialogueLocked(dialogueFileName)) return false;
        return true;
    }

    public void ForceTrigger(string dialogueOverride = "")
    {
        var tb = TB;
        if (tb == null) return;
        if (!string.IsNullOrEmpty(dialogueOverride)) tb.StartDialogue(dialogueOverride);
        else TryTriggerDialogue();
    }
}