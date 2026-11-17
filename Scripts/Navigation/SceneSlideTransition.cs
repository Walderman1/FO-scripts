using DialogueSystem;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

public class SceneSlideTransition : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Character")]
    [SerializeField] private TextBeginner textBeginner;
    [SerializeField] private DialogueData dialogueConfig;

    [Inject] private DialogueData _injectedConfig;
    [Inject] private GameReferences _refs;
    [Inject] private DialogueCharacterManager _dialogueCharacterManager;
    [Inject] private TextBeginner _textBeginner;

    private GameReferences Refs => _refs;

    public event System.Action<GameObject> OnSceneChanged;
    public bool IsTransitioning => isTransitioning;

    private List<GameObject> scenes = new List<GameObject>();
    private GameObject currentScene;
    private GameObject targetScene;
    private bool isTransitioning = false;

    private TextBeginner TB => textBeginner != null ? textBeginner : _textBeginner;
    private DialogueCharacterManager DCM => _dialogueCharacterManager;

    private void Start()
    {
        if (dialogueConfig == null) dialogueConfig = _injectedConfig;

        if (dialogueConfig == null)
            Logger.LogWarning(LogModule.Navigation, "DialogueConfig не найден");

        FindAllScenes();
        InitializeScenes();
        FindCurrentScene();
        InitializeNeighbors();

        OnSceneChanged?.Invoke(currentScene);
    }

    private void FindAllScenes()
    {
        string locationTag = dialogueConfig != null ? dialogueConfig.LocationTag : "Location";
        GameObject[] foundScenes = GameObject.FindGameObjectsWithTag(locationTag);
        scenes = foundScenes.ToList();
        Logger.Log(LogModule.Navigation, $"Найдено {scenes.Count} локаций");
    }

    private void InitializeScenes()
    {
        GameObject activeScene = null;

        foreach (GameObject scene in scenes)
        {
            if (activeScene == null && scene.transform.position.sqrMagnitude < 0.0001f)
                activeScene = scene;
        }

        if (activeScene == null && scenes.Count > 0) activeScene = scenes[0];

        foreach (GameObject scene in scenes)
        {
            bool shouldBeActive = (scene == activeScene);
            if (scene.activeSelf != shouldBeActive) scene.SetActive(shouldBeActive);
            if (shouldBeActive && scene.transform.localPosition != Vector3.zero)
                scene.transform.localPosition = Vector3.zero;
        }

        if (activeScene != null)
            Logger.LogVerbose(LogModule.Navigation, $"Активна локация: {activeScene.name}");
    }

    private void FindCurrentScene()
    {
        currentScene = scenes.FirstOrDefault(s => s.activeSelf);
        if (currentScene == null && scenes.Count > 0)
        {
            currentScene = scenes[0];
            currentScene.SetActive(true);
            Logger.Log(LogModule.Navigation, $"Установлена первая сцена как текущая: {currentScene.name}");
        }
        else if (currentScene != null)
        {
            Logger.LogVerbose(LogModule.Navigation, $"Текущая сцена: {currentScene.name}");
        }
    }

    private void InitializeNeighbors()
    {
        foreach (GameObject scene in scenes)
        {
            LocationNeighbors neighbors = scene.GetComponent<LocationNeighbors>();
            if (neighbors != null && !neighbors.enabled)
            {
                neighbors.enabled = true;
                Logger.Log(LogModule.Navigation, $"Активирован LocationNeighbors для {scene.name}");
            }
        }
    }

    public void SwitchToScene(string sceneName, Vector2 slideDirection)
    {
        if (isTransitioning)
        {
            Logger.Log(LogModule.Navigation, "Переход уже выполняется, пропуск");
            return;
        }

        GameObject scene = scenes.Find(s => s.name == sceneName);
        if (scene == null)
        {
            Logger.LogWarning(LogModule.Navigation, $"Сцена {sceneName} не найдена");
            return;
        }

        if (scene == currentScene)
        {
            Logger.Log(LogModule.Navigation, $"Уже находимся на сцене {sceneName}");
            return;
        }

        targetScene = scene;
        Logger.LogVerbose(LogModule.Navigation, $"Начало перехода на сцену {sceneName} по направлению {slideDirection}");
        StartCoroutine(SlideTransition(slideDirection));
    }

    public void SwitchToScene(string sceneName)
    {
        if (isTransitioning)
        {
            Logger.Log(LogModule.Navigation, "Переход уже выполняется, пропуск");
            return;
        }

        GameObject scene = scenes.Find(s => s.name == sceneName);
        if (scene == null)
        {
            Logger.LogWarning(LogModule.Navigation, $"Сцена {sceneName} не найдена");
            return;
        }

        if (scene == currentScene)
        {
            Logger.Log(LogModule.Navigation, $"Уже находимся на сцене {sceneName}");
            return;
        }

        targetScene = scene;
        Vector2 direction = GetDirection(currentScene, targetScene);
        Logger.Log(LogModule.Navigation, $"Начало перехода на сцену {sceneName} с автоматическим направлением {direction}");
        StartCoroutine(SlideTransition(direction));
    }

    public void SetCurrentSceneInstant(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName)) return;

        GameObject scene = scenes.Find(s => s.name == sceneName);
        if (scene == null)
        {
            Logger.LogWarning(LogModule.Navigation, $"Сцена {sceneName} не найдена");
            return;
        }

        if (scene == currentScene) return;

        if (isTransitioning)
        {
            StartCoroutine(SetCurrentSceneInstantWhenReady(scene));
            return;
        }

        if (currentScene != null)
        {
            currentScene.SetActive(false);
            currentScene.transform.localPosition = Vector3.zero;
        }

        scene.SetActive(true);
        scene.transform.localPosition = Vector3.zero;
        currentScene = scene;

        OnSceneChanged?.Invoke(currentScene);

        var tb = TB;
        if (tb != null)
        {
            DCM?.MoveCharacterToCurrentLocationReference();
            tb.UpdateCurrentLocation();
        }

        Logger.LogVerbose(LogModule.Navigation, $"Мгновенное переключение на сцену: {currentScene.name}");
    }

    private IEnumerator SetCurrentSceneInstantWhenReady(GameObject scene)
    {
        float timeout = 5f;
        float elapsed = 0f;

        while (isTransitioning && elapsed < timeout)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (scene == null) yield break;
        SetCurrentSceneInstant(scene.name);
    }

    private IEnumerator SlideTransition(Vector2 direction)
    {
        isTransitioning = true;

        targetScene.SetActive(true);
        targetScene.transform.localPosition = GetStartPosition(direction);

        Vector3 currentStart = Vector3.zero;
        Vector3 targetStart = GetStartPosition(direction);
        Vector3 currentEnd = GetEndPosition(direction);
        Vector3 targetEnd = Vector3.zero;

        float elapsed = 0f;
        bool switched = false;

        Logger.LogVerbose(LogModule.Navigation, $"Анимация перехода из {currentScene.name} в {targetScene.name}");

        while (elapsed < slideDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / slideDuration;

            currentScene.transform.localPosition = Vector3.Lerp(currentStart, currentEnd, t);
            targetScene.transform.localPosition = Vector3.Lerp(targetStart, targetEnd, t);

            if (t >= 0.95f && !switched)
            {
                switched = true;
                currentScene.SetActive(false);
                targetScene.transform.localPosition = Vector3.zero;
            }

            yield return null;
        }

        if (!switched)
        {
            currentScene.SetActive(false);
            targetScene.transform.localPosition = Vector3.zero;
        }

        currentScene = targetScene;
        targetScene = null;
        isTransitioning = false;

        Logger.LogVerbose(LogModule.Navigation, $"Переход завершён, текущая сцена: {currentScene.name}");

        OnSceneChanged?.Invoke(currentScene);

        yield return null;

        var tb = TB;
        if (tb != null)
        {
            DCM?.MoveCharacterToCurrentLocationReference();
            tb.UpdateCurrentLocation();

            Refs?.EventManager?.TriggerEvent(
                EventTriggerType.EnterLocation,
                new EventContext().WithLocation(currentScene.name));
        }
        else
        {
            Logger.LogWarning(LogModule.Navigation, "TextBeginner отсутствует, перемещение персонажа пропущено");
        }
    }

    private Vector3 GetStartPosition(Vector2 direction)
    {
        float worldWidth = GetWorldWidth();
        float worldHeight = GetWorldHeight();
        return new Vector3(direction.x * worldWidth * 2, direction.y * worldHeight * 2, 0);
    }

    private Vector3 GetEndPosition(Vector2 direction)
    {
        float worldWidth = GetWorldWidth();
        float worldHeight = GetWorldHeight();
        return new Vector3(-direction.x * worldWidth * 2, -direction.y * worldHeight * 2, 0);
    }

    private Vector2 GetDirection(GameObject from, GameObject to)
    {
        Vector3 diff = to.transform.position - from.transform.position;
        Vector2 result = Mathf.Abs(diff.x) > Mathf.Abs(diff.y)
            ? new Vector2(Mathf.Sign(diff.x), 0)
            : new Vector2(0, Mathf.Sign(diff.y));

        Logger.Log(LogModule.Navigation, $"Определено направление: {result} из {from.name} в {to.name}");
        return result;
    }

    private float GetWorldWidth()
    {
        var cam = Refs?.MainCamera;
        if (cam == null)
        {
            Logger.LogWarning(LogModule.Navigation, "MainCamera отсутствует, возвращаем значение по умолчанию");
            return 5f;
        }
        Vector3 worldOffset = cam.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));
        return Mathf.Abs(worldOffset.x);
    }

    private float GetWorldHeight()
    {
        var cam = Refs?.MainCamera;
        if (cam == null)
        {
            Logger.LogWarning(LogModule.Navigation, "MainCamera отсутствует, возвращаем значение по умолчанию");
            return 5f;
        }
        Vector3 worldOffset = cam.ScreenToWorldPoint(new Vector3(0, Screen.height, 0));
        return Mathf.Abs(worldOffset.y);
    }

    public GameObject GetCurrentScene() => currentScene;
    public List<GameObject> GetAllScenes() => scenes;
}