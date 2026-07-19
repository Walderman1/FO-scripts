// EventRequirements.cs - ПОЛНАЯ ВЕРСИЯ
using UnityEngine;

[System.Serializable]
public abstract class EventRequirement
{
    public abstract bool Check(EventContext context);
}

// ============ СТАНДАРТНЫЕ ТРЕБОВАНИЯ ============

[System.Serializable]
public class RequirementHasItem : EventRequirement
{
    public ItemType itemType;
    public int minCount = 1;

    public override bool Check(EventContext context)
    {
        // Проверка наличия предмета в инвентаре
        // Здесь нужна реализация проверки
        return true; // Заглушка
    }
}

// RequirementFlag - ИСПОЛЬЗУЕТ ОБЪЕДИНЕННЫЙ FlagManager
[System.Serializable]
public class RequirementFlag : EventRequirement
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        // ✅ Используем FlagManager вместо GlobalFlagManager
        return FlagManager.Instance?.GetFlag(flagName) == flagValue;
    }
}

[System.Serializable]
public class RequirementLocation : EventRequirement
{
    public string locationName;

    public override bool Check(EventContext context)
    {
        string currentLocation = GetCurrentLocationName();
        return currentLocation == locationName;
    }

    private string GetCurrentLocationName()
    {
        GameObject[] locations = GameObject.FindGameObjectsWithTag("Location");
        foreach (var loc in locations)
        {
            if (loc.activeSelf)
                return loc.name;
        }
        return string.Empty;
    }
}

[System.Serializable]
public class RequirementEventExecuted : EventRequirement
{
    public string eventID;
    public bool executed = true;  // true = выполнено, false = НЕ выполнено

    public override bool Check(EventContext context)
    {
        return EventStateManager.Instance?.IsExecuted(eventID) == executed;
    }
}

// ============ ДОПОЛНИТЕЛЬНЫЕ ТРЕБОВАНИЯ ============

/// <summary>
/// Проверяет, сколько раз событие выполнялось (от min до max)
/// </summary>
[System.Serializable]
public class RequirementEventExecutionCount : EventRequirement
{
    public string eventID;
    public int minCount = 1;
    public int maxCount = int.MaxValue;

    public override bool Check(EventContext context)
    {
        int count = EventStateManager.Instance?.GetExecutionCount(eventID) ?? 0;
        return count >= minCount && count <= maxCount;
    }
}

/// <summary>
/// Случайный шанс выполнения (0 = 0%, 1 = 100%)
/// </summary>
[System.Serializable]
public class RequirementRandomChance : EventRequirement
{
    [Range(0f, 1f)]
    public float chance = 0.5f;
    public bool useSeed = false;
    public int seed = 0;

    public override bool Check(EventContext context)
    {
        if (useSeed)
        {
            Random.InitState(seed);
            float result = Random.value;
            Random.InitState(System.Environment.TickCount);
            return result <= chance;
        }
        return Random.value <= chance;
    }
}

/// <summary>
/// Пользовательское условие через метод в коде
/// </summary>
[System.Serializable]
public class RequirementCustomCondition : EventRequirement
{
    public string methodName;
    public GameObject targetObject;

    public override bool Check(EventContext context)
    {
        if (targetObject == null || string.IsNullOrEmpty(methodName))
        {
            Debug.LogWarning("RequirementCustomCondition: targetObject или methodName не заданы!");
            return true;
        }

        var method = targetObject.GetType().GetMethod(methodName);
        if (method != null)
        {
            try
            {
                return (bool)method.Invoke(targetObject, new object[] { context });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RequirementCustomCondition: ошибка вызова метода {methodName}: {e.Message}");
                return false;
            }
        }
        else
        {
            Debug.LogWarning($"RequirementCustomCondition: метод {methodName} не найден на {targetObject.name}");
            return false;
        }
    }
}