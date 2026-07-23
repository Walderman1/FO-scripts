using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;

public class QuestNotifications : MonoBehaviour
{
    public static QuestNotifications Instance;

    [Header("Конфиг")]
    [SerializeField] private QuestConfigSO config;

    private GameObject notificationPrefab;
    private Transform notificationsParent;
    private List<GameObject> activeNotifications = new List<GameObject>();
    private bool isInitialized = false;
    private Canvas mainCanvas;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.QuestSystem, "QuestNotifications инициализирован");
        }
        else
        {
            Logger.Log(LogModule.QuestSystem, "Уничтожение дублирующего QuestNotifications");
            Destroy(gameObject);
        }

        if (config == null)
        {
            config = Resources.Load<QuestConfigSO>("Configs/QuestConfig");
            if (config == null)
                Logger.LogError(LogModule.QuestSystem, "QuestConfig не найден");
        }

        FindCanvas();
        LoadPrefab();
        FindParent();
        Initialize();
    }

    private void FindCanvas()
    {
        mainCanvas = FindFirstObjectByType<Canvas>();

        if (mainCanvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            mainCanvas = canvasGO.AddComponent<Canvas>();
            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 999;

            CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            canvasGO.AddComponent<GraphicRaycaster>();

            Logger.Log(LogModule.QuestSystem, "Создан Canvas для уведомлений");
        }
    }

    private void LoadPrefab()
    {
        if (config == null) return;

        notificationPrefab = Resources.Load<GameObject>(config.NotificationPrefabPath);

        if (notificationPrefab == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Префаб не найден: {config.NotificationPrefabPath}");
            notificationPrefab = CreateNotificationPrefab();
        }
    }

    private void FindParent()
    {
        if (config == null) return;

        GameObject parentObject = GameObject.FindGameObjectWithTag(config.NotificationsTag);

        if (parentObject != null)
        {
            notificationsParent = parentObject.transform;

            VerticalLayoutGroup vlg = notificationsParent.GetComponent<VerticalLayoutGroup>();
            if (vlg != null) Destroy(vlg);

            ContentSizeFitter csf = notificationsParent.GetComponent<ContentSizeFitter>();
            if (csf != null) Destroy(csf);

            RectTransform rt = notificationsParent.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -100);
            rt.sizeDelta = new Vector2(500, 0);

            Logger.Log(LogModule.QuestSystem, "Родительский объект для уведомлений найден");
            return;
        }

        Logger.LogError(LogModule.QuestSystem, $"Объект с тегом '{config.NotificationsTag}' не найден");
    }

    private GameObject CreateNotificationPrefab()
    {
        GameObject prefab = new GameObject("Notification");
        prefab.SetActive(false);

        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0, 60);
        rt.anchorMin = new Vector2(0, 0);
        rt.anchorMax = new Vector2(1, 0);
        rt.pivot = new Vector2(0.5f, 0);

        Image bg = prefab.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);
        bg.raycastTarget = false;

        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(prefab.transform, false);
        RectTransform textRt = textGO.AddComponent<RectTransform>();
        textRt.anchorMin = new Vector2(0, 0);
        textRt.anchorMax = new Vector2(1, 1);
        textRt.offsetMin = new Vector2(55, 10);
        textRt.offsetMax = new Vector2(-15, -10);

        TextMeshProUGUI tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text = "Уведомление";
        tmp.fontSize = 18;
        tmp.color = Color.white;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.enableWordWrapping = true;
        tmp.raycastTarget = false;

        GameObject iconGO = new GameObject("Icon");
        iconGO.transform.SetParent(prefab.transform, false);
        RectTransform iconRt = iconGO.AddComponent<RectTransform>();
        iconRt.anchorMin = new Vector2(0, 0.5f);
        iconRt.anchorMax = new Vector2(0, 0.5f);
        iconRt.pivot = new Vector2(0, 0.5f);
        iconRt.anchoredPosition = new Vector2(15, 0);
        iconRt.sizeDelta = new Vector2(28, 28);

        Image iconImage = iconGO.AddComponent<Image>();
        if (config != null && config.StartIcon != null)
            iconImage.sprite = config.StartIcon;
        iconImage.color = config != null ? config.StartNotifyColor : Color.white;
        iconImage.raycastTarget = false;
        iconImage.preserveAspect = true;

        Logger.Log(LogModule.QuestSystem, "Создан префаб уведомления по умолчанию");
        return prefab;
    }

    private void Initialize()
    {
        if (isInitialized) return;
        if (notificationPrefab == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "notificationPrefab отсутствует, инициализация отложена");
            return;
        }
        if (notificationsParent == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "notificationsParent отсутствует, инициализация отложена");
            return;
        }

        isInitialized = true;
        Logger.Log(LogModule.QuestSystem, "QuestNotifications инициализирован");
    }

    public void ShowQuestStarted(string questName)
    {
        if (config == null) return;
        ShowNotification($"Начат квест: {questName}", config.StartNotifyColor, config.StartIcon);
        Logger.Log(LogModule.QuestSystem, $"Показано уведомление о начале квеста: {questName}");
    }

    public void ShowQuestCompleted(string questName)
    {
        if (config == null) return;
        ShowNotification($"Квест завершён: {questName}", config.CompleteNotifyColor, config.CompleteIcon);
        Logger.Log(LogModule.QuestSystem, $"Показано уведомление о завершении квеста: {questName}");
    }

    public void ShowQuestUpdated(string questName, string objectiveName, int current, int required)
    {
        if (config == null) return;
        ShowNotification($"{objectiveName}: {current}/{required}", config.UpdateNotifyColor, config.UpdateIcon);
        Logger.Log(LogModule.QuestSystem, $"Показано уведомление об обновлении квеста: {questName} - {objectiveName}");
    }

    public void ShowObjectiveCompleted(string objectiveName)
    {
        if (config == null) return;
        ShowNotification($"Выполнено: {objectiveName}", config.CompleteNotifyColor, config.ObjectiveIcon);
        Logger.Log(LogModule.QuestSystem, $"Показано уведомление о выполнении цели: {objectiveName}");
    }

    public void ShowCustom(string message, Color color, Sprite icon = null)
    {
        if (config == null) return;
        ShowNotification(message, color, icon ?? config.StartIcon);
        Logger.Log(LogModule.QuestSystem, $"Показано пользовательское уведомление: {message}");
    }

    private void ShowNotification(string message, Color color, Sprite icon)
    {
        if (!isInitialized) Initialize();
        if (notificationPrefab == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "notificationPrefab отсутствует, уведомление не показано");
            return;
        }
        if (notificationsParent == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "notificationsParent отсутствует, уведомление не показано");
            return;
        }
        if (config == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, "config отсутствует, уведомление не показано");
            return;
        }

        GameObject notification = Instantiate(notificationPrefab, notificationsParent);
        notification.SetActive(true);
        notification.name = "Notification_" + Time.time;

        Transform iconTransform = notification.transform.Find("Icon");
        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = icon;
                iconImage.color = color;
            }
        }

        Transform textTransform = notification.transform.Find("Text");
        if (textTransform != null)
        {
            TextMeshProUGUI text = textTransform.GetComponent<TextMeshProUGUI>();
            if (text != null)
            {
                text.text = message;
                text.color = Color.white;
            }
        }

        Image bg = notification.GetComponent<Image>();
        if (bg != null)
        {
            Color bgColor = color;
            bgColor.a = config.NotifyBgAlpha;
            bg.color = bgColor;
        }

        activeNotifications.Add(notification);

        while (activeNotifications.Count > config.MaxNotifications)
        {
            GameObject old = activeNotifications[0];
            activeNotifications.RemoveAt(0);
            Destroy(old);
        }

        RepositionNotifications();
        StartCoroutine(AnimateNotification(notification));
    }

    private void RepositionNotifications()
    {
        if (config == null) return;

        for (int i = 0; i < activeNotifications.Count; i++)
        {
            GameObject notif = activeNotifications[i];
            if (notif == null) continue;

            RectTransform rt = notif.GetComponent<RectTransform>();
            if (rt == null) continue;

            float y = -i * config.NotifySpacing;
            rt.anchoredPosition = new Vector2(0, y);
        }
    }

    private IEnumerator AnimateNotification(GameObject notification)
    {
        if (config == null)
        {
            Destroy(notification);
            yield break;
        }

        RectTransform rt = notification.GetComponent<RectTransform>();
        CanvasGroup cg = notification.GetComponent<CanvasGroup>();
        if (cg == null) cg = notification.AddComponent<CanvasGroup>();

        cg.alpha = 0f;
        rt.anchoredPosition = new Vector2(config.NotifySlideOffset, rt.anchoredPosition.y);

        float elapsed = 0f;
        Vector2 startPos = new Vector2(config.NotifySlideOffset, rt.anchoredPosition.y);
        Vector2 endPos = new Vector2(0, rt.anchoredPosition.y);

        while (elapsed < config.NotifySlide)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.NotifySlide;
            float smooth = t * t * (3f - 2f * t);

            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, smooth);
            cg.alpha = Mathf.Lerp(0f, 1f, t);

            yield return null;
        }

        rt.anchoredPosition = endPos;
        cg.alpha = 1f;

        yield return new WaitForSeconds(config.NotifyDuration);

        elapsed = 0f;
        while (elapsed < config.NotifyFade)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / config.NotifyFade;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        activeNotifications.Remove(notification);
        RepositionNotifications();
        Destroy(notification);
    }
}
