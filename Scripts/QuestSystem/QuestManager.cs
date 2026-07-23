using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    [Header("Конфиг")]
    [SerializeField] private string questsFolder = "Quests";
    [SerializeField] private bool autoLoad = true;
    [SerializeField] private bool logQuests = true;

    private List<QuestInstance> activeQuests = new List<QuestInstance>();
    private List<QuestInstance> completedQuests = new List<QuestInstance>();
    private List<QuestInstance> canceledQuests = new List<QuestInstance>();
    private Dictionary<string, QuestSO> allQuestData = new Dictionary<string, QuestSO>();

    public System.Action<QuestInstance> OnQuestStarted;
    public System.Action<QuestInstance> OnQuestUpdated;
    public System.Action<QuestInstance> OnQuestCompleted;
    public System.Action<QuestInstance> OnQuestFailed;
    public System.Action<QuestInstance> OnObjectiveUpdated;
    public List<QuestInstance> GetCanceledQuests() => canceledQuests;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            if (autoLoad) LoadAllQuests();
            Logger.Log(LogModule.QuestSystem, "QuestManager инициализирован");
        }
        else
        {
            Logger.Log(LogModule.QuestSystem, "Уничтожение дублирующего QuestManager");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        SubscribeToEvents();
        StartAutoQuests();
    }

    private void LoadAllQuests()
    {
        allQuestData.Clear();
        QuestSO[] loaded = Resources.LoadAll<QuestSO>(questsFolder);

        foreach (var quest in loaded)
        {
            if (quest != null && !allQuestData.ContainsKey(quest.questID))
            {
                allQuestData.Add(quest.questID, quest);
            }
        }

        if (logQuests) Logger.Log(LogModule.QuestSystem, $"Загружено квестов: {allQuestData.Count}");
    }

    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted += OnEventExecuted;
            Logger.Log(LogModule.QuestSystem, "Подписка на события EventManager выполнена");
        }
        else
        {
            Logger.LogWarning(LogModule.QuestSystem, "EventManager отсутствует");
        }
    }

    private void StartAutoQuests()
    {
        List<string> questIDs = new List<string>(allQuestData.Keys);

        foreach (string questID in questIDs)
        {
            if (allQuestData.TryGetValue(questID, out QuestSO questData))
            {
                if (questData != null && questData.autoStart && CanStartQuest(questData))
                {
                    StartQuest(questData.questID);
                }
            }
        }
    }

    public void StartQuest(string questID)
    {
        if (!allQuestData.TryGetValue(questID, out var data))
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Квест {questID} не найден");
            return;
        }

        if (activeQuests.Any(q => q.QuestID == questID))
        {
            Logger.Log(LogModule.QuestSystem, $"Квест {questID} уже активен");
            return;
        }

        if (!CanStartQuest(data))
        {
            Logger.Log(LogModule.QuestSystem, $"Не выполнены условия для квеста {questID}");
            return;
        }

        var instance = new QuestInstance(data);
        activeQuests.Add(instance);

        EventStateManager.Instance?.MarkExecuted($"Quest_{questID}_Started");

        OnQuestStarted?.Invoke(instance);
        if (logQuests) Logger.Log(LogModule.QuestSystem, $"Квест начат: {data.questName}");

        QuestUI.Instance?.RefreshQuests();

        if (!string.IsNullOrEmpty(data.startDialogueID))
        {
            EventManager.Instance?.TriggerEvent(EventTriggerType.DialogueEnd,
                new EventContext().WithDialogue(data.startDialogueID));
        }
    }

    private bool CanStartQuest(QuestSO data)
    {
        foreach (var condition in data.startConditions)
        {
            if (!condition.IsMet())
            {
                Logger.Log(LogModule.QuestSystem, $"Условие для квеста {data.questName} не выполнено");
                return false;
            }
        }

        if (!data.repeatable && IsQuestCompleted(data.questID))
        {
            Logger.Log(LogModule.QuestSystem, $"Квест {data.questName} уже завершён и не повторяемый");
            return false;
        }

        return true;
    }

    public void UpdateQuest(string questID, string objectiveID, int progress = 1)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Квест {questID} не найден в активных");
            return;
        }

        var objective = instance.GetObjective(objectiveID);
        if (objective == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Цель {objectiveID} не найдена в квесте {questID}");
            return;
        }

        if (objective.isOptional)
        {
            Logger.Log(LogModule.QuestSystem, $"Цель {objectiveID} опциональная, пропуск обновления");
            return;
        }

        int oldProgress = objective.currentCount;
        objective.currentCount += progress;

        if (objective.currentCount > objective.requiredCount)
            objective.currentCount = objective.requiredCount;

        if (oldProgress != objective.currentCount)
        {
            Logger.Log(LogModule.QuestSystem, $"Обновлена цель {objectiveID} в квесте {instance.QuestName}: {objective.currentCount}/{objective.requiredCount}");
            OnObjectiveUpdated?.Invoke(instance);
            CheckQuestCompletion(instance);
            QuestUI.Instance?.RefreshQuests();
        }
    }

    public void UpdateQuestByItem(ItemType itemType, int count = 1)
    {
        var questsCopy = new List<QuestInstance>(activeQuests);

        foreach (var instance in questsCopy)
        {
            foreach (var obj in instance.Objectives)
            {
                if (obj.type == QuestObjectiveType.Collect && obj.targetItem == itemType)
                {
                    Logger.Log(LogModule.QuestSystem, $"Обновление квеста {instance.QuestName} по предмету {itemType}");
                    UpdateQuest(instance.QuestID, obj.objectiveID, count);
                }
            }
        }
    }

    public void UpdateQuestByLocation(string locationName)
    {
        var questsCopy = new List<QuestInstance>(activeQuests);

        foreach (var instance in questsCopy)
        {
            foreach (var obj in instance.Objectives)
            {
                if (obj.type == QuestObjectiveType.GoTo && obj.targetLocation == locationName)
                {
                    Logger.Log(LogModule.QuestSystem, $"Обновление квеста {instance.QuestName} по локации {locationName}");
                    UpdateQuest(instance.QuestID, obj.objectiveID, 1);
                }
            }
        }
    }

    public void UpdateQuestByNPC(string npcName)
    {
        var questsCopy = new List<QuestInstance>(activeQuests);

        foreach (var instance in questsCopy)
        {
            foreach (var obj in instance.Objectives)
            {
                if (obj.type == QuestObjectiveType.TalkTo && obj.targetNPC == npcName)
                {
                    Logger.Log(LogModule.QuestSystem, $"Обновление квеста {instance.QuestName} по NPC {npcName}");
                    UpdateQuest(instance.QuestID, obj.objectiveID, 1);
                }
            }
        }
    }

    private void CheckQuestCompletion(QuestInstance instance)
    {
        if (instance.IsComplete())
        {
            Logger.Log(LogModule.QuestSystem, $"Квест {instance.QuestName} выполнен, запуск завершения");
            CompleteQuest(instance.QuestID);
        }
    }

    public void CompleteQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Квест {questID} не найден в активных для завершения");
            return;
        }

        activeQuests.Remove(instance);
        completedQuests.Add(instance);

        GiveRewards(instance);

        foreach (var reward in instance.Data.rewards)
        {
            if (!string.IsNullOrEmpty(reward.flagToSet))
            {
                FlagManager.Instance?.SetFlag(reward.flagToSet, true);
                Logger.Log(LogModule.QuestSystem, $"Установлен флаг {reward.flagToSet} за завершение квеста {instance.QuestName}");
            }
        }

        OnQuestCompleted?.Invoke(instance);
        if (logQuests) Logger.Log(LogModule.QuestSystem, $"Квест завершён: {instance.QuestName}");

        EventStateManager.Instance?.MarkExecuted($"Quest_{questID}_Completed");

        if (!string.IsNullOrEmpty(instance.Data.completeDialogueID))
        {
            EventManager.Instance?.TriggerEvent(EventTriggerType.DialogueEnd,
                new EventContext().WithDialogue(instance.Data.completeDialogueID));
        }

        QuestUI.Instance?.RefreshQuests();
    }

    private void GiveRewards(QuestInstance instance)
    {
        foreach (var reward in instance.Data.rewards)
        {
            if (reward.item != ItemType.None)
            {
                for (int i = 0; i < reward.amount; i++)
                {
                    InventoryUIManager inventory = FindFirstObjectByType<InventoryUIManager>();
                    inventory?.AddItem(reward.item);
                }
                Logger.Log(LogModule.QuestSystem, $"Награда: {reward.amount}x {reward.item}");
            }
        }
    }

    private void OnEventExecuted(GameEvent evt, EventContext context)
    {
        if (evt == null)
        {
            if (context != null)
            {
                if (context.ItemType != ItemType.None)
                {
                    UpdateQuestByItem(context.ItemType, 1);
                    if (logQuests) Logger.Log(LogModule.QuestSystem, $"Прямой вызов: обновлён квест по предмету {context.ItemType}");
                }

                if (!string.IsNullOrEmpty(context.LocationName))
                {
                    UpdateQuestByLocation(context.LocationName);
                    if (logQuests) Logger.Log(LogModule.QuestSystem, $"Прямой вызов: обновлён квест по локации {context.LocationName}");
                }

                if (!string.IsNullOrEmpty(context.DialogueID))
                {
                    UpdateQuestByNPC(context.DialogueID);
                    if (logQuests) Logger.Log(LogModule.QuestSystem, $"Прямой вызов: обновлён квест по NPC {context.DialogueID}");
                }
            }
            return;
        }

        if (evt.triggerType == EventTriggerType.EnterLocation && context != null && !string.IsNullOrEmpty(context.LocationName))
        {
            UpdateQuestByLocation(context.LocationName);
            if (logQuests) Logger.Log(LogModule.QuestSystem, $"GameEvent '{evt.eventName}': обновлён квест по локации {context.LocationName}");
        }

        if (evt.triggerType == EventTriggerType.DialogueEnd && context != null && !string.IsNullOrEmpty(context.DialogueID))
        {
            UpdateQuestByNPC(context.DialogueID);
            if (logQuests) Logger.Log(LogModule.QuestSystem, $"GameEvent '{evt.eventName}': обновлён квест по NPC {context.DialogueID}");
        }

        if (evt.triggerType == EventTriggerType.Custom && context != null)
        {
            if (context.ItemType != ItemType.None)
            {
                UpdateQuestByItem(context.ItemType, 1);
                if (logQuests) Logger.Log(LogModule.QuestSystem, $"Custom event '{evt.eventName}': обновлён квест по предмету {context.ItemType}");
            }
            if (!string.IsNullOrEmpty(context.LocationName))
            {
                UpdateQuestByLocation(context.LocationName);
                if (logQuests) Logger.Log(LogModule.QuestSystem, $"Custom event '{evt.eventName}': обновлён квест по локации {context.LocationName}");
            }
            if (!string.IsNullOrEmpty(context.DialogueID))
            {
                UpdateQuestByNPC(context.DialogueID);
                if (logQuests) Logger.Log(LogModule.QuestSystem, $"Custom event '{evt.eventName}': обновлён квест по NPC {context.DialogueID}");
            }
        }
    }

    public bool IsQuestActive(string questID)
    {
        return activeQuests.Any(q => q.QuestID == questID);
    }

    public bool IsQuestCompleted(string questID)
    {
        return completedQuests.Any(q => q.QuestID == questID);
    }

    public QuestInstance GetQuest(string questID)
    {
        return activeQuests.FirstOrDefault(q => q.QuestID == questID);
    }

    public List<QuestInstance> GetActiveQuests() => activeQuests;
    public List<QuestInstance> GetCompletedQuests() => completedQuests;

    [System.Serializable]
    public class QuestSaveData
    {
        public string questID;
        public List<ObjectiveSaveData> objectives;
        public bool isCompleted;
    }

    [System.Serializable]
    public class ObjectiveSaveData
    {
        public string objectiveID;
        public int currentCount;
    }

    public QuestSaveData[] SaveQuests()
    {
        var list = new List<QuestSaveData>();
        foreach (var q in activeQuests)
        {
            var data = new QuestSaveData
            {
                questID = q.QuestID,
                objectives = q.Objectives.Select(o => new ObjectiveSaveData
                {
                    objectiveID = o.objectiveID,
                    currentCount = o.currentCount
                }).ToList()
            };
            list.Add(data);
        }
        return list.ToArray();
    }

    public void LoadQuests(QuestSaveData[] data)
    {
        activeQuests.Clear();
        foreach (var d in data)
        {
            if (allQuestData.TryGetValue(d.questID, out var questData))
            {
                var instance = new QuestInstance(questData);
                foreach (var obj in instance.Objectives)
                {
                    var saved = d.objectives.FirstOrDefault(o => o.objectiveID == obj.objectiveID);
                    if (saved != null)
                        obj.currentCount = saved.currentCount;
                }
                activeQuests.Add(instance);
            }
        }
        QuestUI.Instance?.RefreshQuests();
        Logger.Log(LogModule.QuestSystem, $"Загружено {data.Length} квестов");
    }

    public void CancelQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null)
        {
            Logger.LogWarning(LogModule.QuestSystem, $"Квест {questID} не найден в активных");
            return;
        }

        instance.IsCanceled = true;

        foreach (var obj in instance.Objectives)
        {
            obj.currentCount = obj.requiredCount;
        }

        activeQuests.Remove(instance);
        canceledQuests.Add(instance);

        EventStateManager.Instance?.MarkExecuted($"Quest_{questID}_Canceled");

        if (logQuests) Logger.Log(LogModule.QuestSystem, $"Квест отменён: {instance.QuestName}");

        QuestUI.Instance?.RefreshQuests();
    }
}
