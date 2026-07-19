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
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        FindAllScenes();
        InitializeScenes();
        FindCurrentScene();
        InitializeNeighbors();
        FindTextBeginner();
    }

    private void FindTextBeginner()
    {
        if (textBeginner == null)
        {
            textBeginner = FindFirstObjectByType<TextBeginner>();
        }
    }

    private void FindAllScenes()
    {
        GameObject[] foundScenes = GameObject.FindGameObjectsWithTag("Location");
        scenes = foundScenes.ToList();
    }

    private void InitializeScenes()
    {
        foreach (GameObject scene in scenes)
        {
            scene.SetActive(scene.transform.position == Vector3.zero);
        }
    }

    private void FindCurrentScene()
    {
        currentScene = scenes.FirstOrDefault(s => s.activeSelf);
        if (currentScene == null && scenes.Count > 0)
        {
            currentScene = scenes[0];
            currentScene.SetActive(true);
        }
    }

    private void InitializeNeighbors()
    {
        foreach (GameObject scene in scenes)
        {
            LocationNeighbors neighbors = scene.GetComponent<LocationNeighbors>();
            if (neighbors != null && !neighbors.enabled) neighbors.enabled = true;
        }
    }

    public void SwitchToScene(string sceneName, Vector2 slideDirection)
    {
        if (isTransitioning) return;
        GameObject scene = scenes.Find(s => s.name == sceneName);
        if (scene == null || scene == currentScene) return;
        targetScene = scene;
        StartCoroutine(SlideTransition(slideDirection));
    }

    public void SwitchToScene(string sceneName)
    {
        if (isTransitioning) return;
        GameObject scene = scenes.Find(s => s.name == sceneName);
        if (scene == null || scene == currentScene) return;
        targetScene = scene;
        Vector2 direction = GetDirection(currentScene, targetScene);
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

        yield return null;

        // ✅ Перемещаем персонажа на новую сцену
        if (textBeginner != null)
        {
            textBeginner.MoveCharacterToCurrentScene();
            textBeginner.MoveCharacterToActiveLocationReference();
            Debug.Log("Character moved to new location!");

            // Обновляем локацию в TextBeginner
            textBeginner.UpdateCurrentLocation();
            Debug.Log($"📍 Локация обновлена в TextBeginner: {currentScene.name}");

            // Вызываем событие входа в локацию через EventManager
            EventManager.Instance?.TriggerEvent(EventTriggerType.EnterLocation,
                new EventContext().WithLocation(currentScene.name));
            Debug.Log($"📍 Событие входа в локацию: {currentScene.name}");

            // 🔥 ПРЯМОЙ ВЫЗОВ КВЕСТОВ (гарантированно работает)
            if (QuestManager.Instance != null)
            {
                Debug.Log($"✅ Прямое обновление квеста по локации: {currentScene.name}");
                QuestManager.Instance.UpdateQuestByLocation(currentScene.name);
            }
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
        return Mathf.Abs(diff.x) > Mathf.Abs(diff.y)
            ? new Vector2(Mathf.Sign(diff.x), 0)
            : new Vector2(0, Mathf.Sign(diff.y));
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

    public GameObject GetCurrentScene() => currentScene;
    public List<GameObject> GetAllScenes() => scenes;
}