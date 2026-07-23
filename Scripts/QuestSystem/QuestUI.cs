using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    [Header("Конфиг")]
    [SerializeField] private QuestConfigSO config;

    [Header("Панель")]
    [SerializeField] private GameObject questPanel;
    [SerializeField] private CanvasGroup canvasGroup;

    public bool IsOpen => isOpen;
    private bool isOpen = false;
    private int currentTab = 0;
    private QuestInstance selectedQuest;
    private QuestInstance trackedQuest;
    private Coroutine fadeCoroutine;

    private GameObject questItemPrefab;
    private GameObject objectivePrefab;
    private Dictionary<ItemType, GameObject> itemPrefabs = new Dictionary<ItemType, GameObject>();

    private Transform questListContainer;
    private GameObject detailPanel;
    private TextMeshProUGUI detailTitle;
    private TextMeshProUGUI detailDescription;
    private TextMeshProUGUI detailProgressText;
    private Transform detailObjectivesContainer;
    private Button trackButton;
    private Button cancelButton;
    private TextMeshProUGUI trackButtonText;

    private Button activeTabBtn;
    private Button completedTabBtn;
    private Button failedTabBtn;

    private Transform tabsContainer;
    private Transform detailTransform;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.QuestSystem, "QuestUI инициализирован");
        }
        else
        {
            Logger.Log(LogModule.QuestSystem, "Уничтожение дублирующего QuestUI");
            Destroy(gameObject);
        }

        if (config == null)
        {
            config = Resources.Load<QuestConfigSO>("Configs/QuestConfig");
            if (config == null)
                Logger.LogError(LogModule.QuestSystem, "QuestConfig не найден");
        }

        LoadPrefabs();
        LoadItemPrefabs();
        FindQuestPanel();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        isOpen = false;

        if (questPanel != null)
            questPanel.SetActive(true);

        CacheComponents();
        SetupReferences();
        SetupButtons();
    }

    private void Start()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;
            QuestManager.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }
    }

    private void OnDestroy()
    {
        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted -= OnQuestStarted;
            QuestManager.Instance.OnQuestUpdated -= OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted -= OnQuestCompleted;
            QuestManager.Instance.OnObjectiveUpdated -= OnObjectiveUpdated;
        }
    }

    private void CacheComponents()
    {
        if (questPanel == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "questPanel отсутствует");
            return;
        }

        Transform list = questPanel.transform.Find("QuestList");
        if (list != null)
        {
            Transform viewport = list.Find("Viewport");
            if (viewport != null)
            {
                questListContainer = viewport.Find("Content");
            }
        }

        detailTransform = questPanel.transform.Find("DetailPanel");
        if (detailTransform == null)
        {
            foreach (Transform child in questPanel.transform)
            {
                if (child.name.ToLower().Contains("detail"))
                {
                    detailTransform = child;
                    break;
                }
            }
        }

        if (detailTransform != null)
        {
            detailPanel = detailTransform.gameObject;
            detailTitle = detailTransform.Find("Title")?.GetComponent<TextMeshProUGUI>();

            Transform descTransform = detailTransform.Find("QuestDescription");
            if (descTransform == null) descTransform = detailTransform.Find("Description");
            detailDescription = descTransform?.GetComponent<TextMeshProUGUI>();

            Transform progressTransform = detailTransform.Find("QuestProgress");
            if (progressTransform == null) progressTransform = detailTransform.Find("Progress");
            detailProgressText = progressTransform?.GetComponent<TextMeshProUGUI>();

            detailObjectivesContainer = detailTransform.Find("Objectives");
        }

        tabsContainer = questPanel.transform.Find("TabsContainer");
    }

    private bool IsFailed(QuestInstance quest)
    {
        return quest != null && quest.IsCanceled;
    }

    private Color GetProgressColor(QuestInstance quest)
    {
        if (config != null)
            return config.GetProgressColor(quest, currentTab);

        if (IsFailed(quest))
            return Color.red;
        if (currentTab == 1)
            return Color.green;
        return new Color(0.29f, 0.56f, 0.89f);
    }

    private Color GetObjectiveColor(bool isComplete, bool isFailed)
    {
        if (config != null)
            return config.GetObjectiveColor(isComplete, isFailed);

        if (isFailed)
            return Color.red;
        if (isComplete)
            return Color.green;
        return Color.white;
    }

    private void LoadPrefabs()
    {
        if (config == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "config отсутствует, загрузка префабов невозможна");
            return;
        }

        questItemPrefab = Resources.Load<GameObject>(config.QuestItemPrefabPath);
        if (questItemPrefab == null)
            Logger.LogWarning(LogModule.QuestSystem, $"Префаб не найден: {config.QuestItemPrefabPath}");

        objectivePrefab = Resources.Load<GameObject>(config.ObjectivePrefabPath);
        if (objectivePrefab == null)
            Logger.LogWarning(LogModule.QuestSystem, $"Префаб не найден: {config.ObjectivePrefabPath}");
    }

    private void LoadItemPrefabs()
    {
        if (config == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "config отсутствует, загрузка префабов предметов невозможна");
            return;
        }

        itemPrefabs.Clear();
        string itemsPath = config.InventoryItemsPath;
        GameObject[] prefabs = Resources.LoadAll<GameObject>(itemsPath);

        foreach (var prefab in prefabs)
        {
            string name = prefab.name.Replace("_Item", "");
            if (System.Enum.TryParse(name, out ItemType itemType))
            {
                if (!itemPrefabs.ContainsKey(itemType))
                    itemPrefabs.Add(itemType, prefab);
            }
        }
    }

    private void FindQuestPanel()
    {
        questPanel = GameObject.Find("QuestPanel");
        if (questPanel == null)
        {
            try { questPanel = GameObject.FindGameObjectWithTag("QuestPanel"); }
            catch { }
        }
        if (questPanel == null)
        {
            Logger.LogError(LogModule.QuestSystem, "QuestPanel не найден");
            return;
        }
        canvasGroup = questPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = questPanel.AddComponent<CanvasGroup>();
    }

    private void SetupReferences()
    {
        if (questPanel == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "questPanel отсутствует, настройка ссылок невозможна");
            return;
        }

        Logger.Log(LogModule.QuestSystem, "Настройка ссылок...");

        if (tabsContainer != null)
        {
            string activeName = config != null ? config.ActiveTabName : "Активные";
            string completedName = config != null ? config.CompletedTabName : "Выполненные";
            string failedName = config != null ? config.FailedTabName : "Проваленные";

            Transform active = tabsContainer.Find("ActiveTab");
            if (active != null)
            {
                TextMeshProUGUI activeText = active.GetComponentInChildren<TextMeshProUGUI>();
                if (activeText != null) activeText.text = activeName;

                activeTabBtn = active.GetComponent<Button>();
                if (activeTabBtn != null)
                {
                    activeTabBtn.onClick.RemoveAllListeners();
                    activeTabBtn.onClick.AddListener(() => SwitchTab(0));
                    Logger.Log(LogModule.QuestSystem, "ActiveTab привязан");
                }
            }

            Transform completed = tabsContainer.Find("CompletedTab");
            if (completed != null)
            {
                TextMeshProUGUI completedText = completed.GetComponentInChildren<TextMeshProUGUI>();
                if (completedText != null) completedText.text = completedName;

                completedTabBtn = completed.GetComponent<Button>();
                if (completedTabBtn != null)
                {
                    completedTabBtn.onClick.RemoveAllListeners();
                    completedTabBtn.onClick.AddListener(() => SwitchTab(1));
                    Logger.Log(LogModule.QuestSystem, "CompletedTab привязан");
                }
            }

            Transform failed = tabsContainer.Find("FailedTab");
            if (failed != null)
            {
                TextMeshProUGUI failedText = failed.GetComponentInChildren<TextMeshProUGUI>();
                if (failedText != null) failedText.text = failedName;

                failedTabBtn = failed.GetComponent<Button>();
                if (failedTabBtn != null)
                {
                    failedTabBtn.onClick.RemoveAllListeners();
                    failedTabBtn.onClick.AddListener(() => SwitchTab(2));
                    Logger.Log(LogModule.QuestSystem, "FailedTab привязан");
                }
            }
        }
        else
        {
            Logger.LogWarning(LogModule.QuestSystem, "TabsContainer не найден");
        }

        if (detailTransform != null)
        {
            Transform track = detailTransform.Find("TrackButton");
            if (track != null)
            {
                trackButton = track.GetComponent<Button>();
                trackButtonText = track.GetComponentInChildren<TextMeshProUGUI>();
                if (trackButtonText != null && config != null)
                    trackButtonText.text = config.TrackBtnText;
            }

            Transform cancel = detailTransform.Find("CancelButton");
            if (cancel != null)
            {
                cancelButton = cancel.GetComponent<Button>();
                TextMeshProUGUI cancelText = cancel.GetComponentInChildren<TextMeshProUGUI>();
                if (cancelText != null && config != null)
                    cancelText.text = config.CancelBtnText;
            }
        }

        Transform close = questPanel.transform.Find("CloseButton");
        if (close != null)
        {
            Button closeBtn = close.GetComponent<Button>();
            if (closeBtn != null)
                closeBtn.onClick.AddListener(CloseQuestPanel);
        }

        if (trackButton != null)
            trackButton.onClick.AddListener(ToggleTrackQuest);

        if (cancelButton != null)
            cancelButton.onClick.AddListener(CancelSelectedQuest);

        if (detailPanel != null)
            detailPanel.SetActive(false);

        UpdateTabVisuals();
        Logger.Log(LogModule.QuestSystem, "Настройка ссылок завершена");
    }

    private void SetupButtons()
    {
        Button toggleButton = GameObject.Find("QuestToggleButton")?.GetComponent<Button>();
        if (toggleButton != null)
            toggleButton.onClick.AddListener(ToggleQuestPanel);
    }

    private void SwitchTab(int tabIndex)
    {
        Logger.Log(LogModule.QuestSystem, $"Переключение на вкладку {tabIndex}");
        currentTab = tabIndex;

        if (detailPanel != null)
            detailPanel.SetActive(false);

        UpdateTabVisuals();
        RefreshQuests();
    }

    private void UpdateTabVisuals()
    {
        Color activeColor = config != null ? config.ActiveTabColor : new Color(0.3f, 0.5f, 0.8f);
        Color inactiveColor = config != null ? config.InactiveTabColor : new Color(0.2f, 0.2f, 0.3f);
        Color failedColor = config != null ? config.FailedTabColor : new Color(0.8f, 0.3f, 0.3f);

        if (activeTabBtn != null)
        {
            Image img = activeTabBtn.GetComponent<Image>();
            if (img != null)
                img.color = currentTab == 0 ? activeColor : inactiveColor;

            TextMeshProUGUI text = activeTabBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.color = currentTab == 0 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        if (completedTabBtn != null)
        {
            Image img = completedTabBtn.GetComponent<Image>();
            if (img != null)
                img.color = currentTab == 1 ? activeColor : inactiveColor;

            TextMeshProUGUI text = completedTabBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.color = currentTab == 1 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        if (failedTabBtn != null)
        {
            Image img = failedTabBtn.GetComponent<Image>();
            if (img != null)
                img.color = currentTab == 2 ? failedColor : inactiveColor;

            TextMeshProUGUI text = failedTabBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (text != null)
                text.color = currentTab == 2 ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        }

        if (detailPanel != null)
            detailPanel.SetActive(false);
    }

    public void ToggleQuestPanel()
    {
        if (isOpen) CloseQuestPanel();
        else OpenQuestPanel();
    }

    public void OpenQuestPanel()
    {
        if (isOpen) return;
        isOpen = true;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(1f));
        RefreshQuests();
        Logger.Log(LogModule.QuestSystem, "Панель квестов открыта");
    }

    public void CloseQuestPanel()
    {
        if (!isOpen) return;
        isOpen = false;
        if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
        fadeCoroutine = StartCoroutine(FadeCanvasGroup(0f));
        Logger.Log(LogModule.QuestSystem, "Панель квестов закрыта");
    }

    private IEnumerator FadeCanvasGroup(float targetAlpha)
    {
        float fadeDuration = config != null ? config.FadeDuration : 0.3f;

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
            float t = elapsed / fadeDuration;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
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

    private void UpdateQuestItemProgress(GameObject item, QuestInstance quest)
    {
        if (item == null || quest == null) return;

        TextMeshProUGUI[] texts = item.GetComponentsInChildren<TextMeshProUGUI>();
        foreach (TextMeshProUGUI t in texts)
        {
            if (t.name == "ProgressText")
            {
                t.text = quest.GetProgressText();
                break;
            }
        }

        Transform bar = item.transform.Find("ProgressBar");
        if (bar != null)
        {
            Transform fill = bar.Find("Fill");
            if (fill != null)
            {
                Image fillImg = fill.GetComponent<Image>();
                if (fillImg != null)
                {
                    fillImg.fillAmount = quest.Progress;
                    fillImg.color = GetProgressColor(quest);
                }
            }
        }
    }

    public void RefreshQuests()
    {
        if (config != null && config.EnableLogging)
            Logger.Log(LogModule.QuestSystem, $"Обновление квестов: currentTab={currentTab}, isOpen={isOpen}");

        if (questListContainer == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "questListContainer отсутствует");
            return;
        }

        for (int i = questListContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = questListContainer.GetChild(i);
            if (child != null) Destroy(child.gameObject);
        }

        List<QuestInstance> quests;

        if (currentTab == 2)
        {
            quests = QuestManager.Instance?.GetCanceledQuests() ?? new List<QuestInstance>();
        }
        else if (currentTab == 1)
        {
            var allCompleted = QuestManager.Instance?.GetCompletedQuests() ?? new List<QuestInstance>();
            quests = allCompleted.Where(q => !q.IsCanceled).ToList();
        }
        else
        {
            quests = QuestManager.Instance?.GetActiveQuests() ?? new List<QuestInstance>();
        }

        if (quests == null || quests.Count == 0)
        {
            CreateEmptyMessage();
            if (detailPanel != null)
                detailPanel.SetActive(false);
            return;
        }

        quests.Sort((a, b) => b.Priority.CompareTo(a.Priority));

        foreach (var quest in quests)
        {
            GameObject item = CreateQuestItem(quest);
            if (item != null)
            {
                UpdateQuestItemProgress(item, quest);
            }
        }

        if (currentTab == 0)
        {
            if (trackedQuest != null && quests.Contains(trackedQuest))
            {
                ShowQuestDetails(trackedQuest);
            }
            else if (quests.Count > 0)
            {
                ShowQuestDetails(quests[0]);
            }
        }
        else
        {
            if (detailPanel != null)
                detailPanel.SetActive(false);
        }
    }

    private GameObject CreateQuestItem(QuestInstance quest)
    {
        if (questListContainer == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "questListContainer отсутствует");
            return null;
        }

        GameObject item;

        if (questItemPrefab != null)
        {
            item = Instantiate(questItemPrefab, questListContainer);
            item.SetActive(true);

            float itemHeight = config != null ? config.QuestItemHeight : 70f;

            LayoutElement le = item.GetComponent<LayoutElement>();
            if (le == null) le = item.AddComponent<LayoutElement>();
            le.minHeight = itemHeight;
            le.preferredHeight = itemHeight;
            le.flexibleHeight = 0;

            RectTransform rt = item.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = new Vector2(0, 0.5f);
                rt.anchorMax = new Vector2(1, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.sizeDelta = new Vector2(0, itemHeight);
                rt.anchoredPosition = Vector2.zero;
            }

            TextMeshProUGUI title = item.GetComponentInChildren<TextMeshProUGUI>();
            if (title != null)
            {
                title.text = quest.QuestName;
                if (config != null && config.TitleFontSize > 0)
                    title.fontSize = config.TitleFontSize;

                if (IsFailed(quest))
                {
                    title.color = config != null ? config.FailedColor : Color.red;
                    title.fontStyle = FontStyles.Strikethrough;
                }
            }

            Transform bar = item.transform.Find("ProgressBar");
            if (bar != null)
            {
                Transform fill = bar.Find("Fill");
                if (fill != null)
                {
                    Image fillImg = fill.GetComponent<Image>();
                    if (fillImg != null)
                    {
                        fillImg.fillAmount = quest.Progress;
                        fillImg.color = GetProgressColor(quest);
                    }
                }
            }
        }
        else
        {
            item = new GameObject("QuestItem_Fallback");
            item.transform.SetParent(questListContainer, false);

            float itemHeight = config != null ? config.FallbackItemHeight : 60f;

            LayoutElement le = item.AddComponent<LayoutElement>();
            le.minHeight = itemHeight;
            le.preferredHeight = itemHeight;
            le.flexibleHeight = 0;

            RectTransform rt = item.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0.5f);
            rt.anchorMax = new Vector2(1, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(0, itemHeight);

            Text txt = item.AddComponent<Text>();
            txt.text = $"{quest.QuestName} [{quest.GetProgressText()}]";
            txt.color = IsFailed(quest) ? (config != null ? config.FailedColor : Color.red) : Color.white;
            txt.alignment = TextAnchor.MiddleLeft;
            txt.fontSize = config != null ? config.TitleFontSize : 18;

            Button btn = item.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.normalColor = new Color(0.2f, 0.2f, 0.3f);
            btn.colors = colors;
        }

        Button button = item.GetComponent<Button>();
        if (button != null)
        {
            QuestInstance captured = quest;
            button.onClick.AddListener(() => ShowQuestDetails(captured));
        }

        return item;
    }

    private void CreateEmptyMessage()
    {
        if (questListContainer == null) return;

        GameObject item = new GameObject("EmptyMessage");
        item.transform.SetParent(questListContainer, false);

        float height = config != null ? config.EmptyMsgHeight : 50f;
        int fontSize = config != null ? config.EmptyMsgFontSize : 20;
        string text = config != null ? config.GetEmptyText(currentTab) :
            (currentTab == 2 ? "Нет проваленных квестов" :
             currentTab == 1 ? "Нет выполненных квестов" : "Нет активных квестов");

        RectTransform rt = item.AddComponent<RectTransform>();
        rt.anchorMin = new Vector2(0, 0.5f);
        rt.anchorMax = new Vector2(1, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0, height);

        Text txt = item.AddComponent<Text>();
        txt.text = text;
        txt.color = new Color(0.5f, 0.5f, 0.5f);
        txt.alignment = TextAnchor.MiddleCenter;
        txt.fontSize = fontSize;
    }

    public void ShowQuestDetails(QuestInstance quest)
    {
        selectedQuest = quest;

        if (detailPanel == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "detailPanel отсутствует");
            return;
        }

        detailPanel.SetActive(true);

        if (detailTitle != null)
        {
            detailTitle.text = quest.QuestName;
            if (config != null && config.TitleFontSize > 0)
                detailTitle.fontSize = config.TitleFontSize;

            if (IsFailed(quest))
            {
                detailTitle.color = config != null ? config.FailedColor : Color.red;
                detailTitle.fontStyle = FontStyles.Strikethrough;
            }
        }

        if (detailDescription != null)
        {
            detailDescription.text = quest.Description;
            if (IsFailed(quest))
            {
                detailDescription.color = config != null ? config.FailedColor : Color.red;
                detailDescription.fontStyle = FontStyles.Strikethrough;
            }
        }

        if (detailProgressText != null)
        {
            string progressText = quest.GetProgressText();
            int percent = Mathf.RoundToInt(quest.Progress * 100);

            string format = config != null ? config.ProgressDetailFormat : "Прогресс: {0} ({1}%)";
            detailProgressText.text = string.Format(format, progressText, percent);

            detailProgressText.color = GetProgressColor(quest);
            if (config != null && config.ProgressFontSize > 0)
                detailProgressText.fontSize = config.ProgressFontSize;

            if (IsFailed(quest))
                detailProgressText.fontStyle = FontStyles.Strikethrough;
        }

        if (detailObjectivesContainer != null)
        {
            for (int i = detailObjectivesContainer.childCount - 1; i >= 0; i--)
            {
                Transform child = detailObjectivesContainer.GetChild(i);
                if (child != null) Destroy(child.gameObject);
            }
            foreach (var obj in quest.Objectives)
            {
                CreateObjectiveItem(obj);
            }
        }

        ShowQuestItems(quest);

        if (trackButtonText != null && currentTab == 0)
        {
            if (IsFailed(quest))
            {
                trackButtonText.text = config != null ? config.FailedQuestText : "Провален";
                trackButtonText.color = config != null ? config.FailedColor : Color.red;
                if (trackButton != null) trackButton.interactable = false;
                if (cancelButton != null) cancelButton.interactable = false;
            }
            else
            {
                bool isTracked = trackedQuest == quest;
                trackButtonText.text = isTracked ?
                    (config != null ? config.UntrackBtnText : "Отменить отслеживание") :
                    (config != null ? config.TrackBtnText : "Отслеживать");
                trackButtonText.color = Color.white;
                if (trackButton != null) trackButton.interactable = true;
                if (cancelButton != null) cancelButton.interactable = true;
            }
        }
    }

    private void CreateObjectiveItem(QuestObjective objective)
    {
        if (detailObjectivesContainer == null) return;

        if (objectivePrefab == null)
        {
            GameObject go = new GameObject("Objective_Fallback");
            go.transform.SetParent(detailObjectivesContainer, false);
            RectTransform rt = go.AddComponent<RectTransform>();
            float height = config != null ? config.ObjectiveHeight : 25f;
            rt.sizeDelta = new Vector2(0, height);
            Text txt = go.AddComponent<Text>();
            bool isComplete = objective.IsComplete();
            bool isFailed = IsFailed(selectedQuest);

            txt.text = $"{(isComplete ? "✓ " : "○ ")}{objective.description}";
            txt.color = GetObjectiveColor(isComplete, isFailed);
            return;
        }

        GameObject objItem = Instantiate(objectivePrefab, detailObjectivesContainer);
        objItem.SetActive(true);

        TextMeshProUGUI text = objItem.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            bool isComplete = objective.IsComplete();
            bool isFailed = IsFailed(selectedQuest);

            string checkmark = isComplete ? "✓ " : "○ ";
            string progressText = "";
            if (objective.requiredCount > 1)
            {
                string format = config != null ? config.ObjectiveProgressFormat : " ({0}/{1})";
                progressText = string.Format(format, objective.currentCount, objective.requiredCount);
            }

            text.text = $"{checkmark}{objective.description}{progressText}";
            text.color = GetObjectiveColor(isComplete, isFailed);

            if (isFailed)
                text.fontStyle = FontStyles.Strikethrough;
            else if (isComplete)
                text.fontStyle = FontStyles.Strikethrough;
            else
                text.fontStyle = FontStyles.Normal;
        }
    }

    private void ShowQuestItems(QuestInstance quest)
    {
        if (detailTransform == null) return;

        Transform itemsContainer = detailTransform.Find("ItemsContainer");
        if (itemsContainer == null) return;

        for (int i = itemsContainer.childCount - 1; i >= 0; i--)
        {
            Transform child = itemsContainer.GetChild(i);
            if (child != null) DestroyImmediate(child.gameObject);
        }

        string slotPath = config != null ? config.SlotPrefabPath : "Slot";
        GameObject slotPrefab = Resources.Load<GameObject>(slotPath);
        if (slotPrefab == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Слот префаб не найден: {slotPath}");
            return;
        }

        int count = 0;
        foreach (var obj in quest.Objectives)
        {
            if (obj.type == QuestObjectiveType.Collect && obj.targetItem != ItemType.None)
                count++;
        }

        if (count == 0) return;

        float slotSize = config != null ? config.SlotSize : 60f;
        float spacing = config != null ? config.SlotSpacing : 15f;
        float totalWidth = count * slotSize + (count - 1) * spacing;
        float startX = -totalWidth / 2f + slotSize / 2f;

        int index = 0;
        foreach (var obj in quest.Objectives)
        {
            if (obj.type != QuestObjectiveType.Collect || obj.targetItem == ItemType.None) continue;

            GameObject slot = Instantiate(slotPrefab, itemsContainer);
            slot.name = "ItemSlot_" + obj.targetItem;

            RectTransform slotRt = slot.GetComponent<RectTransform>();
            if (slotRt != null)
            {
                slotRt.anchoredPosition = new Vector2(startX + index * (slotSize + spacing), 0);
            }

            InventorySlot invSlot = slot.GetComponent<InventorySlot>();
            if (invSlot != null)
            {
                invSlot.enabled = false;
            }

            if (itemPrefabs.TryGetValue(obj.targetItem, out GameObject itemPrefab))
            {
                GameObject item = Instantiate(itemPrefab, slot.transform);
                item.transform.localPosition = Vector3.zero;
                item.transform.localScale = Vector3.one;

                InventoryItemMarker marker = item.GetComponentInChildren<InventoryItemMarker>(true);
                if (marker != null)
                {
                    marker.enabled = false;
                }
            }

            TextMeshProUGUI quantity = slot.GetComponentInChildren<TextMeshProUGUI>();
            if (quantity != null)
            {
                quantity.text = $"{obj.currentCount}/{obj.requiredCount}";

                if (IsFailed(quest))
                {
                    quantity.color = config != null ? config.FailedColor : Color.red;
                    quantity.fontStyle = FontStyles.Strikethrough;
                }
                else
                {
                    quantity.color = obj.currentCount >= obj.requiredCount ?
                        (config != null ? config.CompletedColor : Color.green) : Color.white;
                    quantity.fontStyle = FontStyles.Normal;
                }
            }

            index++;
        }
    }

    public void CancelSelectedQuest()
    {
        if (selectedQuest == null || currentTab != 0)
        {
            Logger.LogWarning(LogModule.QuestSystem, "Нет выбранного квеста или неактивная вкладка для отмены");
            return;
        }

        Logger.Log(LogModule.QuestSystem, $"Отмена квеста: {selectedQuest.QuestName}");
        QuestManager.Instance?.CancelQuest(selectedQuest.QuestID);
        CloseQuestPanel();
    }

    public void ToggleTrackQuest()
    {
        if (selectedQuest == null || currentTab != 0)
        {
            Logger.LogWarning(LogModule.QuestSystem, "Нет выбранного квеста или неактивная вкладка для отслеживания");
            return;
        }

        if (IsFailed(selectedQuest))
        {
            Logger.Log(LogModule.QuestSystem, $"Квест {selectedQuest.QuestName} провален, отслеживание невозможно");
            return;
        }

        if (trackedQuest == selectedQuest)
        {
            trackedQuest = null;
            if (trackButtonText != null)
                trackButtonText.text = config != null ? config.TrackBtnText : "Отслеживать";
            Logger.Log(LogModule.QuestSystem, $"Отмена отслеживания квеста {selectedQuest.QuestName}");
            RefreshQuests();
        }
        else
        {
            trackedQuest = selectedQuest;
            if (trackButtonText != null)
                trackButtonText.text = config != null ? config.UntrackBtnText : "Отменить отслеживание";
            Logger.Log(LogModule.QuestSystem, $"Начато отслеживание квеста {selectedQuest.QuestName}");
            RefreshQuests();
        }
    }

    private void OnQuestStarted(QuestInstance quest)
    {
        if (config != null && config.EnableLogging)
            Logger.Log(LogModule.QuestSystem, $"Событие: квест начат - {quest?.QuestName}");

        QuestNotifications.Instance?.ShowQuestStarted(quest.QuestName);
        if (isOpen) RefreshQuests();
    }

    private void OnQuestUpdated(QuestInstance quest)
    {
        if (config != null && config.EnableLogging)
            Logger.Log(LogModule.QuestSystem, $"Событие: квест обновлён - {quest?.QuestName}");

        if (isOpen) RefreshQuests();
    }

    private void OnObjectiveUpdated(QuestInstance quest)
    {
        if (config != null && config.EnableLogging)
            Logger.Log(LogModule.QuestSystem, $"Событие: цель обновлена - {quest?.QuestName}");

        if (trackedQuest == quest)
        {
            foreach (var obj in quest.Objectives)
            {
                if (!obj.isOptional && !obj.IsComplete())
                {
                    QuestNotifications.Instance?.ShowQuestUpdated(
                        quest.QuestName,
                        obj.description,
                        obj.currentCount,
                        obj.requiredCount
                    );
                    break;
                }
            }
        }

        if (isOpen) RefreshQuests();
    }

    private void OnQuestCompleted(QuestInstance quest)
    {
        if (config != null && config.EnableLogging)
            Logger.Log(LogModule.QuestSystem, $"Событие: квест завершён - {quest?.QuestName}");

        QuestNotifications.Instance?.ShowQuestCompleted(quest.QuestName);

        if (trackedQuest == quest)
        {
            trackedQuest = null;
            if (trackButtonText != null)
                trackButtonText.text = config != null ? config.TrackBtnText : "Отслеживать";
        }

        if (isOpen) RefreshQuests();
    }
}
