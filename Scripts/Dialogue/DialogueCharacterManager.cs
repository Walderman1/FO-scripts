using DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

[System.Serializable]
public class CharacterData
{
    public string name;
    public Sprite portrait;
    public GameObject characterObject;
    public Vector2 defaultPosition;
}

public class DialogueCharacterManager : MonoBehaviour
{
    [Header("Character Data")]
    [SerializeField] private CharacterData[] characters;

    [Header("References")]
    [SerializeField] private Image portraitImage;
    [SerializeField] private Transform characterContainer;

    [Header("Animation Settings")]
    [SerializeField] private DialogueData dialogueConfig;
    [Inject] private GameReferences _refs;

    private SceneSlideTransition _sceneSlideTransition;

    private GameReferences Refs => _refs ?? GameReferences.Instance;

    private Dictionary<string, CharacterData> characterDict = new Dictionary<string, CharacterData>();
    private Dictionary<string, GameObject> activeCharacters = new Dictionary<string, GameObject>();
    private Dictionary<string, Coroutine> activeAnimations = new Dictionary<string, Coroutine>();
    private Dictionary<string, GameObject> resourceCharacters = new Dictionary<string, GameObject>();
    private bool resourcesLoaded = false;

    private GameObject currentActiveCharacter;
    private string currentCharacterName;

    private DialogueData Config
    {
        get
        {
            if (dialogueConfig == null && Refs != null) dialogueConfig = Refs.DialogueConfig;
            return dialogueConfig;
        }
    }

    private SceneSlideTransition GetSlide()
    {
        if (_sceneSlideTransition != null) return _sceneSlideTransition;

        if (_refs != null) _sceneSlideTransition = _refs.SceneSlideTransition;
        if (_sceneSlideTransition != null) return _sceneSlideTransition;

        _sceneSlideTransition = FindAnyObjectByType<SceneSlideTransition>(FindObjectsInactive.Include);
        return _sceneSlideTransition;
    }

    private void Awake()
    {
        if (dialogueConfig == null && Refs != null) dialogueConfig = Refs.DialogueConfig;

        if (characters != null)
        {
            foreach (CharacterData data in characters)
            {
                if (data == null || string.IsNullOrEmpty(data.name)) continue;
                if (!characterDict.ContainsKey(data.name)) characterDict.Add(data.name, data);
            }
        }

        LoadCharactersFromResources();
    }

    private void LoadCharactersFromResources()
    {
        if (resourcesLoaded) return;

        string path = Config != null ? Config.CharactersResourcesPath : "Characters";
        GameObject[] loadedCharacters = Resources.LoadAll<GameObject>(path);

        if (loadedCharacters != null && loadedCharacters.Length > 0)
        {
            Logger.Log(LogModule.Dialogue, $"Загружено {loadedCharacters.Length} персонажей из Resources/{path}");
            foreach (GameObject characterPrefab in loadedCharacters)
            {
                if (characterPrefab != null && !resourceCharacters.ContainsKey(characterPrefab.name))
                    resourceCharacters.Add(characterPrefab.name, characterPrefab);
            }
        }

        resourcesLoaded = true;
    }

    public void ProcessActions(DialogueNode node)
    {
        if (node == null) return;

        if (!string.IsNullOrEmpty(node.speaker))
        {
            ShowCharacter(node.speaker);
            SetPortrait(node.speaker);
        }

        if (node.actions == null) return;

        foreach (DialogueAction action in node.actions)
        {
            switch (action.type)
            {
                case "ShowCharacter": ShowCharacter(action.target); break;
                case "HideCharacter": HideCharacter(action.target); break;
                case "MoveCharacter": MoveCharacter(action.target, action.value); break;
                case "SetPortrait": SetPortrait(action.target); break;
            }
        }
    }

