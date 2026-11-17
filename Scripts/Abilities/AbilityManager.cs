using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using VContainer;

public class AbilityManager : MonoBehaviour
{
    [Header("Abilities")]
    [SerializeField] private List<Ability> abilities = new List<Ability>();

    [Header("Auto Find")]
    [SerializeField] private bool autoFindInChildren = true;
    [SerializeField] private bool onlyDirectChildren = true;
    [SerializeField] private bool appendToInspectorList = true;

    [Header("UI")]
    [SerializeField] private Transform abilitySlotsParent;
    [SerializeField] private GameObject abilitySlotPrefab;

    [Header("Settings")]
    [SerializeField] private int maxAbilities = 4;

    [Header("Character Particles")]
    [SerializeField] private GameObject characterParticlesObject;
    [SerializeField] private string characterTag = "Character";
    [SerializeField] private string characterParticlesTag = "CharacterParticles";

    [Inject] private GameReferences _refs;

    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private Dictionary<string, Ability> abilityMap = new Dictionary<string, Ability>();
    private readonly HashSet<Ability> particlesRequesters = new HashSet<Ability>();
    private Coroutine retryFindParticlesCoroutine;
    private readonly HashSet<Object> externalParticlesRequesters = new HashSet<Object>();
    public GameObject CharacterParticlesObject => characterParticlesObject;

    // ★ Убран static Instance

    private void Awake()
    {
        // Пусто — регистрация через SceneInjector.RegisterFromScene<AbilityManager>
    }

    private void OnDestroy()
    {
        if (retryFindParticlesCoroutine != null)
        {
            StopCoroutine(retryFindParticlesCoroutine);
            retryFindParticlesCoroutine = null;
        }

        var refs = Refs;
        if (refs != null)
            refs.OnSceneReferencesRefreshed -= RefreshCharacterParticles;
    }

    private void Start()
    {
        if (autoFindInChildren) FindAbilitiesInChildren();

        FindCharacterParticlesIfNeeded();
        RegisterAbilities();
        SetupUI();

        var refs = Refs;
        if (refs != null)
        {
            refs.OnSceneReferencesRefreshed -= RefreshCharacterParticles;
            refs.OnSceneReferencesRefreshed += RefreshCharacterParticles;
        }

        Logger.LogVerbose(LogModule.Abilities, $"AbilityManager инициализирован. Зарегистрировано: {abilityMap.Count}");
    }

    private void FindAbilitiesInChildren()
    {
        List<Ability> found = new List<Ability>();

        if (onlyDirectChildren)
        {
            foreach (Transform child in transform)
            {
                Ability ability = child.GetComponent<Ability>();
                if (ability != null) found.Add(ability);
            }
        }
        else
        {
            Ability[] all = GetComponentsInChildren<Ability>(true);
            foreach (Ability ability in all)
            {
                if (ability == null) continue;
                if (ability.gameObject == gameObject) continue;
                found.Add(ability);
            }
        }

        if (appendToInspectorList)
        {
            foreach (Ability ability in found)
                if (!abilities.Contains(ability)) abilities.Add(ability);
        }
        else
        {
            abilities.Clear();
            abilities.AddRange(found);
        }
    }

    private void FindCharacterParticlesIfNeeded()
    {
        if (characterParticlesObject != null && IsSafeSceneObject(characterParticlesObject))
        {
            ApplyParticlesState();
            return;
        }

        if (characterParticlesObject != null && !IsSafeSceneObject(characterParticlesObject))
            characterParticlesObject = null;

        characterParticlesObject = Refs?.CharacterParticles;
        if (characterParticlesObject != null && !IsSafeSceneObject(characterParticlesObject))
            characterParticlesObject = null;

        if (characterParticlesObject == null)
            characterParticlesObject = FindParticlesIncludingInactive();

        if (characterParticlesObject != null)
        {
            if (retryFindParticlesCoroutine != null)
            {
                StopCoroutine(retryFindParticlesCoroutine);
                retryFindParticlesCoroutine = null;
            }

            characterParticlesObject.SetActive(false);
            ApplyParticlesState();
        }
        else if (retryFindParticlesCoroutine == null)
        {
            retryFindParticlesCoroutine = StartCoroutine(RetryFindParticles());
        }
    }

    private static bool IsSafeSceneObject(GameObject go)
    {
        if (go == null) return false;
        if (!go.scene.IsValid()) return false;
        if (!go.scene.isLoaded) return false;
#if UNITY_EDITOR
        if (UnityEditor.EditorUtility.IsPersistent(go)) return false;
        if (UnityEditor.PrefabUtility.IsPartOfPrefabAsset(go)) return false;
#endif
        return true;
    }

