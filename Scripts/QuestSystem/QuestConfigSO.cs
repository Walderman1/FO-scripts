// QuestConfigSO.cs - КОНФИГУРАЦИЯ КВЕСТОВОЙ СИСТЕМЫ
using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "QuestConfig", menuName = "Quests/Quest Config")]
public class QuestConfigSO : ScriptableObject
{
    [Header("📁 ПУТИ К ПРЕФАБАМ")]
    [SerializeField] private string questItemPrefabPath = "UI/Prefab/Panels/QuestItem";
    [SerializeField] private string objectivePrefabPath = "UI/Prefab/Panels/ObjectiveItem";
    [SerializeField] private string notificationPrefabPath = "UI/Prefab/Notification";
    [SerializeField] private string slotPrefabPath = "Slot";
    [SerializeField] private string questsFolder = "Quests";
    [SerializeField] private string notificationsTag = "QuestNotifications";
    [SerializeField] private string inventoryItemsPath = "Items/Inventory";

    [Header("🎨 ЦВЕТА")]
    [Header("——— Квесты ———")]
    [SerializeField] private Color activeColor = new Color(0.29f, 0.56f, 0.89f);
    [SerializeField] private Color completedColor = Color.green;
    [SerializeField] private Color failedColor = Color.red;
    [SerializeField] private Color trackingColor = new Color(0.3f, 0.8f, 1f);

    [Header("——— Уведомления ———")]
    [SerializeField] private Color startNotifyColor = new Color(0.3f, 0.6f, 1f);
    [SerializeField] private Color updateNotifyColor = new Color(1f, 0.8f, 0.2f);
    [SerializeField] private Color completeNotifyColor = new Color(0.2f, 0.8f, 0.3f);

    [Header("——— Цели ———")]
    [SerializeField] private Color objCompleteColor = Color.green;
    [SerializeField] private Color objIncompleteColor = Color.white;
    [SerializeField] private Color objFailedColor = Color.red;

    [Header("——— Интерфейс ———")]
    [SerializeField] private Color detailBgColor = new Color(0.06f, 0.06f, 0.09f, 0.6f);
    [SerializeField] private Color activeTabColor = new Color(0.3f, 0.5f, 0.8f);
    [SerializeField] private Color inactiveTabColor = new Color(0.2f, 0.2f, 0.3f);
    [SerializeField] private Color failedTabColor = new Color(0.8f, 0.3f, 0.3f);

    [Header("📐 РАЗМЕРЫ UI")]
    [Header("——— Панель квестов ———")]
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float questItemHeight = 70f;
    [SerializeField] private float fallbackItemHeight = 60f;
    [SerializeField] private int titleFontSize = 18;
    [SerializeField] private int progressFontSize = 14;

    [Header("——— Слоты предметов ———")]
    [SerializeField] private float slotSize = 60f;
    [SerializeField] private float slotSpacing = 15f;

    [Header("——— Пустые сообщения ———")]
    [SerializeField] private int emptyMsgFontSize = 20;
    [SerializeField] private float emptyMsgHeight = 50f;

    [Header("——— Цели ———")]
    [SerializeField] private float objectiveHeight = 25f;

    [Header("——— Панель деталей ———")]
    [SerializeField] private Vector2 detailOffsetMin = new Vector2(15, 15);
    [SerializeField] private Vector2 detailOffsetMax = new Vector2(-15, -80);

    [Header("🔔 УВЕДОМЛЕНИЯ")]
    [Header("——— Время ———")]
    [SerializeField] private float notifyDuration = 3f;
    [SerializeField] private float notifyFade = 0.5f;
    [SerializeField] private float notifySlide = 0.3f;

    [Header("——— Расположение ———")]
    [SerializeField] private int maxNotifications = 5;
    [SerializeField] private float notifySpacing = 30f;
    [SerializeField] private float notifyHeight = 60f;
    [SerializeField] private float notifyBgAlpha = 0.15f;
    [SerializeField] private float notifySlideOffset = 80f;

    [Header("🖼️ ИКОНКИ")]
    [SerializeField] private Sprite startIcon;
    [SerializeField] private Sprite completeIcon;
    [SerializeField] private Sprite updateIcon;
    [SerializeField] private Sprite objectiveIcon;
    [SerializeField] private float iconSize = 28f;

    [Header("📝 ТЕКСТЫ")]
    [Header("——— Кнопки ———")]
    [SerializeField] private string trackBtnText = "Отслеживать";
    [SerializeField] private string untrackBtnText = "Отменить отслеживание";
    [SerializeField] private string cancelBtnText = "Отменить квест";

    [Header("——— Сообщения ———")]
    [SerializeField] private string failedQuestText = "❌ Провален";
    [SerializeField] private string questCompleteText = "✅ Квест завершен!";
    [SerializeField] private string emptyActiveText = "Нет активных квестов";
    [SerializeField] private string emptyCompletedText = "Нет выполненных квестов";
    [SerializeField] private string emptyFailedText = "Нет проваленных квестов";

    [Header("——— Форматы ———")]
    [SerializeField] private string progressDetailFormat = "Прогресс: {0} ({1}%)";
    [SerializeField] private string objectiveProgressFormat = " ({0}/{1})";

    [Header("📑 ВКЛАДКИ")]
    [SerializeField] private string activeTabName = "Активные";
    [SerializeField] private string completedTabName = "Выполненные";
    [SerializeField] private string failedTabName = "Проваленные";

    [Header("⚙️ ПРОЧЕЕ")]
    [SerializeField] private bool enableLogging = true;
    [SerializeField] private bool autoLoadQuests = true;