    public void ShowCharacter(string characterName)
    {
        if (string.IsNullOrEmpty(characterName)) return;

        GameObject characterPrefab = null;
        CharacterData data = null;

        if (characterDict.ContainsKey(characterName))
        {
            data = characterDict[characterName];
            characterPrefab = data.characterObject;
        }

        if (characterPrefab == null && resourceCharacters.ContainsKey(characterName))
            characterPrefab = resourceCharacters[characterName];

        if (characterPrefab == null)
        {
            string path = Config != null ? Config.CharactersResourcesPath : "Characters";
            characterPrefab = Resources.Load<GameObject>($"{path}/{characterName}");
            if (characterPrefab != null && !resourceCharacters.ContainsKey(characterName))
                resourceCharacters.Add(characterName, characterPrefab);
        }

        if (characterPrefab == null)
        {
            Logger.LogError(LogModule.Dialogue, $"Персонаж {characterName} НЕ НАЙДЕН!");
            return;
        }

        if (activeCharacters.ContainsKey(characterName))
        {
            StartAnimation(characterName, FadeCharacter(activeCharacters[characterName], 1f));
            return;
        }

        Vector3 spawnPosition = data != null ? (Vector3)data.defaultPosition : Vector3.zero;
        GameObject characterObj = Instantiate(characterPrefab, characterContainer);
        characterObj.transform.localPosition = spawnPosition;
        characterObj.transform.localScale = Vector3.one;

        activeCharacters[characterName] = characterObj;
        currentActiveCharacter = characterObj;
        currentCharacterName = characterName;

        StartAnimation(characterName, FadeCharacter(characterObj, 1f));
    }

    public void HideCharacter(string characterName)
    {
        if (!activeCharacters.ContainsKey(characterName)) return;
        StartAnimation(characterName, FadeCharacter(activeCharacters[characterName], 0f));
    }

    public void MoveCharacter(string characterName, string position)
    {
        if (!activeCharacters.ContainsKey(characterName))
        {
            ShowCharacter(characterName);
            return;
        }

        GameObject characterObj = activeCharacters[characterName];

        Vector2 targetPos;
        if (position == "left") targetPos = Config != null ? Config.DefaultLeftPosition : new Vector2(-4f, 0f);
        else if (position == "right") targetPos = Config != null ? Config.DefaultRightPosition : new Vector2(4f, 0f);
        else if (position == "center") targetPos = Config != null ? Config.DefaultCenterPosition : new Vector2(0f, 0f);
        else
        {
            string[] parts = position.Split(',');
            if (parts.Length == 2) targetPos = new Vector2(float.Parse(parts[0]), float.Parse(parts[1]));
            else return;
        }

        StartAnimation(characterName, MoveCharacterRoutine(characterObj, targetPos));
    }

    public void SetPortrait(string characterName)
    {
        Sprite portrait = null;
        if (characterDict.ContainsKey(characterName) && characterDict[characterName].portrait != null)
            portrait = characterDict[characterName].portrait;

        if (portrait == null)
        {
            string path = Config != null ? Config.PortraitsResourcesPath : "Portraits";
            portrait = Resources.Load<Sprite>($"{path}/{characterName}");
        }

        if (portraitImage != null) portraitImage.sprite = portrait;
    }

    public void MoveCharacterToSpeakerArea()
    {
        if (currentActiveCharacter == null) return;

        GameObject firstArea = Refs?.FirstCharactersArea;
        if (firstArea == null)
        {
            try { firstArea = GameObject.FindGameObjectWithTag("FirstCharactersArea"); }
            catch (UnityException) { }
        }

        if (firstArea == null) { Logger.LogError(LogModule.Dialogue, "FirstCharactersArea НЕ НАЙДЕН!"); return; }

        Transform charactersFolder = firstArea.transform.Find("Characters");
        if (charactersFolder == null) return;

        Animator animator = currentActiveCharacter.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            animator.Rebind();
            animator.Update(0f);
        }

