// TriggerCondition.cs
using UnityEngine;

[System.Serializable]
public abstract class TriggerCondition
{
    public abstract bool Check(EventContext context);
}

// Конкретные условия
[System.Serializable]
public class TriggerConditionItem : TriggerCondition
{
    public ItemType targetItem;
    public bool requireNotEquipped = false;

    public override bool Check(EventContext context)
    {
        if (context.ItemType != targetItem) return false;

        if (requireNotEquipped)
        {
            return !EquipmentSystem.Instance?.IsEquipped(targetItem) ?? true;
        }
        return true;
    }
}

[System.Serializable]
public class TriggerConditionLocation : TriggerCondition
{
    public string locationName;
    public bool exactMatch = true;

    public override bool Check(EventContext context)
    {
        if (string.IsNullOrEmpty(locationName)) return true;

        // ✅ БЕРЁМ ИЗ TEXTBEGINNER
        string currentLocation = GetCurrentLocationFromTextBeginner();

        // Если не нашли в TextBeginner, пробуем через контекст
        if (string.IsNullOrEmpty(currentLocation))
        {
            currentLocation = context.LocationName ?? GetCurrentLocationName();
        }

        if (exactMatch)
            return currentLocation == locationName;
        else
            return currentLocation.Contains(locationName);
    }

    // ✅ БЕРЁТ ЛОКАЦИЮ ИЗ TEXTBEGINNER
    private string GetCurrentLocationFromTextBeginner()
    {
        TextBeginner textBeginner = Object.FindObjectOfType<TextBeginner>();
        if (textBeginner != null)
        {
            var field = textBeginner.GetType().GetField("currentLocation",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                var loc = field.GetValue(textBeginner) as GameObject;
                if (loc != null)
                {
                    return loc.name;
                }
            }
        }
        return string.Empty;
    }

    // Запасной метод (если TextBeginner не найден)
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

// TriggerConditionFlag.cs - ИСПОЛЬЗУЕТ ОБЪЕДИНЕННЫЙ FlagManager
[System.Serializable]
public class TriggerConditionFlag : TriggerCondition
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        // ✅ Используем FlagManager вместо GameStateManager
        return FlagManager.Instance?.GetFlag(flagName) == flagValue;
    }
}

[System.Serializable]
public class TriggerConditionCustom : TriggerCondition
{
    public string customMethodName;

    public override bool Check(EventContext context)
    {
        return true;
    }
}