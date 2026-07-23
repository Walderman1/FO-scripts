using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

public class SceneSlideTransition : MonoBehaviour
{
    public static SceneSlideTransition Instance;

    [Header("Settings")]
    [SerializeField] private float slideDuration = 0.5f;

    [Header("Character")]
    [SerializeField] private TextBeginner textBeginner;

    private List<GameObject> scenes = new List<GameObject>();
    private GameObject currentScene;
    private GameObject targetScene;
    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Logger.Log(LogModule.Navigation, "SceneSlideTransition инициализирован");
        }
        else
        {
            Logger.Log(LogModule.Navigation, "Уничтожение дублирующего SceneSlideTransition");
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        FindAllScenes();
        InitializeScenes();
        FindCurrentScene();
        InitializeNeighbors();
        FindTextBeginner();

        Logger.Log(LogModule.Navigation, $"Найдено {scenes.Count} сцен");
    }

    private void FindTextBeginner()
    {
        if (textBeginner == null)
        {
            textBeginner = FindFirstObjectByType<TextBeginner>();
            if (textBeginner != null)
            {
                Logger.Log(LogModule.Navigation, "TextBeginner найден автоматически");
            }
        }
    }

    private void FindAllScenes()
    {
        GameObject[] foundScenes = GameObject.FindGameObjectsWithTag("Location");
        scenes = foundScenes.ToList();
        Logger.Log(LogModule.Navigation, $"Найдено {scenes.Count} локаций");
    }

    private void InitializeScenes()
    {
        foreach (GameObject scene in scenes)
        {
            bool isActive = scene.transform.position == Vector3.zero;
            scene.SetActive(isActive);
            if (isActive)
            {
                Logger.Log(LogModule.Navigation, $"Сцена {scene.name} активна при инициализации");
            }
        }
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
            Logger.Log(LogModule.Navigation, $"Текущая сцена: {currentScene.name}");
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
        Logger.Log(LogModule.Navigation, $"Начало перехода на сцену {sceneName} по направлению {slideDirection}");
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

        Logger.Log(LogModule.Navigation, $"Анимация перехода из {currentScene.name} в {targetScene.name}");

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

        Logger.Log(LogModule.Navigation, $"Переход завершён, текущая сцена: {currentScene.name}");

        yield return null;

        if (textBeginner != null)
        {
            textBeginner.MoveCharacterToCurrentScene();
            textBeginner.MoveCharacterToActiveLocationReference();
            Logger.Log(LogModule.Navigation, "Персонаж перемещён на новую локацию");

            textBeginner.UpdateCurrentLocation();
            Logger.Log(LogModule.Navigation, $"Локация обновлена в TextBeginner: {currentScene.name}");

            EventManager.Instance?.TriggerEvent(EventTriggerType.EnterLocation,
                new EventContext().WithLocation(currentScene.name));
            Logger.Log(LogModule.Navigation, $"Событие входа в локацию: {currentScene.name}");

            if (QuestManager.Instance != null)
            {
                Logger.Log(LogModule.Navigation, $"Прямое обновление квеста по локации: {currentScene.name}");
                QuestManager.Instance.UpdateQuestByLocation(currentScene.name);
            }
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
        Vector3 worldOffset = Camera.main.ScreenToWorldPoint(new Vector3(Screen.width, 0, 0));
        return Mathf.Abs(worldOffset.x);
    }

    private float GetWorldHeight()
    {
        Vector3 worldOffset = Camera.main.ScreenToWorldPoint(new Vector3(0, Screen.height, 0));
        return Mathf.Abs(worldOffset.y);
    }

    public GameObject GetCurrentScene()
    {
        Logger.Log(LogModule.Navigation, $"Запрос текущей сцены: {currentScene?.name ?? "None"}");
        return currentScene;
    }

    public List<GameObject> GetAllScenes()
    {
        return scenes;
    }
}
