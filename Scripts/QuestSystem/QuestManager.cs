// QuestManager.cs - ИСПРАВЛЕННАЯ ВЕРСИЯ
// ============================================================
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
        }
        else
        {
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

        if (logQuests) Debug.Log($"Загружено квестов: {allQuestData.Count}");
    }

    private void SubscribeToEvents()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.OnEventExecuted += OnEventExecuted;
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
            Debug.LogWarning($"Квест {questID} не найден!");
            return;
        }

        if (activeQuests.Any(q => q.QuestID == questID))
        {
            Debug.Log($"Квест {questID} уже активен");
            return;
        }

        if (!CanStartQuest(data))
        {
            Debug.Log($"Не выполнены условия для квеста {questID}");
            return;
        }

        var instance = new QuestInstance(data);
        activeQuests.Add(instance);

        EventStateManager.Instance?.MarkExecuted($"Quest_{questID}_Started");

        OnQuestStarted?.Invoke(instance);
        if (logQuests) Debug.Log($"Квест начат: {data.questName}");

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
                return false;
        }

        if (!data.repeatable && IsQuestCompleted(data.questID))
            return false;

        return true;
    }

    public void UpdateQuest(string questID, string objectiveID, int progress = 1)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null) return;

        var objective = instance.GetObjective(objectiveID);
        if (objective == null) return;

        if (objective.isOptional) return;

        int oldProgress = objective.currentCount;
        objective.currentCount += progress;

        if (objective.currentCount > objective.requiredCount)
            objective.currentCount = objective.requiredCount;

        if (oldProgress != objective.currentCount)
        {
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
                    UpdateQuest(instance.QuestID, obj.objectiveID, 1);
                }
            }
        }
    }

    private void CheckQuestCompletion(QuestInstance instance)
    {
        if (instance.IsComplete())
        {
            CompleteQuest(instance.QuestID);
        }
    }

    public void CompleteQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null) return;

        activeQuests.Remove(instance);
        completedQuests.Add(instance);

        GiveRewards(instance);

        foreach (var reward in instance.Data.rewards)
        {
            if (!string.IsNullOrEmpty(reward.flagToSet))
            {
                // ✅ ИСПРАВЛЕНО: GlobalFlagManager → FlagManager
                FlagManager.Instance?.SetFlag(reward.flagToSet, true);
            }
        }

        OnQuestCompleted?.Invoke(instance);
        if (logQuests) Debug.Log($"Квест завершён: {instance.QuestName}");

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
                Debug.Log($"Награда: {reward.amount}x {reward.item}");
            }
        }
    }

    private void OnEventExecuted(GameEvent evt, EventContext context)
    {
        // 🔥 ВАЖНО: Обработка ДО проверки evt == null
        // Прямые вызовы (без GameEvent) должны обрабатываться в первую очередь
        if (evt == null)
        {
            if (context != null)
            {
                if (context.ItemType != ItemType.None)
                {
                    UpdateQuestByItem(context.ItemType, 1);
                    if (logQuests) Debug.Log($"✅ Прямой вызов: обновлён квест по предмету {context.ItemType}");
                }

                if (!string.IsNullOrEmpty(context.LocationName))
                {
                    UpdateQuestByLocation(context.LocationName);
                    if (logQuests) Debug.Log($"✅ Прямой вызов: обновлён квест по локации {context.LocationName}");
                }

                if (!string.IsNullOrEmpty(context.DialogueID))
                {
                    UpdateQuestByNPC(context.DialogueID);
                    if (logQuests) Debug.Log($"✅ Прямой вызов: обновлён квест по NPC {context.DialogueID}");
                }
            }
            return;
        }
        if (evt.triggerType == EventTriggerType.EnterLocation && context != null && !string.IsNullOrEmpty(context.LocationName))
        {
            UpdateQuestByLocation(context.LocationName);
            if (logQuests) Debug.Log($"✅ GameEvent '{evt.eventName}': обновлён квест по локации {context.LocationName}");
        }

        if (evt.triggerType == EventTriggerType.DialogueEnd && context != null && !string.IsNullOrEmpty(context.DialogueID))
        {
            UpdateQuestByNPC(context.DialogueID);
            if (logQuests) Debug.Log($"✅ GameEvent '{evt.eventName}': обновлён квест по NPC {context.DialogueID}");
        }

        // Custom события
        if (evt.triggerType == EventTriggerType.Custom && context != null)
        {
            if (context.ItemType != ItemType.None)
            {
                UpdateQuestByItem(context.ItemType, 1);
                if (logQuests) Debug.Log($"✅ Custom event '{evt.eventName}': обновлён квест по предмету {context.ItemType}");
            }
            if (!string.IsNullOrEmpty(context.LocationName))
            {
                UpdateQuestByLocation(context.LocationName);
                if (logQuests) Debug.Log($"✅ Custom event '{evt.eventName}': обновлён квест по локации {context.LocationName}");
            }
            if (!string.IsNullOrEmpty(context.DialogueID))
            {
                UpdateQuestByNPC(context.DialogueID);
                if (logQuests) Debug.Log($"✅ Custom event '{evt.eventName}': обновлён квест по NPC {context.DialogueID}");
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
    }

    public void CancelQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null)
        {
            Debug.LogWarning($"Квест {questID} не найден в активных!");
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

        if (logQuests) Debug.Log($"❌ Квест отменён: {instance.QuestName}");

        QuestUI.Instance?.RefreshQuests();
    }
}