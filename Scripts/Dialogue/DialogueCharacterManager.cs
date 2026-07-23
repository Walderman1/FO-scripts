using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class DialogueCharacterManager : MonoBehaviour
{
    [Header("Character Settings")]
    [SerializeField] private List<GameObject> characters;
    [SerializeField] private GameObject[] interactions;
    [SerializeField] private GameObject character;

    [Header("References")]
    [SerializeField] private DialogueData dialogueConfig;

    private GameObject FirstCharactersArea;
    private GameObject SecondCharactersArea;

    private bool isCharacterSpawned;
    private string currentCharacterName;
    private bool isAnimating;

    #region Properties

    public GameObject CurrentCharacter => character;
    public bool IsCharacterSpawned => isCharacterSpawned;
    public bool IsAnimating => isAnimating;
    public string CurrentCharacterName => currentCharacterName;
    public List<GameObject> Characters => characters;
    public GameObject[] Interactions => interactions;

    #endregion

    #region Initialization

    public void Initialize(DialogueData config)
    {
        dialogueConfig = config;
        LoadCharacters();

        if (interactions == null)
        {
            interactions = new GameObject[0];
        }

        Logger.Log(LogModule.Dialogue, "DialogueCharacterManager инициализирован");
    }

    public void SetCharacterAreas(GameObject firstArea, GameObject secondArea)
    {
        FirstCharactersArea = firstArea;
        SecondCharactersArea = secondArea;
        Logger.Log(LogModule.Dialogue, $"Области персонажей установлены: First={FirstCharactersArea?.name}, Second={SecondCharactersArea?.name}");
    }

    public void LoadCharacters()
    {
        string folder = dialogueConfig != null ? dialogueConfig.charactersFolder : "Characters";
        characters = Resources.LoadAll<GameObject>(folder).ToList();
        Logger.Log(LogModule.Dialogue, $"Загружено персонажей: {characters.Count} из папки {folder}");
    }

    #endregion

    #region Character Management

    public void ActivateCharacter(string characterName)
    {
        if (isCharacterSpawned)
        {
            Logger.Log(LogModule.Dialogue, $"Персонаж уже активен, пропуск активации {characterName}");
            return;
        }

        if (FirstCharactersArea == null)
        {
            Logger.LogError(LogModule.Dialogue, "FirstCharactersArea не найден! Невозможно активировать персонажа");
            return;
        }

        string parentName = dialogueConfig != null ? dialogueConfig.charactersParentName : "Characters";
        Transform target = FirstCharactersArea.transform.Find(parentName);

        if (target != null)
        {
            int index = GetCharacterIndexByName(characterName);
            if (index >= 0 && index < characters.Count)
            {
                GameObject newCharacter = Instantiate(characters[index], target);
                if (newCharacter != null)
                {
                    character = newCharacter;
                    isCharacterSpawned = true;
                    Logger.Log(LogModule.Dialogue, $"Персонаж {characterName} активирован");

                    Animator animator = newCharacter.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.Play("Speak");
                        isAnimating = true;
                        Logger.Log(LogModule.Dialogue, $"Анимация Speak запущена для {characterName}");
                    }
                }
            }
            else
            {
                Logger.LogWarning(LogModule.Dialogue, $"Персонаж {characterName} не найден в списке");
            }
        }
        else
        {
            Logger.LogError(LogModule.Dialogue, $"Целевой объект '{parentName}' не найден в FirstCharactersArea");
        }
    }

    public int GetCharacterIndexByName(string characterName)
    {
        for (int i = 0; i < characters.Count; i++)
        {
            if (characters[i] != null && characters[i].name == characterName)
            {
                return i;
            }
        }
        return -1;
    }

    public void SetCurrentCharacter(string characterName)
    {
        currentCharacterName = characterName;
        Logger.Log(LogModule.Dialogue, $"Текущий персонаж установлен: {characterName}");
    }

    public void ClearCharacter()
    {
        if (character != null)
        {
            Logger.Log(LogModule.Dialogue, $"Уничтожение персонажа: {character.name}");
            Destroy(character);
            character = null;
        }
        isCharacterSpawned = false;
        isAnimating = false;
        currentCharacterName = null;
    }

    public void ClearAllCharacters()
    {
        if (FirstCharactersArea == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "FirstCharactersArea не найден, очистка невозможна");
            return;
        }

        string parentName = dialogueConfig != null ? dialogueConfig.charactersParentName : "Characters";
        Transform target = FirstCharactersArea.transform.Find(parentName);
        if (target != null)
        {
            int count = target.childCount;
            foreach (Transform child in target)
            {
                Destroy(child.gameObject);
            }
            Logger.Log(LogModule.Dialogue, $"Очищено {count} персонажей");
        }

        character = null;
        isCharacterSpawned = false;
        isAnimating = false;
        currentCharacterName = null;
    }

    #endregion

    #region Character Movement

    public void MoveCharacterToSpeakerArea()
    {
        if (character == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Персонаж не найден, перемещение невозможно");
            return;
        }

        if (FirstCharactersArea == null)
        {
            Logger.LogError(LogModule.Dialogue, "FirstCharactersArea не найден");
            return;
        }

        string parentName = dialogueConfig != null ? dialogueConfig.charactersParentName : "Characters";
        Transform target = FirstCharactersArea.transform.Find(parentName);
        if (target == null)
        {
            Logger.LogError(LogModule.Dialogue, $"Целевой объект '{parentName}' не найден в FirstCharactersArea");
            return;
        }

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.SetParent(target);
        character.transform.localPosition = Vector3.zero;
        character.transform.localScale = Vector3.one;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("Speak");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен в область диалога: {target.name}");
    }

    public void MoveCharacterToLocation(GameObject location)
    {
        if (character == null || location == null)
        {
            Logger.LogWarning(LogModule.Dialogue, $"Невозможно переместить персонажа: character={character != null}, location={location != null}");
            return;
        }

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.SetParent(location.transform);
        character.transform.localPosition = Vector3.zero;
        character.transform.localScale = Vector3.one;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("IdleEye");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен в локацию: {location.name}");
    }

    public void MoveCharacterToPosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (character == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Персонаж не найден, перемещение невозможно");
            return;
        }

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.position = position;
        character.transform.rotation = rotation;
        character.transform.localScale = scale;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("IdleEye");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен в позицию {position}");
    }

    public void MoveCharacterToCurrentLocationReference()
    {
        if (character == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Персонаж не найден");
            return;
        }

        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Текущая сцена не найдена");
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

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.SetParent(currentScene.transform);
        character.transform.position = locationRef.transform.position;
        character.transform.localScale = locationRef.transform.localScale;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("IdleEye");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен к LocationReference: {locationRef.name} в сцене {currentScene.name}");
    }

    public void MoveCharacterToCurrentScene()
    {
        if (character == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Персонаж не найден");
            return;
        }

        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Текущая сцена не найдена");
            return;
        }

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.SetParent(currentScene.transform);
        character.transform.localPosition = Vector3.zero;
        character.transform.localScale = Vector3.one;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("IdleEye");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен в сцену: {currentScene.name}");
    }

    public void MoveCharacterToActiveLocationReference()
    {
        if (character == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Персонаж не найден");
            return;
        }

        GameObject locationRef = GetActiveLocationReferenceFromCurrentScene();
        if (locationRef == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Активный LocationReference не найден");
            return;
        }

        if (character.TryGetComponent(out Animator an))
        {
            an.enabled = false;
            an.Rebind();
            an.Update(0f);
        }

        character.transform.position = locationRef.transform.position;
        character.transform.localScale = locationRef.transform.localScale;

        if (character.TryGetComponent(out Animator animator))
        {
            animator.enabled = true;
            animator.Play("IdleEye");
        }

        Logger.Log(LogModule.Dialogue, $"Персонаж перемещен к LocationReference: {locationRef.name}");
    }

    private GameObject GetActiveLocationReferenceFromCurrentScene()
    {
        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Logger.LogWarning(LogModule.Dialogue, "Текущая сцена не найдена");
            return null;
        }

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null)
        {
            Logger.LogWarning(LogModule.Dialogue, $"LocationNeighbors не найден на {currentScene.name}");
            return null;
        }

        return neighbors.GetLocationReference();
    }

    #endregion

    #region Interaction Management

    public GameObject GetInteractionByName(string name)
    {
        if (interactions == null || interactions.Length == 0)
        {
            Logger.Log(LogModule.Dialogue, "Список взаимодействий пуст");
            return null;
        }

        if (string.IsNullOrEmpty(name))
        {
            Logger.Log(LogModule.Dialogue, "Имя взаимодействия пустое");
            return null;
        }

        GameObject result = interactions.FirstOrDefault(i => i != null && i.name == name);
        if (result != null)
        {
            Logger.Log(LogModule.Dialogue, $"Найдено взаимодействие: {name}");
        }
        else
        {
            Logger.LogWarning(LogModule.Dialogue, $"Взаимодействие {name} не найдено");
        }

        return result;
    }

    public void ToggleInteraction(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            Logger.LogWarning(LogModule.Dialogue, "Имя персонажа пустое");
            return;
        }

        GameObject interaction = GetInteractionByName(characterName);
        if (interaction != null)
        {
            interaction.SetActive(!interaction.activeSelf);
            Logger.Log(LogModule.Dialogue, $"Взаимодействие {characterName} переключено в состояние {interaction.activeSelf}");
        }
    }

    #endregion

    #region Public Methods

    public bool IsCharacterActive(string characterName)
    {
        return currentCharacterName == characterName && isCharacterSpawned;
    }

    public void PlayCharacterAnimation(string animationName)
    {
        if (character != null && character.TryGetComponent(out Animator animator))
        {
            animator.Play(animationName);
            Logger.Log(LogModule.Dialogue, $"Запущена анимация {animationName} для {character.name}");
        }
        else
        {
            Logger.LogWarning(LogModule.Dialogue, $"Невозможно запустить анимацию {animationName}: персонаж не найден или нет Animator");
        }
    }

    public void SetCharacterPosition(Vector3 position)
    {
        if (character != null)
        {
            character.transform.position = position;
        }
    }

    public void SetCharacterScale(Vector3 scale)
    {
        if (character != null)
        {
            character.transform.localScale = scale;
        }
    }

    public void SetCharacterRotation(Quaternion rotation)
    {
        if (character != null)
        {
            character.transform.rotation = rotation;
        }
    }

    #endregion
}