        currentActiveCharacter.transform.SetParent(charactersFolder);
        currentActiveCharacter.transform.localPosition = Vector3.zero;
        currentActiveCharacter.transform.localScale = Vector3.one;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(Config != null ? Config.SpeakAnimation : "Speak");
        }
    }

    public void MoveCharacterToCurrentLocationReference()
    {
        if (currentActiveCharacter == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Character is null!");
            return;
        }

        var sceneSlide = GetSlide();
        if (sceneSlide == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "SceneSlideTransition недоступен!");
            return;
        }

        GameObject currentScene = sceneSlide.GetCurrentScene();
        if (currentScene == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Нет текущей сцены!");
            return;
        }

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null)
        {
            Logger.LogWarning(LogModule.Dialogue, $"LocationNeighbors не найден на {currentScene.name}");
            return;
        }

        GameObject locationRef = neighbors.GetLocationReference();
        if (locationRef == null)
        {
            Logger.LogWarning(LogModule.Dialogue, $"LocationReference не найден в {currentScene.name}");
            return;
        }

        Logger.LogVerbose(LogModule.Dialogue, $"Перемещаем персонажа на LocationReference: {locationRef.name}");

        Animator animator = currentActiveCharacter.GetComponent<Animator>();
        if (animator != null)
        {
            animator.enabled = false;
            animator.Rebind();
            animator.Update(0f);
        }

        currentActiveCharacter.transform.SetParent(currentScene.transform);
        currentActiveCharacter.transform.position = locationRef.transform.position;
        currentActiveCharacter.transform.rotation = locationRef.transform.rotation;
        currentActiveCharacter.transform.localScale = locationRef.transform.localScale;

        Logger.LogVerbose(LogModule.Dialogue, $"Персонаж перемещён на LocationReference: {locationRef.name}");
        Logger.LogVerbose(LogModule.Dialogue, $"Позиция: {currentActiveCharacter.transform.position}");
        Logger.LogVerbose(LogModule.Dialogue, $"Поворот: {currentActiveCharacter.transform.rotation.eulerAngles}");
        Logger.LogVerbose(LogModule.Dialogue, $"Масштаб: {currentActiveCharacter.transform.localScale}");

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(Config != null ? Config.IdleEyeAnimation : "IdleEye");
        }
    }

    public void ClearAllCharacters()
    {
        foreach (GameObject obj in activeCharacters.Values)
            if (obj != null) Destroy(obj);
        activeCharacters.Clear();

        foreach (Coroutine coroutine in activeAnimations.Values)
            if (coroutine != null) StopCoroutine(coroutine);
        activeAnimations.Clear();

        currentActiveCharacter = null;
        currentCharacterName = null;
    }

    public void MoveCharacterToCurrentScene() => MoveCharacterToCurrentLocationReference();
    public void MoveCharacterToActiveLocationReference() => MoveCharacterToCurrentLocationReference();

    public void MoveCharacterToLocation(GameObject location)
    {
        if (currentActiveCharacter == null || location == null) return;

        Animator animator = currentActiveCharacter.GetComponent<Animator>();
        if (animator != null) { animator.enabled = false; animator.Rebind(); animator.Update(0f); }

        currentActiveCharacter.transform.SetParent(location.transform);
        currentActiveCharacter.transform.localPosition = Vector3.zero;
        currentActiveCharacter.transform.localScale = Vector3.one;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(Config != null ? Config.IdleEyeAnimation : "IdleEye");
        }
    }

    public void MoveCharacterToPosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (currentActiveCharacter == null) return;

        Animator animator = currentActiveCharacter.GetComponent<Animator>();
        if (animator != null) { animator.enabled = false; animator.Rebind(); animator.Update(0f); }

        currentActiveCharacter.transform.position = position;
        currentActiveCharacter.transform.rotation = rotation;
        currentActiveCharacter.transform.localScale = scale;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play(Config != null ? Config.IdleEyeAnimation : "IdleEye");
        }
    }

    public void SetCurrentCharacter(string characterName) => currentCharacterName = characterName;

    public GameObject CurrentCharacter => currentActiveCharacter;
    public bool IsCharacterSpawned => currentActiveCharacter != null;

    private void StartAnimation(string characterName, IEnumerator animationRoutine)
    {
        if (animationRoutine == null) return;
        if (activeAnimations.ContainsKey(characterName))
        {
            StopCoroutine(activeAnimations[characterName]);
            activeAnimations[characterName] = StartCoroutine(animationRoutine);
        }
        else activeAnimations.Add(characterName, StartCoroutine(animationRoutine));
    }

    private IEnumerator FadeCharacter(GameObject characterObj, float targetAlpha)
    {
        if (characterObj == null) yield break;

        CanvasGroup canvasGroup = characterObj.GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = characterObj.AddComponent<CanvasGroup>();

        float startAlpha = canvasGroup.alpha;
        float elapsed = 0f;
        float duration = Config != null ? Config.FadeDuration : 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, elapsed / duration);
            yield return null;
        }

        canvasGroup.alpha = targetAlpha;

        if (targetAlpha == 0f)
        {
            string characterName = null;
            foreach (var kvp in activeCharacters)
                if (kvp.Value == characterObj) { characterName = kvp.Key; break; }

            if (characterName != null)
            {
                activeCharacters.Remove(characterName);
                activeAnimations.Remove(characterName);
            }
            Destroy(characterObj);
        }
    }

    private IEnumerator MoveCharacterRoutine(GameObject characterObj, Vector2 targetPosition)
    {
        if (characterObj == null) yield break;

        Vector2 startPosition = characterObj.transform.localPosition;
        float elapsed = 0f;
        float duration = Config != null ? Config.MoveDuration : 0.5f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            characterObj.transform.localPosition = Vector2.Lerp(startPosition, targetPosition, elapsed / duration);
            yield return null;
        }

        characterObj.transform.localPosition = targetPosition;
    }
}