using UnityEngine;

[System.Serializable]
public abstract class TriggerCondition
{
    public abstract bool Check(EventContext context);
}

[System.Serializable]
public class TriggerConditionItem : TriggerCondition
{
    public ItemType targetItem;
    public bool requireNotEquipped = false;

    public override bool Check(EventContext context)
    {
        if (context.ItemType != targetItem)
        {
            Logger.Log(LogModule.Event, $"Условие предмета: {targetItem} не совпадает с контекстом");
            return false;
        }

        if (requireNotEquipped)
        {
            bool isEquipped = EquipmentSystem.Instance?.IsEquipped(targetItem) ?? false;
            bool result = !isEquipped;
            Logger.Log(LogModule.Event, $"Условие предмета: {targetItem} {(result ? "не экипирован" : "экипирован")}");
            return result;
        }

        Logger.Log(LogModule.Event, $"Условие предмета: {targetItem} выполнено");
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
        if (string.IsNullOrEmpty(locationName))
        {
            Logger.Log(LogModule.Event, "Условие локации: имя локации пустое, условие пропущено");
            return true;
        }

        string currentLocation = GetCurrentLocationFromTextBeginner();

        if (string.IsNullOrEmpty(currentLocation))
        {
            currentLocation = context.LocationName ?? GetCurrentLocationName();
        }

        bool result;
        if (exactMatch)
            result = currentLocation == locationName;
        else
            result = currentLocation.Contains(locationName);

        Logger.Log(LogModule.Event, $"Условие локации: текущая '{currentLocation}', требуется '{locationName}' (точное={exactMatch}): {(result ? "успешно" : "провал")}");
        return result;
    }

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
public class TriggerConditionFlag : TriggerCondition
{
    public string flagName;
    public bool flagValue = true;

    public override bool Check(EventContext context)
    {
        bool result = FlagManager.Instance?.GetFlag(flagName) == flagValue;
        Logger.Log(LogModule.Event, $"Условие флага: {flagName} = {flagValue}: {(result ? "успешно" : "провал")}");
        return result;
    }
}

[System.Serializable]
public class TriggerConditionCustom : TriggerCondition
{
    public string customMethodName;

    public override bool Check(EventContext context)
    {
        Logger.Log(LogModule.Event, $"Пользовательское условие: {customMethodName} - всегда выполняется");
        return true;
    }
}
