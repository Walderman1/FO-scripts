using System.Collections.Generic;
using UnityEngine;

public class AbilityManager : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private List<Ability> abilities = new List<Ability>();

    [Header("UI")]
    [SerializeField] private Transform abilitySlotsParent;
    [SerializeField] private GameObject abilitySlotPrefab;

    [Header("Settings")]
    [SerializeField] private int maxAbilities = 4;

    private Dictionary<KeyCode, Ability> abilityMap = new Dictionary<KeyCode, Ability>();

    private void Start()
    {
        RegisterAbilities();
        SetupUI();
        Logger.Log(LogModule.Abilities, $"AbilityManager инициализирован. Найдено способностей: {abilities.Count}");
    }

    private void RegisterAbilities()
    {
        abilityMap.Clear();

        foreach (Ability ability in abilities)
        {
            if (!abilityMap.ContainsKey(ability.activationKey))
            {
                abilityMap.Add(ability.activationKey, ability);
                Logger.Log(LogModule.Abilities, $"Способность {ability.abilityName} зарегистрирована с клавишей {ability.activationKey}");
            }
            else
            {
                Logger.LogWarning(LogModule.Abilities, $"Клавиша {ability.activationKey} уже используется другой способностью");
            }
        }
    }

    private void SetupUI()
    {
        if (abilitySlotPrefab == null)
        {
            Logger.LogWarning(LogModule.Abilities, "AbilitySlotPrefab не назначен, UI не будет создан");
            return;
        }

        int slotsToCreate = Mathf.Min(abilities.Count, maxAbilities);
        for (int i = 0; i < slotsToCreate; i++)
        {
            GameObject slot = Instantiate(abilitySlotPrefab, abilitySlotsParent);
        }

        Logger.Log(LogModule.Abilities, $"Создано {slotsToCreate} слотов способностей");
    }

    public T GetAbility<T>() where T : Ability
    {
        foreach (Ability ability in abilities)
        {
            if (ability is T)
            {
                Logger.Log(LogModule.Abilities, $"Найдена способность {ability.abilityName} типа {typeof(T).Name}");
                return ability as T;
            }
        }

        Logger.LogWarning(LogModule.Abilities, $"Способность типа {typeof(T).Name} не найдена");
        return null;
    }

    public void AddAbility(Ability ability)
    {
        if (!abilities.Contains(ability))
        {
            abilities.Add(ability);
            RegisterAbilities();
            Logger.Log(LogModule.Abilities, $"Способность {ability.abilityName} добавлена");
        }
        else
        {
            Logger.LogWarning(LogModule.Abilities, $"Способность {ability.abilityName} уже существует");
        }
    }

    public void RemoveAbility(Ability ability)
    {
        if (abilities.Remove(ability))
        {
            RegisterAbilities();
            Logger.Log(LogModule.Abilities, $"Способность {ability.abilityName} удалена");
        }
        else
        {
            Logger.LogWarning(LogModule.Abilities, $"Способность {ability.abilityName} не найдена для удаления");
        }
    }
}
