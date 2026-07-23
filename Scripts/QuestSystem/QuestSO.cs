using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewQuest", menuName = "Quests/Quest")]
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
    Fetch, Kill, Talk, Explore, Use, Escort, Collection, Story
}

[System.Serializable]
public class QuestObjective
{
    public string objectiveID;
    public string description;
    public int requiredCount = 1;
    public int currentCount = 0;
    public bool isOptional = false;
    public ItemType targetItem;
    public string targetNPC;
    public string targetLocation;
    public QuestObjectiveType type;

    public bool IsComplete()
    {
        return currentCount >= requiredCount;
    }
}

public enum QuestObjectiveType
{
    Collect, TalkTo, GoTo, UseItem, Kill, Interact
}

[System.Serializable]
public class QuestReward
{
    public ItemType item;
    public int amount = 1;
    public int experience = 0;
    public string flagToSet;
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
            bool currentValue = FlagManager.Instance?.GetFlag(flagName) ?? false;
            return currentValue == flagValue;
        }

        if (!string.IsNullOrEmpty(questID))
        {
            bool isCompleted = QuestManager.Instance?.IsQuestCompleted(questID) ?? false;
            return isCompleted == questCompleted;
        }

        return true;
    }
}
