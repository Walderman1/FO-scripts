using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using VContainer;

public class QuestNotifications : MonoBehaviour
{
    [Header("Конфиг")]
    [SerializeField] private QuestConfigSO config;

    [Header("Ссылки")]
    [SerializeField] private GameObject notificationPrefab;

    [Inject] private QuestConfigSO _injectedConfig;

    private List<GameObject> activeNotifications = new List<GameObject>();
    private Stack<GameObject> notificationPool = new Stack<GameObject>();
    private Canvas mainCanvas;
    private Transform notificationsParent;

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (config == null) config = _injectedConfig;
        EnsureUISetup();
    }

    private void EnsureUISetup()
    {
        if (config == null) config = _injectedConfig;

        if (mainCanvas == null)
        {
            mainCanvas = GetComponent<Canvas>();
            if (mainCanvas == null) mainCanvas = gameObject.AddComponent<Canvas>();

            mainCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            mainCanvas.sortingOrder = 32767;

            if (GetComponent<CanvasScaler>() == null)
            {
                var scaler = gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
            }

            if (GetComponent<GraphicRaycaster>() == null) gameObject.AddComponent<GraphicRaycaster>();
        }

        if (notificationsParent == null)
        {
            var existing = transform.Find("QuestNotificationsParent");
            if (existing != null) notificationsParent = existing;
            else
            {
                GameObject parentObj = new GameObject("QuestNotificationsParent", typeof(RectTransform));
                parentObj.transform.SetParent(transform, false);
                notificationsParent = parentObj.transform;
            }

            RectTransform rt = notificationsParent.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(1, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(1, 1);
            rt.anchoredPosition = new Vector2(-20, -100);
            rt.sizeDelta = new Vector2(500, 0);
        }
    }

    private void LoadPrefab()
    {
        if (notificationPrefab != null) return;
        if (config == null) return;

        notificationPrefab = Resources.Load<GameObject>(config.NotificationPrefabPath);
        if (notificationPrefab == null) notificationPrefab = CreateFallbackPrefab();
    }

    private GameObject CreateFallbackPrefab()
    {
        GameObject prefab = new GameObject("Notification_Fallback");
        prefab.SetActive(false);

        RectTransform rt = prefab.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(450, 60);

        Image bg = prefab.AddComponent<Image>();
        bg.color = new Color(0.08f, 0.08f, 0.12f, 0.92f);

        GameObject textGO = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        textGO.transform.SetParent(prefab.transform, false);
        var textRt = textGO.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(55, 10);
        textRt.offsetMax = new Vector2(-15, -10);

        var tmp = textGO.GetComponent<TextMeshProUGUI>();
        tmp.fontSize = 18;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;

        return prefab;
    }

    public void ShowQuestStarted(string questName)
    {
        if (config == null) return;
        ShowNotification($"Начат квест: {questName}", config.StartNotifyColor, config.StartIcon);
    }

    public void ShowQuestCompleted(string questName)
    {
        if (config == null) return;
        ShowNotification($"Квест завершён: {questName}", config.CompleteNotifyColor, config.CompleteIcon);
    }

    public void ShowQuestUpdated(string questName, string objectiveName, int current, int required)
    {
        if (config == null) return;
        ShowNotification($"{objectiveName}: {current}/{required}", config.UpdateNotifyColor, config.UpdateIcon);
    }

    public void ShowObjectiveCompleted(string objectiveName)
    {
        if (config == null) return;
        ShowNotification($"Выполнено: {objectiveName}", config.CompleteNotifyColor, config.ObjectiveIcon);
    }

    public void ShowCustom(string message, Color color, Sprite icon = null)
    {
        if (config == null) return;
        ShowNotification(message, color, icon ?? config.StartIcon);
    }

    private GameObject GetPooledNotification()
    {
        if (notificationPool.Count > 0)
        {
            var pooled = notificationPool.Pop();
            if (pooled != null) { pooled.SetActive(true); return pooled; }
        }

        if (notificationPrefab == null) LoadPrefab();
        if (notificationPrefab == null) return null;

        return Instantiate(notificationPrefab, notificationsParent);
    }

    private void ReturnToPool(GameObject notification)
    {
        if (notification == null) return;
        notification.SetActive(false);
        notificationPool.Push(notification);
    }

    private void ShowNotification(string message, Color color, Sprite icon)
    {
        if (notificationsParent == null || mainCanvas == null) EnsureUISetup();
        if (notificationPrefab == null) LoadPrefab();
        if (notificationsParent == null || notificationPrefab == null) return;

        GameObject notification = GetPooledNotification();
        if (notification == null) return;

        RectTransform notifRt = notification.GetComponent<RectTransform>();
        if (notifRt == null) notifRt = notification.AddComponent<RectTransform>();

        notifRt.anchorMin = new Vector2(1, 1);
        notifRt.anchorMax = new Vector2(1, 1);
        notifRt.pivot = new Vector2(1, 1);
        notifRt.sizeDelta = new Vector2(450, config != null && config.NotifyHeight > 0 ? config.NotifyHeight : 60);
        notifRt.localScale = Vector3.one;

        Image bg = notification.GetComponent<Image>();
        if (bg != null)
        {
            Color bgColor = color;
            bgColor.a = config != null ? config.NotifyBgAlpha : 0.8f;
            bg.color = bgColor;
        }

        Transform iconTransform = notification.transform.Find("Icon");
        if (iconTransform == null && icon != null)
        {
            GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            iconObj.transform.SetParent(notification.transform, false);
            var irt = iconObj.GetComponent<RectTransform>();
            irt.anchorMin = new Vector2(0, 0.5f);
            irt.anchorMax = new Vector2(0, 0.5f);
            irt.pivot = new Vector2(0, 0.5f);
            irt.anchoredPosition = new Vector2(15, 0);
            irt.sizeDelta = new Vector2(30, 30);
            iconTransform = iconObj.transform;
        }

        if (iconTransform != null)
        {
            Image iconImage = iconTransform.GetComponent<Image>();
            if (iconImage == null) iconImage = iconTransform.gameObject.AddComponent<Image>();

            iconImage.sprite = icon;
            iconImage.color = icon != null ? color : Color.clear;
            iconImage.preserveAspect = true;
            iconImage.raycastTarget = false;
        }

        Transform textTransform = notification.transform.Find("Text");
        if (textTransform == null)
        {
            GameObject textObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            textObj.transform.SetParent(notification.transform, false);
            var trt = textObj.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(icon != null ? 55 : 15, 10);
            trt.offsetMax = new Vector2(-15, -10);
            textTransform = textObj.transform;
        }

        TextMeshProUGUI text = textTransform.GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = message;
            text.color = Color.white;
            text.fontSize = 20;
            text.alignment = TextAlignmentOptions.MidlineLeft;
        }

        activeNotifications.Add(notification);

        int maxNotif = config != null ? config.MaxNotifications : 5;
        while (activeNotifications.Count > maxNotif)
        {
            GameObject old = activeNotifications[0];
            activeNotifications.RemoveAt(0);
            ReturnToPool(old);
        }

        RepositionNotifications();
        StartCoroutine(AnimateNotification(notification));
    }

    private void RepositionNotifications()
    {
        float spacing = config != null ? config.NotifySpacing : 70f;

        for (int i = 0; i < activeNotifications.Count; i++)
        {
            GameObject notif = activeNotifications[i];
            if (notif == null) continue;

            RectTransform rt = notif.GetComponent<RectTransform>();
            if (rt != null) rt.anchoredPosition = new Vector2(0, -i * spacing);
        }
    }

    private IEnumerator AnimateNotification(GameObject notification)
    {
        if (notification == null) yield break;

        RectTransform rt = notification.GetComponent<RectTransform>();
        CanvasGroup cg = notification.GetComponent<CanvasGroup>();
        if (cg == null) cg = notification.AddComponent<CanvasGroup>();

        if (rt == null) yield break;

        cg.alpha = 0f;
        Vector2 endPos = rt.anchoredPosition;
        Vector2 startPos = endPos + new Vector2(50, 0);
        rt.anchoredPosition = startPos;

        float elapsed = 0f;
        float slideTime = config != null ? config.NotifySlide : 0.3f;
        float duration = config != null ? config.NotifyDuration : 3f;
        float fadeTime = config != null ? config.NotifyFade : 0.5f;

        while (elapsed < slideTime)
        {
            if (notification == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / slideTime;
            rt.anchoredPosition = Vector2.Lerp(startPos, endPos, t);
            cg.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        if (notification == null) yield break;
        rt.anchoredPosition = endPos;
        cg.alpha = 1f;

        yield return new WaitForSeconds(duration);

        if (notification == null) yield break;
        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            if (notification == null) yield break;
            elapsed += Time.deltaTime;
            float t = elapsed / fadeTime;
            cg.alpha = Mathf.Lerp(1f, 0f, t);
            yield return null;
        }

        if (notification == null) yield break;

        activeNotifications.Remove(notification);
        RepositionNotifications();
        ReturnToPool(notification);
    }
}