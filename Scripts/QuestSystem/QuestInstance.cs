using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[Serializable]
public class QuestInstance
{
    public string QuestID { get; private set; }
    public string QuestName { get; private set; }
    public string Description { get; private set; }
    public Sprite Icon { get; private set; }
    public QuestType QuestType { get; private set; }
    public int Priority { get; private set; }
    public bool IsHidden { get; private set; }
    public bool IsCanceled { get; set; } = false;

    public List<QuestObjective> Objectives { get; private set; }
    public QuestSO Data { get; private set; }

    public float Progress
    {
        get
        {
            if (Objectives.Count == 0) return 0f;

            int totalRequired = 0;
            int totalCollected = 0;

            foreach (var obj in Objectives)
            {
                if (!obj.isOptional)
                {
                    totalRequired += obj.requiredCount;
                    totalCollected += obj.currentCount;
                }
            }

            return totalRequired > 0 ? (float)totalCollected / totalRequired : 0f;
        }
    }

    public bool IsComplete()
    {
        foreach (var obj in Objectives)
        {
            if (!obj.isOptional && !obj.IsComplete())
                return false;
        }
        return true;
    }

    public QuestInstance(QuestSO data)
    {
        Data = data;
        QuestID = data.questID;
        QuestName = data.questName;
        Description = data.description;
        Icon = data.icon;
        QuestType = data.questType;
        Priority = data.priority;
        IsHidden = data.isHidden;

        Objectives = new List<QuestObjective>();
        foreach (var obj in data.objectives)
        {
            Objectives.Add(new QuestObjective
            {
                objectiveID = obj.objectiveID,
                description = obj.description,
                requiredCount = obj.requiredCount,
                currentCount = obj.currentCount,
                isOptional = obj.isOptional,
                targetItem = obj.targetItem,
                targetNPC = obj.targetNPC,
                targetLocation = obj.targetLocation,
                type = obj.type
            });
        }

        Logger.Log(LogModule.QuestSystem, $"Создан экземпляр квеста: {QuestName} (ID: {QuestID})");
    }

    public QuestObjective GetObjective(string id)
    {
        return Objectives.FirstOrDefault(o => o.objectiveID == id);
    }

    public string GetProgressText()
    {
        int totalRequired = 0;
        int totalCollected = 0;

        foreach (var obj in Objectives)
        {
            if (!obj.isOptional)
            {
                totalRequired += obj.requiredCount;
                totalCollected += obj.currentCount;
            }
        }

        return $"{totalCollected}/{totalRequired}";
    }
}
