using DialogueSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

[DefaultExecutionOrder(-7000)]
public class QuestManager : MonoBehaviour
{
    [Header("Конфиг")]
    [SerializeField] private string questsFolder = "Quests";
    [SerializeField] private bool autoLoad = true;

    [Inject] private EventManager _eventManager;
    [Inject] private EventStateManager _eventStateManager;
    [Inject] private FlagManager _flagManager;
    [Inject] private QuestNotifications _questNotifications;

    private EventManager EM => _eventManager;
    private EventStateManager ESM => _eventStateManager;
    private FlagManager FM => _flagManager;
    private QuestNotifications QN => _questNotifications;

    private List<QuestInstance> activeQuests = new List<QuestInstance>();
    private List<QuestInstance> completedQuests = new List<QuestInstance>();
    private List<QuestInstance> canceledQuests = new List<QuestInstance>();
    private Dictionary<string, QuestSO> allQuestData = new Dictionary<string, QuestSO>();

    public System.Action<QuestInstance> OnQuestStarted;
    public System.Action<QuestInstance> OnQuestUpdated;
    public System.Action<QuestInstance> OnQuestCompleted;
    public System.Action<QuestInstance> OnQuestFailed;
    public System.Action<QuestInstance> OnObjectiveUpdated;
    public System.Action OnQuestListChanged;

    public List<QuestInstance> GetCanceledQuests() => canceledQuests;

    private static T SceneResolve<T>() where T : class
    {
        var scope = SceneInjector.Instance?.CurrentSceneScope;
        if (scope == null) return null;
        try { return scope.Resolve<T>(); } catch { return null; }
    }

    private InventoryUIManager Inv => SceneResolve<InventoryUIManager>();
    private TextBeginner TB => SceneResolve<TextBeginner>();

    private void Awake()
    {
        if (transform.parent != null) transform.SetParent(null);

        QuestManager rootQM = null;
        try { rootQM = GameLifetimeScope.Current?.Container?.Resolve<QuestManager>(); }
        catch { }

        if (rootQM != null && rootQM != this)
        {
            enabled = false;
            return;
        }

        DontDestroyOnLoad(gameObject);
        if (autoLoad) LoadAllQuests();
    }

    private void Start()
    {
        SubscribeToEvents();
    }

    private void LoadAllQuests()
    {
        allQuestData.Clear();
        QuestSO[] loaded = Resources.LoadAll<QuestSO>(questsFolder);

        if (loaded == null || loaded.Length == 0)
        {
            Logger.LogWarning(LogModule.QuestSystem,
                $"LoadAllQuests: 0 квестов в Resources/{questsFolder}");
            return;
        }

        foreach (var quest in loaded)
        {
            if (quest == null) continue;

            string id = quest.questID;
            if (string.IsNullOrEmpty(id))
            {
                id = quest.name;
                quest.questID = id;
                Logger.LogWarning(LogModule.QuestSystem,
                    $"LoadAllQuests: у '{quest.name}' пустой questID — автозаполнен как '{id}'");
            }

            if (!allQuestData.ContainsKey(id))
                allQuestData.Add(id, quest);
            else
                Logger.LogWarning(LogModule.QuestSystem,
                    $"LoadAllQuests: дубликат questID '{id}' (у '{quest.name}')");
        }

        Logger.Log(LogModule.QuestSystem,
            $"LoadAllQuests: загружено {allQuestData.Count} квестов из Resources/{questsFolder}");
    }

    private void SubscribeToEvents()
    {
        var em = EM;
        if (em != null)
        {
            em.OnEventExecuted -= OnEventExecuted;
            em.OnEventExecuted += OnEventExecuted;
        }
    }

    public void StartAutoQuests()
    {
        foreach (var kvp in allQuestData)
        {
            var q = kvp.Value;
            if (q == null) continue;
            if (q.autoStart && CanStartQuest(q))
                StartQuest(q.questID);
        }
    }

