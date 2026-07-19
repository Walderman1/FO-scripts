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
    }

    private void RegisterAbilities()
    {
        abilityMap.Clear();

        foreach (Ability ability in abilities)
        {
            if (!abilityMap.ContainsKey(ability.activationKey))
            {
                abilityMap.Add(ability.activationKey, ability);
            }
        }
    }

    private void SetupUI()
    {
        if (abilitySlotPrefab == null) return;

        for (int i = 0; i < abilities.Count && i < maxAbilities; i++)
        {
            GameObject slot = Instantiate(abilitySlotPrefab, abilitySlotsParent);
            // Настройка UI слота
        }
    }

    public T GetAbility<T>() where T : Ability
    {
        foreach (Ability ability in abilities)
        {
            if (ability is T)
            {
                return ability as T;
            }
        }
        return null;
    }

    public void AddAbility(Ability ability)
    {
        if (!abilities.Contains(ability))
        {
            abilities.Add(ability);
            RegisterAbilities();
        }
    }

    public void RemoveAbility(Ability ability)
    {
        abilities.Remove(ability);
        RegisterAbilities();
    }
}