    // ============ СВОЙСТВА ============

    // Пути
    public string QuestItemPrefabPath => questItemPrefabPath;
    public string ObjectivePrefabPath => objectivePrefabPath;
    public string NotificationPrefabPath => notificationPrefabPath;
    public string SlotPrefabPath => slotPrefabPath;
    public string QuestsFolder => questsFolder;
    public string NotificationsTag => notificationsTag;
    public string InventoryItemsPath => inventoryItemsPath;

    // Цвета
    public Color ActiveColor => activeColor;
    public Color CompletedColor => completedColor;
    public Color FailedColor => failedColor;
    public Color TrackingColor => trackingColor;
    public Color StartNotifyColor => startNotifyColor;
    public Color UpdateNotifyColor => updateNotifyColor;
    public Color CompleteNotifyColor => completeNotifyColor;
    public Color ObjCompleteColor => objCompleteColor;
    public Color ObjIncompleteColor => objIncompleteColor;
    public Color ObjFailedColor => objFailedColor;
    public Color DetailBgColor => detailBgColor;
    public Color ActiveTabColor => activeTabColor;
    public Color InactiveTabColor => inactiveTabColor;
    public Color FailedTabColor => failedTabColor;

    // Размеры
    public float FadeDuration => fadeDuration;
    public float QuestItemHeight => questItemHeight;
    public float FallbackItemHeight => fallbackItemHeight;
    public float SlotSize => slotSize;
    public float SlotSpacing => slotSpacing;
    public int TitleFontSize => titleFontSize;
    public int ProgressFontSize => progressFontSize;
    public int EmptyMsgFontSize => emptyMsgFontSize;
    public float EmptyMsgHeight => emptyMsgHeight;
    public float ObjectiveHeight => objectiveHeight;
    public Vector2 DetailOffsetMin => detailOffsetMin;
    public Vector2 DetailOffsetMax => detailOffsetMax;

    // Уведомления
    public float NotifyDuration => notifyDuration;
    public float NotifyFade => notifyFade;
    public float NotifySlide => notifySlide;
    public int MaxNotifications => maxNotifications;
    public float NotifySpacing => notifySpacing;
    public float NotifyHeight => notifyHeight;
    public float NotifyBgAlpha => notifyBgAlpha;
    public float NotifySlideOffset => notifySlideOffset;

    // Иконки
    public Sprite StartIcon => startIcon;
    public Sprite CompleteIcon => completeIcon;
    public Sprite UpdateIcon => updateIcon;
    public Sprite ObjectiveIcon => objectiveIcon;
    public float IconSize => iconSize;

    // Тексты
    public string TrackBtnText => trackBtnText;
    public string UntrackBtnText => untrackBtnText;
    public string CancelBtnText => cancelBtnText;
    public string FailedQuestText => failedQuestText;
    public string QuestCompleteText => questCompleteText;
    public string EmptyActiveText => emptyActiveText;
    public string EmptyCompletedText => emptyCompletedText;
    public string EmptyFailedText => emptyFailedText;
    public string ProgressDetailFormat => progressDetailFormat;
    public string ObjectiveProgressFormat => objectiveProgressFormat;

    // Вкладки
    public string ActiveTabName => activeTabName;
    public string CompletedTabName => completedTabName;
    public string FailedTabName => failedTabName;

    // Прочее
    public bool EnableLogging => enableLogging;
    public bool AutoLoadQuests => autoLoadQuests;

    // ============ МЕТОДЫ ============

    public Color GetQuestColor(QuestInstance quest, int tab)
    {
        if (quest == null) return activeColor;
        if (quest.IsCanceled) return failedColor;
        if (tab == 1) return completedColor;
        return activeColor;
    }

    public Color GetProgressColor(QuestInstance quest, int tab)
    {
        if (quest == null) return activeColor;
        if (quest.IsCanceled) return failedColor;
        if (tab == 1) return completedColor;
        return activeColor;
    }

    public Color GetObjectiveColor(bool isComplete, bool isFailed)
    {
        if (isFailed) return objFailedColor;
        if (isComplete) return objCompleteColor;
        return objIncompleteColor;
    }

    public Color GetNotifyColor(QuestNotifyType type)
    {
        switch (type)
        {
            case QuestNotifyType.Start: return startNotifyColor;
            case QuestNotifyType.Update: return updateNotifyColor;
            case QuestNotifyType.Complete: return completeNotifyColor;
            case QuestNotifyType.Objective: return completeNotifyColor;
            case QuestNotifyType.Tracking: return trackingColor;
            default: return Color.white;
        }
    }

    public Sprite GetNotifyIcon(QuestNotifyType type)
    {
        switch (type)
        {
            case QuestNotifyType.Start: return startIcon;
            case QuestNotifyType.Complete: return completeIcon;
            case QuestNotifyType.Update: return updateIcon;
            case QuestNotifyType.Objective: return objectiveIcon;
            case QuestNotifyType.Tracking: return updateIcon;
            default: return startIcon;
        }
    }

    public Color GetTabColor(int tab, bool active)
    {
        if (tab == 2) return active ? failedTabColor : inactiveTabColor;
        return active ? activeTabColor : inactiveTabColor;
    }

    public string GetEmptyText(int tab)
    {
        switch (tab)
        {
            case 1: return emptyCompletedText;
            case 2: return emptyFailedText;
            default: return emptyActiveText;
        }
    }
}

public enum QuestNotifyType
{
    Start,
    Update,
    Complete,
    Objective,
    Tracking
}