    public void StartQuest(string questID)
    {
        if (!allQuestData.TryGetValue(questID, out var data)) return;
        if (activeQuests.Any(q => q.QuestID == questID)) return;
        if (!CanStartQuest(data)) return;

        var instance = new QuestInstance(data);
        activeQuests.Add(instance);

        ESM?.MarkExecuted($"Quest_{questID}_Started");
        OnQuestStarted?.Invoke(instance);
        OnQuestListChanged?.Invoke();

        if (!string.IsNullOrEmpty(data.startDialogueID))
            TB?.StartDialogue(data.startDialogueID);
    }

    private bool CanStartQuest(QuestSO data)
    {
        foreach (var condition in data.startConditions)
            if (!condition.IsMet()) return false;

        if (!data.repeatable && IsQuestCompleted(data.questID)) return false;
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
            OnQuestUpdated?.Invoke(instance);
            OnObjectiveUpdated?.Invoke(instance);
            CheckQuestCompletion(instance);
            OnQuestListChanged?.Invoke();
        }
    }

    public void UpdateQuestByItem(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;

        var copy = new List<QuestInstance>(activeQuests);
        foreach (var instance in copy)
            foreach (var obj in instance.Objectives)
            {
                if (obj.type != QuestObjectiveType.Collect) continue;
                if (obj.GetTargetItemId() != itemId) continue;
                UpdateQuest(instance.QuestID, obj.objectiveID, count);
            }
    }

    public void UpdateQuestByEquip(string itemId, int count = 1)
    {
        if (string.IsNullOrEmpty(itemId) || count <= 0) return;

        var copy = new List<QuestInstance>(activeQuests);
        foreach (var instance in copy)
            foreach (var obj in instance.Objectives)
            {
                if (obj.type != QuestObjectiveType.UseItem && obj.type != QuestObjectiveType.Interact) continue;
                if (obj.GetTargetItemId() != itemId) continue;
                UpdateQuest(instance.QuestID, obj.objectiveID, count);
            }
    }

    public void UpdateQuestByLocation(string locationName)
    {
        if (string.IsNullOrEmpty(locationName)) return;

        var copy = new List<QuestInstance>(activeQuests);
        foreach (var instance in copy)
            foreach (var obj in instance.Objectives)
            {
                if (obj.type != QuestObjectiveType.GoTo) continue;
                if (obj.targetLocation != locationName) continue;
                UpdateQuest(instance.QuestID, obj.objectiveID, 1);
            }
    }

    public void UpdateQuestByNPC(string npcName)
    {
        if (string.IsNullOrEmpty(npcName)) return;

        var copy = new List<QuestInstance>(activeQuests);
        foreach (var instance in copy)
            foreach (var obj in instance.Objectives)
            {
                if (obj.type != QuestObjectiveType.TalkTo) continue;
                if (obj.targetNPC != npcName) continue;
                UpdateQuest(instance.QuestID, obj.objectiveID, 1);
            }
    }

    private void CheckQuestCompletion(QuestInstance instance)
    {
        if (instance.IsComplete()) CompleteQuest(instance.QuestID);
    }

    public void CompleteQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null) return;

        var inv = Inv;
        if (inv != null && instance.Data.rewards != null)
        {
            foreach (var reward in instance.Data.rewards)
            {
                string itemId = reward.GetItemId();
                if (string.IsNullOrEmpty(itemId) || reward.amount <= 0) continue;

                if (inv.GetFreeSpaceFor(itemId, reward.amount) < reward.amount)
                {
                    QN?.ShowCustom($"Нет места для награды: {itemId} x{reward.amount}", new Color(1f, 0.4f, 0.4f));
                    return;
                }
            }
        }

        activeQuests.Remove(instance);
        completedQuests.Add(instance);
        GiveRewards(instance);

        if (instance.Data.rewards != null)
        {
            var fm = FM;
            foreach (var reward in instance.Data.rewards)
                if (!string.IsNullOrEmpty(reward.flagToSet) && fm != null)
                    fm.SetFlag(reward.flagToSet, true);
        }

        OnQuestCompleted?.Invoke(instance);
        OnQuestListChanged?.Invoke();
        ESM?.MarkExecuted($"Quest_{questID}_Completed");

        if (!string.IsNullOrEmpty(instance.Data.completeDialogueID))
            TB?.StartDialogue(instance.Data.completeDialogueID);
    }

    private void GiveRewards(QuestInstance instance)
    {
        var inv = Inv;
        if (inv == null) return;

        foreach (var reward in instance.Data.rewards)
        {
            string itemId = reward.GetItemId();
            if (string.IsNullOrEmpty(itemId) || reward.amount <= 0) continue;
            inv.AddItem(itemId, reward.amount);
        }
    }

    private void OnEventExecuted(GameEvent evt, EventContext context) { }

    public bool IsQuestActive(string questID) => activeQuests.Any(q => q.QuestID == questID);
    public bool IsQuestCompleted(string questID) => completedQuests.Any(q => q.QuestID == questID);
    public QuestInstance GetQuest(string questID) => activeQuests.FirstOrDefault(q => q.QuestID == questID);
    public List<QuestInstance> GetActiveQuests() => activeQuests;
    public List<QuestInstance> GetCompletedQuests() => completedQuests;

    public void CancelQuest(string questID)
    {
        var instance = activeQuests.FirstOrDefault(q => q.QuestID == questID);
        if (instance == null)
        {
            Logger.LogWarning(LogModule.QuestSystem,
                $"CancelQuest: '{questID}' не найден среди активных " +
                $"(active={activeQuests.Count}, id пустой? {string.IsNullOrEmpty(questID)})");
            return;
        }

        instance.IsCanceled = true;
        activeQuests.Remove(instance);
        canceledQuests.Add(instance);

        Logger.Log(LogModule.QuestSystem,
            $"CancelQuest: '{questID}' → canceled ({canceledQuests.Count} total)");

        ESM?.MarkExecuted($"Quest_{questID}_Canceled");
        OnQuestFailed?.Invoke(instance);
        OnQuestListChanged?.Invoke();
    }

    public void ClearAllQuests()
    {
        activeQuests.Clear();
        completedQuests.Clear();
        canceledQuests.Clear();
    }

    public QuestInstance RestoreQuestState(string questID, bool isCompleted, bool isCanceled, Dictionary<string, int> objectiveProgress)
    {
        if (string.IsNullOrEmpty(questID))
        {
            Logger.LogWarning(LogModule.QuestSystem,
                "RestoreQuestState: пустой questID в сейве");
            return null;
        }

        if (activeQuests.Any(q => q.QuestID == questID)) return null;
        if (completedQuests.Any(q => q.QuestID == questID)) return null;
        if (canceledQuests.Any(q => q.QuestID == questID)) return null;

        if (!allQuestData.TryGetValue(questID, out var data))
        {
            string available = string.Join(", ", allQuestData.Keys.Take(10));
            if (allQuestData.Count > 10) available += "...";
            Logger.LogWarning(LogModule.QuestSystem,
                $"RestoreQuestState: QuestSO '{questID}' не найден в Resources/{questsFolder}. " +
                $"Доступные: [{available}]");
            return null;
        }

        var instance = new QuestInstance(data);

        if (objectiveProgress != null)
            foreach (var obj in instance.Objectives)
                if (objectiveProgress.TryGetValue(obj.objectiveID, out int count))
                    obj.currentCount = Mathf.Clamp(count, 0, obj.requiredCount);

        if (isCanceled) { instance.IsCanceled = true; canceledQuests.Add(instance); }
        else if (isCompleted) completedQuests.Add(instance);
        else activeQuests.Add(instance);

        Logger.LogVerbose(LogModule.QuestSystem,
            $"RestoreQuestState: '{questID}' → " +
            $"{(isCanceled ? "canceled" : isCompleted ? "completed" : "active")}, " +
            $"objectives={(objectiveProgress?.Count ?? 0)}");

        return instance;
    }

    public System.Collections.Generic.IEnumerable<string> GetAllQuestIDs() => allQuestData.Keys;
}