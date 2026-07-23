using UnityEngine;

[System.Serializable]
public abstract class EventRequirement
{
    public abstract bool Check(EventContext context);
}

[System.Serializable]
public class RequirementHasItem : EventRequirement
{
    public ItemType itemType;
    public int minCount = 1;

    public override bool Check(EventContext context)
    {
        Logger.Log(LogModule.Event, $"Проверка наличия предмета {itemType} (минимум {minCount})");
        return true;
    }
}

[System.Serializable]
public class RequirementFlag : EventRequirement
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        bool result = FlagManager.Instance?.GetFlag(flagName) == flagValue;
        Logger.Log(LogModule.Event, $"Проверка флага {flagName} = {flagValue}: {(result ? "успешно" : "провал")}");
        return result;
    }
}

[System.Serializable]
public class RequirementLocation : EventRequirement
{
    public string locationName;

    public override bool Check(EventContext context)
    {
        string currentLocation = GetCurrentLocationName();
        bool result = currentLocation == locationName;
        Logger.Log(LogModule.Event, $"Проверка локации: текущая '{currentLocation}', требуется '{locationName}': {(result ? "успешно" : "провал")}");
        return result;
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
    public bool executed = true;

    public override bool Check(EventContext context)
    {
        bool result = EventStateManager.Instance?.IsExecuted(eventID) == executed;
        Logger.Log(LogModule.Event, $"Проверка события '{eventID}' выполнено={executed}: {(result ? "успешно" : "провал")}");
        return result;
    }
}

[System.Serializable]
public class RequirementEventExecutionCount : EventRequirement
{
    public string eventID;
    public int minCount = 1;
    public int maxCount = int.MaxValue;

    public override bool Check(EventContext context)
    {
        int count = EventStateManager.Instance?.GetExecutionCount(eventID) ?? 0;
        bool result = count >= minCount && count <= maxCount;
        Logger.Log(LogModule.Event, $"Проверка количества выполнений '{eventID}': {count} (требуется {minCount}-{maxCount}): {(result ? "успешно" : "провал")}");
        return result;
    }
}

[System.Serializable]
public class RequirementRandomChance : EventRequirement
{
    [Range(0f, 1f)]
    public float chance = 0.5f;
    public bool useSeed = false;
    public int seed = 0;

    public override bool Check(EventContext context)
    {
        bool result;
        if (useSeed)
        {
            Random.InitState(seed);
            float value = Random.value;
            Random.InitState(System.Environment.TickCount);
            result = value <= chance;
        }
        else
        {
            result = Random.value <= chance;
        }

        Logger.Log(LogModule.Event, $"Проверка случайного шанса {chance * 100}%: {(result ? "успешно" : "провал")}");
        return result;
    }
}

[System.Serializable]
public class RequirementCustomCondition : EventRequirement
{
    public string methodName;
    public GameObject targetObject;

    public override bool Check(EventContext context)
    {
        if (targetObject == null || string.IsNullOrEmpty(methodName))
        {
            Logger.LogWarning(LogModule.Event, "RequirementCustomCondition: targetObject или methodName не заданы");
            return true;
        }

        var method = targetObject.GetType().GetMethod(methodName);
        if (method != null)
        {
            try
            {
                bool result = (bool)method.Invoke(targetObject, new object[] { context });
                Logger.Log(LogModule.Event, $"Пользовательское условие '{methodName}': {(result ? "успешно" : "провал")}");
                return result;
            }
            catch (System.Exception e)
            {
                Logger.LogError(LogModule.Event, $"RequirementCustomCondition: ошибка вызова метода {methodName}: {e.Message}");
                return false;
            }
        }
        else
        {
            Logger.LogWarning(LogModule.Event, $"RequirementCustomCondition: метод {methodName} не найден на {targetObject.name}");
            return false;
        }
    }
}
