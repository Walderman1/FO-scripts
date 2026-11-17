using DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class QuestUI : MonoBehaviour
{
    [Header("Конфиг")]
    [SerializeField] private QuestConfigSO config;

    [Header("Панель")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Ссылки")]
    [SerializeField] private Transform questListContainer;
    [SerializeField] private Transform detailTransform;
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private TextMeshProUGUI detailTitle;
    [SerializeField] private TextMeshProUGUI detailDescription;
    [SerializeField] private TextMeshProUGUI detailProgressText;
    [SerializeField] private Transform detailObjectivesContainer;
    [SerializeField] private Button trackButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private TextMeshProUGUI trackButtonText;
    [SerializeField] private Transform tabsContainer;
    [SerializeField] private Button activeTabBtn;
    [SerializeField] private Button completedTabBtn;
    [SerializeField] private Button failedTabBtn;

    [Inject] private QuestManager _questManager;
    [Inject] private QuestNotifications _questNotifications;
    [Inject] private ItemDatabase _itemDatabase;
    [Inject] private GlobalControl _globalControl;
    [Inject] private QuestConfigSO _injectedConfig;
    [Inject] private IObjectResolver _resolver;

    private TextBeginner _textBeginner;

    public bool IsOpen => isOpen;
    private bool isOpen = false;
    private int currentTab = 0;
    private QuestInstance selectedQuest;
    private QuestInstance trackedQuest;
    private Coroutine fadeCoroutine;

    private GameObject questItemPrefab;
    private GameObject objectivePrefab;
    private GameObject cachedSlotPrefab;
    private Dictionary<string, GameObject> itemPrefabs = new Dictionary<string, GameObject>();
    private readonly Dictionary<string, GameObject> questItemsById = new Dictionary<string, GameObject>();
    private readonly Stack<GameObject> questItemPool = new Stack<GameObject>();
    private GameObject emptyMessageInstance;
    private CanvasGroup buttonPanelCg;
    private GameObject cancelConfirmOverlay;

    private QuestManager QM
    {
        get
        {
            if (_questManager != null) return _questManager;

            var refs = GameReferences.Instance;
            if (refs != null && refs.QuestManager != null)
                _questManager = refs.QuestManager;

            return _questManager;
        }
    }

    private QuestConfigSO Config
    {
        get
        {
            if (config != null) return config;
            if (_injectedConfig != null) { config = _injectedConfig; return config; }

            var refs = GameReferences.Instance;
            if (refs != null && refs.QuestConfig != null) { config = refs.QuestConfig; return config; }

            config = Resources.Load<QuestConfigSO>("Configs/QuestConfig");
            return config;
        }
    }

    private ItemDatabase Items
    {
        get
        {
            if (_itemDatabase != null) return _itemDatabase;
            var refs = GameReferences.Instance;
            if (refs != null && refs.ItemDatabase != null) _itemDatabase = refs.ItemDatabase;
            return _itemDatabase;
        }
    }

    private GlobalControl GC
    {
        get
        {
            if (_globalControl != null) return _globalControl;
            var refs = GameReferences.Instance;
            if (refs != null && refs.GlobalControl != null) _globalControl = refs.GlobalControl;
            return _globalControl;
        }
    }

    private void Awake()
    {
        FindQuestPanel();

        if (canvasGroup == null && questPanel != null)
            canvasGroup = questPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isOpen = false;

        if (questPanel != null) questPanel.SetActive(true);

        CacheComponents();
    }

    private void Start()
    {
        if (Config != null)
        {
            LoadPrefabs();
            LoadItemPrefabs();
        }
        else
        {
            Logger.LogWarning(LogModule.QuestSystem, "QuestUI: конфиг квестов не найден");
        }

        _textBeginner = null;
        if (_resolver != null && _resolver.TryResolve<TextBeginner>(out var tb))
            _textBeginner = tb;

        var qm = QM;
        if (qm != null)
        {
            qm.OnQuestStarted -= OnQuestStarted;
            qm.OnQuestStarted += OnQuestStarted;
            qm.OnQuestUpdated -= OnQuestUpdated;
            qm.OnQuestUpdated += OnQuestUpdated;
            qm.OnQuestCompleted -= OnQuestCompleted;
            qm.OnQuestCompleted += OnQuestCompleted;
            qm.OnObjectiveUpdated -= OnObjectiveUpdated;
            qm.OnObjectiveUpdated += OnObjectiveUpdated;
            qm.OnQuestListChanged -= HandleQuestListChanged;
            qm.OnQuestListChanged += HandleQuestListChanged;
        }
        else
        {
            Logger.LogWarning(LogModule.QuestSystem, "QuestUI: QuestManager не найден!");
        }

        SetupReferences();
        SetupButtons();
        if (isOpen) RefreshQuests();
    }

    private void OnDestroy()
    {
        if (_questManager != null)
        {
            _questManager.OnQuestStarted -= OnQuestStarted;
            _questManager.OnQuestUpdated -= OnQuestUpdated;
            _questManager.OnQuestCompleted -= OnQuestCompleted;
            _questManager.OnObjectiveUpdated -= OnObjectiveUpdated;
            _questManager.OnQuestListChanged -= HandleQuestListChanged;
        }
    }

    private void CacheComponents()
    {
        if (questPanel == null) return;

        if (questListContainer == null)
        {
            questListContainer = questPanel.transform.Find("QuestList/Viewport/Content");
            if (questListContainer == null) questListContainer = questPanel.transform.Find("QuestList/Content");
            if (questListContainer == null) questListContainer = FindDeep(questPanel.transform, "Content");
            if (questListContainer == null) questListContainer = FindDeep(questPanel.transform, "QuestList");
        }

        if (questListContainer != null) EnsureLayout();

        if (detailTransform == null)
        {
            detailTransform = questPanel.transform.Find("DetailPanel");
            if (detailTransform == null)
                foreach (Transform child in questPanel.transform)
                    if (child.name.ToLower().Contains("detail")) { detailTransform = child; break; }
        }

        if (detailTransform != null)
        {
            if (detailPanel == null) detailPanel = detailTransform.gameObject;
            if (detailTitle == null) detailTitle = detailTransform.Find("Title")?.GetComponent<TextMeshProUGUI>();
            if (detailDescription == null)
            {
                Transform d = detailTransform.Find("QuestDescription");
                if (d == null) d = detailTransform.Find("Description");
                detailDescription = d?.GetComponent<TextMeshProUGUI>();
            }
            if (detailProgressText == null)
            {
                Transform p = detailTransform.Find("QuestProgress");
                if (p == null) p = detailTransform.Find("Progress");
                detailProgressText = p?.GetComponent<TextMeshProUGUI>();
            }
            if (detailObjectivesContainer == null) detailObjectivesContainer = detailTransform.Find("Objectives");
            if (trackButton == null)
            {
                Transform t = detailTransform.Find("TrackButton");
                if (t == null) t = FindDeep(detailTransform, "TrackButton");
                if (t != null)
                {
                    trackButton = t.GetComponent<Button>();
                    if (trackButtonText == null) trackButtonText = t.GetComponentInChildren<TextMeshProUGUI>();
                }
            }

            if (cancelButton == null)
            {
                Transform c = detailTransform.Find("CancelButton");
                if (c == null) c = FindDeep(detailTransform, "CancelButton");
                if (c != null) cancelButton = c.GetComponent<Button>();
            }
        }

        if (tabsContainer == null) tabsContainer = questPanel.transform.Find("TabsContainer");
        if (tabsContainer != null)
        {
            if (activeTabBtn == null) activeTabBtn = tabsContainer.Find("ActiveTab")?.GetComponent<Button>();
            if (completedTabBtn == null) completedTabBtn = tabsContainer.Find("CompletedTab")?.GetComponent<Button>();
            if (failedTabBtn == null) failedTabBtn = tabsContainer.Find("FailedTab")?.GetComponent<Button>();
        }
    }

    private void SetButtonPanelVisible(bool visible)
    {
        if (detailTransform == null) return;

        var bp = detailTransform.Find("ButtonPanel");
        if (bp == null) return;

        if (buttonPanelCg == null)
        {
            buttonPanelCg = bp.GetComponent<CanvasGroup>();
            if (buttonPanelCg == null)
                buttonPanelCg = bp.gameObject.AddComponent<CanvasGroup>();
        }

        buttonPanelCg.alpha = visible ? 1f : 0f;
        buttonPanelCg.blocksRaycasts = visible;
        buttonPanelCg.interactable = visible;
    }

    private void EnsureLayout()
    {
        if (questListContainer == null) return;
        var rt = questListContainer.GetComponent<RectTransform>();
        if (rt == null) return;

        if (questListContainer.GetComponent<VerticalLayoutGroup>() == null)
        {
            var vlg = questListContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            vlg.childControlHeight = false;
            vlg.childControlWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 5f;
            vlg.padding = new RectOffset(5, 5, 5, 5);
        }

        if (questListContainer.GetComponent<ContentSizeFitter>() == null)
        {
            var csf = questListContainer.gameObject.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }
    }

    private void LoadPrefabs()
    {
        var cfg = Config;
        if (cfg == null) return;
        questItemPrefab = Resources.Load<GameObject>(cfg.QuestItemPrefabPath);
        objectivePrefab = Resources.Load<GameObject>(cfg.ObjectivePrefabPath);

        if (questItemPrefab == null)
            Logger.LogWarning(LogModule.QuestSystem, $"QuestUI: префаб не найден по пути '{cfg.QuestItemPrefabPath}'");
    }

    private void LoadItemPrefabs()
    {
        var cfg = Config;
        if (cfg == null) return;
        itemPrefabs.Clear();
        string itemsPath = cfg.InventoryItemsPath;
        GameObject[] prefabs = Resources.LoadAll<GameObject>(itemsPath);
        foreach (var prefab in prefabs)
        {
            if (prefab == null) continue;
            InventoryItemMarker marker = prefab.GetComponentInChildren<InventoryItemMarker>(true);
            if (marker != null && marker.ItemSO != null)
            {
                if (!itemPrefabs.ContainsKey(marker.ItemSO.id)) itemPrefabs[marker.ItemSO.id] = prefab;
                continue;
            }
            var db = Items;
            if (db == null) continue;
            string cleanName = prefab.name.Replace("_UI", "").Replace("_Item", "").Replace("_World", "");
            ItemSO so = db.GetItemByName(cleanName);
            if (so == null) so = db.GetItem(cleanName.ToLowerInvariant());
            if (so == null) continue;
            if (!itemPrefabs.ContainsKey(so.id)) itemPrefabs[so.id] = prefab;
        }
    }

    private void FindQuestPanel()
    {
        if (questPanel == null) questPanel = GameObject.Find("QuestPanel");
        if (questPanel == null)
        {
            try { questPanel = GameObject.FindGameObjectWithTag("QuestPanel"); } catch { }
        }
        if (questPanel == null) return;
        canvasGroup = questPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = questPanel.AddComponent<CanvasGroup>();
    }

    private void SetupReferences()
    {
        if (questPanel == null) return;
        var cfg = Config;

        if (tabsContainer != null)
        {
            string activeName = cfg != null ? cfg.ActiveTabName : "Активные";
            string completedName = cfg != null ? cfg.CompletedTabName : "Выполненные";
            string failedName = cfg != null ? cfg.FailedTabName : "Проваленные";

            SetupTab(tabsContainer, "ActiveTab", activeName, activeTabBtn, 0);
            SetupTab(tabsContainer, "CompletedTab", completedName, completedTabBtn, 1);
            SetupTab(tabsContainer, "FailedTab", failedName, failedTabBtn, 2);
        }

        if (detailTransform != null)
        {
            if (trackButtonText != null && cfg != null) trackButtonText.text = cfg.TrackBtnText;
            if (cancelButton != null)
            {
                var t = cancelButton.GetComponentInChildren<TextMeshProUGUI>();
                if (t != null && cfg != null) t.text = cfg.CancelBtnText;
            }
        }

        Transform close = questPanel.transform.Find("CloseButton");
        if (close != null)
        {
            Button c = close.GetComponent<Button>();
            if (c != null) { c.onClick.RemoveAllListeners(); c.onClick.AddListener(CloseQuestPanel); }
        }

        if (trackButton != null) { trackButton.onClick.RemoveAllListeners(); trackButton.onClick.AddListener(ToggleTrackQuest); }
        if (cancelButton != null) { cancelButton.onClick.RemoveAllListeners(); cancelButton.onClick.AddListener(CancelSelectedQuest); }
        if (detailPanel != null) detailPanel.SetActive(false);

        UpdateTabVisuals();
    }

    private void SetupTab(Transform parent, string childName, string label, Button btn, int tabIndex)
    {
        Transform child = parent.Find(childName);
        if (child == null) return;
        var t = child.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.text = label;
        if (btn != null)
        {
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => SwitchTab(tabIndex));
        }
    }

    private void SetupButtons()
    {
        Button toggleButton = GameObject.Find("QuestToggleButton")?.GetComponent<Button>();
        if (toggleButton != null) toggleButton.onClick.AddListener(ToggleQuestPanel);
    }

    private void SwitchTab(int tabIndex)
    {
        currentTab = tabIndex;
        if (detailPanel != null) detailPanel.SetActive(false);
        SetButtonPanelVisible(tabIndex == 0);
        UpdateTabVisuals();
        RefreshQuests();
    }

    private void UpdateTabVisuals()
    {
        var cfg = Config;
        Color activeColor = cfg != null ? cfg.ActiveTabColor : new Color(0.3f, 0.5f, 0.8f);
        Color inactiveColor = cfg != null ? cfg.InactiveTabColor : new Color(0.2f, 0.2f, 0.3f);
        Color failedColor = cfg != null ? cfg.FailedTabColor : new Color(0.8f, 0.3f, 0.3f);

        UpdateTabVisual(activeTabBtn, currentTab == 0, activeColor, inactiveColor);
        UpdateTabVisual(completedTabBtn, currentTab == 1, activeColor, inactiveColor);
        UpdateTabVisual(failedTabBtn, currentTab == 2, failedColor, inactiveColor);
    }

    private void UpdateTabVisual(Button btn, bool isActive, Color activeColor, Color inactiveColor)
    {
        if (btn == null) return;
        var img = btn.GetComponent<Image>();
        if (img != null) img.color = isActive ? activeColor : inactiveColor;
        var t = btn.GetComponentInChildren<TextMeshProUGUI>();
        if (t != null) t.color = isActive ? Color.white : new Color(0.5f, 0.5f, 0.5f);
    }

    public void ToggleQuestPanel()
    {
        if (isOpen) { CloseQuestPanel(); return; }
        if (_textBeginner != null && _textBeginner.IsDialogueActive) return;
        OpenQuestPanel();
    }

    public void OpenQuestPanel()
    {
        if (isOpen) return;
        if (questListContainer == null) { FindQuestPanel(); CacheComponents(); }
        if (questItemPrefab == null && Config != null) LoadPrefabs();

        UICoordinator.BeforeOpen(UIKind.Quests);
        isOpen = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
        RefreshQuests();
    }

    public void CloseQuestPanel()
    {
        if (!isOpen) return;
        isOpen = false;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f));
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float fadeDuration = Config != null ? Config.FadeDuration : 0.3f;
        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;

        if (targetAlpha >= 1f)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / fadeDuration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha <= 0f)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
        fadeCoroutine = null;
    }

    private void HandleQuestListChanged()
    {
        if (!isOpen) return;
        RefreshQuests();
    }

    public void RefreshQuests()
    {
        var qm = QM;
        if (qm == null) return;

        if (questListContainer == null) { FindQuestPanel(); CacheComponents(); }
        if (questListContainer == null) return;

        EnsureLayout();

        foreach (Transform child in questListContainer)
        {
            if (child == null) continue;

            if (emptyMessageInstance != null && child.gameObject == emptyMessageInstance)
            {
                emptyMessageInstance.SetActive(false);
                continue;
            }

            child.gameObject.SetActive(false);
            questItemPool.Push(child.gameObject);
        }
        questItemsById.Clear();

        List<QuestInstance> quests;
        if (currentTab == 2) quests = qm.GetCanceledQuests();
        else if (currentTab == 1) quests = qm.GetCompletedQuests().Where(q => !q.IsCanceled).ToList();
        else quests = qm.GetActiveQuests();

        if (quests == null || quests.Count == 0)
        {
            ShowEmptyMessage();
            if (detailPanel != null) detailPanel.SetActive(false);
            return;
        }

        HideEmptyMessage();
        quests.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        foreach (var quest in quests)
        {
            var item = AcquireQuestItem(quest);
            if (item == null) continue;
            if (!string.IsNullOrEmpty(quest.QuestID)) questItemsById[quest.QuestID] = item;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(questListContainer.GetComponent<RectTransform>());

        if (currentTab == 0)
        {
            if (trackedQuest != null && quests.Contains(trackedQuest)) ShowQuestDetails(trackedQuest);
            else if (quests.Count > 0) ShowQuestDetails(quests[0]);
        }
        else if (detailPanel != null) detailPanel.SetActive(false);
    }

    private GameObject AcquireQuestItem(QuestInstance quest)
    {
        GameObject item = null;
        while (questItemPool.Count > 0)
        {
            var pooled = questItemPool.Pop();
            if (pooled != null) { item = pooled; break; }
        }

        if (item == null)
        {
            item = CreateQuestItem(quest);
            if (item == null) return null;
        }
        else
        {
            item.SetActive(true);
            RefreshQuestItemContent(item, quest);
        }

        var button = item.GetComponent<Button>();
        if (button == null) button = item.AddComponent<Button>();
        button.onClick.RemoveAllListeners();
        QuestInstance captured = quest;
        button.onClick.AddListener(() => ShowQuestDetails(captured));

        UpdateQuestItemProgress(item, quest);
        return item;
    }

    private void RefreshQuestItemContent(GameObject item, QuestInstance quest)
    {
        if (questItemPrefab != null)
        {
            var listItem = item.GetComponent<QuestListItem>();
            if (listItem != null)
            {
                QuestInstance captured = quest;
                listItem.Initialize(captured, () => ShowQuestDetails(captured));
            }
            else
            {
                var title = FindTextByName(item.transform, "Title")
                         ?? FindTextByName(item.transform, "TitleText")
                         ?? FindTextByName(item.transform, "Name")
                         ?? item.GetComponentInChildren<TextMeshProUGUI>();

                if (title != null)
                {
                    title.text = quest.QuestName;
                    var cfg = Config;
                    if (cfg != null && cfg.TitleFontSize > 0) title.fontSize = cfg.TitleFontSize;

                    if (IsFailed(quest))
                    {
                        title.color = cfg != null ? cfg.FailedColor : Color.red;
                        title.fontStyle = FontStyles.Strikethrough;
                    }
                    else
                    {
                        title.color = Color.white;
                        title.fontStyle = FontStyles.Normal;
                    }
                }
            }
        }
        else
        {
            var txt = item.GetComponentInChildren<TextMeshProUGUI>();
            var cfg = Config;
            if (txt != null)
            {
                txt.text = $"{quest.QuestName} [{quest.GetProgressText()}]";
                txt.color = IsFailed(quest) ? (cfg != null ? cfg.FailedColor : Color.red) : Color.white;
            }
        }
    }

    private GameObject CreateQuestItem(QuestInstance quest)
    {
        if (quest == null) return null;

        try
        {
            var cfg = Config;
            float itemHeight = questItemPrefab != null
                ? (cfg != null ? cfg.QuestItemHeight : 70f)
                : (cfg != null ? cfg.FallbackItemHeight : 60f);

            GameObject item;

            if (questItemPrefab != null)
            {
                item = Instantiate(questItemPrefab, questListContainer);
                item.SetActive(true);

                var le = item.GetComponent<LayoutElement>();
                if (le == null) le = item.AddComponent<LayoutElement>();
                le.minHeight = itemHeight;
                le.preferredHeight = itemHeight;
                le.flexibleHeight = 0;

                var rt = item.GetComponent<RectTransform>();
                if (rt != null)
                {
                    rt.anchorMin = new Vector2(0, 0.5f);
                    rt.anchorMax = new Vector2(1, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(0, itemHeight);
                    rt.anchoredPosition = Vector2.zero;
                }

                RefreshQuestItemContent(item, quest);
            }
            else
            {
                item = CreateFallbackQuestItem(quest, itemHeight);
            }

            return item;
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.QuestSystem, $"CreateQuestItem failed '{quest.QuestName}': {e}");
            return null;
        }
    }

    private GameObject CreateFallbackQuestItem(QuestInstance quest, float itemHeight)
    {
        var item = new GameObject("QuestItem_Fallback", typeof(RectTransform));
        item.transform.SetParent(questListContainer, false);

        var rt = item.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, itemHeight);
        rt.anchoredPosition = Vector2.zero;

        var bg = item.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.2f, 0.9f);

        var textObj = new GameObject("Text", typeof(RectTransform));
        textObj.transform.SetParent(item.transform, false);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(15, 5);
        textRt.offsetMax = new Vector2(-15, -5);

        var tmp = textObj.AddComponent<TextMeshProUGUI>();
        tmp.text = $"{quest.QuestName} [{quest.GetProgressText()}]";
        var cfg = Config;
        tmp.color = IsFailed(quest) ? (cfg != null ? cfg.FailedColor : Color.red) : Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.fontSize = cfg != null ? cfg.TitleFontSize : 18;
        tmp.raycastTarget = false;

        var button = item.AddComponent<Button>();
        var colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
        button.colors = colors;

        var le = item.AddComponent<LayoutElement>();
        le.minHeight = itemHeight;
        le.preferredHeight = itemHeight;
        le.flexibleHeight = 0;

        return item;
    }

    private void ShowEmptyMessage()
    {
        if (emptyMessageInstance == null)
            emptyMessageInstance = CreateEmptyMessageInternal();

        var txt = emptyMessageInstance.GetComponent<TextMeshProUGUI>();
        if (txt != null)
        {
            var cfg = Config;
            string text = cfg != null ? cfg.GetEmptyText(currentTab) :
                (currentTab == 2 ? "Нет проваленных" : currentTab == 1 ? "Нет выполненных" : "Нет активных");
            txt.text = text;
        }

        emptyMessageInstance.SetActive(true);
        emptyMessageInstance.transform.SetAsLastSibling();
    }

    private void HideEmptyMessage()
    {
        if (emptyMessageInstance != null) emptyMessageInstance.SetActive(false);
    }

    private GameObject CreateEmptyMessageInternal()
    {
        var item = new GameObject("EmptyMessage", typeof(RectTransform));
        item.transform.SetParent(questListContainer, false);

        var cfg = Config;
        float height = cfg != null ? cfg.EmptyMsgHeight : 50f;
        int fontSize = cfg != null ? cfg.EmptyMsgFontSize : 20;

        var rt = item.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, height);

        var tmp = item.AddComponent<TextMeshProUGUI>();
        tmp.color = new Color(0.5f, 0.5f, 0.5f);
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.fontSize = fontSize;
        tmp.raycastTarget = false;

        return item;
    }

    private void UpdateQuestItemProgress(GameObject item, QuestInstance quest)
    {
        if (item == null || quest == null) return;

        var progressText = FindTextByName(item.transform, "ProgressText")
                        ?? FindTextByName(item.transform, "Progress");
        if (progressText != null)
            progressText.text = quest.GetProgressText();

        UpdateProgressBar(item, quest);
    }

    private void UpdateProgressBar(GameObject item, QuestInstance quest)
    {
        if (item == null || quest == null) return;
        Transform bar = FindDeep(item.transform, "ProgressBar") ?? FindDeep(item.transform, "Progress");
        if (bar == null) return;
        Transform fill = FindDeep(bar, "Fill");
        if (fill == null) return;
        var img = fill.GetComponent<Image>();
        if (img != null) { img.fillAmount = quest.Progress; img.color = GetProgressColor(quest); }
    }

    public void ShowQuestDetails(QuestInstance quest)
    {
        CloseCancelConfirmation();
        selectedQuest = quest;
        if (detailPanel == null) return;

        detailPanel.SetActive(true);
        var cfg = Config;

        if (detailTitle != null)
        {
            detailTitle.text = quest.QuestName;
            if (cfg != null && cfg.TitleFontSize > 0) detailTitle.fontSize = cfg.TitleFontSize;
            if (IsFailed(quest))
            {
                detailTitle.color = cfg != null ? cfg.FailedColor : Color.red;
                detailTitle.fontStyle = FontStyles.Strikethrough;
            }
        }

        if (detailDescription != null)
        {
            detailDescription.text = quest.Description;
            if (IsFailed(quest))
            {
                detailDescription.color = cfg != null ? cfg.FailedColor : Color.red;
                detailDescription.fontStyle = FontStyles.Strikethrough;
            }
        }

        if (detailProgressText != null)
        {
            int percent = Mathf.RoundToInt(quest.Progress * 100);
            string format = cfg != null ? cfg.ProgressDetailFormat : "Прогресс: {0} ({1}%)";
            detailProgressText.text = string.Format(format, quest.GetProgressText(), percent);
            detailProgressText.color = GetProgressColor(quest);
            if (cfg != null && cfg.ProgressFontSize > 0) detailProgressText.fontSize = cfg.ProgressFontSize;
            if (IsFailed(quest)) detailProgressText.fontStyle = FontStyles.Strikethrough;
        }

        if (detailObjectivesContainer != null)
        {
            DestroyChildrenNow(detailObjectivesContainer.gameObject);
            foreach (var obj in quest.Objectives) CreateObjectiveItem(obj);
        }

        ShowQuestItems(quest);
        ShowQuestRewards(quest);

        if (trackButtonText != null && currentTab == 0)
        {
            if (IsFailed(quest))
            {
                trackButtonText.text = cfg != null ? cfg.FailedQuestText : "Провален";
                trackButtonText.color = cfg != null ? cfg.FailedColor : Color.red;
                if (trackButton != null) trackButton.interactable = false;
                if (cancelButton != null) cancelButton.interactable = false;
            }
            else
            {
                bool isTracked = trackedQuest == quest;
                trackButtonText.text = isTracked ?
                    (cfg != null ? cfg.UntrackBtnText : "Отменить") :
                    (cfg != null ? cfg.TrackBtnText : "Отслеживать");
                trackButtonText.color = Color.white;
                if (trackButton != null) trackButton.interactable = true;
                if (cancelButton != null) cancelButton.interactable = true;
            }
        }

        RepositionContainers();
    }

    private void CreateObjectiveItem(QuestObjective objective)
    {
        if (detailObjectivesContainer == null || objective == null) return;

        bool isComplete = objective.IsComplete();
        bool isFailed = IsFailed(selectedQuest);
        string checkmark = isComplete ? "[v] " : "[ ] ";
        var cfg = Config;

        if (objectivePrefab == null)
        {
            var go = new GameObject("Objective_Fallback", typeof(RectTransform));
            go.transform.SetParent(detailObjectivesContainer, false);
            float height = cfg != null ? cfg.ObjectiveHeight : 25f;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(0, height);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = $"{checkmark}{objective.description}";
            tmp.color = GetObjectiveColor(isComplete, isFailed);
            tmp.fontSize = cfg != null ? cfg.ProgressFontSize : 14;
            tmp.raycastTarget = false;
            return;
        }

        var objItem = Instantiate(objectivePrefab, detailObjectivesContainer);
        objItem.SetActive(true);

        var text = objItem.GetComponent<TextMeshProUGUI>()
                ?? objItem.GetComponentInChildren<TextMeshProUGUI>();

        if (text != null)
        {
            string progressText = "";
            if (objective.requiredCount > 1)
            {
                string format = cfg != null ? cfg.ObjectiveProgressFormat : " ({0}/{1})";
                progressText = string.Format(format, objective.currentCount, objective.requiredCount);
            }

            text.text = $"{checkmark}{objective.description}{progressText}";
            text.color = GetObjectiveColor(isComplete, isFailed);

            if (isFailed || isComplete) text.fontStyle = FontStyles.Strikethrough;
            else text.fontStyle = FontStyles.Normal;
        }
    }

    private void DestroyChildrenNow(GameObject container)
    {
        if (container == null) return;
        var t = container.transform;
        for (int i = t.childCount - 1; i >= 0; i--)
        {
            Transform child = t.GetChild(i);
            if (child == null) continue;
            child.SetParent(null);
            Destroy(child.gameObject);
        }
    }

    private TextMeshProUGUI FindTextByName(Transform parent, string name)
    {
        if (parent == null) return null;
        if (string.Equals(parent.name, name, System.StringComparison.OrdinalIgnoreCase))
        {
            var t = parent.GetComponent<TextMeshProUGUI>();
            if (t != null) return t;
        }
        for (int i = 0; i < parent.childCount; i++)
        {
            var found = FindTextByName(parent.GetChild(i), name);
            if (found != null) return found;
        }
        return null;
    }

    private Transform FindDeep(Transform parent, string name)
    {
        if (parent == null) return null;
        if (string.Equals(parent.name, name, System.StringComparison.OrdinalIgnoreCase)) return parent;
        for (int i = 0; i < parent.childCount; i++)
        {
            var f = FindDeep(parent.GetChild(i), name);
            if (f != null) return f;
        }
        return null;
    }

    private bool IsFailed(QuestInstance quest) => quest != null && quest.IsCanceled;

    private Color GetProgressColor(QuestInstance quest)
    {
        if (currentTab == 2) return new Color(1f, 0.35f, 0.35f);
        if (currentTab == 1) return new Color(0.35f, 0.85f, 0.4f);

        var cfg = Config;
        if (cfg != null) return cfg.ActiveColor;
        return new Color(0.29f, 0.56f, 0.89f);
    }

    private Color GetObjectiveColor(bool isComplete, bool isFailed)
    {
        var cfg = Config;
        if (cfg != null) return cfg.GetObjectiveColor(isComplete, isFailed);
        if (isFailed) return Color.red;
        if (isComplete) return Color.green;
        return Color.white;
    }

    private GameObject GetSlotPrefab()
    {
        if (cachedSlotPrefab != null) return cachedSlotPrefab;
        var cfg = Config;
        string path = cfg != null ? cfg.SlotPrefabPath : "Slot";
        cachedSlotPrefab = Resources.Load<GameObject>(path);
        return cachedSlotPrefab;
    }

    private void ShowQuestItems(QuestInstance quest)
    {
        if (detailTransform == null) return;
        Transform itemsContainer = detailTransform.Find("ItemsContainer");
        if (itemsContainer == null) return;
        DestroyChildrenNow(itemsContainer.gameObject);

        var slotPrefab = GetSlotPrefab();
        if (slotPrefab == null) return;

        int count = 0;
        foreach (var obj in quest.Objectives)
            if (obj.type == QuestObjectiveType.Collect && !string.IsNullOrEmpty(obj.GetTargetItemId())) count++;
        if (count == 0) return;

        var cfg = Config;
        float slotSize = cfg != null ? cfg.SlotSize : 60f;
        float spacing = cfg != null ? cfg.SlotSpacing : 15f;
        float totalWidth = count * slotSize + (count - 1) * spacing;
        float startX = -totalWidth / 2f + slotSize / 2f;

        int index = 0;
        foreach (var obj in quest.Objectives)
        {
            if (obj.type != QuestObjectiveType.Collect) continue;
            string itemId = obj.GetTargetItemId();
            if (string.IsNullOrEmpty(itemId)) continue;

            var slot = Instantiate(slotPrefab, itemsContainer);
            slot.name = "ItemSlot_" + itemId;

            var slotRt = slot.GetComponent<RectTransform>();
            if (slotRt != null)
                slotRt.anchoredPosition = new Vector2(startX + index * (slotSize + spacing), 0);

            var invSlot = slot.GetComponent<InventorySlot>();
            if (invSlot != null) invSlot.enabled = false;

            if (itemPrefabs.TryGetValue(itemId, out var itemPrefab))
            {
                var item = Instantiate(itemPrefab, slot.transform);
                var itemRt = item.GetComponent<RectTransform>();
                if (itemRt != null)
                {
                    itemRt.anchorMin = Vector2.zero;
                    itemRt.anchorMax = Vector2.one;
                    itemRt.offsetMin = Vector2.zero;
                    itemRt.offsetMax = Vector2.zero;
                }
                var marker = item.GetComponentInChildren<InventoryItemMarker>(true);
                if (marker != null) marker.enabled = false;
            }

            var quantity = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (quantity != null)
            {
                quantity.text = $"{obj.currentCount}/{obj.requiredCount}";
                if (IsFailed(quest))
                {
                    quantity.color = cfg != null ? cfg.FailedColor : Color.red;
                    quantity.fontStyle = FontStyles.Strikethrough;
                }
                else
                {
                    quantity.color = obj.currentCount >= obj.requiredCount ?
                        (cfg != null ? cfg.CompletedColor : Color.green) : Color.white;
                    quantity.fontStyle = FontStyles.Normal;
                }
            }
            index++;
        }
    }

    private void ShowQuestRewards(QuestInstance quest)
    {
        if (detailTransform == null) return;
        Transform rewardsContainer = detailTransform.Find("RewardsContainer");
        if (rewardsContainer == null)
        {
            var go = new GameObject("RewardsContainer", typeof(RectTransform));
            go.transform.SetParent(detailTransform, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = Vector2.zero;
            rewardsContainer = go.transform;
        }

        DestroyChildrenNow(rewardsContainer.gameObject);

        if (quest.Data == null || quest.Data.rewards == null || quest.Data.rewards.Count == 0) return;

        var slotPrefab = GetSlotPrefab();
        if (slotPrefab == null) return;

        var valid = new List<QuestReward>();
        foreach (var r in quest.Data.rewards)
        {
            if (r == null) continue;
            if (string.IsNullOrEmpty(r.GetItemId())) continue;
            if (r.amount <= 0) continue;
            valid.Add(r);
        }
        if (valid.Count == 0) return;

        var cfg = Config;
        float slotSize = cfg != null ? cfg.SlotSize : 60f;
        float spacing = cfg != null ? cfg.SlotSpacing : 15f;
        float totalWidth = valid.Count * slotSize + (valid.Count - 1) * spacing;
        float startX = -totalWidth / 2f + slotSize / 2f;

        int index = 0;
        foreach (var reward in valid)
        {
            string itemId = reward.GetItemId();
            var slot = Instantiate(slotPrefab, rewardsContainer);
            slot.name = "RewardSlot_" + itemId;
            var slotRt = slot.GetComponent<RectTransform>();
            if (slotRt != null)
                slotRt.anchoredPosition = new Vector2(startX + index * (slotSize + spacing), 0);

            var invSlot = slot.GetComponent<InventorySlot>();
            if (invSlot != null) invSlot.enabled = false;

            if (itemPrefabs.TryGetValue(itemId, out var itemPrefab))
            {
                var item = Instantiate(itemPrefab, slot.transform);
                var itemRt = item.GetComponent<RectTransform>();
                if (itemRt != null)
                {
                    itemRt.anchorMin = Vector2.zero;
                    itemRt.anchorMax = Vector2.one;
                    itemRt.offsetMin = Vector2.zero;
                    itemRt.offsetMax = Vector2.zero;
                }
                var marker = item.GetComponentInChildren<InventoryItemMarker>(true);
                if (marker != null) marker.enabled = false;
            }

            var quantity = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (quantity != null)
            {
                quantity.text = $"x{reward.amount}";
                quantity.color = new Color(1f, 0.84f, 0f);
                quantity.fontStyle = FontStyles.Bold;
            }

            var bg = slot.GetComponent<Image>();
            if (bg != null) bg.color = new Color(1f, 0.84f, 0f, 0.15f);
            index++;
        }
    }

    private void RepositionContainers()
    {
        if (detailTransform == null) return;
        var frame = detailTransform as RectTransform;
        if (frame == null) return;

        DisableConflictingLayouts(frame);

        RectTransform objRt = detailObjectivesContainer as RectTransform;
        RectTransform itemsRt = detailTransform.Find("ItemsContainer") as RectTransform;

        RectTransform rewardsLabelRt = detailTransform.Find("QuestRewards") as RectTransform;
        if (rewardsLabelRt == null) rewardsLabelRt = detailTransform.Find("RewardsLabel") as RectTransform;
        if (rewardsLabelRt == null) rewardsLabelRt = detailTransform.Find("RewardsTitle") as RectTransform;

        RectTransform rewardsRt = detailTransform.Find("RewardsContainer") as RectTransform;
        RectTransform buttonPanelRt = detailTransform.Find("ButtonPanel") as RectTransform;

        for (int i = 0; i < 3; i++)
        {
            Canvas.ForceUpdateCanvases();
            if (objRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(objRt);
            if (buttonPanelRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(buttonPanelRt);
            if (rewardsLabelRt != null) LayoutRebuilder.ForceRebuildLayoutImmediate(rewardsLabelRt);
        }
        Canvas.ForceUpdateCanvases();

        const float padding = 15f;

        if (objRt != null && itemsRt != null &&
            TryGetLocalBoundsY(objRt, frame, out _, out float objBottom) &&
            TryGetLocalBoundsY(itemsRt, frame, out float itemsTop, out _))
        {
            float delta = (objBottom - padding) - itemsTop;
            MoveLocalY(itemsRt, frame, delta);
            Canvas.ForceUpdateCanvases();
        }

        if (buttonPanelRt != null && rewardsRt != null)
        {
            float buttonsTop = GetHighestGraphicLocalTop(buttonPanelRt, frame);
            if (float.IsNaN(buttonsTop))
                TryGetLocalBoundsY(buttonPanelRt, frame, out buttonsTop, out _);

            if (!float.IsNaN(buttonsTop) &&
                TryGetLocalBoundsY(rewardsRt, frame, out _, out float rewardsBottom))
            {
                float delta = (buttonsTop + padding) - rewardsBottom;
                MoveLocalY(rewardsRt, frame, delta);
                Canvas.ForceUpdateCanvases();
            }
        }

        if (rewardsLabelRt != null && rewardsRt != null &&
            TryGetLocalBoundsY(rewardsRt, frame, out float rewardsTop, out _) &&
            TryGetLocalBoundsY(rewardsLabelRt, frame, out _, out float labelBottom))
        {
            float delta = (rewardsTop + padding) - labelBottom;
            MoveLocalY(rewardsLabelRt, frame, delta);
            Canvas.ForceUpdateCanvases();
        }
    }

    private void MoveLocalY(RectTransform rt, RectTransform frame, float deltaFrameY)
    {
        if (rt == null || frame == null) return;
        if (Mathf.Approximately(deltaFrameY, 0f)) return;

        if (rt.parent == frame)
        {
            Vector3 lp = rt.localPosition;
            lp.y += deltaFrameY;
            rt.localPosition = lp;
            return;
        }

        float scaleY = Mathf.Abs(frame.lossyScale.y);
        if (scaleY < 0.0001f) scaleY = 1f;

        Vector3 p = rt.position;
        p.y += deltaFrameY * scaleY;
        rt.position = p;
    }

    private float GetHighestGraphicLocalTop(RectTransform parent, RectTransform frame)
    {
        if (parent == null || frame == null) return float.NaN;

        float highest = float.NaN;
        var corners = new Vector3[4];

        foreach (var g in parent.GetComponentsInChildren<Graphic>())
        {
            if (g == null || !g.enabled || !g.gameObject.activeInHierarchy) continue;
            var rt = g.rectTransform;
            if (rt == null) continue;

            rt.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                Vector3 local = frame.InverseTransformPoint(corners[i]);
                if (float.IsNaN(highest) || local.y > highest)
                    highest = local.y;
            }
        }

        return highest;
    }

    private bool TryGetLocalBoundsY(RectTransform rt, RectTransform frame, out float top, out float bottom)
    {
        top = float.NaN;
        bottom = float.NaN;
        if (rt == null || frame == null) return false;

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float maxY = float.MinValue;
        float minY = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            Vector3 local = frame.InverseTransformPoint(corners[i]);
            if (local.y > maxY) maxY = local.y;
            if (local.y < minY) minY = local.y;
        }

        top = maxY;
        bottom = minY;
        return true;
    }

    private void DisableConflictingLayouts(RectTransform frame)
    {
        if (frame == null) return;

        var ownGroup = frame.GetComponent<LayoutGroup>();
        if (ownGroup != null && ownGroup.enabled)
        {
            ownGroup.enabled = false;
            Logger.LogWarning(LogModule.QuestSystem, $"QuestUI: LayoutGroup '{ownGroup.GetType().Name}' на DetailPanel отключен");
        }

        var ownFitter = frame.GetComponent<ContentSizeFitter>();
        if (ownFitter != null && ownFitter.enabled)
        {
            ownFitter.enabled = false;
            Logger.LogWarning(LogModule.QuestSystem, "QuestUI: ContentSizeFitter на DetailPanel отключен");
        }

        foreach (var child in frame.GetComponentsInChildren<RectTransform>(false))
        {
            if (child == frame) continue;
            var le = child.GetComponent<LayoutElement>();
            if (le != null && !le.ignoreLayout)
                le.ignoreLayout = true;
        }

        WarnIfNotDirectChild(frame, "ItemsContainer");
        WarnIfNotDirectChild(frame, "RewardsContainer");
        WarnIfNotDirectChild(frame, "QuestRewards");
        WarnIfNotDirectChild(frame, "ButtonPanel");
        WarnIfNotDirectChild(frame, "Objectives");
    }

    private void WarnIfNotDirectChild(RectTransform frame, string childName)
    {
        var found = frame.Find(childName);
        if (found == null) return;
        if (found.parent != frame)
        {
            Logger.LogWarning(LogModule.QuestSystem,
                $"QuestUI: '{childName}' найден, но не является прямым ребёнком DetailPanel. Позиционирование может работать некорректно.");
        }
    }

    private void MoveWorldY(RectTransform rt, float deltaWorldY)
    {
        if (rt == null || Mathf.Approximately(deltaWorldY, 0f)) return;
        Vector3 p = rt.position;
        p.y += deltaWorldY;
        rt.position = p;
    }

    private bool TryGetWorldYBounds(RectTransform rt, out float top, out float bottom)
    {
        top = float.NaN;
        bottom = float.NaN;
        if (rt == null) return false;

        var corners = new Vector3[4];
        rt.GetWorldCorners(corners);

        float maxY = float.MinValue;
        float minY = float.MaxValue;
        for (int i = 0; i < 4; i++)
        {
            if (corners[i].y > maxY) maxY = corners[i].y;
            if (corners[i].y < minY) minY = corners[i].y;
        }

        top = maxY;
        bottom = minY;
        return true;
    }

    private float GetHighestGraphicWorldTop(RectTransform parent)
    {
        if (parent == null) return float.NaN;

        float highest = float.NaN;
        var corners = new Vector3[4];

        foreach (var g in parent.GetComponentsInChildren<Graphic>())
        {
            if (g == null || !g.enabled || !g.gameObject.activeInHierarchy) continue;
            var rt = g.rectTransform;
            if (rt == null) continue;

            rt.GetWorldCorners(corners);
            for (int i = 0; i < 4; i++)
            {
                if (float.IsNaN(highest) || corners[i].y > highest)
                    highest = corners[i].y;
            }
        }

        return highest;
    }

    public void CancelSelectedQuest()
    {
        if (selectedQuest == null || currentTab != 0) return;
        ShowCancelConfirmation();
    }

    private void ShowCancelConfirmation()
    {
        if (cancelConfirmOverlay != null) return;
        if (selectedQuest == null) return;

        var parent = questPanel != null ? questPanel.transform : GetComponentInParent<Canvas>()?.transform;
        if (parent == null) return;

        cancelConfirmOverlay = new GameObject("CancelConfirm", typeof(RectTransform), typeof(Image));
        cancelConfirmOverlay.transform.SetParent(parent, false);

        var overlayRt = cancelConfirmOverlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        var overlayImg = cancelConfirmOverlay.GetComponent<Image>();
        overlayImg.color = new Color(0, 0, 0, 0.65f);
        overlayImg.raycastTarget = true;

        var box = new GameObject("Box", typeof(RectTransform), typeof(Image));
        box.transform.SetParent(cancelConfirmOverlay.transform, false);
        var boxRt = box.GetComponent<RectTransform>();
        boxRt.anchorMin = new Vector2(0.5f, 0.5f);
        boxRt.anchorMax = new Vector2(0.5f, 0.5f);
        boxRt.pivot = new Vector2(0.5f, 0.5f);
        boxRt.sizeDelta = new Vector2(560, 220);
        boxRt.anchoredPosition = Vector2.zero;
        box.GetComponent<Image>().color = new Color(0.18f, 0.18f, 0.22f, 1f);

        var titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
        titleObj.transform.SetParent(box.transform, false);
        var titleRt = titleObj.GetComponent<RectTransform>();
        titleRt.anchorMin = new Vector2(0, 0.5f);
        titleRt.anchorMax = new Vector2(1, 1);
        titleRt.offsetMin = new Vector2(20, 10);
        titleRt.offsetMax = new Vector2(-20, -15);
        var titleText = titleObj.GetComponent<TextMeshProUGUI>();
        titleText.text = $"Отменить квест?\n<size=16><color=#aaaaaa>{selectedQuest.QuestName}</color></size>";
        titleText.fontSize = 22;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;

        var yesBtn = CreateConfirmButton(box.transform, "Да, отменить",
            new Vector2(0.3f, 0.22f), new Color(0.75f, 0.25f, 0.25f, 1f),
            ConfirmCancelQuest);
        var noBtn = CreateConfirmButton(box.transform, "Нет",
            new Vector2(0.7f, 0.22f), new Color(0.3f, 0.3f, 0.35f, 1f),
            CloseCancelConfirmation);

        cancelConfirmOverlay.transform.SetAsLastSibling();
    }

    private GameObject CreateConfirmButton(Transform parent, string label, Vector2 anchor, Color bgColor, System.Action onClick)
    {
        var btnObj = new GameObject(label + "_Btn", typeof(RectTransform), typeof(Image), typeof(Button));
        btnObj.transform.SetParent(parent, false);

        var rt = btnObj.GetComponent<RectTransform>();
        rt.anchorMin = anchor;
        rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(200, 55);
        rt.anchoredPosition = Vector2.zero;

        var img = btnObj.GetComponent<Image>();
        img.color = bgColor;

        var btn = btnObj.GetComponent<Button>();
        var colors = btn.colors;
        colors.normalColor = bgColor;
        colors.highlightedColor = bgColor * 1.2f;
        colors.pressedColor = bgColor * 0.8f;
        btn.colors = colors;
        btn.onClick.AddListener(() => onClick?.Invoke());

        var textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(btnObj.transform, false);
        var textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = Vector2.zero;
        textRt.offsetMax = Vector2.zero;
        var text = textObj.GetComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 20;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;
        text.raycastTarget = false;

        return btnObj;
    }

    private void CloseCancelConfirmation()
    {
        if (cancelConfirmOverlay != null)
        {
            Destroy(cancelConfirmOverlay);
            cancelConfirmOverlay = null;
        }
    }

    private void ConfirmCancelQuest()
    {
        CloseCancelConfirmation();
        if (selectedQuest == null) return;

        string questId = selectedQuest.QuestID;
        Logger.Log(LogModule.QuestSystem, $"QuestUI: подтверждена отмена '{questId}'");

        if (trackedQuest == selectedQuest)
        {
            trackedQuest = null;
            if (trackButtonText != null)
                trackButtonText.text = Config != null ? Config.TrackBtnText : "Отслеживать";
        }

        QM?.CancelQuest(questId);
        CloseQuestPanel();
    }

    public void ToggleTrackQuest()
    {
        if (selectedQuest == null || currentTab != 0) return;
        if (IsFailed(selectedQuest)) return;

        var cfg = Config;
        if (trackedQuest == selectedQuest)
        {
            trackedQuest = null;
            if (trackButtonText != null) trackButtonText.text = cfg != null ? cfg.TrackBtnText : "Отслеживать";
            RefreshQuests();
        }
        else
        {
            trackedQuest = selectedQuest;
            if (trackButtonText != null) trackButtonText.text = cfg != null ? cfg.UntrackBtnText : "Отменить";
            RefreshQuests();
        }
    }

    private void OnQuestStarted(QuestInstance quest)
    {
        _questNotifications?.ShowQuestStarted(quest.QuestName);
        if (isOpen) RefreshQuests();
    }

    private void OnQuestUpdated(QuestInstance quest)
    {
        if (isOpen) RefreshQuests();
    }

    private void OnObjectiveUpdated(QuestInstance quest)
    {
        if (trackedQuest == quest)
        {
            foreach (var obj in quest.Objectives)
                if (!obj.isOptional && !obj.IsComplete())
                {
                    _questNotifications?.ShowQuestUpdated(quest.QuestName, obj.description, obj.currentCount, obj.requiredCount);
                    break;
                }
        }

        if (!isOpen || quest == null) return;

        if (questItemsById.TryGetValue(quest.QuestID, out var item) && item != null)
            UpdateQuestItemProgress(item, quest);

        if (selectedQuest == quest) ShowQuestDetails(quest);
    }

    private void OnQuestCompleted(QuestInstance quest)
    {
        _questNotifications?.ShowQuestCompleted(quest.QuestName);

        if (trackedQuest == quest)
        {
            trackedQuest = null;
            var cfg = Config;
            if (trackButtonText != null) trackButtonText.text = cfg != null ? cfg.TrackBtnText : "Отслеживать";
        }

        if (isOpen) RefreshQuests();
    }

    [ContextMenu("Debug State")]
    public void DebugState()
    {
        Logger.Log(LogModule.QuestSystem, "=== QuestUI State ===");
        Logger.Log(LogModule.QuestSystem, $"questPanel={(questPanel != null ? questPanel.name : "NULL")}");
        Logger.Log(LogModule.QuestSystem, $"questListContainer={(questListContainer != null ? questListContainer.name : "NULL")}");
        Logger.Log(LogModule.QuestSystem, $"config={(Config != null ? Config.name : "NULL")}");
        Logger.Log(LogModule.QuestSystem, $"questItemPrefab={(questItemPrefab != null ? questItemPrefab.name : "NULL")}");
        Logger.Log(LogModule.QuestSystem, $"QM={(QM != null ? "OK" : "NULL")}");
        Logger.Log(LogModule.QuestSystem, $"isOpen={isOpen}");

        var qm = QM;
        if (qm != null)
        {
            Logger.Log(LogModule.QuestSystem, $"active={qm.GetActiveQuests().Count}");
            Logger.Log(LogModule.QuestSystem, $"completed={qm.GetCompletedQuests().Count}");
            Logger.Log(LogModule.QuestSystem, $"canceled={qm.GetCanceledQuests().Count}");
        }
        Logger.Log(LogModule.QuestSystem, "=== End ===");
    }
}