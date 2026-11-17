using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "Квест", menuName = "|Квесты|/Квест")]
public class QuestSO : ScriptableObject
{
    [Header("Основное")]
    public string questID;
    public string questName;
    [TextArea(2, 4)] public string description;
    public Sprite icon;
    public int priority = 0;

    [Header("Тип квеста")]
    public QuestType questType = QuestType.Fetch;

    [Header("Цели")]
    public List<QuestObjective> objectives = new List<QuestObjective>();

    [Header("Награды")]
    public List<QuestReward> rewards = new List<QuestReward>();

    [Header("Условия начала")]
    public List<QuestCondition> startConditions = new List<QuestCondition>();

    [Header("Диалоги")]
    public string startDialogueID;
    public string completeDialogueID;

    [Header("Настройки")]
    public bool autoStart = false;
    public bool repeatable = false;
    public bool isHidden = false;
}

public enum QuestType
{
    [InspectorName("Собрать")] Fetch,
    [InspectorName("Убить")] Kill,
    [InspectorName("Поговорить")] Talk,
    [InspectorName("Исследовать")] Explore,
    [InspectorName("Использовать")] Use,
    [InspectorName("Сопроводить")] Escort,
    [InspectorName("Коллекция")] Collection,
    [InspectorName("Сюжетный")] Story
}

[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string description;
    public int requiredCount = 1;
    public int currentCount = 0;
    public bool isOptional = false;

    public ItemSO targetItemSO;

    [System.Obsolete("Используйте targetItemSO")]
    public ItemType targetItem = ItemType.None;

    public string targetNPC;
    public string targetLocation;
    public QuestObjectiveType type;

    public string GetTargetItemId()
    {
        if (targetItemSO != null) return targetItemSO.id;

#pragma warning disable CS0618
        if (targetItem != ItemType.None)
        {
            // ★ ScriptableObject → GameReferences.Instance оправдан
            var db = GameReferences.Instance?.ItemDatabase;
            if (db == null)
            {
                Logger.LogWarning(LogModule.QuestSystem,
                    $"QuestObjective '{objectiveID}': ItemDatabase недоступен");
                return "";
            }
            targetItemSO = db.GetItemByLegacyType(targetItem);
            return targetItemSO != null ? targetItemSO.id : "";
        }
#pragma warning restore CS0618

        return "";
    }

    public bool IsComplete() => currentCount >= requiredCount;
}

public enum QuestObjectiveType
{
    [InspectorName("Собрать")] Collect,
    [InspectorName("Поговорить с")] TalkTo,
    [InspectorName("Посетить")] GoTo,
    [InspectorName("Использовать")] UseItem,
    [InspectorName("Убить")] Kill,
    [InspectorName("Взаимодействовать")] Interact
}

[System.Serializable]
public class QuestReward
{
    public ItemSO itemSO;

    [System.Obsolete("Используйте itemSO")]
    public ItemType item = ItemType.None;

    public int amount = 1;
    public int experience = 0;
    public string flagToSet;

    public string GetItemId()
    {
        if (itemSO != null) return itemSO.id;

#pragma warning disable CS0618
        if (item != ItemType.None)
        {
            var db = GameReferences.Instance?.ItemDatabase;
            if (db == null) return "";
            itemSO = db.GetItemByLegacyType(item);
            return itemSO != null ? itemSO.id : "";
        }
#pragma warning restore CS0618

        return "";
    }
}

[System.Serializable]
public class QuestCondition
{
    public string flagName;
    public bool flagValue = true;
    public string questID;
    public bool questCompleted = true;

    public bool IsMet()
    {
        if (!string.IsNullOrEmpty(flagName))
        {
            bool currentValue = GameReferences.Instance?.FlagManager?.GetFlag(flagName) ?? false;
            return currentValue == flagValue;
        }

        if (!string.IsNullOrEmpty(questID))
        {
            bool isCompleted = GameReferences.Instance?.QuestManager?.IsQuestCompleted(questID) ?? false;
            return isCompleted == questCompleted;
        }

        return true;
    }
}