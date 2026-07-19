// SceneEventController.cs
using UnityEngine;

public class SceneEventController : MonoBehaviour
{
    [Header("Events to execute on scene start")]
    [SerializeField] private GameEvent[] eventsOnAwake;

    [Header("Events to execute on scene enter")]
    [SerializeField] private GameEvent[] eventsOnEnter;

    [Header("Unique scene events (execute once)")]
    [SerializeField] private UniqueSceneEvent[] uniqueEvents;

    [System.Serializable]
    public class UniqueSceneEvent
    {
        public string eventID;
        public GameEvent gameEvent;
        public string sceneName;
    }

    private void Awake()
    {
        // Выполняем события при загрузке сцены
        foreach (var evt in eventsOnAwake)
        {
            if (evt != null)
            {
                var context = new EventContext()
                    .WithLocation(gameObject.scene.name)
                    .WithValue("Awake");
                evt.Execute(context);
            }
        }
    }

    private void Start()
    {
        // Выполняем события при входе в сцену
        foreach (var evt in eventsOnEnter)
        {
            if (evt != null)
            {
                var context = new EventContext()
                    .WithLocation(gameObject.scene.name)
                    .WithValue("Start");
                evt.Execute(context);
            }
        }

        // Проверяем уникальные события
        foreach (var unique in uniqueEvents)
        {
            if (unique.gameEvent != null)
            {
                if (!EventStateManager.Instance?.IsExecuted(unique.eventID) ?? false)
                {
                    unique.gameEvent.Execute(new EventContext()
                        .WithLocation(gameObject.scene.name));
                }
            }
        }
    }

    // Вызов при выходе из сцены
    private void OnDestroy()
    {
        // Можно сохранить состояния перед выходом
    }
}