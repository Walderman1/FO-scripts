using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Text;

public class QuestDebugger : MonoBehaviour
{
    public static QuestDebugger Instance;

    [Header("Настройки")]
    [SerializeField] private bool showInConsole = true;
    [SerializeField] private KeyCode toggleKey = KeyCode.F12;

    [Header("Визуальные настройки")]
    [SerializeField] private Vector2 panelSize = new Vector2(500, 300);
    [SerializeField] private Vector2 panelPosition = new Vector2(10, 10);
    [SerializeField] private int fontSize = 14;
    [SerializeField] private Color panelColor = new Color(0, 0, 0, 0.85f);

    private StringBuilder log = new StringBuilder();
    private GameObject debugPanel;
    private Text debugText;
    private bool isPanelVisible = false;
    private float nextUpdateTime = 0f;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.QuestSystem, "QuestDebugger инициализирован");
        }
        else
        {
            Logger.Log(LogModule.QuestSystem, "Уничтожение дублирующего QuestDebugger");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        CreateDebugPanel();

        if (QuestManager.Instance != null)
        {
            QuestManager.Instance.OnQuestStarted += OnQuestStarted;
            QuestManager.Instance.OnQuestUpdated += OnQuestUpdated;
            QuestManager.Instance.OnQuestCompleted += OnQuestCompleted;
            QuestManager.Instance.OnObjectiveUpdated += OnObjectiveUpdated;
        }

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted += OnEventExecuted;
        }

        AddLog("=== КВЕСТ ДЕБАГГЕР АКТИВИРОВАН ===");
        AddLog($"Активных квестов: {QuestManager.Instance?.GetActiveQuests()?.Count ?? 0}");

        var active = QuestManager.Instance?.GetActiveQuests();
        if (active != null)
        {
            foreach (var q in active)
            {
                AddLog($"Активный квест: {q.QuestName} (ID: {q.QuestID})");
                foreach (var obj in q.Objectives)
                {
                    AddLog($"   {obj.description} - {obj.currentCount}/{obj.requiredCount} ({(obj.IsComplete() ? "выполнено" : "в процессе")})");
                }
            }
        }

        if (showInConsole)
            Logger.Log(LogModule.QuestSystem, "Квест дебаггер активирован. Нажмите F12 для показа панели.");

        if (debugPanel != null)
            debugPanel.SetActive(false);

        Invoke(nameof(CheckProgressBar), 1f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey) && debugPanel != null)
        {
            isPanelVisible = !isPanelVisible;
            debugPanel.SetActive(isPanelVisible);
            if (isPanelVisible)
                UpdateDebugUI();
        }

        if (isPanelVisible && Time.time > nextUpdateTime)
        {
            nextUpdateTime = Time.time + 0.5f;
            UpdateDebugUI();
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

        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted -= OnEventExecuted;
        }
    }

    private void CreateDebugPanel()
    {
        try
        {
            Canvas canvas = FindObjectOfType<Canvas>();
            if (canvas == null)
            {
                GameObject canvasGO = new GameObject("DebugCanvas");
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 9999;
                canvasGO.AddComponent<CanvasScaler>();
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            debugPanel = new GameObject("QuestDebugPanel");
            debugPanel.transform.SetParent(canvas.transform, false);

            RectTransform panelRt = debugPanel.AddComponent<RectTransform>();
            panelRt.anchorMin = new Vector2(0, 1);
            panelRt.anchorMax = new Vector2(0, 1);
            panelRt.pivot = new Vector2(0, 1);
            panelRt.sizeDelta = panelSize;
            panelRt.anchoredPosition = panelPosition;

            Image bg = debugPanel.AddComponent<Image>();
            bg.color = panelColor;
            bg.raycastTarget = true;

            GameObject textGO = new GameObject("Text");
            textGO.transform.SetParent(debugPanel.transform, false);

            RectTransform textRt = textGO.AddComponent<RectTransform>();
            textRt.anchorMin = new Vector2(0, 0);
            textRt.anchorMax = new Vector2(1, 1);
            textRt.offsetMin = new Vector2(10, 10);
            textRt.offsetMax = new Vector2(-10, -10);

            debugText = textGO.AddComponent<Text>();
            debugText.fontSize = fontSize;
            debugText.color = Color.white;
            debugText.alignment = TextAnchor.UpperLeft;
            debugText.raycastTarget = false;
            debugText.supportRichText = true;
            debugText.text = "Загрузка...";

            ScrollRect scroll = debugPanel.AddComponent<ScrollRect>();
            scroll.viewport = textRt;
            scroll.content = textRt;
            scroll.horizontal = false;
            scroll.vertical = true;

            Logger.Log(LogModule.QuestSystem, "Панель дебаггера создана");
        }
        catch (System.Exception e)
        {
            Logger.LogError(LogModule.QuestSystem, $"Ошибка создания панели: {e.Message}");
        }
    }

    private void OnQuestStarted(QuestInstance quest)
    {
        AddLog($"КВЕСТ НАЧАТ: {quest.QuestName} (ID: {quest.QuestID})");
        AddLog($"   Описание: {quest.Description}");
        AddLog($"   Целей: {quest.Objectives.Count}");
        foreach (var obj in quest.Objectives)
        {
            AddLog($"      {obj.description} - 0/{obj.requiredCount} ({(obj.isOptional ? "опционально" : "обязательно")})");
        }
        if (isPanelVisible) UpdateDebugUI();
    }

    private void OnQuestUpdated(QuestInstance quest)
    {
        AddLog($"КВЕСТ ОБНОВЛЁН: {quest.QuestName}");
        AddLog($"   Прогресс: {quest.GetProgressText()} ({quest.Progress * 100:F0}%)");
        if (isPanelVisible) UpdateDebugUI();
    }

    private void OnQuestCompleted(QuestInstance quest)
    {
        AddLog($"КВЕСТ ЗАВЕРШЁН: {quest.QuestName} (ID: {quest.QuestID})");
        AddLog($"   Награды выданы");
        if (isPanelVisible) UpdateDebugUI();
    }

    private void OnObjectiveUpdated(QuestInstance quest)
    {
        AddLog($"ЦЕЛЬ ОБНОВЛЕНА в квесте: {quest.QuestName}");
        foreach (var obj in quest.Objectives)
        {
            AddLog($"   {obj.description} - {obj.currentCount}/{obj.requiredCount} ({(obj.IsComplete() ? "выполнено" : "в процессе")})");
        }
        if (isPanelVisible) UpdateDebugUI();
    }

    private void OnEventExecuted(GameEvent evt, EventContext context)
    {
        string eventName = (evt != null) ? $"{evt.triggerType} - {evt.eventName}" : "Direct Trigger";
        AddLog($"СОБЫТИЕ: {eventName}");

        if (context != null)
        {
            if (context.ItemType != ItemType.None)
                AddLog($"   Предмет: {context.ItemType}");
            if (!string.IsNullOrEmpty(context.LocationName))
                AddLog($"   Локация: {context.LocationName}");
            if (!string.IsNullOrEmpty(context.DialogueID))
                AddLog($"   Диалог: {context.DialogueID}");
        }
        if (isPanelVisible) UpdateDebugUI();
    }

    public void CheckProgressBar()
    {
        var active = QuestManager.Instance?.GetActiveQuests();
        if (active == null || active.Count == 0)
        {
            AddLog("Нет активных квестов для проверки");
            return;
        }

        var quest = active[0];
        AddLog("=== ПРОВЕРКА PROGRESSBAR ===");
        AddLog($"Квест: {quest.QuestName}");
        AddLog($"Прогресс: {quest.Progress * 100:F0}%");

        GameObject panel = GameObject.Find("QuestPanel");
        if (panel == null)
        {
            AddLog("QuestPanel не найден");
            return;
        }

        Transform list = panel.transform.Find("QuestList");
        if (list == null)
        {
            AddLog("QuestList не найден");
            return;
        }

        Transform viewport = list.Find("Viewport");
        if (viewport == null)
        {
            AddLog("Viewport не найден");
            return;
        }

        Transform content = viewport.Find("Content");
        if (content == null)
        {
            AddLog("Content не найден");
            return;
        }

        int childCount = content.childCount;
        AddLog($"Дочерних объектов в Content: {childCount}");

        if (childCount == 0)
        {
            AddLog("В Content нет элементов QuestItem");
            return;
        }

        Transform firstItem = content.GetChild(0);
        AddLog($"Первый элемент: {firstItem.name}");

        Transform bar = firstItem.Find("ProgressBar");
        if (bar == null)
        {
            AddLog("ProgressBar не найден в QuestItem");
            AddLog($"Дочерние элементы: {GetChildNames(firstItem)}");
            return;
        }

        AddLog("ProgressBar найден");

        Transform fill = bar.Find("Fill");
        if (fill == null)
        {
            AddLog("Fill не найден в ProgressBar");
            AddLog($"Дочерние элементы ProgressBar: {GetChildNames(bar)}");
            return;
        }

        AddLog("Fill найден");

        Image fillImg = fill.GetComponent<Image>();
        if (fillImg == null)
        {
            AddLog("Image не найден на Fill");
            return;
        }

        AddLog($"Image найден! fillAmount = {fillImg.fillAmount}");
        AddLog($"   Image Type: {fillImg.type}");
        AddLog($"   Fill Method: {fillImg.fillMethod}");

        if (fillImg.type != Image.Type.Filled)
        {
            AddLog($"Image Type должен быть Filled, сейчас {fillImg.type}");
        }

        if (fillImg.fillMethod != Image.FillMethod.Horizontal)
        {
            AddLog($"Fill Method должен быть Horizontal, сейчас {fillImg.fillMethod}");
        }

        fillImg.fillAmount = 0.75f;
        AddLog("Принудительно установлен fillAmount = 0.75");

        UpdateDebugUI();
    }

    private string GetChildNames(Transform parent)
    {
        string names = "";
        foreach (Transform child in parent)
        {
            names += child.name + ", ";
        }
        return names;
    }

    private void AddLog(string message)
    {
        string time = $"[{Time.time:F2}]";
        string fullMessage = $"{time} {message}";

        log.AppendLine(fullMessage);

        if (showInConsole)
            Logger.Log(LogModule.QuestSystem, fullMessage);

        if (log.Length > 15000)
        {
            string temp = log.ToString();
            int cutIndex = temp.IndexOf('\n', temp.Length / 2);
            if (cutIndex > 0)
                log.Remove(0, cutIndex + 1);
        }
    }

    private void UpdateDebugUI()
    {
        if (debugText == null) return;

        try
        {
            string[] lines = log.ToString().Split('\n');
            int start = Mathf.Max(0, lines.Length - 40);
            StringBuilder display = new StringBuilder();
            for (int i = start; i < lines.Length; i++)
            {
                display.AppendLine(lines[i]);
            }
            debugText.text = display.ToString();
        }
        catch (System.Exception e)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Ошибка обновления UI: {e.Message}");
        }
    }

    [ContextMenu("Показать все квесты")]
    public void ShowAllQuests()
    {
        AddLog("=== ТЕКУЩИЕ КВЕСТЫ ===");

        var active = QuestManager.Instance?.GetActiveQuests();
        if (active == null || active.Count == 0)
        {
            AddLog("Нет активных квестов");
            return;
        }

        foreach (var quest in active)
        {
            AddLog($"{quest.QuestName} (ID: {quest.QuestID})");
            AddLog($"   Прогресс: {quest.GetProgressText()} ({quest.Progress * 100:F0}%)");
            foreach (var obj in quest.Objectives)
            {
                AddLog($"   {obj.description} - {obj.currentCount}/{obj.requiredCount} ({(obj.IsComplete() ? "выполнено" : "в процессе")})");
            }
        }
        UpdateDebugUI();
    }

    public void ShowMessage(string message)
    {
        AddLog($"{message}");
        UpdateDebugUI();
    }
}
