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

    // Эти поля устанавливаются из TextBeginner
    private GameObject FirstCharactersArea;
    private GameObject SecondCharactersArea;

    // State
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
    }

    public void SetCharacterAreas(GameObject firstArea, GameObject secondArea)
    {
        FirstCharactersArea = firstArea;
        SecondCharactersArea = secondArea;
        Debug.Log($"Character areas set: First={FirstCharactersArea?.name}, Second={SecondCharactersArea?.name}");
    }

    public void LoadCharacters()
    {
        string folder = dialogueConfig != null ? dialogueConfig.charactersFolder : "Characters";
        characters = Resources.LoadAll<GameObject>(folder).ToList();
    }

    #endregion

    #region Character Management

    public void ActivateCharacter(string characterName)
    {
        if (isCharacterSpawned) return;
        if (FirstCharactersArea == null)
        {
            Debug.LogError("FirstCharactersArea is null! Cannot activate character.");
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

                    Animator animator = newCharacter.GetComponent<Animator>();
                    if (animator != null)
                    {
                        animator.Play("Speak");
                    }
                    isAnimating = true;
                }
            }
        }
        else
        {
            Debug.LogError($"Target '{parentName}' not found in FirstCharactersArea!");
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
    }

    public void ClearCharacter()
    {
        if (character != null)
        {
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
            Debug.LogWarning("FirstCharactersArea is null, cannot clear characters.");
            return;
        }

        string parentName = dialogueConfig != null ? dialogueConfig.charactersParentName : "Characters";
        Transform target = FirstCharactersArea.transform.Find(parentName);
        if (target != null)
        {
            foreach (Transform child in target)
            {
                Destroy(child.gameObject);
            }
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
            Debug.LogWarning("Character is null, cannot move to speaker area!");
            return;
        }

        if (FirstCharactersArea == null)
        {
            Debug.LogError("FirstCharactersArea is null!");
            return;
        }

        string parentName = dialogueConfig != null ? dialogueConfig.charactersParentName : "Characters";
        Transform target = FirstCharactersArea.transform.Find(parentName);
        if (target == null)
        {
            Debug.LogError($"Target '{parentName}' not found in FirstCharactersArea!");
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

        Debug.Log($"Character moved to speaker area: {target.name}");
    }

    public void MoveCharacterToLocation(GameObject location)
    {
        if (character == null || location == null) return;

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
    }

    public void MoveCharacterToPosition(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        if (character == null) return;

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
    }

    public void MoveCharacterToCurrentLocationReference()
    {
        if (character == null)
        {
            Debug.LogWarning("Character is null!");
            return;
        }

        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Debug.LogWarning("No current scene found!");
            return;
        }

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null)
        {
            Debug.LogWarning($"No LocationNeighbors on {currentScene.name}");
            return;
        }

        GameObject locationRef = neighbors.GetLocationReference();
        if (locationRef == null)
        {
            Debug.LogWarning($"No LocationReference in {currentScene.name}");
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

        Debug.Log($"Character moved to LocationReference: {locationRef.name} in scene: {currentScene.name}");
    }

    public void MoveCharacterToCurrentScene()
    {
        if (character == null) return;

        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Debug.LogWarning("No current scene found!");
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

        Debug.Log($"Character moved to scene: {currentScene.name}");
    }

    public void MoveCharacterToActiveLocationReference()
    {
        if (character == null) return;

        GameObject locationRef = GetActiveLocationReferenceFromCurrentScene();
        if (locationRef == null)
        {
            Debug.LogWarning("No active LocationReference found!");
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

        Debug.Log($"Character moved to LocationReference: {locationRef.name}");
    }

    private GameObject GetActiveLocationReferenceFromCurrentScene()
    {
        GameObject currentScene = SceneSlideTransition.Instance?.GetCurrentScene();
        if (currentScene == null)
        {
            Debug.LogWarning("No current scene found!");
            return null;
        }

        LocationNeighbors neighbors = currentScene.GetComponent<LocationNeighbors>();
        if (neighbors == null)
        {
            Debug.LogWarning($"No LocationNeighbors on {currentScene.name}");
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
            return null;
        }

        if (string.IsNullOrEmpty(name))
        {
            return null;
        }

        return interactions.FirstOrDefault(i => i != null && i.name == name);
    }

    public void ToggleInteraction(string characterName)
    {
        if (string.IsNullOrEmpty(characterName))
        {
            return;
        }

        GameObject interaction = GetInteractionByName(characterName);
        if (interaction != null)
        {
            interaction.SetActive(!interaction.activeSelf);
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