    private System.Collections.IEnumerator RetryFindParticles()
    {
        const float timeout = 10f;
        const float step = 0.5f;
        float elapsed = 0f;
        var delay = new WaitForSeconds(step);

        while (elapsed < timeout && characterParticlesObject == null)
        {
            yield return delay;
            elapsed += step;

            var candidate = Refs?.CharacterParticles;
            if (candidate != null && IsSafeSceneObject(candidate)) characterParticlesObject = candidate;
            else characterParticlesObject = FindParticlesIncludingInactive();
        }

        if (characterParticlesObject != null)
        {
            characterParticlesObject.SetActive(false);
            ApplyParticlesState();
        }

        retryFindParticlesCoroutine = null;
    }

    public void RefreshCharacterParticles() => FindCharacterParticlesIfNeeded();

    private GameObject FindParticlesIncludingInactive()
    {
        try
        {
            GameObject go = GameObject.FindGameObjectWithTag(characterParticlesTag);
            if (go != null) return go;
        }
        catch (UnityException) { }

        try
        {
            GameObject character = GameObject.FindGameObjectWithTag(characterTag);
            if (character != null)
            {
                GameObject found = FindByTagRecursive(character.transform, characterParticlesTag);
                if (found != null) return found;
            }
        }
        catch (UnityException) { }

        Scene scene = SceneManager.GetActiveScene();
        GameObject[] roots = scene.GetRootGameObjects();
        foreach (GameObject root in roots)
        {
            GameObject found = FindByTagRecursive(root.transform, characterParticlesTag);
            if (found != null) return found;
        }

        return null;
    }

    private GameObject FindByTagRecursive(Transform parent, string tag)
    {
        if (parent.CompareTag(tag)) return parent.gameObject;
        foreach (Transform child in parent)
        {
            GameObject result = FindByTagRecursive(child, tag);
            if (result != null) return result;
        }
        return null;
    }

    public void SetCharacterParticlesActive(Ability requester, bool active)
    {
        if (requester == null) return;
        if (active) particlesRequesters.Add(requester);
        else particlesRequesters.Remove(requester);
        ApplyParticlesState();
    }

    public void SetCharacterParticlesActiveExternal(Object requester, bool active)
    {
        if (requester == null) return;
        if (active) externalParticlesRequesters.Add(requester);
        else externalParticlesRequesters.Remove(requester);
        ApplyParticlesState();
    }

    private void ApplyParticlesState()
    {
        if (characterParticlesObject == null) return;
        if (!IsSafeSceneObject(characterParticlesObject))
        {
            characterParticlesObject = null;
            return;
        }

        bool shouldBeActive = particlesRequesters.Count > 0 || externalParticlesRequesters.Count > 0;
        if (characterParticlesObject.activeSelf != shouldBeActive)
            characterParticlesObject.SetActive(shouldBeActive);
    }

    private void RegisterAbilities()
    {
        abilityMap.Clear();

        foreach (Ability ability in abilities)
        {
            if (ability == null) continue;
            if (!abilityMap.ContainsKey(ability.abilityName))
                abilityMap.Add(ability.abilityName, ability);
            else
                Logger.LogWarning(LogModule.Abilities, $"Имя '{ability.abilityName}' уже занято");
        }
    }

    private void SetupUI()
    {
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex == 0) return;

        if (abilitySlotPrefab == null || abilitySlotsParent == null)
        {
            Logger.LogVerbose(LogModule.Abilities, "AbilitySlotPrefab или AbilitySlotsParent не назначены");
            return;
        }

        int slotsToCreate = Mathf.Min(abilities.Count, maxAbilities);
        for (int i = 0; i < slotsToCreate; i++)
            Instantiate(abilitySlotPrefab, abilitySlotsParent);
    }

    public IEnumerable<Ability> GetRegisteredAbilities() => abilityMap.Values;

    public T GetAbility<T>() where T : Ability
    {
        foreach (Ability ability in abilities)
            if (ability is T typed) return typed;
        return null;
    }

    public Ability GetAbilityByName(string name)
    {
        if (abilityMap.TryGetValue(name, out Ability ability)) return ability;
        return null;
    }

    public void TryActivate(string abilityName)
    {
        Ability ability = GetAbilityByName(abilityName);
        if (ability == null)
        {
            Logger.LogWarning(LogModule.Abilities, $"Способность '{abilityName}' не найдена");
            return;
        }
        ability.TryActivate();
    }

    public void AddAbility(Ability ability)
    {
        if (ability == null || abilities.Contains(ability)) return;
        abilities.Add(ability);
        RegisterAbilities();
    }

    public void RemoveAbility(Ability ability)
    {
        if (ability == null) return;
        if (abilities.Remove(ability))
        {
            particlesRequesters.Remove(ability);
            ApplyParticlesState();
            RegisterAbilities();
        }
    }
}